# Migrating to rhino3dm 9.2

rhino3dm 9.2's Python and JavaScript bindings are generated from RhinoCommon, the .NET SDK of Rhino, so
their API is RhinoCommon's: its names, signatures and behaviour. rhino3dm 8.x was written by hand and
often differed from RhinoCommon, so most 8.x Python and JavaScript scripts need changes. The .NET
package was RhinoCommon's code already, and changes little.

| Language | Guide | Renamed and removed members |
|---|---|---|
| Python | [Migrating to 9.2 (Python)](migrating-to-9/python.md) | [Python members](migrating-to-9/python-members.md) |
| JavaScript | [Migrating to 9.2 (JavaScript)](migrating-to-9/javascript.md) | [JavaScript members](migrating-to-9/javascript-members.md) |
| .NET | [Migrating to 9.2 (.NET)](migrating-to-9/dotnet.md) | |

The [RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/) describes rhino3dm 9.2
in every language.
