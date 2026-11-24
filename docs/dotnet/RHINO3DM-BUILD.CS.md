# Build Instructions for .NET

## Get the source

This repo uses [OpenNURBS](https://github.com/mcneel/opennurbs), [nanobind](https://github.com/wjakob/nanobind) and Draco as submodules:

```commandline
git clone --recurse-submodules https://github.com/mcneel/rhino3dm.git
```

## Install the tools

* CMake 3.21 or later
* the .NET 10 SDK (`global.json` sets the oldest)
* a C++17 compiler: Xcode on macOS; Visual Studio 2022 on Windows, with the v142 toolset component
  for the x64 library (`MSVC v142 - VS 2019 C++ x64/x86 build tools`); GCC or Clang on Linux
* Python 3.10 or later, to run the scripts

[Current_Development_Tools.md](../../Current_Development_Tools.md) lists the versions CI uses.
`python bootstrap.py dotnet` checks them; `python bootstrap.py dotnet --install` installs CMake
and the .NET SDK.

## What is compiled

* **librhino3dm_native**: opennurbs and RhinoCommon's C functions (`src/librhino3dm_native`),
  compiled with CMake into a `.dll`, `.dylib` or `.so`.
* **methodgen** (`src/methodgen`) reads the C functions and writes the C# P/Invoke declarations
  (`src/dotnet/AutoNativeMethods.cs`, `AutoNativeEnums.cs`; not checked in).
* **Rhino3dm.dll** (`src/dotnet/Rhino3dm.csproj`): RhinoCommon's C# for rhino3dm, built for
  netstandard2.0, net48 and net10.0. The NuGet package carries each platform's native library under
  `runtimes/<rid>/native`.

## Compile

```commandline
python build.py dotnet
```

runs methodgen, builds the native library for this machine into `build/native/<rid>` (`win-x64`,
`win-arm64`, `osx` (universal), `linux-x64`, `linux-arm64`), then Rhino3dm.dll. Options:

* `--arch arm64` / `--arch x64`: on Windows, the other architecture (cross-compiled)
* `--library-only`: the native library only
* `--debug`: Debug builds

`python build.py methodgen` regenerates the P/Invoke declarations alone. The native build trees
are CMake presets (`build/dotnet-<os>`); `dotnet pack src/dotnet/Rhino3dm.csproj` packs every native
library present in `build/native`.

## Test

```commandline
cd tests/dotnet && dotnet test
```
