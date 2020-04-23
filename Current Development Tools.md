# Current Development Tools (rhino3dm)

Last updated by dan@mcneel.com on April 23, 2020

:warning: Change this file at your own risk. This file is read by scripts to determine the required development tools and versions of those tools.  Though it is human-readable, it is part of build processes. Renaming or reformatting this file may cause undesired results.

## Windows

### msbuild

We are currently using msbuild 16.4.0.56107.  Scripts read this:

`msbuild_currently_using = 16.4.0.56107`
`msbuild_archive_url_windows = https://visualstudio.microsoft.com/downloads/`
`msbuild_install_notes_windows = MSBuild is installed as part of Visual Studio.`

## Linux

### dotnet

We are currently using the .NET Core SDK 2.2.402.  Scripts read this:

`dotnet_currently_using = 2.2.402`
`dotnet_install_notes_linux = On Ubuntu, you can install the dotnet SDK using the apt-get package manager.  Follow these instructions: https://docs.microsoft.com/en-us/dotnet/core/install/linux-package-manager-ubuntu-1804`

## iOS

### Xamarin.iOS

We are currently using Xamarin.iOS 13.14.1.39.  Scripts read this:

`xamios_currently_using = 13.14.1.39`
`xamios_archive_url = https://dl.xamarin.com/MonoTouch/Mac/xamarin.ios-13.14.1.39.pkg`

## Android

### Android NDK

We are currently using the Android NDK 21.0.6113669 (r21).  Scripts read this:

`ndk_currently_using = 21.0.6113669`
`ndk_archive_url_macos = https://dl.google.com/android/repository/android-ndk-r21-darwin-x86_64.zip`
`ndk_install_notes_macos = To install the NDK, decompress the folder, then move it to /Users/~/Library/Developer/Xamarin/android-ndk/.  On macOS Catalina, you will need to remove the Apple Quarantine extended attributes on all fiules in this folder.  This can be done by running xattr -dr com.apple.quarantine {path_to_ndk}.  Next, add the following line to your ~/.bash_profile: export ANDROID_NDK="/Users/~/Library/Developer/Xamarin/android-ndk/android-ndk-r21/"`

### Xamarin.Android

We are currently using the Xamarin.Android Framework 10.1.3.  Scripts read this:

`xamandroid_currently_using = 10.1.3`
`xamandroid_archive_url = https://dl.xamarin.com/MonoforAndroid/Mac/xamarin.android-10.1.3.7.pkg`

## Javascript

### Emscripten

We are currently using Emscripten 1.39.5. Scripts read this:

`emscripten_currently_using = 1.39.5`
`emscripten_install_notes = To install Emscripten, follow these instructions: https://emscripten.org/docs/getting_started/downloads.html. You must activate PATH and other environment variables in the current terminal. You can verify the installation following these instructions: https://emscripten.org/docs/building_from_source/verify_emscripten_environment.html`
`emscripten_install_notes_windows =  To install Emscripten, follow these instructions: https://emscripten.org/docs/getting_started/downloads.html. You must activate PATH and other environment variables in the current terminal.  You can verify the installation following these instructions: https://emscripten.org/docs/building_from_source/verify_emscripten_environment.html.  Be sure to use the --global flag when running the activate batch file to set all the path variables correctly.`

## Python (TODO)

TODO: Not yet supported from our build scripts, but planned.

## Shared

The following are shared between multiple platform targets...

### Git

We are currently using Git 2.17.1. Scripts read this:

`git_currently_using = 2.17.1`
`git_install_notes_macos = You can download and install git from https://git-scm.com/downloads.  In order to run the pkg installer, you must right-click and select Open from the drop-down menu to bypass Gatekeeper checks.`
`git_install_notes_windows = You can download and install git from https://git-scm.com/downloads`

### Python 2

We are currently using Python 2.7.17. Scripts read this:

`python2_currently_using = 2.7.17`
`python2_archive_url_macos = https://www.python.org/ftp/python/2.7.17/python-2.7.17-macosx10.9.pkg`
`python2_archive_url_windows = https://www.python.org/ftp/python/2.7.17/python-2.7.17.amd64.msi`
`python2_install_notes_linux = On Ubuntu, you can install python 2 using sudo apt install python`

### Python 3

We are currently using Python 3.7.1. Scripts read this:

`python3_currently_using = 3.7.1`
`python3_archive_url_macos = https://www.python.org/ftp/python/3.7.1/python-3.7.1-macosx10.9.pkg`
`python3_archive_url_windows = https://www.python.org/ftp/python/3.7.1/python-3.7.1-amd64.exe`
`python3_install_notes_linux = On Ubuntu, you can install python 3 using sudo apt install python3`

### CMake

We are currently using CMake 3.16.2. Scripts read this:

`cmake_currently_using = 3.16.2`
`cmake_archive_url_macos = https://github.com/Kitware/CMake/releases/download/v3.16.2/cmake-3.16.2-Darwin-x86_64.dmg`
`cmake_archive_url_windows = https://github.com/Kitware/CMake/releases/download/v3.16.2/cmake-3.16.2-win64-x64.msi`
`cmake_install_notes_macos = Once the CMake.app is installed, launch it, and follow the directions in Tools > How to Install for Command Line Use`
`cmake_install_notes_linux = On Ubuntu, you can install CMake using: sudo snap install cmake --classic`
`cmake_install_notes_windows = When installing, be sure to check the box to add CMake to the system path.`

### macOS

We are currently using macOS 10.15.3. Scripts read this:

`macos_currently_using = 10.15.3`
`macos_install_notes = The exact version likely does not matter.  You may not need to update/roll-back macOS if your version is close enough.`

### Xcode

We are currently using Xcode 11.4. Scripts read this:

`xcode_currently_using = 11.4`
`xcode_install_notes = Xcode can be downloaded for free from the macOS App Store or from https://developer.apple.com/download/ (Apple developer ID required for the latter option).`

### Mono Framework MDK

We are currently using the Mono Framework MDK 6.8.0.123.  Scripts read this:

`mdk_currently_using = 6.8.0.123`
`mdk_archive_url = https://dl.xamarin.com/MonoFrameworkMDK/Macx86/MonoFramework-MDK-6.8.0.123.macos10.xamarin.universal.pkg`
`mdk_install_notes = The Mono.framework does not seem to be installed in the /Library/Frameworks folder or the current version is not set.`

---

## Related Topics

- [Scripts README.md](scripts/README.md) for an overview of the build scripts that use this file
- [README.md](README.md) for an overview of the rhino3dm project
