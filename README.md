# rhino3dm
rhino3dm is a set of standalone libraries based on the [OpenNURBS](https://developer.rhino3d.com/guides/opennurbs/what-is-opennurbs/) geometry library with a ["RhinoCommon"](https://developer.rhino3d.com/guides/rhinocommon/what-is-rhinocommon/) style. This provides the ability to access and manipulate geometry through .NET , Python or JavaScript applications independent of Rhino.  

Functionality includes

- Create, interrogate and store all geometry types supported in Rhino. This includes points, point clouds, NURBS curves and surfaces, polysurfaces (B-Reps), meshes, annotations, and extrusions.
- Work with non-geometry classes supported in Rhino like layers, object attributes, tranforms and viewports
- Read and write all of the above information to and from the *.3dm* file format
- Use as a client to make calls into the [Rhino Compute cloud server](https://www.rhino3d.com/compute) for advanced manipulation of geometry objects
- Available on most platforms (Windows, Mac, Linux)

For bug reports or feature requests see the [contributing guide](CONTRIBUTING.md)

----

### rhino3dm.py (Python)
**rhino3dm.py** is a python package that can be used on all current versions of CPython (both 2.7 and 3.7) and is available on all platforms (Windows, macOS, Linux) [through PyPi.org](https://pypi.org/project/rhino3dm/).

`pip install rhino3dm`

See [our python documentation](RHINO3DM.PY.md) for details


### rhino3dm.js (Javascript/web assembly)
[![CircleCI](https://circleci.com/gh/mcneel/rhino3dm/tree/master.svg?style=shield&circle-token=53733a2fe2cf99a11808d1e5210bc1aeb3f13ea9)](https://circleci.com/gh/mcneel/rhino3dm/tree/master)

**rhino3dm.js** is a javascript library with an associated web assembly (rhino3dm.wasm). rhino3dm.js should run on all major browsers as well as [node.js](https://nodejs.org/).

<img src="docs/images/rhino3dm_rhinologo.png" width="200"></img>

```html
<html>
  <!-- stuff -->
  <body>
    <script async type="text/javascript" src="https://files.mcneel.com/rhino3dm/js/latest/rhino3dm.js"></script>
    <!-- more stuff -->
  </body>
</html>
```

See [our javascript documentation](RHINO3DM.JS.md) for details

### Rhino3dm.NET

Rhino3dm.Net allows you to write standalone .NET applications.

See the [documentation on installing and using Rhino3dmIO packages on nuget](https://developer.rhino3d.com/guides/opennurbs/what-is-rhino3dmio/)

### How to Participate

The libraries are still very new and changing rapidly (with the exception of Rhino3dm.NET). Give them a try or get involved.

Up to date technical information can also be found on [Steve Baer's Blog](https://stevebaer.wordpress.com/)
