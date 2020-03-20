cmake_minimum_required(VERSION 3.10)

project(librhino3dmio_native)

if(MACOS_BUILD)
  message("macOS Compile")
endif()

if(IOS)
  message("iOS Compile")
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Android")
  message("Android Compile")
endif()

file(GLOB bindings_SRC "./*.h" "./*.cpp")
file(GLOB zlib_SRC "../lib/opennurbs/zlib/*.h" "../lib/opennurbs/zlib/*.c")

# temporarily rename the 3 cpp files that we don't want to compile on OSX
file(RENAME "../lib/opennurbs/opennurbs_gl.cpp" "../lib/opennurbs/opennurbs_gl.skip")
file(RENAME "../lib/opennurbs/opennurbs_unicode_cp932.cpp" "../lib/opennurbs/opennurbs_unicode_cp932.skip")
file(RENAME "../lib/opennurbs/opennurbs_unicode_cp949.cpp" "../lib/opennurbs/opennurbs_unicode_cp949.skip")
file(GLOB opennurbs_SRC "../lib/opennurbs/*.h" "../lib/opennurbs/*.cpp")
file(RENAME "../lib/opennurbs/opennurbs_gl.skip" "../lib/opennurbs/opennurbs_gl.cpp")
file(RENAME "../lib/opennurbs/opennurbs_unicode_cp932.skip" "../lib/opennurbs/opennurbs_unicode_cp932.cpp")
file(RENAME "../lib/opennurbs/opennurbs_unicode_cp949.skip" "../lib/opennurbs/opennurbs_unicode_cp949.cpp")

# temporarily rename the files we don't want to compile unless we're targeting Android
if(NOT CMAKE_SYSTEM_NAME STREQUAL "Android")
  file(RENAME "../lib/opennurbs/android_uuid/gen_uuid_nt.c" "../lib/opennurbs/android_uuid/gen_uuid_nt.skip")
  file(GLOB uuid_SRC "../lib/opennurbs/android_uuid/*.h" "../lib/opennurbs/android_uuid/*.c")
  file(RENAME "../lib/opennurbs/android_uuid/gen_uuid_nt.skip" "../lib/opennurbs/android_uuid/gen_uuid_nt.c")
endif()

set (CMAKE_CXX_STANDARD 14)

#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")

add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
add_definitions(-DOPENNURBS_FREETYPE_INC_)
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
add_definitions(-DUNICODE)
add_definitions(-DRHINO3DMIO_BUILD)
add_definitions(-DCMAKE_BUILD)

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  set (CMAKE_CXX_STANDARD 17)
  add_definitions(-DON_RUNTIME_LINUX)
  add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
  add_library(librhino3dmio_native SHARED ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
  #target_link_libraries(librhino3dmio_native -luuid)
endif()

if(MACOS_BUILD)
  # [dan@mcneel.com]: For now, we're dumping all this into the same target.  Once we get 
  # a good handle on CMake, these should be compiled as separate CMake targets and linked accordingly.
  add_library(librhino3dmio_native SHARED ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
endif()

if(IOS)
  add_library(librhino3dmio_native STATIC ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
  # set_xcode_property is a macro defined in src/ios.toolchain.cmake
  # set_xcode_property (TARGET XCODE_PROPERTY XCODE_VALUE XCODE_VARIANT)
  #   A convenience macro for setting xcode specific properties on targets.
  #   Available variants are: All, Release, RelWithDebInfo, Debug, MinSizeRel
  #   example: set_xcode_property (myioslib IPHONEOS_DEPLOYMENT_TARGET "3.1" "all").
  set_xcode_property(librhino3dmio_native SDKROOT "iphoneos" "All")
  set_xcode_property(librhino3dmio_native TARGETED_DEVICE_FAMILY "Universal" "All")
  set_xcode_property(librhino3dmio_native EXECUTABLE_EXTENSION "a" "All")
  set_xcode_property(librhino3dmio_native MACH_O_TYPE "staticlib" "All")
  set_xcode_property(librhino3dmio_native CODE_SIGN_STYLE "Manual" "All")
  set_xcode_property(librhino3dmio_native CODE_SIGN_IDENTITY "Developer ID Application" "All")
  #target_link_libraries(librhino3dmio_native)
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Android")
  set (CMAKE_CXX_STANDARD 17)
  add_definitions(-DRHINO3DMIO_BUILD)
  add_definitions(-DON_COMPILING_OPENNURBS)
  add_definitions(-DFT2_BUILD_LIBRARY)
  add_definitions(-DMONO_BUILD)
  add_definitions(-D__ANDROID__)
  add_definitions(-DON_64BIT_RUNTIME)
  add_definitions(-DON_LITTLE_ENDIAN)
  add_definitions(-DON_COMPILER_ANDROIDNDK)   
  add_definitions(-D__GXX_EXPERIMENTAL_CXX0X__)
  add_library(librhino3dmio_native STATIC ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
  target_link_libraries(librhino3dmio_native android log)
endif()

set_target_properties(librhino3dmio_native PROPERTIES PREFIX "")

