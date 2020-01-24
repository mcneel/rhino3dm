cmake_minimum_required(VERSION 3.10)
project(rhino3dm)

# 24 Jan 2002 S. Baer
# When I added draco to this project we ended up in a situation where both
# EMSCRIPTEN and PYTHON_EXECUTABLE were defined which is what we were keying
# off of to figure out what type of project we should be building. Adding
# some local cmake varaibles to work around this issue. It seems like we can
# figure out something cleaner to use in the future as a command line arg
set(RHINO3DM_JS "NO")
set(RHINO3DM_PY "NO")
if (EMSCRIPTEN)
  set(RHINO3DM_JS "YES")
else()
  set(RHINO3DM_PY "YES")
endif()

if (${RHINO3DM_JS})
  message("Web Assembly Compile")
endif()

if (${RHINO3DM_PY})
  message("Python Compile")
 add_subdirectory(lib/pybind11)
endif()

# Add draco add_library
add_subdirectory(lib/draco)
include_directories(${PROJECT_NAME} PUBLIC lib/draco/src ${CMAKE_BINARY_DIR})
# link_directories(../draco/build)

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

if(${RHINO3DM_JS})
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
if(${RHINO3DM_JS})
  # these shouldn't be necessary in the future as we adjust opennurbs to be
  # emscripten aware
  add_definitions(-DON_32BIT_RUNTIME)
  add_definitions(-DON_LITTLE_ENDIAN)
  add_definitions(-DON_RUNTIME_ANDROID)
endif()


if(${RHINO3DM_JS})
  set(CMAKE_EXECUTABLE_SUFFIX ".html")
endif()

if(${RHINO3DM_JS})
  add_executable(rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC})
endif()

if (${RHINO3DM_PY})
  if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    set (CMAKE_CXX_STANDARD 17)
    add_definitions(-DON_RUNTIME_LINUX)
    add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
    pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
    #target_link_libraries(_rhino3dm -luuid)
  else()
    add_library(zlib_static STATIC ${zlib_SRC})
    add_library(opennurbs_static STATIC ${opennurbs_SRC})
    pybind11_add_module(_rhino3dm ${bindings_SRC})
    target_link_libraries(_rhino3dm PRIVATE zlib_static)
    target_link_libraries(_rhino3dm PRIVATE opennurbs_static)
  endif()
endif()

target_link_libraries(_rhino3dm PRIVATE draco dracoenc dracodec)

# I plan to mode the following install phase out of this file and into the top
# level python script for generating wasm 
# dan@mcneel.com: this is already in the build.py -p js (this can be deleted as soon
# as the scripts are used in the build process)
if(${RHINO3DM_JS})
  install(DIRECTORY DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/../docs/javascript/samples/resources")
  install(FILES "build/javascript/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/../docs/javascript/samples/resources")
endif()
