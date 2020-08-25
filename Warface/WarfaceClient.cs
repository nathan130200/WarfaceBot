using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AgsXMPP;
using AgsXMPP.Protocol;
using AgsXMPP.Protocol.Sasl;
using AgsXMPP.Protocol.Stream;
using AgsXMPP.Protocol.Tls;
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

		internal WarfaceClientConfiguration Config;
		internal WarfaceCryptoProvider Crypto;
		internal StreamParser Parser;
		internal Socket Socket;
		internal Stream Stream;

		public string StreamId { get; private set; }
		public string StreamVersion { get; private set; }
		public bool IsConnected => !this.IsClosed && !this.IsPaused;
		public bool IsAuthenticated { get; private set; }
		public bool IsTlsStarted => this.Config.UseTls && this.Stream is SslStream;
		public Jid Jid { get; }

		protected volatile bool IsClosed, IsPaused;

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
			this.Config = config;

			if (string.IsNullOrEmpty(this.Config.ConnectServer))
				throw new ArgumentNullException(nameof(this.Config.ConnectServer));

			if (string.IsNullOrEmpty(this.Config.Username))
				throw new ArgumentNullException(nameof(this.Config.Username));

			if (string.IsNullOrEmpty(this.Config.Password))
				throw new ArgumentNullException(nameof(this.Config.Password));

			if (string.IsNullOrEmpty(this.Config.Version))
				throw new ArgumentNullException(nameof(this.Config.Version));

			this.Jid = new Jid($"{this.Config.Username}@{this.Config.Server}/GameClient");
			this.Setup();
		}

		protected internal void Setup()
		{
			this.Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			this.Parser = new StreamParser();
			this.Parser.OnStreamStart += async (s, e) => await this.HandleStreamStartAsync(e as XmppStream).ConfigureAwait(false);
			this.Parser.OnStreamEnd += async (s, e) => await this.HandleStreamEndAsync(e as XmppStream).ConfigureAwait(false);
			this.Parser.OnStreamElement += async (s, e) => await this.HandleStreamElementAsync(e as Element).ConfigureAwait(false);
			this.Parser.OnStreamError += async (s, e) => await this.HandleStreamErrorAsync(e).ConfigureAwait(false);
		}

		#region Client: Connection Management

		public Task ConnectAsync()
		{
			if (this.IsClosed)
			{
				this.IsClosed = this.IsPaused = false;
				this.Setup();
			}

			_ = Task.Factory.StartNew(async () => await this.ConnectInternalAsync());
			return Task.CompletedTask;
		}

		public async Task DisconnectAsync()
		{
			if (this.IsClosed)
				return;

			this.IsClosed = this.IsPaused = true;

			if (this.Crypto != null)
				this.Crypto.Dispose();

			if (this.Parser != null)
			{
				this.Parser.Reset();
				this.Parser = null;
			}

			if (this.Stream != null)
			{
				this.Stream.Dispose();
				this.Stream = null;
			}

			if (this.Socket != null)
			{
				this.Socket.Dispose();
				this.Socket = null;
			}

			await this._disconnectedEvent.InvokeAsync(new WarfaceClientEventArgs { Client = this });
		}

		async Task ConnectInternalAsync()
		{
			try
			{
				await this.Socket.ConnectAsync(this.Config.ConnectServer, this.Config.Port);
				await this._connectedEvent.InvokeAsync(new WarfaceClientEventArgs { Client = this });

				this.Stream = new NetworkStream(this.Socket, true);
				_ = Task.Factory.StartNew(async () => await this.BeginReceiveAsync());
				await this.ResetStreamAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await this._erroredEvent.InvokeAsync(new WarfaceClientErrorEventArgs { Client = this, Exception = ex });
				await this.DisconnectAsync().ConfigureAwait(false);
			}
		}

		#endregion

		#region Socket: Handle Receive/Send

		internal Task SendAsync(string str)
		{
			_ = Task.Factory.StartNew(async () =>
			{
				await this.SendInternalAsync(str.GetBytes());
				Utilities.LogVerbose(this, $"send >>:\n{str}\n");
			});

			return Task.CompletedTask;
		}

		internal Task SendAsync(Element e)
		{
			_ = Task.Factory.StartNew(async () =>
			{
				await this.SendInternalAsync(e.GetBytes());
				Utilities.LogVerbose(this, $"send >>:\n{e.ToString(true, 2)}\n");
			});

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
					if (!this.Config.Protect.UseProtect)
						await this.Stream.WriteAsync(buffer, 0, buffer.Length);
					else
					{
						var packet = new Packet(type, buffer);

						if (type == PacketType.Plain && this.Crypto?.IsInitialized == true)
							packet.Encrypt(this.Crypto);

						await this.Stream.WritePacketAsync(packet);
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
				while (!this.IsClosed)
				{
					if (!this.IsPaused)
					{
						if (this.Config.Protect.UseProtect)
						{
							var packet = await this.Stream.ReadPacketAsync();

							if (packet == null)
								await this.DisconnectAsync().ConfigureAwait(false);
							else
							{
								if (this.Crypto?.IsInitialized == true && packet.Type == PacketType.Encrypted)
									packet.Decrypt(this.Crypto);

								if (packet.Type == PacketType.Plain)
									this.Parser.Push(packet.Buffer, 0, packet.BufferSize);
								else
								{
									switch (packet.Type)
									{
										case PacketType.ServerKey:
											await this.HandleServerKeyAsync(packet.Buffer).ConfigureAwait(false);
											break;

										default:
											this.LogWarning($"Received unknown packet type {packet.Type}. (size={packet.BufferSize})");
											break;
									}
								}
							}
						}
						else
						{
							int count;

							if ((count = await this.Stream.ReadAsync(buffer, 0, buffer.Length)) <= 0)
								await this.DisconnectAsync().ConfigureAwait(false);
							else
								this.Parser.Push(buffer, 0, count);
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

		#endregion

		#region Protect: Handle Server Key

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
			this.IsPaused = true;

			Array.Reverse(buffer);

			var union = new ServerKeyUnionStruct(buffer);

			this.Crypto = new WarfaceCryptoProvider(union.value, this.Config.Version,
				this.Config.Protect.CryptKey, this.Config.Protect.CryptIv);

			if (this.Crypto?.IsInitialized == false)
			{
				this.LogWarning("Cannot initialize native crypto service! Disconnecting from the server.");
				await this.DisconnectAsync().ConfigureAwait(false);
				return;
			}

			await this.SendInternalAsync(default, PacketType.ClientAck, true);
			this.IsPaused = false;
		}

		#endregion

		Task HandleStreamStartAsync(XmppStream stream)
		{
			this.LogVerbose($"recv <<:\n{stream.StartTag()}\n");
			this.StreamId = stream.Id;
			this.StreamVersion = stream.Version;
			return Task.CompletedTask;
		}

		async Task HandleStreamEndAsync(XmppStream stream)
		{
			this.LogVerbose($"recv <<:\n{stream.EndTag()}\n");
			await this.DisconnectAsync();
		}

		volatile bool /*WasBindRequested,*/ WasAuthRequested, WasTlsHandshakeRequested;

		public bool SupportStartTls => !this.IsTlsStarted && this.Config.UseTls;

		async Task HandleStreamElementAsync(Element e)
		{
			this.LogVerbose($"recv <<:\n{e.ToString(true, 2)}\n");

			if (e is StreamFeatures features)
			{
				if (!this.IsAuthenticated)
				{
					if ((this.SupportStartTls && features.SupportsStartTls) && !this.IsTlsStarted)
					{
						this.WasTlsHandshakeRequested = true;
						await this.SendAsync(new StartTls());
						return;
					}

					if (this.WasAuthRequested) // an auth request already sent!
						return;

					var auth = new Auth { Value = this.Config.BuildSaslArguments() };
					auth.SetAttribute("mechanism", "WARFACE");

					this.WasAuthRequested = true;
					await this.SendAsync(auth);
				}
				else
				{
					if (features.SupportsBind)
					{
						// TOOD: Send bind request
					}
				}
			}
			else if (e is Proceed && this.SupportStartTls)
			{
				if (!this.WasTlsHandshakeRequested)
				{
					this.LogWarning("An handshake proceed received but no tls handshaek request was sent before.");
					return;
				}

				await this.DoTlsHandshakeAsync().ConfigureAwait(false);
			}
		}

		internal async Task ResetStreamAsync()
		{
			this.Parser.Reset();
			await this.SendAsync(new XmppStream { To = new Jid(this.Config.Server), Version = "1.0", }.StartTag());
		}

		async Task HandleStreamErrorAsync(Exception ex)
		{
			await this._erroredEvent.InvokeAsync(new WarfaceClientErrorEventArgs { Client = this, Exception = ex });
			await this.DisconnectAsync();
		}

		async Task DoTlsHandshakeAsync()
		{
			try
			{
				this.IsPaused = true;

				await this.Stream.FlushAsync();

				this.Stream = new SslStream(this.Stream, true, (a, b, c, d) =>
				{
					if (!this.Config.PerformCertificateValidation)
						return true;

					return this.Config.CertificateValidationCallback(a, b, c, d);
				});

				await ((SslStream)this.Stream).AuthenticateAsClientAsync(this.Config.ConnectServer).ConfigureAwait(false);
				this.IsPaused = false;

				await this.ResetStreamAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await this._erroredEvent.InvokeAsync(new WarfaceClientErrorEventArgs { Client = this, Exception = ex });
				await this.DisconnectAsync().ConfigureAwait(false);
			}
		}
	}
}
