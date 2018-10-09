# rhino3dm.js
Web Assembly library based on OpenNURBS with a RhinoCommon style

[![CircleCI](https://circleci.com/gh/mcneel/rhino3dm/tree/master.svg?style=shield&circle-token=53733a2fe2cf99a11808d1e5210bc1aeb3f13ea9)](https://circleci.com/gh/mcneel/rhino3dm/tree/master)

----

## Downloads

Get `rhino3dm.wasm` and `rhino3dm.js` from the [releases](https://github.com/mcneel/rhino3dm/releases), or try the _latest_ build straight from the `master` branch.

```html
<html>
  <!-- stuff -->
  <body>
    <script async type="text/javascript" src="https://files.mcneel.com/rhino3dm/js/latest/rhino3dm.js"></script>
    <!-- more stuff -->
  </body>
</html>
```

_**Note:** You can replace `latest` with `dujour/BUILD_NUMBER` if you know what you're [looking for](https://circleci.com/gh/mcneel/rhino3dm)!_


## Build it yourself

### Get the Source

This repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run
  * `git submodule update --init`

### Install the Tools

* Make sure you have python 2.7.12 or newer installed. python is available at https://www.python.org/
* Install emscripten http://kripken.github.io/emscripten-site/docs/getting_started/downloads.html to compile C++ to web assembly (wasm)
* Install CMake (https://cmake.org/download/)
* (Windows) Make sure to have make installed. https://sourceforge.net/projects/mingw-w64/files/latest/download

### Compile

* After installation, make sure you have  emcc, cmake, and python on your path. Emscripten provides instructions for adding path information during install.
* From the command line (or bash), go to the root directory of this repo and type `python build_rhino3dm.py`. If everything is configured correctly, you should have a compiled wasm, js, and html file after a couple minutes.

### Test

* Type `python serve.py`. This will run a simple web server which serves files in the artifacts directory
* Go to your browser and navigate to `http://localhost:8080/rhino3dm.html`
* For chrome, right click and select `inspect`. Click on the `console` tab and try typing in the following javascript
  * `sphere = new Module.Sphere([1,2,3],12);`
  * `brep = sphere.toBrep();`
  * `jsonobject = brep.encode();`
