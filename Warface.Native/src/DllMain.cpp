#include "DllMain.h"

#define r2 wake->tmp
#define r1 wake->tmp
#define counter wake->counter
#define swap(x, y) Wake::Swap(x, y, wake)

// #ifndef strlen
// #define strlen std::strlen
// #endif

static const uint32_t tt[10] = {
	0x726a8f3bUL,
	0xe69a3b5cUL,
	0xd3c71fe5UL,
	0xab3c73d2UL,
	0x4d3a8eb3UL,
	0x0396d6e8UL,
	0x3d4c2f7aUL,
	0x9ee27cf3UL};

#if defined(WORDS_BIGENDIAN)

template <typename _Type>
_Type Wake::SwapValue(_Type value)
{
	return ((((value)&0xFF) << 24) | (((value) >> 24) & 0xFF) | (((value)&0x0000FF00) << 8) | (((value)&0x00FF0000) >> 8));
}

#endif

uint8_t Wake::Initialize(WakeKey *wake, uint32_t *key, int32_t key_size, uint32_t *iv, int32_t iv_size)
{
	uint32_t x, z, p;
	uint32_t k[4];

	if (ArraySize(key) != 32)
		return 1;

#if defined(WORDS_BIGENDIAN)
	k[0] = Wake::SwapValue(key[0]);
	k[1] = Wake::SwapValue(key[1]);
	k[2] = Wake::SwapValue(key[2]);
	k[3] = Wake::SwapValue(key[3]);
#else
	k[0] = key[0];
	k[1] = key[1];
	k[2] = key[2];
	k[3] = key[3];
#endif

	for (p = 0; p < 4; p++)
		wake->t[p] = k[p];

	for (p = 4; p < 256; p++)
	{
		x = wake->t[p - 4] + wake->t[p - 1];
		wake->t[p] = x >> 3 ^ tt[x & 7];
	}

	for (p = 0; p < 23; p++)
		wake->t[p] += wake->t[p + 89];

	x = wake->t[33];
	z = wake->t[59] | 0x01000001;
	z &= 0xff7fffff;

	for (p = 0; p < 256; p++)
	{
		x = (x & 0xff7fffff) + z;
		wake->t[p] = (wake->t[p] & 0x00ffffff) ^ x;
	}

	wake->t[256] = wake->t[0];
	x &= 0xff;

	for (p = 0; p < 256; p++)
	{
		wake->t[p] = wake->t[x =
								 (wake->t[p ^ x] ^ x) &
								 0xff];
		wake->t[x] = wake->t[p + 1];
	}

	counter = 0;
	wake->r[0] = k[0];
	wake->r[1] = k[1];
	wake->r[2] = k[2];

#if defined(WORDS_BIGENDIAN)
	wake->r[3] = Wake::SwapValue(k[3]);
#else
	wake->r[3] = k[3];
#endif

	wake->state = 0;

	if (iv_size > 32)
		wake->iv_size = 32;
	else
		wake->iv_size = iv_size / 4 * 4;

	if (iv == NULL)
		wake->iv_size = 0;
	else
		memcpy(&wake->iv, iv, iv_size);

	return 0;
}

void Wake::Encrypt(WakeKey *wake, uint8_t *buffer, int32_t size)
{
	uint32_t r3, r4, r5, r6;
	int32_t i;

	if (size <= 0)
		return;

	r3 = wake->r[0];
	r4 = wake->r[1];
	r5 = wake->r[2];
	r6 = wake->r[3];

	if (wake->state == 0)
	{
		wake->state = 1;
		Wake::Encrypt(wake, (uint8_t *)&wake->iv, wake->iv_size);
	}

	for (i = 0; i < size; i++)
	{
		buffer[i] ^= ((uint8_t *)&r6)[counter];
		((uint8_t *)&r2)[counter] = buffer[i];
		counter++;

		if (counter == 4)
		{
			counter = 0;

#if defined(WORDS_BIGENDIAN)
			r2 = Wake::SwapValue(r2);
			r6 = Wake::SwapValue(r6);
#endif
			r3 = swap(r3, r2);
			r4 = swap(r4, r3);
			r5 = swap(r5, r4);
			r6 = swap(r6, r5);

#if defined(WORDS_BIGENDIAN)
			r6 = Wake::SwapValue(r6);
#endif
		}
	}

	wake->r[0] = r3;
	wake->r[1] = r4;
	wake->r[2] = r5;
	wake->r[3] = r6;
}

