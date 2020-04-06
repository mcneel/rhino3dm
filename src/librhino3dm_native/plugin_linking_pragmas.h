#pragma once

#if defined(CMAKE_BUILD)
#pragma comment(lib, "Shlwapi.lib")
#endif

#if !defined(__APPLE__) && !defined(__ANDROID__) && !defined(CMAKE_BUILD)

#if defined(RHINO3DM_BUILD)

#pragma message( " --- linking required Win libs" )
#pragma comment(lib, "Advapi32.lib")
#pragma comment(lib, "Gdi32.lib")
#pragma comment(lib, "Rpcrt4.lib")
#pragma comment(lib, "Shlwapi.lib")
#pragma comment(lib, "User32.lib")
#pragma comment(lib, "Ole32.lib")
#pragma comment(lib, "Shell32.lib")

#if !defined(ON_MSC_SOLUTION_DIR)
// ON_MSC_SOLUTION_DIR must have a trailing slash
#define ON_MSC_SOLUTION_DIR "./"
#endif

#if !defined(ON_MSC_LIB_DIR)

#if defined(WIN64)

// x64 (64 bit) static libraries

#if defined(NDEBUG)

// Release x64 (64 bit) libs
#define ON_MSC_LIB_DIR "x64/Release/"
#pragma message( " --- statically linking opennurbs." )
#pragma comment(lib, "./opennurbs_public/bin/x64/Release/zlib.lib")
#pragma comment(lib, "./opennurbs_public/bin/x64/Release/opennurbs_public_staticlib.lib")

#else // _DEBUG

// Debug x64 (64 bit) libs
#define ON_MSC_LIB_DIR "x64/Debug/"
#pragma message( " --- statically linking opennurbs." )
#pragma comment(lib, "./opennurbs_public/bin/x64/Debug/zlib.lib")
#pragma comment(lib, "./opennurbs_public/bin/x64/Debug/opennurbs_public_staticlib.lib")

#endif // NDEBUG else _DEBUG

#else // WIN32

#if defined(NDEBUG)

// Release x86 (32 bit) libs
#define ON_MSC_LIB_DIR "Release/"
#pragma message( " --- statically linking opennurbs." )
#pragma comment(lib, "./opennurbs_public/bin/Win32/Release/zlib.lib")
#pragma comment(lib, "./opennurbs_public/bin/Win32/Release/opennurbs_public_staticlib.lib")

#else // _DEBUG

// Debug x86 (32 bit) libs
#define ON_MSC_LIB_DIR "Debug/"
#pragma message( " --- statically linking opennurbs." )
#pragma comment(lib, "./opennurbs_public/bin/Win32/Debug/zlib.lib")
#pragma comment(lib, "./opennurbs_public/bin/Win32/Debug/opennurbs_public_staticlib.lib")

#endif // NDEBUG else _DEBUG

#endif // WIN64 else WIN32

#endif //  !defined(ON_MSC_LIB_DIR)

#else

#include "../../../rhino4/RhinoCoreLinkingPragmas.h"


#pragma comment(lib, "OPENGL32.LIB")
#pragma comment(lib, "GLU32.LIB")

#endif

#endif //!defined(__APPLE__)
