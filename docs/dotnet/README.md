# Rhino3dm

**Read, write and create Rhino 3D models (`.3dm` files) in .NET.**

Rhino3dm is a subset of [RhinoCommon](https://developer.rhino3d.com/api/rhinocommon/) for applications and web services on Windows, macOS and Linux. It is built from RhinoCommon's own code
over [openNURBS](https://www.rhino3d.com/opennurbs).

## Coming from Rhino3dm 8.x?

Most 8.x code builds and runs unchanged. What changes:

### Target frameworks

The package targets `netstandard2.0`, `net48` and `net10.0` (8.x: `netstandard2.0`, `net48` and
`net7.0`). A .NET 7, 8 or 9 project uses the `netstandard2.0` build.

### No 32-bit Windows library

8.x had `win-x86`; opennurbs 9 needs 64-bit pointers, so 9.2 has no 32-bit Windows library. A .NET
Framework application that loads the package must run as a 64-bit process (turn off "Prefer 32-bit").

### Members that threw at runtime

Some 8.x members compiled but threw `EntryPointNotFoundException` on every call, because the native
function behind them is not part of rhino3dm's native library:

- `Curve.SpanVector()` works now.
- `SubD.UpdateSurfaceMeshCache()`, `SubD.SurfaceMeshCacheExists()` and `Material.RdkMaterialId` are
  gone. They never worked outside Rhino.
- About 30 other members with the same problem are no longer in the package, so a call to one of them
  is now a compile error instead of a runtime exception.

### New from Rhino 9

The RhinoCommon API added in Rhino 9 that works outside Rhino, for example `LengthUnit` (a length unit
with conversions between unit systems). The members are marked `<since>9.0</since>` and later in the
[RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/).

## Install

```
dotnet add package Rhino3dm
```

Pre-releases, such as betas, install with `dotnet add package Rhino3dm --prerelease`.

## Example

```csharp
using Rhino.FileIO;
using Rhino.Geometry;

// build a model
var model = new File3dm();
model.Objects.AddSphere(new Sphere(Point3d.Origin, 5));
model.Objects.AddCircle(new Circle(new Point3d(20, 0, 0), 3));
model.Objects.AddPoint(new Point3d(10, 10, 10));
model.Write("example.3dm", 0);

// read it back
model = File3dm.Read("example.3dm");
foreach (var obj in model.Objects)
{
    var bbox = obj.Geometry.GetBoundingBox(true);
    Console.WriteLine($"{obj.Geometry.ObjectType} {bbox.Min} {bbox.Max}");
}

// Surface -5,-5,-5 5,5,5
// Curve 17,-3,0 23,3,0
// Point 10,10,10 10,10,10
```

## Features

- **Files**: read `.3dm` files from any Rhino version and write new ones, on disk or in memory.
- **Geometry**: points, curves, NURBS curves and surfaces, breps, extrusions, meshes, SubD,
  point clouds, text and dimensions, with RhinoCommon's members.
- **Model content**: layers, materials, linetypes, groups, blocks, views, user strings and
  embedded files.
- **Documented**: the package ships RhinoCommon's XML documentation, so your editor shows it for every
  member.

## Documentation

- [RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/): the API reference
- [Samples](https://github.com/mcneel/rhino-developer-samples/tree/9/rhino3dm/cs)

## Platforms

| | Native library |
|---|---|
| Windows | x64, Arm64 |
| macOS 14 or later | Apple silicon and Intel |
| Linux | x64, Arm64 |

Target frameworks: `netstandard2.0`, `net48` and `net10.0`.

## Also for Python and JavaScript

rhino3dm is also available for [Python](https://pypi.org/project/rhino3dm/) and as
[rhino3dm.js](https://www.npmjs.com/package/rhino3dm) (WebAssembly, for browsers and Node.js).

## Help

Ask on the [forum](https://discourse.mcneel.com/c/rhino-developer/rhino3dm/), and report bugs on
[GitHub](https://github.com/mcneel/rhino3dm/issues).
