cmake_minimum_required(VERSION 3.16)
project(rhino3dm)

set (CMAKE_CXX_STANDARD 14)

if (EMSCRIPTEN)
  set(RHINO3DM_JS "YES")
else()
  set(RHINO3DM_PY "YES")
endif()

# Add draco headers to include path
include_directories(${PROJECT_NAME} PUBLIC lib/draco/src ${CMAKE_BINARY_DIR})

file(GLOB bindings_SRC "bindings/*.h" "bindings/*.cpp")

add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
add_definitions(-DOPENNURBS_FREETYPE_INC_)
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
add_definitions(-DUNICODE)
add_definitions(-DON_INCLUDE_DRACO)

if(${RHINO3DM_JS})
  message("Web Assembly Compile")

  message("NODE=${NODE}")
  if( NODE ) 
    message("NODE evaluates to True")
  endif()
  # we will eventually want the following warning flags on both compiles
  # for now, just WASM
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
  if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    message("debug mode")
    set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -g")
  else()
    set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Oz")
  endif()
  if(NODE)
    set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s MEMORY64=1")
  endif()
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s MODULARIZE=1 -s EXPORT_NAME=rhino3dm -s EXPORT_ES6=1 -s ALLOW_MEMORY_GROWTH=1 --bind")

  # temporary until we adjust opennurbs to be emscripten aware
  if(NODE)
    add_definitions(-DON_64BIT_RUNTIME)
  else()
    add_definitions(-DON_32BIT_RUNTIME)
  endif()
  add_definitions(-DON_LITTLE_ENDIAN)
  add_definitions(-DON_RUNTIME_ANDROID)

  set(CMAKE_EXECUTABLE_SUFFIX ".js")

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

  add_subdirectory(lib/draco)
  add_executable(rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC} ${uuid_SRC})
  target_link_libraries( rhino3dm ${PROJECT_SOURCE_DIR}/build/javascript/draco_wasm/libdraco.a)
endif()

if (${RHINO3DM_PY})
  message("Python Compile")
  add_subdirectory(lib/opennurbs) # add opennurbs
  add_subdirectory(lib/draco)
  add_subdirectory(lib/pybind11)

  if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    add_definitions(-DON_RUNTIME_LINUX)
    add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
  elseif(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
    if(CMAKE_SYSTEM_PROCESSORa STREQUAL "arm64")
      set(CMAKE_OSX_ARCHITECTURE arm64)
    else()
      set(CMAKE_OSX_ARCHITECTURE x86_64)
    endif()
  endif()

  pybind11_add_module(_rhino3dm ${bindings_SRC})
  set(CMAKE_POSITION_INDEPENDENT_CODE ON)
  target_link_libraries(_rhino3dm PRIVATE opennurbsStatic)
  if (MSVC)
    target_link_libraries(_rhino3dm PRIVATE debug lib/draco/Debug/draco)
    target_link_libraries(_rhino3dm PRIVATE optimized ${CMAKE_BINARY_DIR}/draco_static/Release/draco.lib)
  else()
    target_link_libraries(_rhino3dm PRIVATE ${CMAKE_BINARY_DIR}/draco_static/libdraco.a)
  endif()
endif()
