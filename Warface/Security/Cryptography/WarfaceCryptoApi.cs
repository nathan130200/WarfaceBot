using System;
using System.Runtime.InteropServices;

namespace Warface.Security.Cryptography
{
	internal static class WarfaceCryptoApi
	{
		const string LibraryName = "Warface.Native";

		[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Crypto_Initialize")]
		internal static extern IntPtr Create(int salt, string version, string key, string iv);

		[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Crypto_Release")]
		internal static extern void Release(IntPtr instance);

		[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Crypto_Encrypt")]
		internal static extern void Encrypt(IntPtr instance, IntPtr buffer, int count);

		[DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Crypto_Decrypt")]
		internal static extern void Decrypt(IntPtr instance, IntPtr buffer, int count);
	}
}
