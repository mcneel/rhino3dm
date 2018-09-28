import os, platform
from shutil import copyfile, copytree, rmtree

windows_build = os.name == 'nt'
mac_build = platform.system() == 'Darwin'

if windows_build:
    print "Compiling for Windows"
    os.system(r'"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" rhino3dm_py.sln /p:Configuration=Release')
if mac_build:
    if not os.path.exists("build"):
        os.mkdir("build")

    os.chdir("build")
    PYTHON_LIBRARY="/System/Library/Frameworks/Python.framework/Versions/2.7/lib/libpython2.7.dylib"
    PYTHON_INCLUDE_DIR="/System/Library/Frameworks/Python.framework/Versions/2.7/include/python2.7"
    BUILD_TYPE="DEBUG"
    args = "-DPYTHON_LIBRARY={} -DPYTHON_INCLUDE_DIR={} -DCMAKE_BUILD_TYPE={} .. && make".format(PYTHON_LIBRARY, PYTHON_INCLUDE_DIR, BUILD_TYPE)
    os.system("cmake "+ args)
    os.chdir("..")


artifacts = "artifacts"
#create artifacts directory and copy
if os.path.exists(artifacts):
    rmtree(artifacts)

os.mkdir(artifacts)
copytree("pysrc/rhino3dm", artifacts +"/rhino3dm")
if windows_build:
    copyfile("build/_rhino3dm.pyd", artifacts + "/rhino3dm/_rhino3dm.pyd")
if mac_build:
    copyfile("build/_rhino3dm.so", artifacts + "/rhino3dm/_rhino3dm.so")

samples = os.listdir("samples")
for sample in samples:
    copyfile("samples/"+sample, artifacts + "/"+sample)


