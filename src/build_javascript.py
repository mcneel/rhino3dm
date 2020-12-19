import os, platform, sys, glob, struct

dirname = os.path.dirname(os.path.abspath(__file__))
build_dir = os.path.join(dirname, "build/javascript")

# all compilation and staging occurs in the build directory
if not os.path.exists(build_dir):
    base_build_dir = os.path.join(dirname, "build")
    if not os.path.exists(base_build_dir):
        os.mkdir(base_build_dir)
    os.mkdir(build_dir)

def system(cmd):
    rv = os.system(cmd)
    rc = os.WEXITSTATUS(rv) # get return code of cmd
    if (rc != 0): sys.exit(rc)

def compilebinaries():
    os.chdir(build_dir)
    system("emcmake cmake ../..")
    system("make")
    system("make install")
    os.chdir("../..")

compilebinaries()
