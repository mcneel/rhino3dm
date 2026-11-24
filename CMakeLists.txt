# rhino3dm: one CMake entry point for every library the repository builds.
#
#   RHINO3DM_TARGET=python      the _rhino3dm extension module (nanobind)      -> src/CMakeLists.txt
#   RHINO3DM_TARGET=javascript  rhino3dm.js + rhino3dm.wasm (Emscripten)       -> src/CMakeLists.txt
#   RHINO3DM_TARGET=dotnet      librhino3dm_native, the library Rhino3dm.dll   -> src/librhino3dm_native
#                               P/Invokes into (Windows, macOS, Linux, iOS, Android)
#
# CMakePresets.json names a configuration for each; build.py drives them. Under the Emscripten
# toolchain the target defaults to javascript.

cmake_minimum_required(VERSION 3.21)

project(rhino3dm_build LANGUAGES C CXX)

if (EMSCRIPTEN)
  set(_rhino3dm_default_target javascript)
else()
  set(_rhino3dm_default_target python)
endif()
set(RHINO3DM_TARGET "${_rhino3dm_default_target}" CACHE STRING "library to build: python, javascript or dotnet")
set_property(CACHE RHINO3DM_TARGET PROPERTY STRINGS python javascript dotnet)

if (RHINO3DM_TARGET STREQUAL "javascript")
  if (NOT EMSCRIPTEN)
    message(FATAL_ERROR "RHINO3DM_TARGET=javascript needs the Emscripten toolchain (emcmake, or a js-* preset)")
  endif()
  add_subdirectory(src)
elseif (RHINO3DM_TARGET STREQUAL "python")
  if (EMSCRIPTEN)
    message(FATAL_ERROR "RHINO3DM_TARGET=python cannot use the Emscripten toolchain")
  endif()
  add_subdirectory(src)
elseif (RHINO3DM_TARGET STREQUAL "dotnet")
  add_subdirectory(src/librhino3dm_native)
else()
  message(FATAL_ERROR "RHINO3DM_TARGET must be python, javascript or dotnet (got '${RHINO3DM_TARGET}')")
endif()
