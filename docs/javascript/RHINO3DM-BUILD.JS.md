# rhino3dm.js build instructions
### Get the Source

If the pre-compiled libraries above do not work in your situation, you can compile the libraries from their source, the repo uses OpenNURBS as a submodule, so you need to run a couple more git commands after you have cloned. `cd` into the new repository directory and run

```commandline
git submodule update --init
```

## Required Tools

Compiling *rhino3dm.js* can be done on macOS, Linux and Windows (via [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10)). The following tools are required...

* [Python](https://www.python.org/) 2 (>2.17.12) or 3 (> 3.6.9)
* [Emscripten](https://emscripten.org/) - See Emscripten's [Getting started guide](https://emscripten.org/docs/getting_started/downloads.html#platform-notes-installation-instructions-sdk) or WebAssembly's [Developer's Guide](https://webassembly.org/getting-started/developers-guide/) .
* [CMake](https://cmake.org/) (>3.12.2) - _**Note:** The version of CMake distributed with Ubuntu 18.04 LTS isn't new enough so you'll have to [build it from source](https://cmake.org/install/). This may also be true for other package managers._

## Scripts

A number of scripts are used to setup and build rhino3dm:

- *script/bootstrap.py* - checks for (and downloads) the required tools
- *script/setup.py* - generates the platform-specific project files using CMake
- *script/cibuild.py* - builds the library project(s)

These scripts support Python 2 and 3 on Windows (linux subsystem), macOS, or Linux (Ubuntu).

### bootstrap.py

The `script/bootstrap.py` script can be used to check your system for (and, in some cases, download) the necessary tools for a specific build target.  For example, you can run:

```bash
$ python3 bootstrap.py --platform js
```

to check for all the tools needed to build the javascript version of rhino3dm.

### setup.py

The _setup.py_ script uses CMake to generate the make files necessary to build the project.  These projects are generated into _build/javascript_ folder.  To setup a JavaScript build, you can run:

```bash
$ python3 setup.py --platform js
```

### build.py

The _build.py_ script run `make` to build the _rhino3dm.js_ and _rhino3dm.wasm_ files to the _build/javascript/artifacts\_js_ folder.  To build, run:

```bash
$ python3 build.py --platform js --overwrite
```

The build might take a few minutes, but if everything is configured correctly you should now have _rhino3dm.js_ and _rhino3dm.wasm_ in the _build/javascript/artifacts\_js_ folder.  The script also copies these files to the _docs/javascript/samples/resources_ folder where they can be used for testing.  

## Test

* `cd` to the `docs/javascript/samples` folder.

* Type `python3 -m http.server`. This will run a simple web server which serves files in the artifacts directory

* Go to your browser and navigate to `http://localhost:8080/rhino3dm.html`

* For chrome, right click and select `inspect`. Click on the `console` tab and try typing in the following javascript:
  
```js
> sphere = new _rhino3dm.Sphere([1,2,3], 12);
> brep = sphere.toBrep();
> jsonobject = brep.encode();
```

## Related Topics

- [Current Development Tools](../../Current Development Tools.md) - This file (which is used by bootstrap) lists the current build tools and versions we have tested with.