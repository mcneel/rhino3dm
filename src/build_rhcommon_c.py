"""
Create / compile projects for .NET version of rhino3dm
"""
import os
import sys
import fileinput


def methodgen():
    # compile methodgen
    os.system('msbuild ./methodgen')
    # execute methodgen for Rhino3dm
    dir_cpp = os.getcwd() + '/rhcommon_c'
    dir_cs = os.getcwd() + '/dotnet'
    path_replace = '../lib/opennurbs'
    app = os.getcwd() + '/methodgen/bin/Debug/methodgen.exe'
    args = ' "{0}" "{1}" "{2}"'.format(dir_cpp, dir_cs, path_replace)
    os.system(app + args)


def createproject(bitness, compile):
    # staging and compilation occurs in the build directory
    build_dir = "build/rhcommon_c_{0}".format(bitness)
    if not os.path.exists(build_dir):
        if(not os.path.exists("build")):
            os.mkdir("build")
        os.mkdir(build_dir)

    os.chdir(build_dir)
    if os.name == 'nt':  # windows build
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
        if compile:
            os.system("cmake --build . --config Release --target rhcommon_c")
    else:
        rv = os.system("cmake ../..".format(sys.executable))
        if compile and int(rv) == 0:
            os.system("make")

    os.chdir("../..")


if __name__ == '__main__':
    # always compile and run methodgen first to make sure the pinvoke
    # definitions are in place
    methodgen()
"""
    # only create 32 bit compile on windows
    if os.name == 'nt':
        createproject(32, True)
    createproject(64, True)
"""
