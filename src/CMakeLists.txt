cmake_minimum_required(VERSION 3.10)

project(rhino3dm)

if (EMSCRIPTEN)
  message("Web Assembly Compile")
else()
  message("Python Compile")
  add_subdirectory(lib/pybind11)
endif()

file(GLOB bindings_SRC "bindings/*.h" "bindings/*.cpp")
file(GLOB zlib_SRC "lib/opennurbs/zlib/*.h" "lib/opennurbs/zlib/*.c")

# temporarily rename the 3 cpp files that we don't want to compile on OSX
file(RENAME "lib/opennurbs/opennurbs_gl.cpp" "lib/opennurbs/opennurbs_gl.skip")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp932.cpp" "lib/opennurbs/opennurbs_unicode_cp932.skip")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp949.cpp" "lib/opennurbs/opennurbs_unicode_cp949.skip")
file(GLOB opennurbs_SRC "lib/opennurbs/*.h" "lib/opennurbs/*.cpp")
file(RENAME "lib/opennurbs/opennurbs_gl.skip" "lib/opennurbs/opennurbs_gl.cpp")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp932.skip" "lib/opennurbs/opennurbs_unicode_cp932.cpp")
file(RENAME "lib/opennurbs/opennurbs_unicode_cp949.skip" "lib/opennurbs/opennurbs_unicode_cp949.cpp")

file(RENAME "lib/opennurbs/android_uuid/gen_uuid_nt.c" "lib/opennurbs/android_uuid/gen_uuid_nt.skip")
file(GLOB uuid_SRC "lib/opennurbs/android_uuid/*.h" "lib/opennurbs/android_uuid/*.c")
file(RENAME "lib/opennurbs/android_uuid/gen_uuid_nt.skip" "lib/opennurbs/android_uuid/gen_uuid_nt.c")

set (CMAKE_CXX_STANDARD 14)

if(EMSCRIPTEN)
  # we will eventually want the following warnging flags on both compiles
  # for now, just WASM
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s ALLOW_MEMORY_GROWTH=1 --bind")
endif()
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")


add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
add_definitions(-DOPENNURBS_FREETYPE_INC_)
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
add_definitions(-DUNICODE)
if(EMSCRIPTEN)
  # these shouldn't be necessary in the future as we adjust opennurbs to be
  # emscripten aware
  add_definitions(-DON_32BIT_RUNTIME)
  add_definitions(-DON_LITTLE_ENDIAN)
  add_definitions(-DON_RUNTIME_ANDROID)
endif()


if(EMSCRIPTEN)
  set(CMAKE_EXECUTABLE_SUFFIX ".html")
endif()

if(EMSCRIPTEN)
  add_executable(rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC})
else()
  if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    set (CMAKE_CXX_STANDARD 17)
    add_definitions(-DON_RUNTIME_LINUX)
    add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
    pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
    #target_link_libraries(_rhino3dm -luuid)
    target_link_libraries(_rhino3dm PRIVATE atomic)
  else()
    add_library(zlib_static STATIC ${zlib_SRC})
    add_library(opennurbs_static STATIC ${opennurbs_SRC})
    pybind11_add_module(_rhino3dm ${bindings_SRC})
    target_link_libraries(_rhino3dm PRIVATE zlib_static)
    target_link_libraries(_rhino3dm PRIVATE opennurbs_static)
  endif()
endif()

# I plan to mode the following install phase out of this file and into the top
# level python script for generating wasm
if(EMSCRIPTEN)
  install(DIRECTORY DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/../docs/javascript/samples/resources")
  install(FILES "build/javascript/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/../docs/javascript/samples/resources")
endif()
