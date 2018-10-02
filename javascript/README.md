# rhino3dm.js
Web Assembly library based on OpenNURBS with a RhinoCommon style

[![Build Status](https://travis-ci.com/mcneel/rhino-geometry.js.svg?token=xzPnCyqvoS75zbqdJSUR&branch=master)](https://travis-ci.com/mcneel/rhino-geometry.js)

----

## Downloads

Get `rhino3dm.wasm` and `rhino3dm.js` from the [releases](https://github.com/mcneel/rhino3dm.js/releases), or try the _latest_ build straight from the `master` branch.

```html
<html>
  <!-- stuff -->
  <body>
    <script async type="text/javascript" src="https://files.mcneel.com/rhino-geometry.js/latest/rhino-geometry.js"></script>
    <!-- more stuff -->
  </body>
</html>
```

_**Note:** You can replace `latest` with a [specific build number](https://travis-ci.com/mcneel/rhino-geometry.js/builds) if you know what you're looking for!_


## Build it yourself

### Get The Source

This repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run
  * `git submodule update --init`

### Install the Tools

* Make sure you have python 2.7.12 or newer installed. python is available at https://www.python.org/
* Install emscripten http://kripken.github.io/emscripten-site/docs/getting_started/downloads.html to compile C++ to web assembly (wasm)
* Install CMake (https://cmake.org/download/)
* (Windows) Make sure to have make installed. https://sourceforge.net/projects/mingw-w64/files/latest/download

### Compile

* After installation, make sure you have  emcc, cmake, and python on your path. Emscripten provides instructions for adding path information during install.
* From the command line (or bash), go to the root directory of this repo and type `./build.sh`. If everything is configured correctly, you should have a compiled wasm, js, and html file after a couple minutes.

### Test

* Make sure you are in the `artifacts` directory that was created from a compile.
* Type `python -m SimpleHTTPServer 8080`
* Go to your browser and navigate to `http://localhost:8080/rhino3dm.html`
* For chrome, right click and select `inspect`. Click on the `console` tab and try typing in the following javascript
  * `sphere = new Module.Sphere([1,2,3],12);`
  * `brep = sphere.toBrep();`
  * `jsonobject = brep.encode();`
