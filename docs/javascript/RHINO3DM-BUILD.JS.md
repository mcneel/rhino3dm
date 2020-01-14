# rhino3dm.js build instructions
### Get the Source

If the pre-compiled libraries above do not work in your situation, you can compile the libraries from their source, the repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run

```commandline
git submodule update --init
```

### Required Tools

Compiling *rhino3dm.js* can be done on macOS, Linux and Windows (via [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10)). The following tools are required...

* [Python](https://www.python.org/) 2 (>2.17.12)
* [Emscripten](https://emscripten.org/) - See Emscripten's [Getting started guide](https://emscripten.org/docs/getting_started/downloads.html#platform-notes-installation-instructions-sdk) or WebAssembly's [Developer's Guide](https://webassembly.org/getting-started/developers-guide/) .
* [CMake](https://cmake.org/) (>3.12.2) - _**Note:** The version of CMake distributed with Ubuntu 18.04 LTS isn't new enough so you'll have to [build it from source](https://cmake.org/install/). This may also be true for other package managers._

### bootstrap

The `script/bootstrap.py` script can be used to check your system for (and, in some cases, download) the necessary tools for a specific build target.  For example, you can run:

```bash
$ python bootstrap.py --platform js
```

to check for all the tools needed to build the javascript version of rhino3dm.

`bootstrap.py` supports Python 2 and 3 and can be run from Windows, macOS, or Linux.

### Compile

After installation, make sure you have `emcmake`, `make`, and `python` on your path. Emscripten provides instructions for [adding the Emscripten tools to your PATH after install](https://emscripten.org/docs/getting_started/downloads.html#installation-instructions).

```bash
$ cd src
$ python build_javascript.py
```

The build might take a few minutes, but if everything is configured correctly you should now have `rhino3dm.js`, `rhino3dm.wasm` and several samples in the `src/build/artifacts_js` directory.

### Test

* `cd` to the `docs/javascript/samples` folder.

* Type `python3 -m http.server`. This will run a simple web server which serves files in the artifacts directory

* Go to your browser and navigate to `http://localhost:8080/rhino3dm.html`

* For chrome, right click and select `inspect`. Click on the `console` tab and try typing in the following javascript
  ```js
  > sphere = new _rhino3dm.Sphere([1,2,3], 12);
  > brep = sphere.toBrep();
  > jsonobject = brep.encode();
  ```

## Related Topics

- [Current Development Tools](../../Current Development Tools.md) - This file (which is used by bootstrap) lists the current build tools and versions we have tested with.