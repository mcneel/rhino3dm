cmake_minimum_required(VERSION 3.10)

add_subdirectory(pybind11)

file(GLOB bindings_SRC "../src/bindings/*.h" "../src/bindings/*.cpp")
file(GLOB zlib_SRC "../src/opennurbs/zlib/*.h" "../src/opennurbs/zlib/*.c")

# temporarily rename the 3 cpp files that we don't want to compile on OSX
file(RENAME "../src/opennurbs/opennurbs_gl.cpp" "../src/opennurbs/opennurbs_gl.skip")
file(RENAME "../src/opennurbs/opennurbs_unicode_cp932.cpp" "../src/opennurbs/opennurbs_unicode_cp932.skip")
file(RENAME "../src/opennurbs/opennurbs_unicode_cp949.cpp" "../src/opennurbs/opennurbs_unicode_cp949.skip")
file(GLOB opennurbs_SRC "../src/opennurbs/*.h" "../src/opennurbs/*.cpp")
file(RENAME "../src/opennurbs/opennurbs_gl.skip" "../src/opennurbs/opennurbs_gl.cpp")
file(RENAME "../src/opennurbs/opennurbs_unicode_cp932.skip" "../src/opennurbs/opennurbs_unicode_cp932.cpp")
file(RENAME "../src/opennurbs/opennurbs_unicode_cp949.skip" "../src/opennurbs/opennurbs_unicode_cp949.cpp")


set (CMAKE_CXX_STANDARD 14)
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")
add_definitions(-D_GNU_SOURCE)
add_definitions(-DON_COMPILING_OPENNURBS)
add_definitions(-DOPENNURBS_FREETYPE_INC_)
add_definitions(-DMY_ZCALLOC -DZ_PREFIX)
add_definitions(-DOPENNURBS_ZLIB_LIB_DIR)
add_definitions(-DUNICODE)

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
    target_link_libraries(_rhino3dm -luuid)
else()
    add_library(zlib_static STATIC ${zlib_SRC})
    add_library(opennurbs_static STATIC ${opennurbs_SRC})
    pybind11_add_module(_rhino3dm ${bindings_SRC})
    target_link_libraries(_rhino3dm PRIVATE zlib_static)
    target_link_libraries(_rhino3dm PRIVATE opennurbs_static)
endif()
