# automated testing

As of rhino3dm 8.9.0-beta, there are a set of automated tests for each language. These tests reside in the tests directory.

## javascript

For javascript / nodejs, we use Jest.

### running tests locally. 

The package.json file in tests/javascript/package.json includes a hardcoded local dependency for the automated tests run via GitHub actions:

``` json

"dependencies": {
    "rhino3dm": "file:./lib"
  }

```

If you have built rhino3dm.js from source, you can put the rhino3dm.wasm and rhino3dm.js files in the `tests/javascript/lib` folder and run the following from the tests/javascript directory:

- `npm i` to install the library
- `npm test` to run the tests

If you want to tests agains a published version of rhino3dm.js, you would need to install the published library:

- `npm i rhino3dm@8.17.0-beta` for example, to install version 8.17.0-beta of rhino3dm
- `npm test` to run the tests
- do not commit this change.

## python

### running tests locally

If you have built rhino3dm.py from source, you need to install the resulting whl.

From the rhino3dm root:

- `python3 -m pip install --no-index --force-reinstall dist/rhino3dm-8.17.0b0-cp311-cp311-macosx_14_0_arm64.whl` to install version 8.17.0-beta of the python 3.11 arm64 whl compatible with macos 14. The `--force-reinstall` option is only if you've already installed an 8.17.0b0 version and you want to replace it.
- `python3 -m unittest discover tests/python`to run the tests

If you want to tests agains a published version of rhino3dm.py, you would need to install the published library:
- `python3 -m pip install rhino3dm==8.17.0b0` --force-reinstall` to install rhino3dm.py version 8.17.0-beta. `--force-reinstall` is only needed if you have already installed a version 8.17.0b0 and want to overwrite it.
-`python3 -m unittest discover tests/python`to run the tests

## dotnet

### running tests locally

If you have built rhino3dm.net from source, you need to follow a few additional steps to prepare the testing project:

- `dotnet pack src/dotnet/Rhino3dm.csproj` to create a nuget package.
- `dotnet nuget add source "/Users/<username>/dev/rhino3dm/src/dotnet/bin/Debug"` for example on macos
- `cd tests/dotnet`
- `dotnet add package Rhino3dm -v 8.17.0-beta`
- `dotnet build`
- `dotnet test`

If you want to test with a published version:

- `cd tests/dotnet`
- `dotnet add package Rhino3dm -v 8.17.0-beta`
- `dotnet build`
- `dotnet test`

