import os, platform, sys, glob, struct

def system(cmd):
    rv = os.system(cmd)
    rc = os.WEXITSTATUS(rv) # get return code of cmd
    if (rc != 0): sys.exit(rc)

# make sure the build directory exists
build_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "build")
if not os.path.exists(build_dir): os.mkdir(build_dir)

# compile draco wasm as a static library
draco_wasm_build_dir = os.path.join(build_dir, "draco_wasm")
if not os.path.exists(draco_wasm_build_dir): os.mkdir(draco_wasm_build_dir)
os.chdir(draco_wasm_build_dir)
system("emcmake cmake ../../lib/draco")
system("emmake make")

# compile rhino3dm js/wasm
rhino3dm_wasm_build_dir = os.path.join(build_dir, "javascript")
if not os.path.exists(rhino3dm_wasm_build_dir): os.mkdir(rhino3dm_wasm_build_dir)
os.chdir(rhino3dm_wasm_build_dir)
system("emcmake cmake ../../")
system("emmake make")
