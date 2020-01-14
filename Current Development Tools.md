# Current Development Tools (rhino3dm)

Last updated by dan@mcneel.com on January 13, 2020

:warning: Change this file at your own risk. This file is read by scripts to determine the required development tools and versions of those tools.  Though it is human-readable, it is part of build processes. Renaming or reformatting this file may cause undesired results.

## Shared

The following are shared between multiple platform targets...

#### macOS

We are currently using macOS 10.15.2. Scripts read this:

`macos_currently_using = 10.15.2`
`macos_install_notes = The exact version likely does not matter.  You may not need to update/roll-back macOS if your version is close enough.`

#### Git

We are currently using Git 2.17.1. Scripts read this:

`git_currently_using = 2.17.1`
`git_install_notes_macos = You can download and install git from https://git-scm.com/downloads.  In order to run the pkg installer, you must right-click and select Open from the drop-down menu to bypass Gatekeeper checks.`
`git_install_notes_windows = You can download and install git from https://git-scm.com/downloads`

#### Python

We are currently using Python 2.7.17. Scripts read this:

`python_currently_using = 2.7.17`
`python_archive_url_macos = https://www.python.org/ftp/python/2.7.17/python-2.7.17-macosx10.9.pkg`
`python_archive_url_windows = https://www.python.org/ftp/python/2.7.17/python-2.7.17.amd64.msi`
`python_install_notes_linux = On Ubuntu, you can install python 2.7 using sudo apt install python`

#### Xcode

We are currently using Xcode 11.2.1. Scripts read this:

`xcode_currently_using = 11.2.1` 
`xcode_install_notes = Xcode can be downloaded for free from the macOS App Store or from https://developer.apple.com/download/ (Apple developer ID required for the latter option).`

## Android (TODO)

TODO

## Javascript

#### Emscripten

We are currently using Emscripten 1.39.5. Scripts read this:

`emscripten_currently_using = 1.39.5`
`emscripten_install_notes = To install Emscripten, follow these instructions: https://emscripten.org/docs/getting_started/downloads.html. You must add emscripten to your default shell PATH variable. You can verify the installation following these instructions: https://emscripten.org/docs/building_from_source/verify_emscripten_environment.html`
`emscripten_install_notes_windows =  To install Emscripten, follow these instructions: https://emscripten.org/docs/getting_started/downloads.html. You must add emscripten to your default shell PATH variable. You can verify the installation following these instructions: https://emscripten.org/docs/building_from_source/verify_emscripten_environment.html.  Be sure to use the --global flag when running the activate batch file to set all the path variables correctly.`

#### CMake

We are currently using CMake 3.16.2. Scripts read this:

`cmake_currently_using = 3.16.2`
`cmake_archive_url_macos = https://github.com/Kitware/CMake/releases/download/v3.16.2/cmake-3.16.2-Darwin-x86_64.dmg`
`cmake_archive_url_windows = https://github.com/Kitware/CMake/releases/download/v3.16.2/cmake-3.16.2-win64-x64.msi`
`cmake_install_notes_macos = Once the CMake.app is installed, launch it, and follow the directions in Tools > How to Install for Command Line Use`
`cmake_install_notes_linux = On Ubuntu, you can install CMake using: sudo snap install cmake --classic`
`cmake_install_notes_windows = When installing, be sure to check the box to add CMake to the system path.`

## Linux (TODO)

TODO

## macOS (TODO)

TODO

## Python (TODO)

TODO

## Windows (TODO)

TODO


---

## Related Topics

- [README.md](README.md) for an overview of the rhino3dm project
