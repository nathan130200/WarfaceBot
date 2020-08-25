#include <string>

#ifndef TYPES_H
#define TYPES_H

namespace t
{
	typedef unsigned char byte;
	typedef unsigned int uint32;
	typedef int int32;
	typedef char* string;
}

#if WORDS_BIGENDIAN

template <typename T>
T SwapEndianess(T value)
{
	return ((((value)&0xFF) << 24)
		| (((value) >> 24) & 0xFF)
		| (((value)&0x0000FF00) << 8)
		| (((value)&0x00FF0000) >> 8));
}

#endif // WORDS_BIGENDIAN

#endif // !TYPES_H