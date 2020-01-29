# rhino3dm build process outline [draft]

Last updated by dan@mcneel.com on January 29, 2020

**WARNING**: This is currently a work-in-progress draft.

The build process for rhino3dm is run on [CircleCi](https://circleci.com/).  All changes to build configuration settings should be made from the [CircleCI rhino3dm project](link needed).  That said, this process can be tested locally.

## Overview

rhino3dm - a subset of RhinoCommon - is a collection of wrapper libraries around the native openNURBS 3dm file IO library.  rhino3dm is published for the various platforms it supports as packages (NuGet for .NET, pip for python, etc).  

We build and publish rhino3dm for a number of platforms:

- Windows (Desktop)
- macOS (Desktop)
- iOS
- Android
- JavaScript
- Python (CPython)

The steps to create rhino3dm are as follows:

1. Check for the required build tools ([bootstrap.py](#bootstrap))
2. Setup the native library platform projects by generating them using CMake ([setup.py](#setup.py))
3. Build the native library projects and wrapper projects ([build.py](#build.py))
4. Build and publish the various packages (CircleCI?)

## Scripts

These scripts are used to setup and build rhino3dm:

- *script/bootstrap*.py - checks for (and download) the required tools
- *script/setup.py* - generates the platform-specific project files for the native libraries
- *script/build.py* - builds the native library project(s) and the wrapper projects

The scripts can be run from Python 2 or Python 3.

The following table's first column shows the platforms you would like to target.  The right three columns show the operating system you are using.  

|            |    Windows/WLS     |       Linux        |       macOS        |
| ---------: | :----------------: | :----------------: | :----------------: |
|    Windows |      planned       |                    |                    |
|      Linux |                    |      planned       |                    |
|      macOS |                    |                    | :white_check_mark: |
|        iOS |                    |                    |      planned       |
|    Android |      planned       |      planned       |      planned       |
| JavaScript | :white_check_mark: | :white_check_mark: | :white_check_mark: |
|     Python |      planned       |      planned       |      planned       |

 As you can see, targeting the three desktop platforms requires that you run the scripts on those operating systems (is that true, can macOS build for Linux?)  Android, JavaScript, and Python targets can be built from any platform.  With the exception of the Windows (Desktop) target, Windows users must use the Windows Linus Subsystem (WLS).

### Bootstrap.py

There many necessary tools to build rhino3dm.  These are listed in the [Current Developer Tools](../Current%20Developer%20Tools.md) in the root folder of this repository.  The _bootstrap.py_ script reads from the _Current Development Tools.md files_ and checks the system to make sure these tools are present.  You can also use this script to download the tools if you do not have them (when available).

