cmake_minimum_required(VERSION 3.16)
project(rhino3dm)

set (CMAKE_CXX_STANDARD 14)

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
include_directories(${PROJECT_NAME} PUBLIC lib/draco/src ${CMAKE_BINARY_DIR})

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

if(${RHINO3DM_JS})
  # we will eventually want the following warning flags on both compiles
  # for now, just WASM
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s MODULARIZE=1 -s EXPORT_NAME=rhino3dm -g -gsource-map ")
  set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s ALLOW_MEMORY_GROWTH=1 -s MAXIMUM_MEMORY=4294967296 --bind")
endif()

add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
add_definitions(-DOPENNURBS_FREETYPE_INC_)
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
add_definitions(-DUNICODE)
add_definitions(-DON_INCLUDE_DRACO)
if(${RHINO3DM_JS})
  # temporary until we adjust opennurbs to be emscripten aware
  add_definitions(-DON_32BIT_RUNTIME)
  add_definitions(-DON_LITTLE_ENDIAN)
  add_definitions(-DON_RUNTIME_ANDROID)

  set(CMAKE_EXECUTABLE_SUFFIX ".js")

  add_subdirectory(lib/draco)
  add_executable(rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC} ${uuid_SRC})
  target_link_libraries( rhino3dm ${PROJECT_SOURCE_DIR}/build/javascript/draco_wasm/libdraco.a)
endif()

if (${RHINO3DM_PY})
  if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    add_definitions(-DON_RUNTIME_LINUX)
    add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
    pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
    #target_link_libraries(_rhino3dm -luuid)
  elseif(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
    if(CMAKE_SYSTEM_PROCESSORa STREQUAL "arm64")
      set(CMAKE_OSX_ARCHITECTURE arm64)
    else()
      set(CMAKE_OSX_ARCHITECTURE x86_64)
    endif()
    add_library(zlib_static STATIC ${zlib_SRC})
    add_library(opennurbs_static STATIC ${opennurbs_SRC})
    pybind11_add_module(_rhino3dm ${bindings_SRC})
    target_link_libraries(_rhino3dm PRIVATE zlib_static)
    target_link_libraries(_rhino3dm PRIVATE opennurbs_static)
  else()
    add_library(zlib_static STATIC ${zlib_SRC})
    add_library(opennurbs_static STATIC ${opennurbs_SRC})
    pybind11_add_module(_rhino3dm ${bindings_SRC})
    target_link_libraries(_rhino3dm PRIVATE zlib_static)
    target_link_libraries(_rhino3dm PRIVATE opennurbs_static)
  endif()

  add_subdirectory(lib/draco)
  if (MSVC)
  target_link_libraries(_rhino3dm PRIVATE debug lib/draco/Debug/draco)
  target_link_libraries(_rhino3dm PRIVATE optimized ${CMAKE_BINARY_DIR}/draco_static/Release/draco.lib)
  else()
    target_link_libraries(_rhino3dm PRIVATE ${CMAKE_BINARY_DIR}/draco_static/libdraco.a)
  endif()
endif()


# I plan to mode the following install phase out of this file and into the top
# level python script for generating wasm 
# dan@mcneel.com: this is already in the build.py -p js (this can be deleted as soon
# as the scripts are used in the build process)
if(${RHINO3DM_JS})
  install(DIRECTORY DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
  install(FILES "build/javascript/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/build/artifacts_js")
endif()

# patch rhino3dm.js using the supplied diff to produce rhino3dm.module.js
if(${RHINO3DM_JS})
  find_program(PATCH patch)
  if(NOT PATCH)
      message(WARNING "rhino3dm.module.js generation will fail: patch not found!")
  endif()
  add_custom_command(TARGET rhino3dm POST_BUILD
    COMMENT "Generating ${CMAKE_BINARY_DIR}/rhino3dm.module.js"
    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:rhino3dm> ${CMAKE_BINARY_DIR}/rhino3dm.module.js
    COMMAND ${PATCH} ${CMAKE_BINARY_DIR}/rhino3dm.module.js ${CMAKE_CURRENT_SOURCE_DIR}/rhino3dm.module.js.diff
  )
endif()
