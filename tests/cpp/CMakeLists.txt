cmake_minimum_required(VERSION 3.16)
project(ON_Test)

# GoogleTest requires at least C++14
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set (CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR})
set (CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR})

include(FetchContent)
FetchContent_Declare(
  googletest
  URL https://github.com/google/googletest/archive/b10fad38c4026a29ea6561ab15fc4818170d1c10.zip
)
# For Windows: Prevent overriding the parent project's compiler/linker settings
set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(googletest)

enable_testing()

add_subdirectory(../../src/lib/opennurbs build_opennurbs)
if (MSVC)
  add_definitions(-DOPENNURBS_IMPORTS)
endif()

#[[
# run UpdateVersionNumbers before compiling C++ projects
add_custom_target(updateversionnumbers ALL
    COMMAND sh -c "dotnet build ../../../UpdateVersionNumbers.csproj"
    BYPRODUCTS version.h
    COMMENT "Create version source files"
    )
]]

# Add tests
function(set_test TESTNAME)
    add_executable(${TESTNAME} ${ARGN})
    #add_dependencies(${TESTNAME} updateversionnumbers)
    target_link_libraries(${TESTNAME} GTest::gtest_main OpenNURBS)
    # we occasionally get an error building on Windows. Adding discovery mode
    # pre-test fixes this, though I'm not sure what the drawbacks are
    gtest_discover_tests(${TESTNAME} DISCOVERY_MODE PRE_TEST)
endfunction()

include(GoogleTest)

# devs: to add a test use set_test(testName testName.cpp)
set_test(test_ON_View      ontest_view.cpp)