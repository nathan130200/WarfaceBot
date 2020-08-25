#include "Types.h"

#ifndef WAKEKEY_H
#define WAKEKEY_H

struct WakeKey {
	t::uint32 t[257];
	t::uint32 r[4];
	t::int32 counter;
	t::uint32 tmp;
	t::byte started;
	t::uint32 iv[8];
	t::int32 ivsize;
};

static const t::uint32 tt[10] = {
	0x726a8f3bUL,
	0xe69a3b5cUL,
	0xd3c71fe5UL,
	0xab3c73d2UL,
	0x4d3a8eb3UL,
	0x0396d6e8UL,
	0x3d4c2f7aUL,
	0x9ee27cf3UL
};

void Wake_Initialize(WakeKey* wake_key,
	t::uint32* key, t::int32 len,
	t::uint32* IV, t::int32 ivlen);
	
void Wake_Encrypt(WakeKey* wake_key,
	t::byte* buffer, t::int32 len);
	
void Wake_Decrypt(WakeKey* wake_key,
	t::byte* buffer, t::int32 len);

#endif // !WAKEKEY_H