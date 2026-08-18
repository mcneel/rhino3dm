cmake_minimum_required(VERSION 3.16)
project(rhino3dm)

set (CMAKE_CXX_STANDARD 17)

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
  if(PYTHON_BINDING_LIB STREQUAL "NANOBIND")
    message("Using nanobind")
    add_compile_definitions( NANOBIND )
    if (CMAKE_VERSION VERSION_LESS 3.18)
      set(DEV_MODULE Development)
    else()
      set(DEV_MODULE Development.Module)
    endif()
    find_package(Python 3.8 COMPONENTS Interpreter ${DEV_MODULE} REQUIRED)

    if (NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
      set(CMAKE_BUILD_TYPE Release CACHE STRING "Choose the type of build." FORCE)
      set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "Debug" "Release" "MinSizeRel" "RelWithDebInfo")
    endif()
    
    add_subdirectory(lib/nanobind)
  else()
    message("Using pybind11")
    add_subdirectory(lib/pybind11)
  endif()
endif()

if (NODE)
  message("NODE=${NODE}")
  message("NODE evaluates to True")
endif()

if (MODULE)
  message("MODULE=${MODULE}")
  message("MODULE evaluates to True")
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
  if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    message("debug mode")
    set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -g")
  else()
    set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Oz")
  endif()
  # WASM64 (wasm memory64) is its own build dimension, independent of NODE.
  # It produces a 64-bit-memory binary that breaks the 4GB limit. Chrome ships
  # memory64 unflagged and Node supports it; Firefox/Safari are still gated, so
  # the default build stays wasm32 and WASM64 is opt-in.
  if(WASM64)
    message("WASM64 (memory64) build")
    # -sMEMORY64 is a codegen/target flag (wasm32 -> wasm64): it must be applied
    # at compile time to C sources (zlib, uuid) as well as C++, and to the draco
    # sub-build (see script/setup.py), or the objects link as mixed
    # wasm32/wasm64 and fail. (It also reaches the link line via CXX flags.)
    set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -sMEMORY64=1")
    set (CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -sMEMORY64=1")
    # -sMEMORY64 only lifts the address width; the heap cap still defaults to
    # 2GB when ALLOW_MEMORY_GROWTH is on and MAXIMUM_MEMORY is unset. Without
    # this the wasm64 binary is still capped at 2GB (memory.grow fails past it,
    # malloc returns null), defeating the point of the build. Raise it past 4GB
    # (link-only flag; only valid with MEMORY64, hence gated here).
    set (CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -sMAXIMUM_MEMORY=16GB")
  endif()
  # The remaining -s settings and -lembind are link-only; keep them out of the
  # compile flags so every object doesn't emit a "linker setting ignored during
  # compilation" / "linker input unused" warning.
  set (CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -sMODULARIZE=1 -sEXPORT_NAME=rhino3dm -sALLOW_MEMORY_GROWTH=1 -lembind")
  if(MODULE)
    set (CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -sEXPORT_ES6=1")
  endif()
endif()

if (${RHINO3DM_PY})
  # Set default symbol visibility to hidden to prevent conflicts with other libraries
  if(NOT MSVC)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fvisibility=hidden")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fvisibility=hidden")
  endif()
endif()

if(MSVC)
  # Faster MSVC builds: parallelize the solution over projects (/MP), and each
  # project over files via the multi-tool task scheduler; use incremental linking.
  add_compile_options("/MP")
  set (CMAKE_VS_GLOBALS "${CMAKE_VS_GLOBALS};UseMultiToolTask=true;EnforceProcessCountAcrossBuilds=true")
  add_compile_options("/openmp")     # OpenNURBS can use OpenMP; no need to force the single-threaded variant
  add_link_options("/INCREMENTAL")
endif()

add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
add_definitions(-DOPENNURBS_FREETYPE_INC_)
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
# RH3DM-179: we link zlib via CMake, not the MSVC #pragma comment(lib,...) path, so tell
# opennurbs to skip that pragma block (otherwise OPENNURBS_ZLIB_LIB_DIR being valueless
# yields a malformed string -> MSVC C4081). The native librhino3dm_native build already
# defines this; mirror it here for the JS/Python build.
add_definitions(-DON_CMAKE_BUILD)
add_definitions(-DUNICODE)
add_definitions(-DON_INCLUDE_DRACO)
if(${RHINO3DM_JS})
  # opennurbs (>= 8.32) is emscripten-aware: it auto-defines ON_RUNTIME_WASM,
  # ON_LITTLE_ENDIAN, and selects ON_64BIT_RUNTIME / ON_32BIT_RUNTIME from the
  # actual wasm pointer size. So a WASM64 build only needs -sMEMORY64=1 (set
  # above) and opennurbs derives the 64-bit runtime itself. No manual defines.

  set(CMAKE_EXECUTABLE_SUFFIX ".js")

  add_subdirectory(lib/draco)
  add_executable(rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC} ${uuid_SRC})
  target_link_libraries( rhino3dm ${PROJECT_SOURCE_DIR}/build/javascript/draco_wasm/libdraco.a)
endif()

if (${RHINO3DM_PY})
  if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    add_definitions(-DON_RUNTIME_LINUX)
    add_definitions(-DON_CLANG_CONSTRUCTOR_BUG)
    if(PYTHON_BINDING_LIB STREQUAL "NANOBIND")
      #need to build nanobind to link it?
      nanobind_build_library(nanobind)
      nanobind_add_module(_rhino3dm NB_STATIC ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
      target_link_libraries(_rhino3dm PRIVATE nanobind-static)
    else()
      pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
      target_link_libraries(_rhino3dm PRIVATE pybind11::module)
    endif()
    # Hide all symbols by default to prevent conflicts with other libraries (e.g., OpenVDB's zlib)
    set_target_properties(_rhino3dm PROPERTIES
      CXX_VISIBILITY_PRESET hidden
      C_VISIBILITY_PRESET hidden
      VISIBILITY_INLINES_HIDDEN ON
    )
    #pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC} ${uuid_SRC})
    #target_link_libraries(_rhino3dm -luuid)
  else()
    add_library(zlib_static STATIC ${zlib_SRC})
    set_target_properties(zlib_static PROPERTIES 
      CXX_VISIBILITY_PRESET hidden
      C_VISIBILITY_PRESET hidden
      VISIBILITY_INLINES_HIDDEN ON
    )
    add_library(opennurbs_static STATIC ${opennurbs_SRC})
    set_target_properties(opennurbs_static PROPERTIES
      CXX_VISIBILITY_PRESET hidden
      C_VISIBILITY_PRESET hidden
      VISIBILITY_INLINES_HIDDEN ON
    )
    if(PYTHON_BINDING_LIB STREQUAL "NANOBIND")
      #need to build nanobind to link it?
      nanobind_build_library(nanobind)
      nanobind_add_module(_rhino3dm NB_STATIC ${bindings_SRC})
      target_link_libraries(_rhino3dm PRIVATE nanobind-static)
    else()
      pybind11_add_module(_rhino3dm ${bindings_SRC})
      target_link_libraries(_rhino3dm PRIVATE pybind11::module)
    endif()
    
    target_link_libraries(_rhino3dm PRIVATE zlib_static)
    target_link_libraries(_rhino3dm PRIVATE opennurbs_static)
    # Hide all symbols by default to prevent conflicts with other libraries
    set_target_properties(_rhino3dm PROPERTIES
      CXX_VISIBILITY_PRESET hidden
      C_VISIBILITY_PRESET hidden
      VISIBILITY_INLINES_HIDDEN ON
    )
  endif()

  add_subdirectory(lib/draco)
  if (MSVC)
    target_link_libraries(_rhino3dm PRIVATE debug lib/draco/Debug/draco)
    target_link_libraries(_rhino3dm PRIVATE optimized ${CMAKE_BINARY_DIR}/draco_static/Release/draco.lib)
  else()
    target_link_libraries(_rhino3dm PRIVATE draco_static)
  endif()
endif()

if (MSVC)
  # Build the rhino3dm code with warnings on. TODO: raise to /W4 (and eventually
  # /WX) once OpenNURBS builds clean at that level.
  if (TARGET _rhino3dm)
    target_compile_options(_rhino3dm PRIVATE "/W3")
  endif()
  if (TARGET opennurbs_static)
    target_compile_options(opennurbs_static PRIVATE "/W3")
  endif()
endif()
