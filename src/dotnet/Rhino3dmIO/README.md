# Rhino3dmIO build process outline

Last updated by dan@mcneel.com on October 2nd, 2017

The build process for Rhino3dmIO is run on http://teamcity.mcneel.com (requires VPN).  All changes to build configuration settings should be made from the [Teamcity Rhino3dmIO project](http://teamcity.mcneel.com/admin/editProject.html?projectId=RhinoWip_Rhino3dmIO).  That said, this process can be tested locally.

## Overview

Rhino3dmIO - a subset of RhinoCommon - is a .NET wrapper library around the native openNURBS 3dm file IO library.  Rhino3dmIO is published as a NuGet package.  The final destination for these packages is: https://www.nuget.org/profiles/McNeel

We build and publish Rhino3dmIO for a number of platforms:

- Windows
- macOS
- iOS
- Android

The steps to create Rhino3dmIO are as follows:

1. Check for the required build tools ([bootstrap](#bootstrap))
2. Setup the native library platform projects by generating them using gyp ([setup](#setup))
3. Build the native library projects ([cibuild](#build))
4. Build the [wrapper .NET projects](#wrapper-projects) (TeamCity)
5. Build and publish the NuGet packages (TeamCity)

## Scripts

A number of scripts are used to setup and build Rhino3dmIO:

- *script/bootstrap* - checks for (and downloads) the required tools
- *script/setup* - generates the platform-specific project files for the native libraries
- *script/cibuild* - builds the native library project(s)

### Bootstrap

There are a number of necessary tools to build Rhino3dmIO.  These are listed in the [Current Developer Tools (Rhino3dmIO)](Current%20Developer%20Tools.md) in this folder and [Current Development Tools (macOS)](../../../../../build/mac/Current%20Development%20Tools.md).  The _bootstrap_ script reads from the _Current Development Tools.md files_ and checks the system to make sure these tools are present.  You can also use this script to download the tools if you do not have them (when available).

#### Windows

From `cmd.exe` you can run the script like this:

`python bootstrap -c all`

to check all supported platforms.

#### macOS

From `Terminal.app` you can run the script like this:

`./bootstrap -c all`

### Setup

The _setup_ script uses [gyp (Generate Your Projects)](https://gyp.gsrc.io/) to read the .gyp files in the gyp folder and generates the platform-specific native library projects.  These projects are moved into the _rhino/src4/bin/Rhino3dmIO_ folder where they are used to build the native libraries.

#### Windows

From `cmd.exe` you can run the script like this:

`python setup -p windows` or `python setup -p android`

to check all supported platforms.

#### macOS

From `Terminal.app` you can run the script like this:

`./setup -p ios` or `./setup -p android`

### Build

The _cibuild_ build is used by TeamCity to build the respective native libraries.  (The wrapper libraries are then built from TeamCity projects.)

#### Windows

**TODO**: `cibuild` is not yet working on Windows.

#### macOS

From `Terminal.app` you can run the script like this:

`./cibuild -p macos` or `./cibuild -p ios` or `./cibuild -p android`


## Wrapper projects

There are .NET wrapper projects that wrap the native libraries in the parent folder...

- _Rhino3dmIO.csproj_ - for Windows and macOS
- _Rhino3dmIO.iOS.csproj_ - for iOS
- _Rhino3dmIO.Android.csproj_ - for Android

Because these projects are build from TeamCity, they do not run the scripts listed above, which are instead run as part of the TeamCity process.  However, in the case of the mobile projects (iOS and Android) there are "Custom Commands" added to each of the respective projects that can be run from within _Visual Studio for Mac_ from the _Project_ menu, in case you would like to build this locally for some reason.

### Package

The packaging steps for the NuGet packages happens on TeamCity.  See the [Rhino3dmIO project](http://teamcity.mcneel.com/admin/editProject.html?projectId=RhinoWip_Rhino3dmIO) for details.

### Publish

The publishing steps for the NuGet packages happens on TeamCity.  See the [Rhino3dmIO project](http://teamcity.mcneel.com/admin/editProject.html?projectId=RhinoWip_Rhino3dmIO) for details.

The final destination for these packages is: https://www.nuget.org/profiles/McNeel.

---

## Related Topics

- [Current Development Tools (macOS)](../../../../../build/mac/Current%20Development%20Tools.md)
- [Current Development Tools (Rhino3dmIO)](Current%20Development%20Tools.md)
