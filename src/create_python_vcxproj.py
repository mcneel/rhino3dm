"""
Used primarily to create a vcxproj in the build directory that can be used
to edit and test the source code. If you want to build the actual python
libraries, use the buildall_py_win batch file one directory up or run
python setup.py bdist_wheel one directory up
"""
import os, platform, sys, glob, struct
import distutils.util
from shutil import copyfile, copytree, rmtree, copy
import fileinput

windows_build = os.name == 'nt'

bitness = 8 * struct.calcsize("P")

build_dir = "build/py{}{}_{}bit".format(sys.version_info.major, sys.version_info.minor, bitness)

# all compilation and staging occurs in the build directory
if not os.path.exists(build_dir):
    if(not os.path.exists("build")):
        os.mkdir("build")
    os.mkdir(build_dir)

def createproject():
    """ compile for the platform we are running on """
    os.chdir(build_dir)
    if windows_build:
        command = 'cmake -A {} -DPYTHON_EXECUTABLE:FILEPATH="{}" -DPYTHON_BUILD=1 ../..'.format("win32" if bitness==32 else "x64", sys.executable)
        os.system(command)
        if bitness==64:
            for line in fileinput.input("_rhino3dm.vcxproj", inplace=1):
                print(line.replace("WIN32;", "WIN64;"))
            for line in fileinput.input("opennurbs_static.vcxproj", inplace=1):
                print(line.replace("WIN32;", "WIN64;"))
        #os.system("cmake --build . --config Release --target _rhino3dm")
    else:
        rv = os.system("cmake -DPYTHON_BUILD=1 -DPYTHON_EXECUTABLE:FILEPATH={} ../..".format(sys.executable))
        if int(rv) > 0: sys.exit(1)
        #rv = os.system("make")
        #if int(rv) > 0: sys.exit(1)
    #os.chdir("../..")


createproject()
