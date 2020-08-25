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

		public bool IsInitialized =>
			this.Handle != IntPtr.Zero;

		public WarfaceCryptoProvider(int salt, string version, string key, string iv)
		{
			this.Handle = WarfaceCryptoApi.Crypto_Initialize(salt, version, key, iv);
		}

		public unsafe void Encrypt(byte[] buffer)
		{
			this.EnsureNotDisposed();

			var pGC = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			try
			{
				WarfaceCryptoApi.Crypto_Encrypt(this.Handle, pGC.AddrOfPinnedObject(), buffer.Length);
			}
			finally
			{
				pGC.Free();
			}
		}

		public unsafe void Decrypt(byte[] buffer)
		{
			var pGC = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			try
			{
				WarfaceCryptoApi.Crypto_Decrypt(this.Handle, pGC.AddrOfPinnedObject(), buffer.Length);
			}
			finally
			{
				pGC.Free();
			}
		}

		public override int GetHashCode()
			=> this.Handle.GetHashCode();

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
				WarfaceCryptoApi.Crypto_Release(this.Handle);
				this.Handle = IntPtr.Zero;
			}
		}
	}
}
