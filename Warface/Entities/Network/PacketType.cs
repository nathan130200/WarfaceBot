using System;
using System.IO;
using System.Threading.Tasks;
using Warface.Security.Cryptography;

namespace Warface.Entities.Network
{
	public enum PacketType
	{
		Plain = 0,
		Encrypted = 1,
		//ClientKey = 2,
		ServerKey = 3,
		ClientAck = 4
	}

	public class Packet
	{
		public PacketType Type { get; internal set; }
		public byte[] Buffer { get; internal set; }
		public int BufferSize => this.Buffer?.Length ?? 0;

		public Packet(PacketType type, byte[] buffer = default)
		{
			this.Type = type;
			this.Buffer = buffer ?? Array.Empty<byte>();
		}

		internal byte[] Serialize()
		{
			var buffer = new byte[12 + this.BufferSize];

			var magicBuff = BitConverter.GetBytes(PacketUtilities.Magic);
			var lengthBuff = BitConverter.GetBytes(this.BufferSize);
			var typeBuff = BitConverter.GetBytes((int)this.Type);

			Array.Copy(magicBuff, 0, buffer, 0, 4);
			Array.Copy(lengthBuff, 0, buffer, 4, 4);
			Array.Copy(typeBuff, 0, buffer, 8, 4);

			if (this.BufferSize > 0)
				Array.Copy(this.Buffer, 0, buffer, 12, this.BufferSize);

			return buffer;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.Type, this.Buffer);
		}
	}

	public static class PacketUtilities
	{
		/// <summary>
		/// Stream Magic Header
		/// </summary>
		public const uint Magic = 0xFEEDDEAD;

		public static async Task<Packet> ReadPacketAsync(this Stream stream)
		{
			var buffer = new byte[12];

			if (await stream.ReadAsync(buffer, 0, buffer.Length) == buffer.Length
				&& ExtractPacketInformation(buffer, out var size, out var type))
			{
				buffer = new byte[size];

				if (await stream.ReadAsync(buffer, 0, buffer.Length) == buffer.Length)
					return new Packet(type, buffer);
			}

			return default;
		}

		public static async Task WritePacketAsync(this Stream stream, Packet packet)
		{
			var result = packet.Serialize();
			await stream.WriteAsync(result, 0, result.Length).ConfigureAwait(false);
		}

		public static void Encrypt(this Packet p, WarfaceCryptoProvider provider)
		{
			if (p.Type != PacketType.Plain)
				throw new ArgumentException("Cannot encrypt packet, because it is not valid packet to encrypt.", nameof(p));

			if (p.BufferSize == 0)
				throw new ArgumentException("Cannot encrypt packet, because it has zero length.", nameof(p));

			//var buffer = new byte[p.BufferSize];
			//Array.Copy(p.Buffer, 0, buffer, 0, buffer.Length);
			//provider.Encrypt(buffer);

			//return new Packet(PacketType.Encrypted, buffer);

			var data = new byte[p.BufferSize];
			Array.Copy(p.Buffer, 0, data, 0, data.Length);
			provider.Encrypt(data);

			p.Buffer = data;
			p.Type = PacketType.Encrypted;
		}

		public static void Decrypt(this Packet p, WarfaceCryptoProvider provider)
		{
			if (p.Type != PacketType.Encrypted)
				throw new ArgumentException("Cannot decrypt packet, because it is not encrypted.", nameof(p));

			if (p.BufferSize == 0)
				throw new ArgumentException("Cannot decrypt packet, because it has zero length.", nameof(p));

			//var buffer = new byte[p.BufferSize];
			//Array.Copy(p.Buffer, 0, buffer, 0, buffer.Length);
			//provider.Decrypt(buffer);

			//return new Packet(PacketType.Plain, buffer);

			provider.Decrypt(p.Buffer);
			p.Type = PacketType.Plain;
		}

		static bool ExtractPacketInformation(byte[] buffer, out int length, out PacketType type)
		{
			length = default;
			type = default;

			if (buffer.Length == 12)
			{
				var magicBuff = new byte[4];
				var lengthBuff = new byte[4];
				var typeBuff = new byte[4];

				Array.Copy(buffer, 0, magicBuff, 0, 4);
				Array.Copy(buffer, 4, lengthBuff, 0, 4);
				Array.Copy(buffer, 8, typeBuff, 0, 4);

				var magic = BitConverter.ToUInt32(magicBuff);
				length = BitConverter.ToInt32(lengthBuff);
				type = (PacketType)BitConverter.ToInt32(typeBuff);

				return magic == Magic;
			}

			return false;
		}
	}
}
