cmake_minimum_required(VERSION 3.10)

project(librhino3dm_native)

if(MACOS_BUILD)
  message(STATUS "macOS Compile")
endif()

if(IOS)
  message(STATUS "iOS Compile")
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Android")
  message(STATUS "Android Compile")
endif()

file(GLOB bindings_SRC "./*.h" "./*.cpp")
# draco.cpp (synced from RhinoCommon's c/draco.cpp): the Draco encode/decode subset rhino3dm compiles,
# not all of Draco (../draco_sources.cmake, written by tools/dev/draco_subset.py)
include("${CMAKE_CURRENT_SOURCE_DIR}/../draco_sources.cmake")
include_directories(${draco_subset_INCLUDE})
list(APPEND bindings_SRC ${draco_subset_SRC})
include("${CMAKE_CURRENT_SOURCE_DIR}/../../cmake/opennurbs_sources.cmake")
set(zlib_SRC ${RHINO3DM_ZLIB_SRC})
set(opennurbs_SRC ${RHINO3DM_OPENNURBS_SRC})
set(uuid_SRC ${RHINO3DM_UUID_SRC})

# C++17 on every platform: opennurbs 9 uses <execution> (std::execution::par_unseq) on MSVC and
# std::to_chars for doubles
set (CMAKE_CXX_STANDARD 17)
# No fused multiply-add contraction on Linux: GCC defaults to -ffp-contract=fast, which fuses
# across statements. Apple clang keeps its default (on: within an expression), so macOS arm64
# can differ from x64 in the last bit; MSVC does not contract unless /fp:contract
if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  add_compile_options(-ffp-contract=off)
endif()

#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")

add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
# NOTE: OPENNURBS_FREETYPE_INC_ is opennurbs_freetype.h's OWN include guard.
# Pre-defining it here makes that header a no-op, suppressing every
# ON_FreeType* declaration. That was harmless while nothing referenced them,
# but opennurbs 9.x defines OPENNURBS_FREETYPE_SUPPORT for ON_RUNTIME_LINUX,
# so the .cpp files now call functions this would hide. Skip it on Linux.
if(NOT CMAKE_SYSTEM_NAME STREQUAL "Linux")
  add_definitions(-DOPENNURBS_FREETYPE_INC_)
endif()
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
add_definitions(-DUNICODE)
add_definitions(-DRHINO3DM_BUILD)
add_definitions(-DCMAKE_BUILD)
add_definitions(-DON_CMAKE_BUILD)

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  set (CMAKE_CXX_STANDARD 17)
  add_definitions(-DON_RUNTIME_LINUX)
  add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
  add_library(librhino3dm_native SHARED ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
  #target_link_libraries(librhino3dm_native -luuid)
  # opennurbs 9.x defines OPENNURBS_FREETYPE_SUPPORT whenever ON_RUNTIME_LINUX is
  # set (opennurbs_system.h); in 8.x that was ON_RUNTIME_ANDROID only. The vendored
  # FreeType is therefore now required to build opennurbs on Linux. This mirrors
  # opennurbs' own CMakeLists: build freetype263, add its include dir, link it.
  # CMAKE_DL_LIBS is for dladdr(), used by Internal_GetLinuxInstalledFonts.
  add_subdirectory(../lib/opennurbs/freetype263 freetype263)
  target_include_directories(librhino3dm_native PRIVATE ../lib/opennurbs/freetype263/include)
  target_link_libraries(librhino3dm_native opennurbs_public_freetype ${CMAKE_DL_LIBS})
endif()

if(MACOS_BUILD)
  # [dan@mcneel.com]: For now, we're dumping all this into the same target.  Once we get 
  # a good handle on CMake, these should be compiled as separate CMake targets and linked accordingly.
  # universal (arm64 + x86_64): CMAKE_OSX_ARCHITECTURES must be set before the target exists, so
  # the dotnet-macos preset passes it on the command line
  add_library(librhino3dm_native SHARED ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
endif()

if(IOS)
  add_library(librhino3dm_native STATIC ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
  # set_xcode_property is a macro defined in src/ios.toolchain.cmake
  # set_xcode_property (TARGET XCODE_PROPERTY XCODE_VALUE XCODE_VARIANT)
  #   A convenience macro for setting xcode specific properties on targets.
  #   Available variants are: All, Release, RelWithDebInfo, Debug, MinSizeRel
  #   example: set_xcode_property (myioslib IPHONEOS_DEPLOYMENT_TARGET "3.1" "all").
  set_xcode_property(librhino3dm_native SDKROOT "iphoneos" "All")
  set_xcode_property(librhino3dm_native TARGETED_DEVICE_FAMILY "Universal" "All")
  set_xcode_property(librhino3dm_native EXECUTABLE_EXTENSION "a" "All")
  set_xcode_property(librhino3dm_native MACH_O_TYPE "staticlib" "All")
  set_xcode_property(librhino3dm_native CODE_SIGN_STYLE "Manual" "All")
  set_xcode_property(librhino3dm_native CODE_SIGN_IDENTITY "Developer ID Application" "All")
  #target_link_libraries(librhino3dm_native)
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Android")
  set (CMAKE_CXX_STANDARD 17)
  add_definitions(-DRHINO3DM_BUILD)
  add_definitions(-DON_COMPILING_OPENNURBS)
  add_definitions(-DFT2_BUILD_LIBRARY)
  add_definitions(-DMONO_BUILD)
  add_definitions(-D__ANDROID__)
  add_definitions(-DON_64BIT_RUNTIME)
  add_definitions(-DON_LITTLE_ENDIAN)
  add_definitions(-DON_COMPILER_ANDROIDNDK)   
  add_definitions(-D__GXX_EXPERIMENTAL_CXX0X__)
  add_library(librhino3dm_native SHARED ${uuid_SRC} ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
  find_library(log-lib log)
  target_link_libraries(librhino3dm_native ${log-lib})
  add_library(app-glue STATIC ${ANDROID_NDK}/sources/android/native_app_glue/android_native_app_glue.c )
  target_link_libraries(librhino3dm_native app-glue ${log-lib})
endif()

if (WIN32)
  add_library(librhino3dm_native SHARED ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
  # 64-bit only (opennurbs 9): opennurbs refuses WIN32 together with WIN64 on x64
  # (opennurbs_system.h), so WIN64 replaces CMake's /DWIN32, as opennurbs's own CMakeLists does
  # (src/lib/opennurbs/CMakeLists.txt, MSVC block). Replaces the old script/setup.py .vcxproj edit.
  string(REPLACE "/DWIN32" "/DWIN64" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  string(REPLACE "/DWIN32" "/DWIN64" CMAKE_C_FLAGS "${CMAKE_C_FLAGS}")
endif()

set_target_properties(librhino3dm_native PROPERTIES PREFIX "")

