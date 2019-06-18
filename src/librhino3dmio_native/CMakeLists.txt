cmake_minimum_required(VERSION 3.10)

project(librhino3dmio_native)

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

file(RENAME "../lib/opennurbs/android_uuid/gen_uuid_nt.c" "../lib/opennurbs/android_uuid/gen_uuid_nt.skip")
file(GLOB uuid_SRC "../lib/opennurbs/android_uuid/*.h" "../lib/opennurbs/android_uuid/*.c")
file(RENAME "../lib/opennurbs/android_uuid/gen_uuid_nt.skip" "../lib/opennurbs/android_uuid/gen_uuid_nt.c")

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
  add_library(librhino3dmio_native SHARED ${bindings_SRC})
  #target_link_libraries(librhino3dmio_native -luuid)
else()
  add_library(zlib_static STATIC ${zlib_SRC})
  add_library(opennurbs_static STATIC ${opennurbs_SRC})
  add_library(librhino3dmio_native SHARED ${bindings_SRC})
  target_link_libraries(librhino3dmio_native PRIVATE zlib_static)
  target_link_libraries(librhino3dmio_native PRIVATE opennurbs_static)
endif()

set_target_properties(librhino3dmio_native PROPERTIES PREFIX "")

