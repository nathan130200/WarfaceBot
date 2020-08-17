using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AgsXMPP;
using AgsXMPP.Protocol;
using AgsXMPP.Xml;
using AgsXMPP.Xml.Dom;
using Warface.Entities.Network;
using Warface.EventArgs;
using Warface.Security.Cryptography;

namespace Warface
{
	public class WarfaceClient
	{
		internal readonly AsyncEvent<WarfaceClientEventArgs> _connectedEvent = new AsyncEvent<WarfaceClientEventArgs>();
		internal readonly AsyncEvent<WarfaceClientEventArgs> _disconnectedEvent = new AsyncEvent<WarfaceClientEventArgs>();
		internal readonly AsyncEvent<WarfaceClientErrorEventArgs> _erroredEvent = new AsyncEvent<WarfaceClientErrorEventArgs>();

		internal WarfaceClientConfiguration config;
		internal WarfaceCryptoProvider crypto;
		internal StreamParser parser;
		internal Socket socket;
		internal Stream stream;

		protected volatile bool closed, paused;

		public string StreamId { get; private set; }
		public string StreamVersion { get; private set; }
		public bool IsConnected => !this.closed && !this.paused;
		public bool IsAuthenticated { get; private set; }
		public bool IsTlsStarted => this.config.UseTls && this.stream is SslStream;
		public Jid Jid { get; }

		public event AsyncEventHandler<WarfaceClientEventArgs> Connected
		{
			add => this._connectedEvent.Register(value);
			remove => this._connectedEvent.Unregister(value);
		}

		public event AsyncEventHandler<WarfaceClientEventArgs> Disconnected
		{
			add => this._disconnectedEvent.Register(value);
			remove => this._disconnectedEvent.Unregister(value);
		}

		public event AsyncEventHandler<WarfaceClientErrorEventArgs> ClientErrored
		{
			add => this._erroredEvent.Register(value);
			remove => this._erroredEvent.Unregister(value);
		}

		public WarfaceClient(WarfaceClientConfiguration config)
		{
			this.config = config;

			if (string.IsNullOrEmpty(this.config.ConnectServer))
				throw new ArgumentNullException(nameof(this.config.ConnectServer));

			if (string.IsNullOrEmpty(this.config.Username))
				throw new ArgumentNullException(nameof(this.config.Username));

			if (string.IsNullOrEmpty(this.config.Password))
				throw new ArgumentNullException(nameof(this.config.Password));

			if (string.IsNullOrEmpty(this.config.Version))
				throw new ArgumentNullException(nameof(this.config.Version));

			this.Jid = new Jid($"{this.config.Username}@{this.config.Server}/GameClient");
			this.Setup();
		}

		protected internal void Setup()
		{
			this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			this.parser = new StreamParser();
			this.parser.OnStreamStart += async (s, e) => await this.HandleStreamStartAsync(e as XmppStream).ConfigureAwait(false);
			this.parser.OnStreamEnd += async (s, e) => await this.HandleStreamEndAsync(e as XmppStream).ConfigureAwait(false);
			this.parser.OnStreamElement += async (s, e) => await this.HandleStreamElementAsync(e as Element).ConfigureAwait(false);
			this.parser.OnStreamError += async (s, e) => await this.HandleStreamErrorAsync(e).ConfigureAwait(false);
		}

		public Task ConnectAsync()
		{
			if (this.closed)
			{
				this.closed = this.paused = false;
				this.Setup();
			}

			_ = Task.Factory.StartNew(async () => await this.ConnectInternalAsync());
			return Task.CompletedTask;
		}

		public async Task DisconnectAsync()
		{
			if (this.closed)
				return;

			this.closed = this.paused = true;

			if (this.crypto != null)
				this.crypto.Dispose();

			if (this.parser != null)
			{
				this.parser.Reset();
				this.parser = null;
			}

			if (this.stream != null)
			{
				this.stream.Dispose();
				this.stream = null;
			}

			if (this.socket != null)
			{
				this.socket.Dispose();
				this.socket = null;
			}

			await this._disconnectedEvent.InvokeAsync(new WarfaceClientEventArgs { Client = this });
		}

		async Task ConnectInternalAsync()
		{
			try
			{
				await this.socket.ConnectAsync(this.config.ConnectServer, this.config.Port);
				await this._connectedEvent.InvokeAsync(new WarfaceClientEventArgs { Client = this });

				this.stream = new NetworkStream(this.socket, true);
				_ = Task.Factory.StartNew(async () => await this.BeginReceiveAsync());

				await this.SendAsync(new XmppStream
				{
					To = new Jid(this.config.Server),
					Version = "1.0",
				}.StartTag());
			}
			catch (Exception ex)
			{
				await this._erroredEvent.InvokeAsync(new WarfaceClientErrorEventArgs { Client = this, Exception = ex });
				await this.DisconnectAsync().ConfigureAwait(false);
			}
		}

