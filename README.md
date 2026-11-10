# rhino3dm

[![workflow ci](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_ci.yml/badge.svg?branch=main)](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_ci.yml)
[![workflow release](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml/badge.svg?branch=main)](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml)
[![PyPI](https://img.shields.io/pypi/v/rhino3dm.svg?style=flat-square)](https://pypi.org/project/rhino3dm)
[![PyPI - Downloads](https://img.shields.io/pypi/dm/rhino3dm?style=flat-square)](https://pypi.org/project/rhino3dm)
[![npm](https://img.shields.io/npm/v/rhino3dm.svg?style=flat-square)](https://www.npmjs.com/package/rhino3dm)
[![npm - Downloads](https://img.shields.io/npm/dm/rhino3dm?style=flat-square)](https://www.npmjs.com/package/rhino3dm)
[![NuGet](https://img.shields.io/nuget/v/Rhino3dm.svg?style=flat-square)](https://www.nuget.org/packages/Rhino3dm/)
[![NuGet - Downloads](https://img.shields.io/nuget/dt/rhino3dm.svg?style=flat-square)](https://www.nuget.org/packages/Rhino3dm/)

**Read, write and create Rhino 3D models (`.3dm` files) in Python, JavaScript and .NET.**

rhino3dm brings [openNURBS](https://developer.rhino3d.com/guides/opennurbs/what-is-opennurbs/), the
library behind Rhino's file format, to Python, JavaScript and .NET with [RhinoCommon's](https://developer.rhino3d.com/guides/rhinocommon/what-is-rhinocommon/) API.

- Create, inspect and edit every geometry type Rhino supports: points, point clouds, NURBS curves and
  surfaces, polysurfaces (B-Reps), meshes, SubDs, extrusions and annotations
- Work with the rest of a model: layers, materials, object attributes, transforms, views
- Read and write all of it to and from `.3dm` files
- Send geometry to [Rhino.Compute](https://www.rhino3d.com/compute) for operations that need Rhino
- Runs on Windows, macOS and Linux, and in the browser

## rhino3dm 9.2 (beta)

In 9.2 the Python and JavaScript libraries are generated from RhinoCommon, so their API is
RhinoCommon's in every language: the same class names, member names, signatures and behaviour, and the
[RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/) applies to all three. Most
8.x Python and JavaScript scripts need changes; .NET code mostly does not.

- [Migrating to rhino3dm 9.2](docs/migrating-to-9.md): what changed for each language
- [CHANGELOG](CHANGELOG.md)

9.2 is a pre-release. The default install is still the latest 8.x; install the beta as shown below.

## Python

```
pip install rhino3dm           # latest stable (8.x)
pip install --pre rhino3dm     # 9.2 beta
```

Wheels for CPython 3.10 to 3.14 on Windows, macOS and Linux.

```python
import rhino3dm as r

model = r.File3dm()
model.Objects.AddSphere(r.Sphere(r.Point3d(1, 2, 3), 12))
model.Write("sphere.3dm", 8)
```

[rhino3dm.py documentation](docs/python/RHINO3DM.PY.md) ·
[API reference](https://mcneel.github.io/rhino3dm/python/api/index.html) ·
[PyPI](https://pypi.org/project/rhino3dm/) ·
[Samples](https://github.com/mcneel/rhino-developer-samples/tree/9/rhino3dm/py)

## JavaScript

```
npm install rhino3dm           # latest stable (8.x)
npm install rhino3dm@next      # 9.2 beta
```

rhino3dm.js is compiled to WebAssembly and runs in every current browser and in Node.js 22+. In a
browser you can load it from a CDN without installing anything.

```js
import rhino3dm from 'rhino3dm'

const rhino = await rhino3dm()
const model = new rhino.File3dm()
model.objects.addSphere(new rhino.Sphere({ X: 1, Y: 2, Z: 3 }, 12))
const bytes = model.toByteArray()
```

For three.js there is a [3dm loader](https://threejs.org/examples/?q=3dm#webgl_loader_3dm) built on
rhino3dm.js.

[rhino3dm.js documentation](docs/javascript/RHINO3DM.JS.md) ·
[API reference](https://mcneel.github.io/rhino3dm/javascript/api/index.html) ·
[npm](https://www.npmjs.com/package/rhino3dm) ·
[Samples](https://github.com/mcneel/rhino-developer-samples/tree/9/rhino3dm/js)

## .NET

```
dotnet add package Rhino3dm                 # latest stable (8.x)
dotnet add package Rhino3dm --prerelease    # 9.2 beta
```

Rhino3dm (formerly Rhino3dmIO) is built from RhinoCommon's own source. One NuGet package carries the
native libraries for Windows, macOS and Linux.

```csharp
using Rhino.FileIO;
using Rhino.Geometry;

var model = new File3dm();
model.Objects.AddSphere(new Sphere(new Point3d(1, 2, 3), 12));
model.Write("sphere.3dm", 8);
```

[Rhino3dm documentation](docs/dotnet/README.md) ·
[RhinoCommon API reference](https://developer.rhino3d.com/api/rhinocommon/) ·
[NuGet](https://www.nuget.org/packages/Rhino3dm/) ·
[Samples](https://github.com/mcneel/rhino-developer-samples/tree/9/rhino3dm/cs)

## Building from source

See the build guides for [Python](docs/python/RHINO3DM-BUILD.PY.md),
[JavaScript](docs/javascript/RHINO3DM-BUILD.JS.md) and [.NET](docs/dotnet/RHINO3DM-BUILD.CS.md), and
[CONTRIBUTING](CONTRIBUTING.md).

## Help and discussion

- [rhino3dm on the McNeel forum](https://discourse.mcneel.com/c/rhino-developer/rhino3dm/)
- [Issues](https://github.com/mcneel/rhino3dm/issues)

## License

[MIT](LICENSE)
