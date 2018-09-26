# rhino-geometry.py
Native python based on OpenNURBS with a RhinoCommon style

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

* (Mac) open bash in the `rhino-python.py` directory and type `./build.sh`
* (Win) open `rhino_geometry.sln` and compile a release|x64 build

## Test

* `cd build` and start `python`
```
>>> import rhino_geometry as rg
>>> center = rg.Point3d(1,2,3)
>>> arc = rg.Arc(center, 10, 1)
>>> nc = arc.ToNurbsCurve()
>>> start = nc.PointAtStart
>>> print start.X, start.Y, start.Z
```
