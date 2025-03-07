# rhino3dm

[![workflow ci](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_ci.yml/badge.svg?branch=main)](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_ci.yml)
[![workflow ci](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml/badge.svg?branch=main)](https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml)


[![PyPI](https://img.shields.io/pypi/v/rhino3dm.svg?style=flat-square)](https://pypi.org/project/rhino3dm)![PyPI - Downloads](https://img.shields.io/pypi/dm/rhino3dm?style=flat-square)

[![NuGet](https://img.shields.io/nuget/v/Rhino3dm.svg?style=flat-square)](https://www.nuget.org/profiles/McNeel)[![NuGet Downloads](https://img.shields.io/nuget/dt/rhino3dm.svg?style=flat-square)](https://www.nuget.org/packages/rhino3dm/)

[![npm](https://img.shields.io/npm/v/rhino3dm.svg?style=flat-square)](https://www.npmjs.com/package/rhino3dm)![npm](https://img.shields.io/npm/dm/rhino3dm?style=flat-square)


**rhino3dm** is a set of libraries based on the [OpenNURBS](https://developer.rhino3d.com/guides/opennurbs/what-is-opennurbs/) geometry library with a ["RhinoCommon"](https://developer.rhino3d.com/guides/rhinocommon/what-is-rhinocommon/) style. This provides the ability to access and manipulate geometry through .NET, Python or JavaScript applications independent of Rhino.  

Functionality includes

- Create, interrogate, and store all geometry types supported in Rhino. This includes points, point clouds, NURBS curves and surfaces, polysurfaces (B-Reps), meshes, annotations, extrusions, and SubDs.
- Work with non-geometry classes supported in Rhino like layers, object attributes, transforms and viewports
- Read and write all of the above information to and from the *.3dm* file format
- Use as a client to make calls into the [Rhino Compute cloud server](https://www.rhino3d.com/compute) for advanced manipulation of geometry objects
- Available on most platforms (Windows, macOS, Linux)

---

## rhino3dm.py (Python)
**rhino3dm.py** is a python package that can be used on all current versions of CPython (3.7 - 3.11) and is available on all platforms (Windows, macOS, Linux) 

rhino3dm.js packages are available on pypi: https://pypi.org/project/rhino3dm/

`pip install --user rhino3dm`

See [our python documentation](docs/python/RHINO3DM.PY.md) for details


## rhino3dm.js (JavaScript and node.js)

**rhino3dm.js** is a javascript library with an associated web assembly (rhino3dm.wasm). rhino3dm.js should run on all major browsers as well as [node.js](https://nodejs.org/).

rhino3dm.js packages are available on npm: https://www.npmjs.com/package/rhino3dm

<img src="docs/images/rhino3dm_rhinologo.png" width="200"></img>

```html
<!DOCTYPE html>

<body>

  <!-- Import maps polyfill -->
  <!-- Remove this when import maps will be widely supported -->
  <script async src="https://unpkg.com/es-module-shims@1.8.2/dist/es-module-shims.js"></script>

  <script type="importmap">
      {
          "imports": {
            "rhino3dm":"https://unpkg.com/rhino3dm@8.17.0-beta1/rhino3dm.module.min.js"
          }
      }
  </script>

  <script type="module">

    import rhino3dm from 'rhino3dm'
    const rhino = await rhino3dm()
    const sphere = new rhino.Sphere( [1,2,3,], 12 )
    console.log(sphere.diameter)

  </script>

</body>

</html>
```

See [our javascript documentation](docs/javascript/RHINO3DM.JS.md) for details

## Rhino3dm.NET

Rhino3dm.NET (formerly known as Rhino3dmIO) allows you to write standalone .NET applications. 

rhino3dm.net packages are available on nuget: https://www.nuget.org/packages/Rhino3dm/

From this repository we build macOS, windows, and linux packages in various runtimes which all all delivered via the nuget package.

### More

Some more details and discussions can be found at:
  * [discourse.mcneel.com](https://discourse.mcneel.com/c/rhino-developer/rhino3dm/)
  * [Steve Baer's Blog](https://stevebaer.wordpress.com/2018/10/15/rhino3dm-geometry-toolkits-for-net-python-and-javascript/)
  
