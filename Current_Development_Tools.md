# Current Development Tools (rhino3dm)

Last updated by luis@mcneel.com on November 24, 2026

:warning: `bootstrap.py` reads this file. The `` `key = value` `` lines are its input: keep their
spelling, and keep each on its own line. The prose around them is for people.

Keys follow rhino's bootstrap (`build/mac/Current Development Tools.md` in the rhino repository):

- `currently_using_<tool>`: the version CI uses and `bootstrap.py --install` installs. Another
  version is a warning.
- `minimum_<tool>`: the oldest version that builds rhino3dm. An older one is an error.
- `<tool>_archive_url_<os>_<arch>`: where `--download` / `--install` get it (`<os>` is `macos`,
  `linux` or `windows`; `<arch>` is `x64`, `arm64` or `universal`).
- `<tool>_sha256_<os>_<arch>` / `<tool>_sha512_<os>_<arch>`: the publisher's checksum of that
  file; a download that does not match is deleted.

`python bootstrap.py` checks the tools for every library; `python bootstrap.py js` only those for
rhino3dm.js. `--install` installs system-wide (administrator / sudo), as rhino's bootstrap does.

| library | tools |
|---|---|
| Python wheel (`py`) | CMake, Python, a C++17 compiler (Xcode on macOS, Visual Studio on Windows) |
| rhino3dm.js (`js`) | CMake, Python, Emscripten, Node.js |
| .NET (`dotnet`) | CMake, .NET SDK, a C++17 compiler (Xcode on macOS, Visual Studio on Windows) |
| bindings generator (`generator`) | Python, libclang |

## CMake

The root `CMakeLists.txt` and `CMakePresets.json` (presets need 3.21). The Python wheel's
stable-ABI (abi3) module needs 3.26; scikit-build-core downloads a newer CMake from PyPI for it when
the installed one is older. Installed to `/Applications/CMake.app` with links in `/usr/local/bin` (macOS, where
Xcode finds them), `/opt/cmake-<version>` with links in `/usr/local/bin` (Linux), or with the MSI
(Windows).

`currently_using_cmake = 4.4.5`
`minimum_cmake = 3.21`
`cmake_archive_url_macos_universal = https://github.com/Kitware/CMake/releases/download/v4.4.5/cmake-4.4.5-macos-universal.tar.gz`
`cmake_sha256_macos_universal = 679e77a96b9ef9484cc6c1445255fc4f84206b48a65420efbedf16bdab55fbff`
`cmake_archive_url_linux_x64 = https://github.com/Kitware/CMake/releases/download/v4.4.5/cmake-4.4.5-linux-x86_64.tar.gz`
`cmake_sha256_linux_x64 = ed85135c5a5135b45e50423cb406dea04ca3338da0b8ef3c9ede963559680be0`
`cmake_archive_url_linux_arm64 = https://github.com/Kitware/CMake/releases/download/v4.4.5/cmake-4.4.5-linux-aarch64.tar.gz`
`cmake_sha256_linux_arm64 = 301d5063ab259577c1b57ce5490b1aab9ea5c56dc4c822f73499553dab94925f`
`cmake_archive_url_windows_x64 = https://github.com/Kitware/CMake/releases/download/v4.4.5/cmake-4.4.5-windows-x86_64.msi`
`cmake_sha256_windows_x64 = c18b52b4017c4e5ffcd7d516ea39c4e0ed37f58920b90a5c98a46280b1857145`
`cmake_archive_url_windows_arm64 = https://github.com/Kitware/CMake/releases/download/v4.4.5/cmake-4.4.5-windows-arm64.msi`
`cmake_sha256_windows_arm64 = fcbdbd8327f1416bb34c566f413a9f92f1e35b014909dee89ce42035a131e206`

## Python

Builds the wheel (`build.py py`, scikit-build-core) and runs `build.py`, `bootstrap.py` and the
generator. Wheels are built for 3.10, 3.11 and 3.12 (abi3, for 3.12 and later). Checked, not
installed: use python.org, your package manager or pyenv.

`currently_using_python = 3.12`
`minimum_python = 3.10`

## .NET SDK

Builds methodgen and Rhino3dm.dll. The oldest SDK that builds rhino3dm is pinned in the root
`global.json` (`rollForward: latestFeature`: that version or any later 10.0 SDK), and
`bootstrap.py` reads its minimum from there. CI and `--install` use the SDK below. Installed to `/usr/local/share/dotnet` (macOS), `/usr/share/dotnet` (Linux) or
`C:\Program Files\dotnet` (Windows).

