# rhino3dm.js build instructions
### Get the Source

If the pre-compiled libraries above do not work in your situation, you can compile the libraries from their source, the repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run

```commandline
git submodule update --init
```

### Install the Tools

Compiling rhino3dm.js can be done on macOS, Linux and Windows (via [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10)). The following tools are required...

* Python 2 (>2.17.12)
* Emscripten
* CMake (>3.12.2)

See Emscripten's [Getting started guide](https://emscripten.org/docs/getting_started/downloads.html#platform-notes-installation-instructions-sdk) or WebAssembly's [Developer's Guide](https://webassembly.org/getting-started/developers-guide/) for more info.

_**Note:** The version of CMake distributed with Ubuntu 18.04 LTS isn't new enough so you'll have to [build it from source](https://cmake.org/install/). This may also be true for other package managers._

### Compile

After installation, make sure you have `emcmake`, `make`, and `python` on your path. Emscripten provides instructions for [adding the Emscripten tools to your PATH after install](https://emscripten.org/docs/getting_started/downloads.html#installation-instructions).

```commandline
$ cd src
$ python build_javascript_lib.py
```

The build might take a few minutes, but if everything is configured correctly you should now have `rhino3dm.js`, `rhino3dm.wasm` and several samples in the `src/build/artifacts_js` directory.

### Test

* Type `python serve_javascript_lib.py`. This will run a simple web server which serves files in the artifacts directory
* Go to your browser and navigate to `http://localhost:8080/rhino3dm.html`
* For chrome, right click and select `inspect`. Click on the `console` tab and try typing in the following javascript
  ```js
  > sphere = new Module.Sphere([1,2,3],12);
  > brep = sphere.toBrep();
  > jsonobject = brep.encode();
  ```
