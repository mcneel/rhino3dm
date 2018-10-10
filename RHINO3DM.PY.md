# rhino3dm.py
CPython package based on OpenNURBS with a RhinoCommon style

## Install from pip
`pip install rhino3dm`  
If you get an error, you may need to run `pip install --user rhino3dm`

### Supported platforms
* Python 2.7 - Windows (32 and 64 bit)
* Python 3.7 - Windows (32 and 64 bit)
* Python 2.7 - OSX (installed through homebrew)
* Python 3.7 - OSX (installed through homebrew)
* We are currently working on getting linux packages available

---

## Build it yourself

### Get The Source

This repo uses [OpenNURBS](https://github.com/mcneel/opennurbs) and [pybind11](https://github.com/pybind/pybind11) as submodules, so you need to run another git command after you have cloned. `cd` into the new repository directory and run
  * `git submodule update --init`

## Install the Tools

CMake 3.12.1 is the minimum required CMake version.

* Mac
  * Install Homebrew (https://brew.sh/)
  * `brew install python2 cmake` (for Python 2.7 compile)
  * `brew install python3 cmake` (for Python 3.7 compile)
* Windows
  * This project uses Visual Studio 2017
  * Install the flavor of CPython that you prefer to work with from python.org
  * Install CMake (https://cmake.org/download/) and make sure that cmake.exe is added to the path
* Linux
  * Tested with Clang 3.8.0 on Linux Mint 18.3
  * Install CMake 3.12.1
  * `sudo aptitude install python2 python3 python2-dev python3-dev uuid uuid-dev`

## Compile

* (All platforms) run `python build_python_lib.py` in the `src` directory to compile and configure. The library will compile for the version of python that you are executing.

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
