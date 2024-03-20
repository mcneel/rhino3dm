cmake_minimum_required(VERSION 3.16)
project(samplecppreadtextentities)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_subdirectory(../../src/lib/opennurbs build_opennurbs)

if (MSVC)
  add_definitions(-DOPENNURBS_IMPORTS)
endif()


add_executable(
  samplecppreadtextentities
  SampleCppReadTextEntity.cpp
)

target_link_libraries(samplecppreadtextentities OpenNURBS)