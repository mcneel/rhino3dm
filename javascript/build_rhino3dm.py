import os, platform, sys, glob, struct

build_dir = "build"

# all compilation and staging occurs in the build directory
if not os.path.exists(build_dir):
    os.mkdir(build_dir)

def compilebinaries():
    os.chdir(build_dir)
    os.system("emcmake cmake ..")
    os.system("make")
    os.system("make install")
    os.chdir("..")

compilebinaries()
