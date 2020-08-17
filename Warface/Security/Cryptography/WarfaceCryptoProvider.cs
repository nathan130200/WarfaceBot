using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Warface.Security.Cryptography
{
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class WarfaceCryptoProvider : IDisposable
	{
		private IntPtr Handle;
		private volatile bool Disposed;
		private readonly int Salt;
		private readonly string Version;
		private readonly string Key;
		private readonly string Iv;

		public bool IsInitialized => this.Handle != IntPtr.Zero;

		public WarfaceCryptoProvider(int salt, string version, string key, string iv)
		{
			this.Salt = salt;
			this.Version = version;
			this.Key = key;
			this.Iv = iv;

			this.Handle = WarfaceCryptoApi.Create(this.Salt, this.Version, this.Key, this.Iv);
		}

		public void Encrypt(byte[] buffer)
		{
			this.EnsureNotDisposed();

			var ptr = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			try
			{
				WarfaceCryptoApi.Encrypt(this.Handle, ptr.AddrOfPinnedObject(), buffer.Length);
			}
			finally
			{
				ptr.Free();
			}
		}

		public void Decrypt(byte[] buffer)
		{
			this.EnsureNotDisposed();

			var ptr = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			try
			{
				WarfaceCryptoApi.Decrypt(this.Handle, ptr.AddrOfPinnedObject(), buffer.Length);
			}
			finally
			{
				ptr.Free();
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.Salt, this.Version);
		}

		public override string ToString()
		{
			return $"Salt={this.Salt}; Version={this.Version}";
		}

		void EnsureNotDisposed()
		{
			if (this.Disposed)
				throw new ObjectDisposedException(nameof(this.Handle));
		}

		public void Dispose()
		{
			this.EnsureNotDisposed();

			this.Disposed = true;

			if (this.Handle != IntPtr.Zero)
			{
				WarfaceCryptoApi.Release(this.Handle);
				this.Handle = IntPtr.Zero;
			}
		}
	}
}
