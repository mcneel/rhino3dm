# Build rhino3dm.py for yourself

## Get the source code

This repo uses [OpenNURBS](https://github.com/mcneel/opennurbs), [nanobind](https://github.com/wjakob/nanobind) and Draco as submodules:

```commandline
git clone --recurse-submodules https://github.com/mcneel/rhino3dm.git
```

## Install the tools

* Python 3.10 or later
* CMake 3.21 or later (the wheel build fetches a newer one from PyPI when it needs to)
* a C++17 compiler: Xcode on macOS, Visual Studio 2022 on Windows, GCC or Clang on Linux

[Current_Development_Tools.md](../../Current_Development_Tools.md) lists the versions CI uses.
`python bootstrap.py py` checks them.

## Compile

```commandline
python build.py py
```

builds a wheel for the Python that runs it into `dist/` (`pip wheel .`, driven by `pyproject.toml`
and scikit-build-core; on Python 3.12 and later it is one abi3 wheel for every later version).
`python build.py py --sdist` builds the source distribution (needs `pip install build`).

## Compile and install a development version

```commandline
pip install -e . --no-build-isolation
```

installs an editable rhino3dm; `pip install scikit-build-core` first, for `--no-build-isolation`.
After changing the C++ sources, run it again to rebuild.

For a native build tree to work in (an IDE, compile_commands.json): `cmake --preset py`, then
`cmake --build --preset py` (build tree: `build/py`).

## Test

```commandline
pip install dist/rhino3dm-*.whl
cd tests/python && python -m unittest discover .
```