		internal Task SendAsync(string str)
		{
			Trace.TraceInformation($"send >>:\n{str}\n");
			_ = Task.Factory.StartNew(async () => await this.SendInternalAsync(str.GetBytes()));
			return Task.CompletedTask;
		}

		internal Task SendAsync(Element e)
		{
			Trace.TraceInformation($"send >>\n{e.ToString(true, 2)}\n");
			_ = Task.Factory.StartNew(async () => await this.SendInternalAsync(e.GetBytes()));
			return Task.CompletedTask;
		}

		async Task SendInternalAsync(byte[] buffer, PacketType type = PacketType.Plain, bool force = false)
		{
			if (!this.IsConnected && !force)
				return;
			else
			{
				try
				{
					if (!this.config.Protect.UseProtect)
						await this.stream.WriteAsync(buffer, 0, buffer.Length);
					else
					{
						var packet = new Packet(type, buffer);

						if (type == PacketType.Plain && this.crypto?.IsInitialized == true)
							packet = packet.EncryptPacket(this.crypto);

						await this.stream.WritePacketAsync(packet);
					}
				}
				catch (Exception ex)
				{
					await this._erroredEvent.InvokeAsync(new WarfaceClientErrorEventArgs { Client = this, Exception = ex });
					await this.DisconnectAsync().ConfigureAwait(false);
				}
			}
		}

		async Task BeginReceiveAsync()
		{
			var buffer = new byte[ushort.MaxValue];

			try
			{
				while (!this.closed)
				{
					if (!this.paused)
					{
						if (this.config.Protect.UseProtect)
						{
							var packet = await this.stream.ReadPacketAsync();

							if (packet.BufferSize <= 0)
								await this.DisconnectAsync().ConfigureAwait(false);
							else
							{
								if (this.crypto != null && packet.Type == PacketType.Encrypted)
									packet = packet.DecryptPacket(this.crypto);

								if (packet.Type == PacketType.Plain)
									this.parser.Push(packet.Buffer, 0, packet.BufferSize);
								else
								{
									switch (packet.Type)
									{
										case PacketType.ServerKey:
											await this.HandleServerKeyAsync(packet.Buffer);
											break;

										default:
											Trace.TraceWarning($"Unknown packet type: {packet.Type} [{packet.BufferSize}]");
											break;
									}
								}
							}
						}
						else
						{
							int count;

							if ((count = await this.stream.ReadAsync(buffer, 0, buffer.Length)) <= 0)
								await this.DisconnectAsync().ConfigureAwait(false);
							else
								this.parser.Push(buffer, 0, count);
						}
					}

					await Task.Delay(1);
				}
			}
			catch (Exception ex)
			{
				await this._erroredEvent.InvokeAsync(new WarfaceClientErrorEventArgs { Client = this, Exception = ex });
				await this.DisconnectAsync().ConfigureAwait(false);
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		internal struct ServerKeyUnionStruct
		{
			internal ServerKeyUnionStruct(byte[] buff) : this()
			{
				if (buff.Length >= 1) this.unk0 = buff[0];
				if (buff.Length >= 2) this.unk1 = buff[1];
				if (buff.Length >= 3) this.unk2 = buff[2];
				if (buff.Length >= 4) this.unk3 = buff[3];
			}

			[FieldOffset(0)]
			public int value;

			[FieldOffset(0)]
			public byte unk0;

			[FieldOffset(1)]
			public byte unk1;

			[FieldOffset(2)]
			public byte unk2;

			[FieldOffset(3)]
			public byte unk3;
		}

		protected async Task HandleServerKeyAsync(byte[] buffer)
		{
			this.paused = true;

			var salt = new ServerKeyUnionStruct(buffer).value;
			Trace.TraceInformation($"Recv server key! salt={salt}");

			this.crypto = new WarfaceCryptoProvider(salt, this.config.Version,
				this.config.Protect.CryptKey, this.config.Protect.CryptIv);

			if (this.crypto?.IsInitialized == false)
			{
				await this.DisconnectAsync().ConfigureAwait(false);
				return;
			}

			await this.SendInternalAsync(default, PacketType.ClientAck, true);
			this.paused = false;
		}

		Task HandleStreamStartAsync(XmppStream stream)
		{
			Trace.TraceInformation($"recv <<:\n{stream.StartTag()}\n");
			this.StreamId = stream.Id;
			this.StreamVersion = stream.Version;
			return Task.CompletedTask;
		}

		async Task HandleStreamEndAsync(XmppStream stream)
		{
			Trace.TraceInformation($"recv <<:\n{stream.EndTag()}\n");
			await this.DisconnectAsync();
		}

		async Task HandleStreamElementAsync(Element e)
		{
			Trace.TraceInformation($"recv <<:\n{e.ToString(true, 2)}\n");
			await this.DisconnectAsync();
		}

		async Task HandleStreamErrorAsync(Exception ex)
		{
			await this._erroredEvent.InvokeAsync(new WarfaceClientErrorEventArgs { Client = this, Exception = ex });
			await this.DisconnectAsync();
		}
	}
}
