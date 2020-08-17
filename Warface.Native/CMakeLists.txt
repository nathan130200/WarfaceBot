cmake_minimum_required(VERSION 3.8)

project("Warface.Native")

include(TestBigEndian)

TEST_BIG_ENDIAN(WORDS_BIGENDIAN)

IF(${WORDS_BIGENDIAN})
	add_definitions(-DWORDS_BIGENDIAN=1)
ENDIF()

add_library(Warface.Native SHARED
	"src/DllMain.cpp"
)

target_include_directories(Warface.Native PUBLIC
	${PROJECT_SOURCE_DIR}/include	
)