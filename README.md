# rhino3dm
rhino3dm is a set of standalone libraries based on the [OpenNURBS](https://developer.rhino3d.com/guides/opennurbs/what-is-opennurbs/) geometry library with a [RhinoCommon](https://developer.rhino3d.com/guides/rhinocommon/what-is-rhinocommon/) style functionality. This allows you to access and manipulate geometry through your .NET , Python or JavaScript applications independent of Rhino.  

Specific functionality includes

- Create, interrogate and store NURBS geometry through  [OpenNURBS](https://developer.rhino3d.com/guides/opennurbs/what-is-opennurbs/). 
- Use meshes and curve geometry in your application.
- Use these rhino3dm as a client to make calls into the [Rhino Compute cloud server](https://www.rhino3d.com/compute) for advanced manipulation of geometry objects
- The library also includes a full set of tools to read/write the *.3dm*file format.
- Accessible on many platforms through C#, Python and Javascript.

### rhino3dm.py (Python)
**rhino3dm.py** is a python package that can be used on all current versions of CPython (both 2.7 and 3.7) and is available on all platforms (Windows, OSX, Unix) [through PyPi.org](https://pypi.org/project/rhino3dm/).

`pip install rhino3dm`

For more details on running rhino3dm see [our python documentation](RHINO3DM.PY.md)


### rhino3dm.js (Javascript/web assembly)
[![CircleCI](https://circleci.com/gh/mcneel/rhino3dm/tree/master.svg?style=shield&circle-token=53733a2fe2cf99a11808d1e5210bc1aeb3f13ea9)](https://circleci.com/gh/mcneel/rhino3dm/tree/master)

**rhino3dm.js** is a javascript library with an associated web assembly (rhino3dm.wasm) that is OpenNURBS plus additional javascript bindings. rhino3dm.js allows for geometry access and manipulation on all major browsers as well as [node.js](https://nodejs.org/).

![Rhino Logo in Web Browser](docs\images\rhino3dm_rhinologo.png)

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

Rhino3dm.Net allows you to write standalone .NET applications that can create and manipulate OpenNURBS Geometry. The library also includes a full set of tools to read/write the *.3dm*file format.

See the [documentation on installing and using Rhino3dmIO packages on nuget](https://developer.rhino3d.com/guides/opennurbs/what-is-rhino3dmio/)

### How to Participate

The libraries are still very new and changing rapidly. Give them a try or get involved with the [github repository](https://github.com/mcneel/rhino3dm) and help make them even better.

Up to date technical information can also be found on [Steve Baer's Blog](https://stevebaer.wordpress.com/)