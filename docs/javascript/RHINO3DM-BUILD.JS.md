# rhino3dm.js build instructions
### Get the Source

If the pre-compiled libraries above do not work in your situation, you can compile the libraries from their source, the repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run
  * `git submodule update --init`

### Install the Tools

* Make sure you have python 2.7.12 or newer installed. python is available at https://www.python.org/
* Install emscripten http://kripken.github.io/emscripten-site/docs/getting_started/downloads.html to compile C++ to web assembly (wasm)
* Install CMake (https://cmake.org/download/)
* (Windows) Make sure to have make installed. https://sourceforge.net/projects/mingw-w64/files/latest/download

### Compile

* After installation, make sure you have  emcc, cmake, and python on your path. Emscripten provides instructions for adding path information during install.
* From the command line (or bash), go to the `src` directory of this repo and type `python build_javascript_lib.py`. If everything is configured correctly, you should have a compiled wasm, js, and html file after a couple minutes. These files will be built to the `src/build/artifacts_js` directory.

### Test

* Type `python serve_javascript_lib.py`. This will run a simple web server which serves files in the artifacts directory
* Go to your browser and navigate to `http://localhost:8080/rhino3dm.html`
* For chrome, right click and select `inspect`. Click on the `console` tab and try typing in the following javascript
  * `sphere = new Module.Sphere([1,2,3],12);`
  * `brep = sphere.toBrep();`
  * `jsonobject = brep.encode();`
