"""
Create / compile projects for .NET version of rhino3dm
"""
import os
import sys
import fileinput
import shutil


def system(cmd):
    # copied from setup.py
    rv = os.system(cmd)
    rc = rv if os.name == 'nt' else os.WEXITSTATUS(rv)
    if (rc != 0):
        raise RuntimeError('The command "{}" exited with {}'.format(cmd, rc))


def methodgen(dotnetcore):
    # set up args to pass to methodgen application
    dir_cpp = os.getcwd() + '/librhino3dm_native'
    dir_cs = os.getcwd() + '/dotnet'
    path_replace = '../lib/opennurbs'
    args = ' "{0}" "{1}" "{2}"'.format(dir_cpp, dir_cs, path_replace)
    system('dotnet build ./methodgen/methodgen.sln')
    app = os.getcwd() + '/methodgen/bin/Debug/methodgen.exe'
    system(app + args)


def create_cpp_project(bitness, compile):
    # staging and compilation occurs in the build directory
    build_dir = "build/librhino3dm_native_{0}".format(bitness)
    if not os.path.exists(build_dir):
        if(not os.path.exists("build")):
            os.mkdir("build")
        os.mkdir(build_dir)

    os.chdir(build_dir)
    if os.name == 'nt':  # windows build
        arch = "Win32"
        if bitness == 64:
            arch = "x64"
        args = '-G "Visual Studio 17 2022" -A {0}'.format(arch)
        system('cmake ' + args + ' ../../librhino3dm_native')
        if bitness == 64:
            for line in fileinput.input("librhino3dm_native.vcxproj", inplace=1):
                print(line.replace("WIN32;", "WIN64;"))
        if compile:
            system("cmake --build . --config Release --target librhino3dm_native")
    else:
        system("cmake ../../librhino3dm_native")
        if compile:
            system("make")

    os.chdir("../..")


def compilerhino3dm(dotnetcore):
    conf = '/p:Configuration=Release;OutDir="../build/dotnet"'
    system("dotnet restore ./dotnet/Rhino3dm.sln")
    system('dotnet build ./dotnet/Rhino3dm.csproj {}'.format(conf))


if __name__ == '__main__':
    dotnetcore = False
    if len(sys.argv) > 1 and sys.argv[1] == '--core':
        dotnetcore = True
    if sys.platform.startswith('linux'):
        dotnetcore = True

    # make the script always execute from it's directory
    scriptpath = os.path.realpath(__file__)
    os.chdir(os.path.dirname(scriptpath))

    # always compile and run methodgen first to make sure the pinvoke
    # definitions are in place
    methodgen(dotnetcore)

    # only create 32 bit compile on windows
    if os.name == 'nt':
        create_cpp_project(32, True)
    create_cpp_project(64, True)

    # compile Rhino3dm .NET project
    compilerhino3dm(dotnetcore)