`currently_using_dotnet = 10.0.404`
`dotnet_archive_url_macos_arm64 = https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-osx-arm64.tar.gz`
`dotnet_sha512_macos_arm64 = 1c9a13f625d0c08f08ef5a53597bababe3d2473527e98db2199210940c69dd5fc599a39a4d7c30ebf297ca2334a81b2f64b9bf05ff99585de9af4a5d85215e96`
`dotnet_archive_url_macos_x64 = https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-osx-x64.tar.gz`
`dotnet_sha512_macos_x64 = 88ca5dac50562b619f852b1b9845f85243f00434be52d2149db18ab4dc05f527f3fca8e1dac82e87b9eeb46a98de1ba4a6d612714caf2222f37b95aeebb611b6`
`dotnet_archive_url_linux_x64 = https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-linux-x64.tar.gz`
`dotnet_sha512_linux_x64 = 577d0eb17fec33d029104c5c63a1ab0accc3e28776274abef2218dbac5af18d2109b5406d26b31d76f5c245215e3ba0926a94c20246c672c41847c9e1eb1dc50`
`dotnet_archive_url_linux_arm64 = https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-linux-arm64.tar.gz`
`dotnet_sha512_linux_arm64 = 3533a3869da219cac45c567aadac3d8e96cdf1c6a04fe1c1d5fdeefea5d45af8424488f0144730d11b4a751f7ad546fb11e4c2b2725cd8401efee726711f8f11`
`dotnet_archive_url_windows_x64 = https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-win-x64.zip`
`dotnet_sha512_windows_x64 = 330ddd2665b43cd2e428216a0d8ea1ac7bb19b5434fdc530e62010dc3b52823a620bf95489d628e1ed6f5122401793ee4887cca4a019122361a1231497f632ee`
`dotnet_archive_url_windows_arm64 = https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-win-arm64.zip`
`dotnet_sha512_windows_arm64 = 55c041098c709c898d416eaea9a56ab1d17ee884167b2093a5b3d68935a283d15ea796a880b9cf281baccdab56eae1537fae91627f480a68dec081734f535cf5`

## Emscripten

Compiles rhino3dm.js. CI builds in the `emscripten/emsdk:<version>` container image. Installed
with emsdk, cloned at the matching tag into `/opt/emsdk` (macOS, Linux) or `C:\emsdk` (Windows);
emsdk verifies its own downloads. `build.py js` needs `EMSDK` set: `source /opt/emsdk/emsdk_env.sh`.

`currently_using_emscripten = 5.0.7`
`minimum_emscripten = 5.0.7`
`emscripten_archive_url = https://github.com/emscripten-core/emsdk.git`

## Node.js

Runs the JavaScript tests and the memory64 (wasm64) build, which needs Node 24; the package
supports Node 22 and later (`package.json` engines). Installed to `/usr/local/lib/node-<version>`
with links in `/usr/local/bin` (macOS, Linux) or `C:\Program Files\nodejs` (Windows).

`currently_using_node = 24.22.3`
`minimum_node = 22`
`node_archive_url_macos_arm64 = https://nodejs.org/dist/v24.22.3/node-v24.22.3-darwin-arm64.tar.gz`
`node_sha256_macos_arm64 = ff6e17d4874b76d272212b908cc020d6e815fba5a6c7eaaa06add725840307fd`
`node_archive_url_macos_x64 = https://nodejs.org/dist/v24.22.3/node-v24.22.3-darwin-x64.tar.gz`
`node_sha256_macos_x64 = 977614af0504c81045e7fd85596dbca0e5f951b96681516dadafedcca8c447f7`
`node_archive_url_linux_x64 = https://nodejs.org/dist/v24.22.3/node-v24.22.3-linux-x64.tar.xz`
`node_sha256_linux_x64 = 4f2b175a6d53a5c617a80dba2676238a0688cef28c6da4eba35c7d844e66fcfb`
`node_archive_url_linux_arm64 = https://nodejs.org/dist/v24.22.3/node-v24.22.3-linux-arm64.tar.xz`
`node_sha256_linux_arm64 = 6e51a2ada00f0011cc266f65d98922b403d7ad88f809ca2aa615a5fe1ce6e86c`
`node_archive_url_windows_x64 = https://nodejs.org/dist/v24.22.3/node-v24.22.3-win-x64.zip`
`node_sha256_windows_x64 = e612af99f626ab415ea225af6bce5c971feb448cb5cae250bb1c3fa50c5f7d9f`
`node_archive_url_windows_arm64 = https://nodejs.org/dist/v24.22.3/node-v24.22.3-win-arm64.zip`
`node_sha256_windows_arm64 = b3cc74a295b39d4f273271e96f58c06dd2b35757ca12abec64c7d0598b434f14`

## Xcode (macOS)

The C++ compiler for the wheel and the .NET native library (universal: arm64 + x86_64). Release
wheels and libraries target macOS 14 (`MACOSX_DEPLOYMENT_TARGET`). Checked, not installed: get it
from the App Store or developer.apple.com; `bootstrap.py --set xcode <version>` switches between
installed versions (`xcode-select`).

`currently_using_xcode = 26.6`
`minimum_xcode = 16.0`

## Visual Studio (Windows)

Visual Studio 2022 (17.x) with the C++ workload. The Windows x64 .NET native library is built with
the v142 toolset (`MSVC v142 - VS 2019 C++ x64/x86 build tools` component); arm64 with the default
(v143). Checked, not installed.

`currently_using_msvc = 17`
`minimum_msvc = 17`

## Git

Clones the repository and its submodules (`git submodule update --init --recursive`). Checked, not
installed.

`minimum_git = 2.30`

## libclang

The bindings generator reads opennurbs and the C layer with libclang (the `libclang` package on
PyPI). Installed with pip into the Python that runs `bootstrap.py`.

`currently_using_libclang = 18.1.1`
`minimum_libclang = 18.1.1`

## iOS and Android

Not built in CI since the Xamarin builds were retired; being revived on .NET 10 (`net10.0-ios`,
`net10.0-android`). Their tools (Android NDK, the .NET `ios` / `android` workloads) will be listed
here.

## Related Topics

- [script/README.md](script/README.md) for the build scripts
- [README.md](README.md) for an overview of the rhino3dm project
