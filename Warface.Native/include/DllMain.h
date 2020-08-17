#pragma once

#ifndef NATIVE_EXPORTS
#if defined(WIN32) || defined(__MINGW32__) || defined(_WIN32) || defined(_WIN64)
#define NATIVE_EXPORTS __declspec(dllexport)
#else
#define NATIVE_EXPORTS __attribute__((visibility("default")))
#endif
#endif //!NATIVE_EXPORTS

#define KEY_SIZE 32
#define IV_SIZE 8

#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

struct WarfaceCryptInformation
{
	int32_t salt;
	char *version;
	char *key;
	char *iv;

	uint32_t wake_key[32];
	uint32_t wake_iv[8]{
		0x31C0E100,
		0x01C8008C,
		0x329F0AE5,
		0x00D80763,
		0x2E7D7958,
		0x39CF165A,
		0x137F7D26,
	};
};

#ifdef __cplusplus
extern "C"
{
#endif

	NATIVE_EXPORTS WarfaceCryptInformation *Crypto_Initialize(int32_t salt, char *version, char *key, char *iv);
	NATIVE_EXPORTS void Crypto_Release(WarfaceCryptInformation *wci);
	NATIVE_EXPORTS void Crypto_Encrypt(WarfaceCryptInformation *wci, uint8_t *buffer, int32_t size);
	NATIVE_EXPORTS void Crypto_Decrypt(WarfaceCryptInformation *wci, uint8_t *buffer, int32_t size);

#ifdef __cplusplus
}
#endif

struct WakeKey
{
	uint32_t t[257];
	uint32_t r[4];
	uint32_t counter;
	uint32_t tmp;
	uint8_t state;
	uint32_t iv[8];
	int32_t iv_size;
};

template <typename _Type>
size_t ArraySize(_Type array)
{
	return (sizeof(array) / sizeof(array[0]));
}

namespace Wake
{
	uint8_t Initialize(WakeKey *wake, uint32_t *key, int32_t key_size, uint32_t *iv, int32_t iv_size);
	int32_t Swap(int32_t x, int32_t y, WakeKey *wake);
	void Encrypt(WakeKey *key, uint8_t *buffer, int32_t size);
	void Decrypt(WakeKey *key, uint8_t *buffer, int32_t size);

#if defined(WORDS_BIGENDIAN)
	template <typename _Type>
	_Type SwapValue(_Type value);
#endif
} // namespace Wake