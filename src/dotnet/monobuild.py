import subprocess

tool = "xbuild"
project = "RhinoCommonMono.sln"
#had to add tools version /tv parameter because the 4.0 compile seems to be a little
#hosed in 2.8.1
options = "/property:configuration=Mono"
cmd = tool + " " + options + " " + project
subprocess.call( cmd, shell=True)