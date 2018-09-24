# rhino-geometry.py
Native python based on OpenNURBS with a RhinoCommon style

# NOTE: ONLY AVAILABLE ON OSX AT THE MOMENT
## Build it yourself

### Get The Source

This repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run
  * `git submodule init`
  * `git submodule update`

## Install the Tools
### Mac OSX
* Install Homebrew (https://brew.sh/)
* `brew install cmake boost-python`

### Compile

* open bash in the `rhino-python.py` directory and type `./build.sh`

### Test

* `cd build` and start `python`
* `>>> import rhinocommon`
* `>>> center = rhinocommon.Point3d(1,2,3)`
* `>>> arc = rhinocommon.Arc(center, 10, 1)`
* `>>> nc = arc.ToNurbsCurve()`
* `>>> start = nc.PointAtStart`
* `>>> print start.X, start.Y, start.Z`
