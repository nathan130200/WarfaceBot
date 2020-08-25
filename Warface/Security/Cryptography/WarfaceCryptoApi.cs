#pragma warning disable

using System;
using System.Runtime.InteropServices;

namespace Warface.Security.Cryptography
{
	internal static class WarfaceCryptoApi
	{

		const string LibraryName = "Warface.Native";

		[DllImport(LibraryName)]
		internal static extern IntPtr Crypto_Initialize(int salt, string version, string key, string iv);

		[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void Crypto_Release(IntPtr instance);

		[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void Crypto_Encrypt(IntPtr instance, IntPtr buffer, int size);

		[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void Crypto_Decrypt(IntPtr instance, IntPtr buffer, int size);
	}
}

#pragma warning restore