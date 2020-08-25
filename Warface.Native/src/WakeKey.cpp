#include "WakeKey.h"

#define r2 wake_key->tmp
#define r1 wake_key->tmp
#define counter wake_key->counter
#define M(X,Y) _uint32_M(X,Y, wake_key)

static t::uint32 _uint32_M(t::uint32 X, t::uint32 Y, WakeKey* wake_key)
{
	t::uint32 TMP = X + Y;
	return ((((TMP) >> 8) & 0x00ffffff) ^ wake_key->t[(TMP)& 0xff]);
}

void Wake_Initialize(WakeKey* wake_key, t::uint32* key, t::int32 len, t::uint32* IV, t::int32 ivlen)
{
	t::uint32 x, z, p;
	t::uint32 k[4];
	
	if (len != 32)
		return;

#ifdef WORDS_BIGENDIAN
	k[0] = SwapEndianess(key[0]);
	k[1] = SwapEndianess(key[1]);
	k[2] = SwapEndianess(key[2]);
	k[3] = SwapEndianess(key[3]);
#else
	k[0] = key[0];
	k[1] = key[1];
	k[2] = key[2];
	k[3] = key[3];
#endif

	for (p = 0; p < 4; p++) {
		wake_key->t[p] = k[p];
	}

	for (p = 4; p < 256; p++) {
		x = wake_key->t[p - 4] + wake_key->t[p - 1];
		wake_key->t[p] = x >> 3 ^ tt[x & 7];
	}

	for (p = 0; p < 23; p++)
		wake_key->t[p] += wake_key->t[p + 89];

	x = wake_key->t[33];
	z = wake_key->t[59] | 0x01000001;
	z &= 0xff7fffff;

	for (p = 0; p < 256; p++) {
		x = (x & 0xff7fffff) + z;
		wake_key->t[p] = (wake_key->t[p] & 0x00ffffff) ^ x;
	}

	wake_key->t[256] = wake_key->t[0];
	x &= 0xff;

	for (p = 0; p < 256; p++) {
		wake_key->t[p] = wake_key->t[x =
			(wake_key->t[p ^ x] ^ x) &
			0xff];
		wake_key->t[x] = wake_key->t[p + 1];
	}

	counter = 0;
	wake_key->r[0] = k[0];
	wake_key->r[1] = k[1];
	wake_key->r[2] = k[2];
#ifdef WORDS_BIGENDIAN
	wake_key->r[3] = SwapEndianess(k[3]);
#else
	wake_key->r[3] = k[3];
#endif

	wake_key->started = 0;
	if (ivlen > 32)
		wake_key->ivsize = 32;
	else
		wake_key->ivsize = ivlen / 4 * 4;

	if (IV == NULL)
		wake_key->ivsize = 0;
		
	if (wake_key->ivsize > 0 && IV != NULL)
		memcpy(&wake_key->iv, IV, wake_key->ivsize);
}
	
void Wake_Encrypt(WakeKey* wake_key, t::byte* buffer, t::int32 len)
{
	t::uint32 r3, r4, r5, r6;
	t::int32 i;

	if (len == 0)
		return;

	r3 = wake_key->r[0];
	r4 = wake_key->r[1];
	r5 = wake_key->r[2];
	r6 = wake_key->r[3];

	if (wake_key->started == 0) {
		wake_key->started = 1;
		Wake_Encrypt(wake_key, (t::byte*)& wake_key->iv,
			wake_key->ivsize);
	}

	for (i = 0; i < len; i++) {
		/* R1 = V[n] = V[n] XOR R6 - here we do it per byte --sloooow */
		/* R1 is ignored */
		buffer[i] ^= ((t::byte*)& r6)[counter];

		/* R2 = V[n] = R1 - per byte also */
		((t::byte*)& r2)[counter] = buffer[i];
		counter++;

		if (counter == 4) {	/* r6 was used - update it! */
			counter = 0;

#ifdef WORDS_BIGENDIAN
			/* these swaps are because we do operations per byte */
			r2 = SwapEndianess(r2);
			r6 = SwapEndianess(r6);
#endif
			r3 = M(r3, r2);
			r4 = M(r4, r3);
			r5 = M(r5, r4);
			r6 = M(r6, r5);

#ifdef WORDS_BIGENDIAN
			r6 = SwapEndianess(r6);
#endif
		}
	}

	wake_key->r[0] = r3;
	wake_key->r[1] = r4;
	wake_key->r[2] = r5;
	wake_key->r[3] = r6;

}
	
void Wake_Decrypt(WakeKey* wake_key, t::byte* buffer, t::int32 len)
{
	t::uint32 r3, r4, r5, r6;
	t::int32 i;

	if (len == 0)
		return;

	r3 = wake_key->r[0];
	r4 = wake_key->r[1];
	r5 = wake_key->r[2];
	r6 = wake_key->r[3];

	if (wake_key->started == 0) {
		wake_key->started = 1;
		Wake_Encrypt(wake_key, (t::byte*)& wake_key->iv,
			wake_key->ivsize);
		wake_key->r[0] = r3;
		wake_key->r[1] = r4;
		wake_key->r[2] = r5;
		wake_key->r[3] = r6;
		
		Wake_Decrypt(wake_key, (t::byte*)& wake_key->iv,
			wake_key->ivsize);
	}

	for (i = 0; i < len; i++) {
		/* R1 = V[n] */
		((t::byte*)& r1)[counter] = buffer[i];
		/* R2 = V[n] = V[n] ^ R6 */
		/* R2 is ignored */
		buffer[i] ^= ((t::byte *)& r6)[counter];
		counter++;

		if (counter == 4) {
			counter = 0;

#ifdef WORDS_BIGENDIAN
			r1 = SwapEndianess(r1);
			r6 = SwapEndianess(r6);
#endif
			r3 = M(r3, r1);
			r4 = M(r4, r3);
			r5 = M(r5, r4);
			r6 = M(r6, r5);

#ifdef WORDS_BIGENDIAN
			r6 = SwapEndianess(r6);
#endif
		}
	}

	wake_key->r[0] = r3;
	wake_key->r[1] = r4;
	wake_key->r[2] = r5;
	wake_key->r[3] = r6;
}