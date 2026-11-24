# rhino3dm.js build instructions

## Get the source

The repository uses opennurbs, nanobind and Draco as submodules:

```commandline
git clone --recurse-submodules https://github.com/mcneel/rhino3dm.git
```

or, in an existing clone, `git submodule update --init --recursive`.

## Required tools

rhino3dm.js builds on macOS, Linux and Windows. [Current_Development_Tools.md](../../Current_Development_Tools.md)
lists the versions CI uses:

* [Python](https://www.python.org/) 3.10 or later, to run `bootstrap.py` and `build.py`
* [Emscripten](https://emscripten.org/) (emsdk)
* [CMake](https://cmake.org/) 3.21 or later
* [Node.js](https://nodejs.org/) 22 or later (24 for the wasm64 build), for the tests

Check them, and install what is missing (system-wide; asks for administrator rights):

```bash
python3 bootstrap.py js
python3 bootstrap.py js --install
```

Before building, put Emscripten on the path: `source /opt/emsdk/emsdk_env.sh` (`C:\emsdk\emsdk_env.bat`
on Windows). The [emscripten/emsdk](https://hub.docker.com/r/emscripten/emsdk) Docker image, which CI
uses, has it all: `docker run --rm -it -v "$PWD":/src -w /src emscripten/emsdk:5.0.7 bash`.

## Build

```bash
python3 build.py js
```

builds rhino3dm.js and rhino3dm.wasm (wasm32, the package's main build) into `build/js/dist`. The
other variants, each a preset in `CMakePresets.json`:

| `--variant` | output in `build/js/dist` | |
|---|---|---|
| `wasm32` (default) | `rhino3dm.js`, `rhino3dm.wasm` | Node.js and browsers |
| `wasm32-module` | `rhino3dm.module.js` | ES module for browsers and bundlers |
| `wasm64` | `wasm64/rhino3dm.js`, `wasm64/rhino3dm.wasm` | memory64: models over 4 GB; Chrome 133+, Firefox 134+, Node.js 24+ |
| `wasm64-module` | `wasm64/rhino3dm.module.js` | |

`--all` builds every variant, laid out as the npm package (with `package.json`, `README.md` and
`rhino3dm.d.ts`); `--debug` makes a debug build; `--minify` also writes `*.min.js` (needs
`npm install -g uglify-js`). Each variant has its own build tree in `build/js-<variant>`, so
rebuilding one is incremental.

## Test

```bash
cp build/js/dist/* tests/javascript/lib/
cd tests/javascript && npm i && npm test
```

## Related topics

- [Current_Development_Tools.md](../../Current_Development_Tools.md): the tools and versions CI uses
