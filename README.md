# rhino3dm.py
CPython 2.7 module based on OpenNURBS with a RhinoCommon style

## Build it yourself

### Get The Source

This repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run
  * `git submodule update --init`

## Install the Tools

* Mac
  * Install Homebrew (https://brew.sh/)
  * `brew install cmake boost-python`
* Windows
  * This project uses Visual Studio 2017 (specifically the C++ portion)
  * Install boost (TODO: describe process)
  * Install python (Visual Studio compile assumes python is installed to `C:\Python27`)

## Compile

* (All platforms) run the `buildRhino3dm.py` script to compile and configure

## Test

* `cd artifacts` and start `python`
```
>>> from rhino3dm import *
>>> center = Point3d(1,2,3)
>>> arc = Arc(center, 10, 1)
>>> nc = arc.ToNurbsCurve()
>>> start = nc.PointAtStart
>>> print start
```
