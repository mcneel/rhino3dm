# The opennurbs, zlib and android_uuid sources rhino3dm compiles into its own targets
# (src/CMakeLists.txt for Python and JavaScript, src/librhino3dm_native/CMakeLists.txt for .NET).
# One copy for both, until rhino3dm links opennurbs's own OpenNURBS target
# (src/lib/opennurbs/CMakeLists.txt), which needs upstream CMake fixes first.
#
# Sets RHINO3DM_OPENNURBS_SRC, RHINO3DM_ZLIB_SRC, RHINO3DM_UUID_SRC (absolute paths).

get_filename_component(_rhino3dm_on_dir "${CMAKE_CURRENT_LIST_DIR}/../src/lib/opennurbs" ABSOLUTE)

file(GLOB RHINO3DM_ZLIB_SRC "${_rhino3dm_on_dir}/zlib/*.h" "${_rhino3dm_on_dir}/zlib/*.c")

file(GLOB RHINO3DM_OPENNURBS_SRC "${_rhino3dm_on_dir}/*.h" "${_rhino3dm_on_dir}/*.cpp")
# not compiled: OpenGL, and the cp932 / cp949 code-page tables (opennurbs's own source list leaves
# them out as well)
list(FILTER RHINO3DM_OPENNURBS_SRC EXCLUDE REGEX "/opennurbs_(gl|unicode_cp932|unicode_cp949)\\.cpp$")

# android_uuid (Linux, Android, WebAssembly); gen_uuid_nt.c is its Windows variant
file(GLOB RHINO3DM_UUID_SRC "${_rhino3dm_on_dir}/android_uuid/*.h" "${_rhino3dm_on_dir}/android_uuid/*.c")
list(FILTER RHINO3DM_UUID_SRC EXCLUDE REGEX "/gen_uuid_nt\\.c$")

unset(_rhino3dm_on_dir)
