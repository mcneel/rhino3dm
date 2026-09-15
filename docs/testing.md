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

- `npm i rhino3dm@8.17.0` for example, to install version 8.17.0 of rhino3dm
- `npm test` to run the tests
- do not commit this change.

## python

### running tests locally

If you have built rhino3dm.py from source, you need to install the resulting whl.

From the rhino3dm root:

- `python3 -m pip install --no-index --force-reinstall dist/rhino3dm-8.17.0-cp311-cp311-macosx_14_0_arm64.whl` to install version 8.17.0 of the python 3.11 arm64 whl compatible with macos 14. The `--force-reinstall` option is only if you've already installed an 8.17.0 version and you want to replace it.
- `python3 -m unittest discover tests/python`to run the tests

If you want to tests agains a published version of rhino3dm.py, you would need to install the published library:
- `python3 -m pip install rhino3dm==8.17.0` --force-reinstall` to install rhino3dm.py version 8.17.0. `--force-reinstall` is only needed if you have already installed a version 8.17.0 and want to overwrite it.
-`python3 -m unittest discover tests/python`to run the tests

### running a subset

`discover` finds and runs the tests in one step - there is no separate run
command afterwards. To narrow what runs, filter the discovery pattern or name a
module, class, or test directly:

- `python3 -m unittest discover tests/python -p "test_SubD*.py"` to discover only the modules whose filename matches
- `cd tests/python && python3 -m unittest test_SubD_RH3DM169_Creases` for one module
- `cd tests/python && python3 -m unittest test_SubD_RH3DM169_Creases.TestSoftCreases` for one class
- `cd tests/python && python3 -m unittest test_SubD_RH3DM169_Creases.TestSoftCreases.test_uniform_sharpness_values` for one test
- add `-v` to any of these to list each test as it runs, or `-k <substring>` to filter by name

The module, class, and test forms are import paths rather than file paths, so
run them from `tests/python` - or from anywhere the module is importable.

### the SubD fixture

The tests for RH3DM-169/175/176/177/178 read `tests/models/subd_creases.3dm`, a
10-unit SubD box whose creases and edge sharpness are known exactly. rhino3dm
cannot author it - its SubD bindings are read-only, and nothing outside Rhino
can set edge sharpness - so the .3dm is committed, and the scripts that produced
it live in `tests/models/authoring`. Both run inside Rhino 8.36 or later:

- `make_subd_fixture.py` builds the .3dm and adds the SubD to the current document.
- `report_subd_fixture.py` reads the .3dm back through RhinoCommon and prints expected against actual, line by line. Run it when a test goes red, to tell a wrong binding from a wrong expectation.

Every value the tests assert lives in `tests/python/subd_fixture_spec.py`, which
the authoring scripts and the tests both import. `test_SubD_FixtureSpec.py`
checks that file for internal consistency and needs neither rhino3dm nor the
.3dm, so it runs anywhere. If the .3dm is missing, the five issue modules skip
rather than fail.

### per-issue verdicts

`unittest` reports per test. When what you want is a line per YouTrack issue,
run the wrapper instead:

- `cd tests/python && python3 run_subd_issue_tests.py` for one line per issue
- `cd tests/python && python3 run_subd_issue_tests.py -v` to also print every failure in full

It exits non-zero if any issue failed. It is only a reporting view over the same
modules `discover` already runs, so it is not part of CI, and a new test file
picked up by discovery does not have to be listed in it.

## dotnet

### running tests locally

If you have built rhino3dm.net from source, you need to follow a few additional steps to prepare the testing project:

- `dotnet pack src/dotnet/Rhino3dm.csproj` to create a nuget package.
- `dotnet nuget add source "/Users/<username>/dev/rhino3dm/src/dotnet/bin/Debug"` for example on macos
- `cd tests/dotnet`
- `dotnet add package Rhino3dm -v 8.17.0`
- `dotnet build`
- `dotnet test`

If you want to test with a published version:

- `cd tests/dotnet`
- `dotnet add package Rhino3dm -v 8.17.0`
- `dotnet build`
- `dotnet test`

