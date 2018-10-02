# rhino3dm.py
Python 2.7 package based on OpenNURBS with a RhinoCommon style

Project Hompage at: https://github.com/mcneel/rhino3dm.py

### Supported platforms
* Python27 - Windows (32 and 64 bit)
* Python27 - OSX (installed through homebrew)
* Python37 - OSX (installed through homebrew)
* Other distributions are possible, just let us know where you need this package to run


## Test

* start `python`
```
>>> from rhino3dm import *
>>> center = Point3d(1,2,3)
>>> arc = Arc(center, 10, 1)
>>> nc = arc.ToNurbsCurve()
>>> start = nc.PointAtStart
>>> print start
```
