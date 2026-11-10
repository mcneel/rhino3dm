cmake_minimum_required (VERSION 3.24) #version where registry reading was added
#set(CMAKE_OSX_ARCHITECTURES "$(ARCHS_STANDARD)")
project(rhinocore_py_bindings CXX)

# specify the C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

file(GLOB bindings_SRC "../bindings/*.h" "../bindings/*.cpp")

set(RHINO3DM_PY "YES")

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
add_subdirectory(${CMAKE_CURRENT_SOURCE_DIR}/../lib/nanobind nanobind_dir)
nanobind_add_module(rhinocore_py_bindings ${bindings_SRC} stdafx.cpp)


add_definitions(-DRHINOCORE_BINDINGS)
add_definitions(-DON_CMAKE_BUILD)
if (MSVC)
  add_definitions(-DUNICODE)
  add_definitions(-D_UNICODE)
  add_definitions(-D_AFXDLL)
endif()
if (WIN32)
  cmake_host_system_information(
    RESULT RhinoSdkPath
    QUERY WINDOWS_REGISTRY "HKLM/SOFTWARE/McNeel/Rhinoceros/SDK/8.0"
    VALUE "InstallPath"
  )
  message("Install Path: ${RhinoSdkPath}")
  set_property(TARGET rhinocore_py_bindings PROPERTY VS_PROJECT_IMPORT
    "PropertySheets/Rhino.Cpp.PlugInComponent.props"
  )
endif()
