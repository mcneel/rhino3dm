import os
from shutil import copyfile, copytree, rmtree

if os.name == 'nt':
    print "Compiling for Windows"
    os.system(r'"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" rhino3dm_py.sln /p:Configuration=Release')

artifacts = "artifacts"
#create artifacts directory and copy
if os.path.exists(artifacts):
    rmtree(artifacts)

os.mkdir(artifacts)
copytree("pysrc/rhino3dm", artifacts +"/rhino3dm")
copyfile("build/_rhino3dm.pyd", artifacts + "/rhino3dm/_rhino3dm.pyd")

samples = os.listdir("samples")
for sample in samples:
    copyfile("samples/"+sample, artifacts + "/"+sample)


