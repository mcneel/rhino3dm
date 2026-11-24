# rhino3dm build scripts

rhino3dm is built from the repository root. CI (GitHub Actions) runs the same commands.

| | |
|---|---|
| `bootstrap.py` | checks, downloads and installs the tools ([Current_Development_Tools.md](../Current_Development_Tools.md)) |
| `build.py` | builds the Python wheel (`py`), rhino3dm.js (`js`) and the .NET library (`dotnet`) |
| `CMakeLists.txt`, `CMakePresets.json` | one CMake entry point; a preset per library, variant and platform |
| `pyproject.toml` | the Python package (scikit-build-core) |

```commandline
python bootstrap.py js           # check the tools for rhino3dm.js
python bootstrap.py js --install # install what is missing (administrator rights)
python build.py js               # build it
```

The build guides: [Python](../docs/python/RHINO3DM-BUILD.PY.md),
[JavaScript](../docs/javascript/RHINO3DM-BUILD.JS.md), [.NET](../docs/dotnet/RHINO3DM-BUILD.CS.md).

## In this folder

- `bump_version.py`: writes the version in `src/version.txt` into every file that carries one
  ([publishing](../docs/publishing.md))
- `codesign-native.sh`, `notarize-native.sh`: sign and notarize the macOS native library in the
  release workflow

## Platforms

| library | built on | output |
|---|---|---|
| Python wheel | Windows (x64, arm64), macOS (universal), Linux (x86_64, aarch64; manylinux_2_28) | `dist/` |
| rhino3dm.js | any (Emscripten) | `build/js/dist` |
| .NET native library | Windows (x64, arm64), macOS (universal), Linux (x64, arm64) | `build/native/<rid>` |
| iOS, Android | not built yet: being revived on .NET 10 (`net10.0-ios`, `net10.0-android`) | |
