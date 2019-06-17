# rhino3dm.js
**rhino3dm.js** is a javascript library with an associated web assembly (rhino3dm.wasm) that is OpenNURBS plus additional C++ to javascript bindings compiled to web assembly. The library based on OpenNURBS with a RhinoCommon style. The libraries will run on on all major browsers as well as node.js.

[![CircleCI](https://circleci.com/gh/mcneel/rhino3dm/tree/master.svg?style=shield&circle-token=53733a2fe2cf99a11808d1e5210bc1aeb3f13ea9)](https://circleci.com/gh/mcneel/rhino3dm/tree/master)

----

## Usage

Rhino3dm.js requires two files`rhino3dm.wasm` and `rhino3dm.js`.  It is easiest to simply reference the *latest* build directly from our servers:

```html
<html>
  <!-- stuff -->
  <body>
    <script type="text/javascript" src="https://files.mcneel.com/rhino3dm/js/latest/rhino3dm.js"></script>
    <script>
      // NOTE: the rhino3dm library is compiled with the MODULARIZE
      // option to avoid collisions with other web assemblies
      // here's one way of using it...
      rhino3dm.then((Module) => {
        sphere = new Module.Sphere([1,2,3], 12)
        // more stuff
      })
      // even more stuff
    </script>
    <!-- you get the idea -->
  </body>
</html>
```

### Download the files

If it would be better to download a static build locally, download the latest build of the two files:
-  [rhino3dm.js](https://files.mcneel.com/rhino3dm/js/latest/rhino3dm.js)
-  [rhino3dm.wasm](https://files.mcneel.com/rhino3dm/js/latest/rhino3dm.wasm)


Place these in the same folder. The `rhino3dm.js` references the `rhino3d.wasm`.

Note: A list of builds is available on our [Circleci project](https://circleci.com/gh/mcneel/rhino3dm). You can replace latest with dujour/BUILD_NUMBER if you know what you're looking for!

### Node.js

**rhino3dm.js** is also available on npm; try `npm install rhino3dm`.

```js
$ node
> rhino3dm = require('rhino3dm')() // note the trailing "()"
> sphere = new rhino3dm.Sphere([1,2,3,], 12)
```

It takes a moment to load the ~5 MB wasm file – this happens asycnhronously. Unlike interactive usage, when scripting with `rhino3dm` you can use the fact that the `rhino3dm()` function returns a `Promise`.

```js
# script.js
rhino3dm = require('rhino3dm')

rhino3dm().then((rhino) => {
  sphere = new rhino.Sphere([1,2,3,], 12)
  console.log(sphere.radius)
})
```


## API Docs
The latest [rhino3dm.js API Documentation](https://mcneel.github.io/rhino3dm/javascript/api/index.html)

## Examples

There a few samples are available in the [Github Repo Samples folder](https://github.com/mcneel/rhino3dm/tree/master/samples/javascript)

An advanced sample creates a 3dm file viewer in a web browser.  The html+javascript to create the viewer is around 300 lines (including comments) and runs on all browsers including mobile devices.  

<img src="docs/images/rhino3dm_rhinologo.png" width="300"></img>

**rhino3dm.js** is used to read a 3dm file and create an instance of a File3dm class in the browser’s memory.  It then walks through the objects in the model and calls compute.rhino3d.com to create meshes and isocurves for the polysurface. These meshes and isocurves are then added to a three.js scene for display.

Here's [another example](https://observablehq.com/@pearswj/using-rhino3dm-in-observable/2) of rhino3dm.js, this time running in one of [Observable](http://observablehq.com/)'s live notebooks. Dive right in an tweak the code!

## Build from source

If the pre-compiled libraries above do not work in your situation, you can compile the libraries from their source. For detailed instructions go to [rhino3dm.js and rhino3dm.wasm](/docs/javascript/RHINO3DM-BUILD.JS.md)
