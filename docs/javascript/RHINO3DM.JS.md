# rhino3dm.js

[![CircleCI](https://circleci.com/gh/mcneel/rhino3dm/tree/master.svg?style=shield&circle-token=53733a2fe2cf99a11808d1e5210bc1aeb3f13ea9)](https://circleci.com/gh/mcneel/rhino3dm/tree/master)
[![](https://data.jsdelivr.com/v1/package/npm/rhino3dm/badge)](https://www.jsdelivr.com/package/npm/rhino3dm)

**rhino3dm.js** is a javascript library with associated web assembly (rhino3dm.wasm) that is OpenNURBS plus additional C++ to javascript bindings compiled to web assembly. The library based on OpenNURBS with a RhinoCommon style. The libraries will run on on all major browsers as well as node.js.


## Usage

The easiest way to get started is to reference a specific version of the library using jsDelivr.

```html
<!DOCTYPE html>
<html>
  <!-- stuff -->
  <body>
    <script src="https://cdn.jsdelivr.net/npm/rhino3dm@0.13.0/rhino3dm.min.js"></script>
    <script>
      rhino3dm().then((rhino) => {
        sphere = new rhino.Sphere([1,2,3], 12)
        // more stuff
      })
      // even more stuff
    </script>
    <!-- you get the idea -->
  </body>
</html>
```

You can also [download the files](https://www.jsdelivr.com/package/npm/rhino3dm) if you want to bake them into your site or application. You'll need the `.wasm` web assembly along with the `.js` (or `.min.js`) wrapper.


### Node.js

**rhino3dm.js** is available on [npm](https://www.npmjs.com/package/rhino3dm); try `npm install rhino3dm`.

```js
$ node
> rhino3dm = require('rhino3dm')() // note the trailing "()"
> sphere = new rhino3dm.Sphere([1,2,3,], 12)
> sphere.radius
12
```

It takes a moment to load the ~5 MB wasm file – this happens asycnhronously. Unlike interactive usage, when scripting with `rhino3dm` you can use the fact that the `rhino3dm()` function returns a `Promise`.

```js
// script.js
const rhino3dm = require('rhino3dm')

rhino3dm().then((rhino) => {
  const sphere = new rhino.Sphere([1,2,3,], 12)
  console.log(sphere.radius)
})
```


## API Docs

The latest [rhino3dm.js API Documentation](https://mcneel.github.io/rhino3dm/javascript/api/index.html)


## Examples

There a few samples are available in the [Github Repo Samples folder](https://github.com/mcneel/rhino3dm/tree/master/docs/javascript/samples)

An advanced sample creates a 3dm file viewer in a web browser.  The html+javascript to create the viewer is around 300 lines (including comments) and runs on all browsers including mobile devices.  

<img src="https://mcneel.github.io/rhino3dm/images/rhino3dm_rhinologo.png" width="300"></img>

**rhino3dm.js** is used to read a 3dm file and create an instance of a File3dm class in the browser’s memory.  It then walks through the objects in the model and calls compute.rhino3d.com to create meshes and isocurves for the polysurface. These meshes and isocurves are then added to a three.js scene for display.

Here's [another example](https://observablehq.com/@pearswj/using-rhino3dm-in-observable/2) of rhino3dm.js, this time running in one of [Observable](http://observablehq.com/)'s live notebooks. Dive right in an tweak the code!

## Build from source

If the pre-compiled libraries above do not work in your situation, you can compile the libraries from their source. For detailed instructions go to [rhino3dm.js and rhino3dm.wasm](RHINO3DM-BUILD.JS.md)
