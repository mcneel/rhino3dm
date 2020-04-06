# Build Instructions for .NET
### Get The Source

This repo uses [OpenNURBS](https://github.com/mcneel/opennurbs) and [pybind11](https://github.com/pybind/pybind11) as submodules, so you need to run another git command after you have cloned. `cd` into the new repository directory and run
  * `git submodule update --init`

## Install the Tools

CMake 3.12.1 is the minimum required CMake version.

* Mac
  * Install Homebrew (https://brew.sh/)
  * `brew install python2 cmake` (for Python 2.7 compile)
  * `brew install python3 cmake` (for Python 3.7 compile)
* Windows
  * This project uses Visual Studio 2017
  * Install the flavor of CPython that you prefer to work with from python.org
  * Install CMake (https://cmake.org/download/) and make sure that cmake.exe is added to the path
* Linux
  * Tested with Clang 3.8.0 on Linux Mint 18.3
  * Install CMake 3.12.1
  * `sudo aptitude install python2 python3 python2-dev python3-dev uuid uuid-dev`

## What needs to be compiled
The compilation process involves several different pieces that eventually make up rhino3dm as a whole
  * CMake is used to compile the opennurbs and pinvoke `C` functions into a .DLL, .DYLIB, or .SO depending on what platform you are running on. The compiled native code is compiled to a binary named `rhino3dm_native` or `librhino3dm_native` depending on the platform you are targetting.
  * A .NET project named methodgen is compiled and executed to generate the C# pInvoke declarations based on the native code exports
  * A .NET project named rhino3dm is compiled using msbuild or dotnet for .NET core compiles
  * On Windows, the rhino3dm_native.dll is embedded into the Rhino3dm.dll to allow for a single file

## Compile

* `python src/build_dotnet.py`
  * This will compile all of the pieces necessary to create rhino3dm.dll
  * If you pass `--core` as a command line argument (or are compiling on Linux). The .NET core tools are used instead for compilation. In this case you also need ot have .NET core installed on your computer.
