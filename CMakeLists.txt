cmake_minimum_required(VERSION 2.8.3)

FIND_PACKAGE(PythonInterp)

if (PYTHONINTERP_FOUND)
    if (PYTHON_VERSION_MAJOR EQUAL 3)
        FIND_PACKAGE(Boost COMPONENTS python${PYTHON_VERSION_MAJOR}${PYTHON_VERSION_MINOR})
        FIND_PACKAGE(PythonInterp 3)
        FIND_PACKAGE(PythonLibs 3 REQUIRED)
    else()
        FIND_PACKAGE(Boost COMPONENTS python${PYTHON_VERSION_MAJOR}${PYTHON_VERSION_MINOR})
        FIND_PACKAGE(PythonInterp)
        FIND_PACKAGE(PythonLibs REQUIRED)
    endif()
else()
    message("Python not found")
endif()

message(STATUS "PYTHON_LIBRARIES = ${PYTHON_LIBRARIES}")
message(STATUS "PYTHON_EXECUTABLE = ${PYTHON_EXECUTABLE}")
message(STATUS "PYTHON_INCLUDE_DIRS = ${PYTHON_INCLUDE_DIRS}")
message(STATUS "Boost_LIBRARIES = ${Boost_LIBRARIES}")

# ENABLE_TESTING()
INCLUDE_DIRECTORIES(${Boost_INCLUDE_DIRS} ${PYTHON_INCLUDE_DIRS})
LINK_LIBRARIES(${Boost_LIBRARIES} ${PYTHON_LIBRARIES}) # Deprecated but so convenient!


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
set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-overloaded-virtual -Wno-switch -Wno-unknown-pragmas -Wno-unused-private-field")
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")
add_compile_definitions(ON_COMPILING_OPENNURBS)
add_compile_definitions(OPENNURBS_FREETYPE_INC_)
add_compile_definitions(MY_ZCALLOC Z_PREFIX)

PYTHON_ADD_MODULE(_rhino3dm ${bindings_SRC} ${zlib_SRC} ${opennurbs_SRC})

install(DIRECTORY DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
install(DIRECTORY "pysrc/rhino3dm" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts")
install(FILES "build/_rhino3dm.so" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts/rhino3dm")
install(DIRECTORY "samples/" DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/artifacts" PATTERN "samples/*")
