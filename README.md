# rhino3dm
Stand alone libraries based on OpenNURBS with a RhinoCommon style.

### Try it out
* Python
  * `pip install rhino3dm`
  * See python directory for details
* Javascript (web assembly)
  * See javascript directory for details
* .NET (coming soon)
  * currently available as Rhino3dmIO packages on nuget
  * https://www.nuget.org/packages?q=rhino3dmio

----

### Get The Source

This repo uses [OpenNURBS](https://github.com/mcneel/opennurbs) and [pybind11](https://github.com/pybind/pybind11) as submodules, so you need to run another git command after you have cloned. `cd` into the new repository directory and run
  * `git submodule update --init`
