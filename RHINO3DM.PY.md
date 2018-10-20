# rhino3dm.py
**rhino3dm.py** is a Python package that is OpenNURBS plus additional python bindings compiled to a C-extension library that can be used on all current versions of CPython (both 2.7 and 3.7) and is available on all platforms [through PyPi.org](https://pypi.org/project/rhino3dm/)

#### Supported Python platforms:

- Python 2.7 - Windows (32 and 64 bit)
- Python 3.7 - Windows (32 and 64 bit)
- Python 2.7 - OSX (installed through homebrew)
- Python 3.7 - OSX (installed through homebrew)
- Linux and other python versions are supported through source distributions on PyPi

## Install using pip
The easiest way to access the rhino3dm.py libraries is to use the pip installer from the Python console:

`pip install rhino3dm`  

If you get an error, you may need to run `pip install --user rhino3dm`

If `pip` is not installed, go to the [Pip Installation instructions](https://pip.pypa.io/en/latest/installing/)

## API Docs
The latest [rhino3dm.py API Documentation](https://mcneel.github.io/rhino3dm/python/api/index.html)

## Example usage

* Start the `python` console, then type:
```python
>>> from rhino3dm import *
>>> center = Point3d(1,2,3)
>>> arc = Arc(center, 10, 1)
>>> nc = arc.ToNurbsCurve()
>>> start = nc.PointAtStart
>>> print(start)
```

See the [RhinoCommon Documentation for further details on the class layout](https://developer.rhino3d.com/guides/rhinocommon/)

## Build it yourself

rhino3dm.py may be built from the source.  To find out how to build rhino3dm.py for yourself go to the [rhino3dm.py build page](docs/python/RHINO3DM-BUILD.PY.md)

---
