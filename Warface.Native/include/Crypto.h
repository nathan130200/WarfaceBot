#include "Types.h"

#ifndef CRYPTO_H
#define CRYPTO_H

#ifndef CRYPTO_API
#ifdef _WIN32
#define CRYPTO_API __declspec(dllexport)
#else
#define CRYPTO_API
#endif
#endif

typedef struct CryptoInfo
{
public:
	t::int32 salt;
	t::string key;
	t::string iv;
	t::string version;

	t::uint32 crypt_key[32];
	t::uint32 crypt_iv[8] =
		{
			0x31C0E100,
			0x01C8008C,
			0x329F0AE5,
			0x00D80763,
			0x2E7D7958,
			0x39CF165A,
			0x137F7D26,
	};

} * CryptoInfoPtr;

#define DECLARE_NATIVE_API(FunctionName, ReturnType, ...) CRYPTO_API ReturnType FunctionName(__VA_ARGS__)

#ifdef __cplusplus
extern "C"
{
#endif

	DECLARE_NATIVE_API(Crypto_Initialize, CryptoInfoPtr, t::int32, t::string, t::string, t::string);
	DECLARE_NATIVE_API(Crypto_Release, void, CryptoInfoPtr);
	DECLARE_NATIVE_API(Crypto_Encrypt, void, CryptoInfoPtr, t::byte *, t::int32);
	DECLARE_NATIVE_API(Crypto_Decrypt, void, CryptoInfoPtr, t::byte *, t::int32);

#ifdef __cplusplus
}
#endif

#endif // !CRYPTO_H