cmake_minimum_required(VERSION 3.12.2)

project(rhino-geometry)

enable_testing()

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
set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} --bind")

add_compile_definitions(_GNU_SOURCE)
add_compile_definitions(ON_COMPILING_OPENNURBS)
#add_compile_definitions(OPENNURBS_FREETYPE_INC_)
add_compile_definitions(MY_ZCALLOC)
add_compile_definitions(Z_PREFIX)
add_compile_definitions(ON_32BIT_RUNTIME)
add_compile_definitions(ON_LITTLE_ENDIAN)
add_compile_definitions(ON_RUNTIME_ANDROID)

set(CMAKE_EXECUTABLE_SUFFIX ".html")
add_executable(rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC})

install(DIRECTORY DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
#install(FILES "build/rhino3dm.html" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
install(FILES "build/rhino3dm.js" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
install(FILES "build/rhino3dm.wasm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
install(DIRECTORY "samples/" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts" PATTERN "samples/*")
