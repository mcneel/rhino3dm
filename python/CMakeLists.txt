cmake_minimum_required(VERSION 3.12.1)

add_subdirectory(pybind11)

file(GLOB bindings_SRC "../shared_src/bindings/*.h" "../shared_src/bindings/*.cpp")
file(GLOB zlib_SRC "../shared_src/opennurbs/zlib/*.h" "../shared_src/opennurbs/zlib/*.c")

# temporarily rename the 3 cpp files that we don't want to compile on OSX
file(RENAME "../shared_src/opennurbs/opennurbs_gl.cpp" "../shared_src/opennurbs/opennurbs_gl.skip")
file(RENAME "../shared_src/opennurbs/opennurbs_unicode_cp932.cpp" "../shared_src/opennurbs/opennurbs_unicode_cp932.skip")
file(RENAME "../shared_src/opennurbs/opennurbs_unicode_cp949.cpp" "../shared_src/opennurbs/opennurbs_unicode_cp949.skip")
file(GLOB opennurbs_SRC "../shared_src/opennurbs/*.h" "../shared_src/opennurbs/*.cpp")
file(RENAME "../shared_src/opennurbs/opennurbs_gl.skip" "../shared_src/opennurbs/opennurbs_gl.cpp")
file(RENAME "../shared_src/opennurbs/opennurbs_unicode_cp932.skip" "../shared_src/opennurbs/opennurbs_unicode_cp932.cpp")
file(RENAME "../shared_src/opennurbs/opennurbs_unicode_cp949.skip" "../shared_src/opennurbs/opennurbs_unicode_cp949.cpp")


set (CMAKE_CXX_STANDARD 14)
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")
add_compile_definitions(_GNU_SOURCE)
add_compile_definitions(ON_COMPILING_OPENNURBS)
add_compile_definitions(OPENNURBS_FREETYPE_INC_)
add_compile_definitions(MY_ZCALLOC Z_PREFIX)
add_compile_definitions(OPENNURBS_ZLIB_LIB_DIR)
add_compile_definitions(UNICODE)

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




