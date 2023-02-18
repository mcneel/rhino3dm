import os, platform, sys, glob, struct

def system(cmd):
    rv = os.system(cmd)
    rc = os.WEXITSTATUS(rv) # get return code of cmd
    if (rc != 0): sys.exit(rc)

# make sure the build directory exists
build_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "build")
build_dir_rhino3dm_wasm = os.path.join(build_dir, "javascript")
build_dir_draco_wasm = os.path.join(build_dir_rhino3dm_wasm, "draco_wasm")
if not os.path.exists(build_dir): os.mkdir(build_dir)
if not os.path.exists(build_dir_rhino3dm_wasm): os.mkdir(build_dir_rhino3dm_wasm)
if not os.path.exists(build_dir_draco_wasm): os.mkdir(build_dir_draco_wasm)

# compile draco wasm as a static library
os.chdir(build_dir_draco_wasm)
system("emcmake cmake ../../../lib/draco")
system("emmake make")

# compile rhino3dm js/wasm
os.chdir(build_dir_rhino3dm_wasm)
system("emcmake cmake ../../")
system("emmake make")
