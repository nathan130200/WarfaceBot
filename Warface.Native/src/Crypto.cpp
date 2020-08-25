#include "WakeKey.h"
#include "Crypto.h"

#define crypt_key_len(Q)  sizeof(Q->crypt_key) / sizeof(Q->crypt_key[0])
#define crypt_iv_len(Q) sizeof(Q->crypt_iv) / sizeof(Q->crypt_iv[0])

CryptoInfoPtr Crypto_Initialize(t::int32 salt,
								t::string version,
								t::string key,
								t::string iv)
{
	auto info = new CryptoInfo;
	
	 int i = 0;

    for (; i < crypt_iv_len(info); ++i)
        info->crypt_iv[i] ^= salt;

    if (version != NULL)
    {
        const char *p = version;

        int ver[4];
        int ver_len = sizeof (ver) / sizeof (ver[0]);

        for (int i = 0; i < ver_len; ++i)
        {
            char *end;

            ver[i] = strtoul(p, &end, 10);

            if (end == NULL)
                break;

            p = end + 1;
        }

        for (int j = 0; j < 8; ++j)
            for (int i = 0; i < ver_len; ++i)
                info->crypt_key[i + (j * 4)] =
                    (ver[i] ^ (ver[ver_len - 1] + j)) & 0xFF;
    }

    if (key != NULL)
    {
        const char *p = key;

        for (int i = 0; i < crypt_key_len(info); ++i)
        {
            char *end;

            info->crypt_key[i] = strtoul(p, &end, 10);

            if (end == NULL)
                break;

            p = end + 1;
        }
    }

    if (iv != NULL)
    {
        const char *p = iv;

        for (int i = 0; i < crypt_iv_len(info); ++i)
        {
            char *end;

            info->crypt_iv[i] = strtoul(p, &end, 10);

            if (end == NULL)
                break;

            p = end + 1;
        }
    }
	
	return (CryptoInfoPtr)info;
}

void Crypto_Release(CryptoInfoPtr ptr){
	if(ptr && ptr != NULL) {
		delete ptr;
	}
}

void Crypto_Encrypt(CryptoInfoPtr ptr, t::byte* buffer, t::int32 size){
	WakeKey key;
	Wake_Initialize(&key, ptr->crypt_key, crypt_key_len(ptr), ptr->crypt_iv, crypt_iv_len(ptr));
	Wake_Encrypt(&key, buffer, size);
}

void Crypto_Decrypt(CryptoInfoPtr ptr, t::byte* buffer, t::int32 size){
	WakeKey key;
	Wake_Initialize(&key, ptr->crypt_key, crypt_key_len(ptr), ptr->crypt_iv, crypt_iv_len(ptr));
	Wake_Decrypt(&key, buffer, size);
}