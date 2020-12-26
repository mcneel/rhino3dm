# Build rhino3dm.py for yourself

## Install the Tools

* Mac
  * `brew install python3 cmake`
* Windows
  * Visual Studio 2019
  * [Python](https://www.python.org/downloads/windows/)
  * [CMake](https://cmake.org/download/) (make sure that cmake.exe is added to the `%PATH%`)
* Linux
  * Tested with Ubuntu 20.04 LTS
  * `sudo apt install git python3 python3-dev python3-setuptools cmake gcc g++`

We've been using CMake 16.6.2, but you might be able to get away with an older version.

## Get the source code

This repo uses [OpenNURBS](https://github.com/mcneel/opennurbs) and [pybind11](https://github.com/pybind/pybind11) as submodules. Make sure they're properly initialised and updated.

```commandline
git clone --recurse-submodules https://github.com/mcneel/rhino3dm.git
```

## Compile

* (All platforms) run `python setup.py bdist` in the root directory to compile and configure. The library will compile for the version of python that you are executing.

* (Windows) If you are on Windows, you can create a Visual Studio project file for editing and compiling code by running the `create_python_vcxproj.py` script in the `src` directory. This is the easiest way to add new code to the project.

## Compile and install a development version

* (All platforms) Alternatively, running `pip install -e .` from the root of the repository will compile the Python extension, copy it in `/src/rhino3dm`, and link `/src/rhino3dm` in your installed packages. Rebuilds will be faster than building a binary distribution. To rebuild after modifying the C++ source files, run `python setup.py develop`.

## Test

* `cd build_{pyver}/stage` and start `python`
```
>>> from rhino3dm import *
>>> center = Point3d(1,2,3)
>>> arc = Arc(center, 10, 1)
>>> nc = arc.ToNurbsCurve()
>>> start = nc.PointAtStart
>>> print(start)
```
