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
		ClientKey = 2,
		ServerKey = 3,
		ClientAck = 4
	}

	public struct Packet
	{
		public PacketType Type { get; }
		public byte[] Buffer { get; }
		public int BufferSize => this.Buffer?.Length ?? 0;

		public Packet(PacketType type, byte[] buffer = default) : this()
		{
			this.Type = type;
			this.Buffer = buffer;
		}

		internal byte[] Serialize()
		{
			var buffer = new byte[12 + this.BufferSize];

			var magicBuff = BitConverter.GetBytes(PacketExtensions.Magic);
			var lengthBuff = BitConverter.GetBytes(this.BufferSize);
			var typeBuff = BitConverter.GetBytes((int)this.Type);

			Array.Copy(magicBuff, 0, buffer, 0, 4);
			Array.Copy(lengthBuff, 0, buffer, 4, 4);
			Array.Copy(typeBuff, 0, buffer, 8, 4);

			if (this.BufferSize > 0)
				Array.Copy(this.Buffer, 0, buffer, 12, this.BufferSize);

			return buffer;
		}
	}

	public static class PacketExtensions
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
			await stream.WriteAsync(result, 0, result.Length);
		}

		public static Packet EncryptPacket(this Packet p, WarfaceCryptoProvider provider)
		{
			if (p.Type != PacketType.Encrypted)
				throw new ArgumentException("Cannot encrypt packet, because it isn't encrypted.");

			if (p.BufferSize == 0)
				throw new ArgumentException("Cannot encrypt packet, because it has zero length.");

			var buffer = new byte[p.BufferSize];
			Array.Copy(p.Buffer, 0, buffer, 0, buffer.Length);
			provider.Encrypt(buffer);
			return new Packet(PacketType.Encrypted, buffer);
		}

		public static Packet DecryptPacket(this Packet p, WarfaceCryptoProvider provider)
		{
			if (p.Type != PacketType.Encrypted)
				throw new ArgumentException("Cannot decrypt packet, because it isn't encrypted.");

			if (p.BufferSize == 0)
				throw new ArgumentException("Cannot decrypt packet, because it has zero length.");

			var buffer = new byte[p.BufferSize];
			Array.Copy(p.Buffer, 0, buffer, 0, buffer.Length);
			provider.Decrypt(buffer);
			return new Packet(PacketType.Plain, buffer);
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
