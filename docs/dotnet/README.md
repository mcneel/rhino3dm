# Rhino3dm

A .NET library for reading and writing Rhino's `.3dm` files and working with the geometry in them, without
Rhino. Its API is RhinoCommon's: the package is built from RhinoCommon's own code over the
[OpenNURBS](https://github.com/mcneel/opennurbs) geometry library.

```csharp
using Rhino.FileIO;

var model = File3dm.Read("model.3dm");
foreach (var obj in model.Objects)
{
    var bbox = obj.Geometry.GetBoundingBox(true);
    Console.WriteLine($"{bbox.Min}, {bbox.Max}");
}
```

## Coming from Rhino3dm 8.x?

The package was RhinoCommon's code in 8.x, and still is. 9.2 takes it from Rhino 9 instead of Rhino 8, so
most code builds and runs unchanged. What changes:

### Target frameworks

The package targets `netstandard2.0`, `net48` and `net10.0` (8.x: `netstandard2.0`, `net48` and
`net7.0`). A .NET 7, 8 or 9 project uses the `netstandard2.0` build.

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

## Links

- [RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/): the API reference
- [Project homepage](https://github.com/mcneel/rhino3dm)
- [Forum](https://discourse.mcneel.com/c/rhino-developer/rhino3dm/)
- [Report an issue](https://github.com/mcneel/rhino3dm/issues)
