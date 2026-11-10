# Migrating to rhino3dm 9.2 (.NET)

<!-- Also the NuGet package page: docs/dotnet/README.md repeats this guide; change both. -->

[← Other languages](../migrating-to-9.md)

The `Rhino3dm` NuGet package was RhinoCommon's code in 8.x, and still is. 9.2 takes it from Rhino 9
instead of Rhino 8, so most code builds and runs unchanged. What changes:

## Target frameworks

The package targets `netstandard2.0`, `net48` and `net10.0` (8.x: `netstandard2.0`, `net48` and
`net7.0`). A .NET 7, 8 or 9 project uses the `netstandard2.0` build.

## Platforms

Native libraries for Windows x64 and Arm64, macOS (Apple silicon and Intel) and Linux x64 and Arm64.
There is no 32-bit Windows library any more (8.x had `win-x86`): opennurbs 9 needs 64-bit pointers.
A .NET Framework application that loads the package must run as a 64-bit process (turn off "Prefer
32-bit"). macOS 14 or later.

## Members that threw at runtime

Some 8.x members compiled but threw `EntryPointNotFoundException` on every call, because the native
function behind them is not part of rhino3dm's native library:

- `Curve.SpanVector()` works now.
- `SubD.UpdateSurfaceMeshCache()`, `SubD.SurfaceMeshCacheExists()` and `Material.RdkMaterialId` are
  gone. They never worked outside Rhino.
- About 30 other members with the same problem are no longer in the package, so a call to one of them
  is now a compile error instead of a runtime exception.

## New from Rhino 9

The RhinoCommon API added in Rhino 9 that works outside Rhino, for example `LengthUnit` (a length unit
with conversions between unit systems). The members are marked `<since>9.0</since>` and later in the
[RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/).
