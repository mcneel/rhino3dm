"""
Create C++ projects for rhcommon_c native compiles used by .NET
"""
import os
import sys
import fileinput


def createproject(bitness):
    # staging and compilation occurs in the build directory
    build_dir = "build/rhcommon_c_{0}".format(bitness)
    if not os.path.exists(build_dir):
        if(not os.path.exists("build")):
            os.mkdir("build")
        os.mkdir(build_dir)

    os.chdir(build_dir)
    windows_build = os.name == 'nt'
    if windows_build:
        arch = ""
        if bitness == 64:
            arch = " Win64"
        args = '-G "Visual Studio 15 2017{0}" ../../rhcommon_c'.format(arch)
        os.system('cmake ' + args)
        if bitness == 64:
            for line in fileinput.input("rhcommon_c.vcxproj", inplace=1):
                print(line.replace("WIN32;", "WIN64;"))
            for line in fileinput.input("opennurbs_static.vcxproj", inplace=1):
                print(line.replace("WIN32;", "WIN64;"))
        os.system("cmake --build . --config Release --target rhcommon_c")
    else:
        rv = os.system("cmake -DPYTHON_EXECUTABLE:FILEPATH={} ../..".format(sys.executable))
        if int(rv) > 0: sys.exit(1)
        #rv = os.system("make")
        #if int(rv) > 0: sys.exit(1)

    os.chdir("../..")


# only create 32 bit compile on windows
if os.name == 'nt':
    createproject(32)
createproject(64)
