import os, platform, sys, glob, struct
import distutils.util
from shutil import copyfile, copytree, rmtree, copy
import fileinput

windows_build = os.name == 'nt'

bitness = 8 * struct.calcsize("P")

build_dir = "build_py{}{}_{}bit".format(sys.version_info.major, sys.version_info.minor, bitness)

# all compilation and staging occurs in the build directory
if not os.path.exists(build_dir):
    os.mkdir(build_dir)

def compilebinaries():
    """ compile for the platform we are running on """
    os.chdir(build_dir)
    if windows_build:
        command = 'cmake -A {} -DPYTHON_EXECUTABLE:FILEPATH="{}" ..'.format("win32" if bitness==32 else "x64", sys.executable)
        os.system(command)
        if bitness==64:
            for line in fileinput.input("_rhino3dm.vcxproj", inplace=1):
                print(line.replace("WIN32;", "WIN64;"))
        os.system("cmake --build . --config Release --target _rhino3dm")
    else:
        os.system("cmake -DPYTHON_EXECUTABLE:FILEPATH={} ..".format(sys.executable))
        os.system("make")
    os.chdir("..")

def createwheel():
    """stage files and generate wheel for distribution"""
    current_dir = os.path.abspath(".")
    staging_dir = os.path.abspath(build_dir + "/stage")
    if os.path.exists(staging_dir):
        rmtree(staging_dir)

    os.chdir(build_dir)
    os.mkdir(staging_dir)
    os.chdir("..")
    copytree("pysrc/rhino3dm", staging_dir +"/rhino3dm")
    for file in glob.glob(build_dir + "/Release/*.pyd"):
        copy(file, staging_dir + "/rhino3dm")
    for file in glob.glob(build_dir + "/*.so"):
        copy(file, staging_dir + "/rhino3dm")

    copyfile("../LICENSE", staging_dir + "/LICENSE")
    copyfile("pysrc/README.md", staging_dir + "/README.md")
    copyfile("pysrc/MANIFEST.in", staging_dir + "/MANIFEST.in")
    copyfile("pysrc/setup.py", staging_dir + "/setup.py")
    os.chdir(staging_dir)
    options = []
    #platform is found with distutils.util.get_platform()
    python_tag = "cp{}{}".format(sys.version_info.major, sys.version_info.minor)
    options = "--python-tag={} --plat-name={}".format(python_tag, distutils.util.get_platform())
    os.system('"' + sys.executable + '"' + " setup.py bdist_wheel " + options)
    os.chdir(current_dir)
    if not os.path.exists("wheels_for_pypi"):
        os.mkdir("wheels_for_pypi")
    for file in glob.glob(staging_dir + "/dist/*.whl"):
        print (file)
        copy(file, "wheels_for_pypi" )


compilebinaries()
createwheel()
