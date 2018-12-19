import os, platform, sys, glob, struct

dirname = os.path.dirname(os.path.abspath(__file__))
build_dir = os.path.join(dirname, "build/javascript")

# all compilation and staging occurs in the build directory
if not os.path.exists(build_dir):
    base_build_dir = os.path.join(dirname, "build")
    if not os.path.exists(base_build_dir):
        os.mkdir(base_build_dir)
    os.mkdir(build_dir)

def compilebinaries():
    os.chdir(build_dir)
    os.system("emcmake cmake -DCMAKE_CXX_FLAGS=\"-s MODULARIZE=1 -s 'EXPORT_NAME=\\\"Rhino3dm\\\"'\" ../..")
    os.system("make")
    os.system("make install")
    os.chdir("../..")

compilebinaries()
