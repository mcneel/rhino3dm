cmake_minimum_required(VERSION 3.12.1)

add_subdirectory(pybind11)

file(GLOB bindings_SRC "bindings/*.h" "bindings/*.cpp")
file(GLOB zlib_SRC "opennurbs/zlib/*.h" "opennurbs/zlib/*.c")

# temporarily rename the 3 cpp files that we don't want to compile on OSX
file(RENAME "opennurbs/opennurbs_gl.cpp" "opennurbs/opennurbs_gl.skip")
file(RENAME "opennurbs/opennurbs_unicode_cp932.cpp" "opennurbs/opennurbs_unicode_cp932.skip")
file(RENAME "opennurbs/opennurbs_unicode_cp949.cpp" "opennurbs/opennurbs_unicode_cp949.skip")
file(GLOB opennurbs_SRC "opennurbs/*.h" "opennurbs/*.cpp")
file(RENAME "opennurbs/opennurbs_gl.skip" "opennurbs/opennurbs_gl.cpp")
file(RENAME "opennurbs/opennurbs_unicode_cp932.skip" "opennurbs/opennurbs_unicode_cp932.cpp")
file(RENAME "opennurbs/opennurbs_unicode_cp949.skip" "opennurbs/opennurbs_unicode_cp949.cpp")


set (CMAKE_CXX_STANDARD 14)
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")
add_compile_definitions(_GNU_SOURCE)
add_compile_definitions(ON_COMPILING_OPENNURBS)
add_compile_definitions(OPENNURBS_FREETYPE_INC_)
add_compile_definitions(MY_ZCALLOC Z_PREFIX)
add_compile_definitions(OPENNURBS_ZLIB_LIB_DIR)
add_compile_definitions(UNICODE)

#add_library(zlib_static STATIC ${zlib_SRC})

pybind11_add_module(_rhino3dm ${bindings_SRC} ${opennurbs_SRC} ${zlib_SRC})
if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
target_link_libraries(_rhino3dm -luuid)
endif()
#target_link_libraries(_rhino3dm PRIVATE zlib_static)

#install(DIRECTORY DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
#install(DIRECTORY "pysrc/rhino3dm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
#install(FILES "build/_rhino3dm.so" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts/rhino3dm")
#install(DIRECTORY "samples/" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts" PATTERN "samples/*")
