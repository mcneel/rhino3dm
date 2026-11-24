# rhino3dm

**Read, write and create Rhino 3D models (`.3dm` files) in Python.**

rhino3dm brings [openNURBS](https://www.rhino3d.com/opennurbs), the library behind Rhino's file
format, to Python with [RhinoCommon](https://developer.rhino3d.com/api/rhinocommon/)'s API (Rhino's
.NET SDK). Use it in scripts, notebooks and web services on Windows, macOS and Linux.

## Coming from rhino3dm 8.x?

Most 8.x scripts need changes. For example, editing an object's geometry no longer changes the file,
`out` parameters come back as a tuple with the success flag first, and bad arguments raise.

* [Migrating to rhino3dm 9.2](https://mcneel.github.io/rhino3dm/python/api/migrating.html): what
  changed, with before-and-after code
* [Renamed and removed members](https://mcneel.github.io/rhino3dm/python/api/migrating-members.html):
  every 8.x name and what replaces it

## Install

```
pip install rhino3dm
```

Pre-releases, such as betas, install with `pip install --pre rhino3dm`.

## Example

```python
import rhino3dm

# build a model
model = rhino3dm.File3dm()
model.Objects.AddSphere(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5))
model.Objects.AddCircle(rhino3dm.Circle(rhino3dm.Point3d(20, 0, 0), 3))
model.Objects.AddPoint(rhino3dm.Point3d(10, 10, 10))
model.Write("example.3dm", 0)

# read it back
model = rhino3dm.File3dm.Read("example.3dm")
for obj in model.Objects:
    geometry = obj.Geometry
    bbox = geometry.GetBoundingBox(True)
    print(geometry.ObjectType.name, bbox.Min, bbox.Max)

# Surface -5,-5,-5 5,5,5
# Curve 17,-3,0 23,3,0
# Point 10,10,10 10,10,10
```

## Features

* **Files**: read `.3dm` files from any Rhino version and write new ones, on disk or in memory.
* **Geometry**: points, curves, NURBS curves and surfaces, breps, extrusions, meshes, SubD,
  point clouds, text and dimensions, with RhinoCommon's members.
* **Model content**: layers, materials, linetypes, groups, blocks, views, user strings and
  embedded files.
* **Typed**: the package ships type hints (`__init__.pyi`) with documentation for every member.

## Documentation

* [API reference](https://mcneel.github.io/rhino3dm/python/api/index.html)
* [RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/): rhino3dm follows its
  names and behaviour
* [Samples](https://github.com/mcneel/rhino-developer-samples/tree/9/rhino3dm/py)

## Platforms

| | Python 3.10 or later |
|---|---|
| Windows | x64, Arm64 |
| macOS 14 or later | universal (Apple silicon and Intel) |
| Linux | x86_64, aarch64 (manylinux_2_28) |

Anywhere else, pip builds the package from source, which needs CMake and a C++17 compiler.

## Also for JavaScript and .NET

rhino3dm is also available as [rhino3dm.js](https://www.npmjs.com/package/rhino3dm) (WebAssembly,
for browsers and Node.js) and [Rhino3dm](https://www.nuget.org/packages/Rhino3dm) for .NET.

## Help

Ask on the [forum](https://discourse.mcneel.com/c/rhino-developer/rhino3dm/), and report bugs on
[GitHub](https://github.com/mcneel/rhino3dm/issues).
