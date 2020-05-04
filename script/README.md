# rhino3dm build scripts

Last updated by dan@mcneel.com on May 4, 2020

This folder contains scripts that can used to build rhino3dm for the supported platforms.  The build process for rhino3dm is run on [CircleCi](https://circleci.com/).  That said, this process can be tested locally by using these scripts.

**WARNING**: Targeting python is not yet supported from these scripts, but is planned.

## Overview

rhino3dm - a subset of RhinoCommon - is a collection of wrapper libraries around the native openNURBS 3dm file IO library.  rhino3dm is published for the various platforms it supports as packages (NuGet for .NET, pip for python, etc).  

We build and publish rhino3dm for a number of platforms:

- Windows (Desktop)
- macOS (Desktop)
- Linux (Desktop)
- iOS
- Android
- JavaScript
- Python (CPython)

The steps to create rhino3dm are as follows:

1. Check for the required build tools ([bootstrap.py](#bootstrappy))
2. Setup the native library platform projects by generating them using CMake ([setup.py](#setuppy))
3. Build the native library projects and wrapper projects ([build.py](#buildpy))
5. Build and publish the various packages (CircleCI supported for Python and Javascript).  Support for the .NET projects is coming soon.

## Scripts

These scripts are used to setup and build rhino3dm:

- *script/bootstrap.py* - checks for (and downloads) the required tools
- *script/setup.py* - generates the platform-specific project files for the native libraries
- *script/build.py* - builds the native library project and wrapper project(s)

The scripts can be run from Python 2 or Python 3.

The following table's first column shows the platform you would like to target.  The right three columns show the operating system you are running.

<table>
  <thead>
    <tr>
    <th></th>
    <th></th>
    <th colspan="3" align="center">Your Operating System</th>
  </tr>
    <tr>
      <td colspan=2></td>
      <td align="center">Windows</td>
      <td align="center">Linux</td>
      <td align="center">macOS</td>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td rowspan=5>.NET</td>
      <td align="right">Windows</td>
      <td align="center">✅</td>
      <td></td>
      <td></td>
    </tr>
    <tr>
      <td align="right">Linux</td>
      <td></td>
      <td align="center">✅</td>
      <td></td>
    </tr>
    <tr>
      <td align="right">macOS</td>
      <td></td>
      <td></td>
      <td align="center">✅</td>
    </tr>
    <tr>
      <td align="right">iOS</td>
      <td></td>
      <td></td>
      <td align="center">✅</td>
    </tr>
    <tr>
      <td align="right">Android</td>
      <td></td>
      <td></td>
      <td align="center">✅</td>
    </tr>
    <tr>
      <td align="right" colspan=2>JavaScript</td>
      <td align="center">✅*</td>
      <td align="center">✅</td>
      <td align="center">✅</td>
    </tr>
    <tr>
      <td align="right" colspan=2>Python</td>
      <td align="center"><em>planned</em></td>
      <td align="center"><em>planned</em></td>
      <td align="center"><em>planned</em></td>
    </tr>
  </tbody>
</table>

*requires [Windows Linux Subsystem](https://docs.microsoft.com/en-us/windows/wsl/install-win10)

As you can see, targeting the three desktop platforms requires that you run the scripts on those operating systems. JavaScript and Python targets can be built on any operating system (or that is the plan).  With the exception of the Windows (Desktop) target, Windows users must use the Windows Linus Subsystem (WLS).

### bootstrap.py

There many necessary tools to build rhino3dm.  These are listed in the [Current Developer Tools](../Current%20Developer%20Tools.md) in the root folder of this repository.  The _bootstrap.py_ script reads from the _Current Development Tools.md files_ and checks the system to make sure these tools are present.  You can also use this script to download the tools if you do not have them (when available).

You can run the _bootstrap.py_ script like this:

`python bootstrap.py -p js`

to check for all the necessary tools to build for JavaScript.

### setup.py

The _setup_ script uses [CMake](https://cmake.org/) to write the platform-specific native library projects.  These projects are generated into the _src/build/[platform]/_ folder where they are used to build the native libraries which are, in turn, used by the wrapper projects.

You can run the _setup.py_ script like this:

`python setup.py -p js`

to generate the project files to build for JavaScript.

### build.py

Once you have run the _setup.py_ script for a particular platform, you can use the _build.py_ script to build the native library and there wrapper library (for .NET builds).  The native library project is built into the same _src/build/[platform]/_ folder, sometimes in a subfolder, depending on the platform being targeted.

You can run the _setup.py_ script like this:

`python setup.py -p js`

to build the library for JavaScript.

## Wrapper projects

There are .NET wrapper projects that wrap the native libraries in the _src/dotnet_ folder...

- _Rhino3dm.csproj_ - for Windows and macOS
- _Rhino3dm.core.csproj_ - for Linux (and dotnet core)
- _Rhino3dm.iOS.csproj_ - for iOS
- _Rhino3dm.Android.csproj_ - for Android

### Package

**TODO**: We plan to package these projects as part of a continuous delivery process, but this has not yet been done for all platforms.

### Publish

**TODO**: We plan to publish these projects as part of a continuous delivery process, but this has not yet been done for all platforms.

The final destinations for these packages is: 

- https://www.nuget.org/profiles/McNeel for .NET builds (currently, these are being built from internal source, but we plan to switch to use this repository in the near future.)
- https://www.npmjs.com/package/rhino3dm for Javascript builds
- https://pypi.org/project/rhino3dm/ for Python builds.

---

## Related Topics

- [Current Development Tools (Rhino3dm)](../Current%20Development%20Tools.md)
