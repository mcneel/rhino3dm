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

⭐️ _**TIP**: A great way to get a build environment that's ready to go for building rhino3dm.js is to use a [Dev Container](#dev-container) in VS Code!_ 🐳

## Scripts

A number of scripts are used to setup and build rhino3dm:

- *script/bootstrap.py* - checks for (and downloads) the required tools
- *script/setup.py* - generates the platform-specific project files using CMake
- *script/build.py* - builds the library project(s)

These scripts support Python 2 and 3 on Windows (linux subsystem), macOS, or Linux (Ubuntu).

### Running scripts on Windows

You have to make sure that the Emscripten environment is set up. Here are the steps to install Emscripten and the make tool to make it work. After that you can set up the environment by running `emsdk_env.bat`.

```
git clone https://github.com/emscripten-core/emsdk.git
cd emsdk
git pull
emsdk install latest
emsdk activate latest
emsdk install mingw-4.6.2-32bit
emsdk activate mingw-4.6.2-32bit
```

### bootstrap.py

The `script/bootstrap.py` script can be used to check your system for (and, in some cases, download) the necessary tools for a specific build target.  For example, you can run:

```bash
$ python3 bootstrap.py --platform js
```

to check for all the tools needed to build the javascript version of rhino3dm.

### setup.py

The _setup.py_ script uses CMake to generate the make files necessary to build the project.  These projects are generated into _build/javascript_ folder.  To setup a JavaScript build, you can run:

```bash
$ python3 setup.py --platform js --verbose
```

### build.py

The _build.py_ script run `make` to build the _rhino3dm.js_ and _rhino3dm.wasm_ files to the _build/javascript/artifacts\_js_ folder.  To build, run:

```bash
$ python3 build.py --platform js --verbose --overwrite
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

## Dev Container

Getting the toolchain set up for building rhino3dm.js can be painful. Visual Studio Code can help by using a preconfigured Docker container as a development environment. 

macos setup
1. Install Docker desktop: https://docs.docker.com/desktop/install/mac-install/ and start the docker app
2. Install vscode extension for remote: https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack
3. In a terminal, run the following command:

    `docker run --rm -dit -v $(pwd):/src emscripten/emsdk:2.0.10`

    `--rm` deletes the container when it's stopped
    
    `-dit` runs the container as a daemon (in the background) but still allows interaction
    
    `-v $(pwd):/src` maps the current directory (rhino3dm) to /src in the container

4. In vs code, open remote explorer
5. You should see a container in the list. Right-click on it and select "Attach to Container"
6. A new vscode window will appear, but it will not have any directory associated with it. Open the `/src` folder.
7. Open a terminal in vscode and navigate into the `/script` directory
8. Run the following command to setup the rhino3dm.js build:
`python3 setup.py --platform js`
9. Run the following command to build rhino3dm.js:

    `python3 build.py --platform js --verbose --overwrite`

There are two options (assuming you already have Docker installed and running)...

1. Open the project in VS Code and run the **Remote-Containers: Reopen Folder in Container** command to start a container with the Emscripten toolchain set up and the current directory mapped to a volume.
1. Alternatively, for slightly faster build times (I/O between the container and the host filesystem can slow things down), open VS Code and run the **Remote-Containers: Clone Repository in Container Volume...** command. Enter `mcneel/rhino3dm` in the input box and press <kbd>Enter</kbd>. Note, that if you need to copy any files from the container volume to the host filesystem, you can right-click on them in the Explorer side bar and choose _Download_.

In both cases, once VS Code has relaunched you may need to open a new terminal (**Terminal** > **New Terminal**). This terminal will be running inside the container and that's where you'll run the [setup](#setuppy) and [build](#buildpy) scripts.

## Related Topics

- [Current Development Tools](../../Current%20Development%20Tools.md) - This file (which is used by bootstrap) lists the current build tools and versions we have tested with.
