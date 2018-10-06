import os, platform, sys, glob, struct

dirname = os.path.dirname(os.path.abspath(__file__))
build_dir = os.path.join(dirname, "build")

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