void Wake::Decrypt(WakeKey *wake, uint8_t *buffer, int32_t size)
{
	uint32_t r3, r4, r5, r6;
	int32_t i;

	if (size <= 0)
		return;

	r3 = wake->r[0];
	r4 = wake->r[1];
	r5 = wake->r[2];
	r6 = wake->r[3];

	if (wake->state == 0)
	{
		wake->state = 1;
		Wake::Encrypt(wake, (uint8_t *)&wake->iv, wake->iv_size);

		wake->r[0] = r3;
		wake->r[1] = r4;
		wake->r[2] = r5;
		wake->r[3] = r6;
		Wake::Decrypt(wake, (uint8_t *)&wake->iv, wake->iv_size);
	}

	for (i = 0; i < size; i++)
	{
		((uint8_t *)&r1)[counter] = buffer[i];
		buffer[i] ^= ((uint8_t *)&r6)[counter];
		counter++;

		if (counter == 4)
		{
			counter = 0;

#if defined(WORDS_BIGENDIAN)
			r1 = Wake::SwapValue(r1);
			r6 = Wake::SwapValue(r6);
#endif
			r3 = swap(r3, r1);
			r4 = swap(r4, r3);
			r5 = swap(r5, r4);
			r6 = swap(r6, r5);

#if defined(WORDS_BIGENDIAN)
			r6 = Wake::SwapValue(r6);
#endif
		}
	}

	wake->r[0] = r3;
	wake->r[1] = r4;
	wake->r[2] = r5;
	wake->r[3] = r6;
}

int32_t Wake::Swap(int32_t x, int32_t y, WakeKey *wake)
{
	int32_t temp = x + y;
	return ((((temp) >> 8) & 0x00ffffff) ^ wake->t[(temp)&0xff]);
}

WarfaceCryptInformation *Crypto_Initialize(int32_t salt, char *version, char *key, char *iv)
{
	auto wci = new WarfaceCryptInformation;
	wci->salt = salt;
	wci->version = version;
	wci->key = key;
	wci->iv = iv;

	for (int i = 0; i < IV_SIZE; ++i)
		wci->iv[i] ^= wci->salt;

	if (wci->version != NULL && strlen(wci->version) > 0)
	{
		char *p = wci->version;
		int32_t v[4];

		for (int i = 0; i < 4; ++i)
		{
			char *end;
			v[i] = strtoul(p, &end, 10);

			if (end == NULL)
				break;

			for (int j = 0; j < 8; ++j)
				for (int i = 0; i < 4; ++i)
					wci->wake_key[i + (j * 4)] = (v[i] ^ (v[4 - 1] + j)) & 0xFF;
		}
	}

	if (wci->key != NULL && strlen(wci->key) > 0)
	{
		char *p = wci->key;

		for (int i = 0; i < KEY_SIZE; ++i)
		{
			char *end;
			wci->wake_key[i] = strtoul(p, &end, 10);

			if (end == NULL)
				break;

			p = end + 1;
		}
	}

	if (wci->iv != NULL && strlen(wci->iv) > 0)
	{
		char *p = wci->iv;

		for (int i = 0; i < IV_SIZE; ++i)
		{
			char *end;
			wci->wake_iv[i] = strtoul(p, &end, 10);

			if (end == NULL)
				break;

			p = end + 1;
		}
	}

	return wci;
}

void Crypto_Release(WarfaceCryptInformation *wci)
{
	if (wci && wci != NULL)
		delete wci;
}

void Crypto_Encrypt(WarfaceCryptInformation *wci, uint8_t *buffer, int32_t size)
{
	WakeKey k;
	Wake::Initialize(&k, wci->wake_key, KEY_SIZE, wci->wake_iv, IV_SIZE);
	Wake::Encrypt(&k, buffer, size);
}

void Crypto_Decrypt(WarfaceCryptInformation *wci, uint8_t *buffer, int32_t size)
{
	WakeKey k;
	Wake::Initialize(&k, wci->wake_key, KEY_SIZE, wci->wake_iv, IV_SIZE);
	Wake::Decrypt(&k, buffer, size);
}