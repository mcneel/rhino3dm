# Publishing new versions

## Updating version numbers

There are several places where version numbers should be updated:

- [JavaScript] package.json, line 3
- [.NET] 
    - src/dotnet/Rhino3dm.csproj, line 11
    - src/dotnet/Properties/AssemblyInfo.cs, line 78
- [Python]
  - setup.py, line 127
  - src/rhino3dm/\_\_init\_\_.py, line 7
- src/version.txt, line 1

## Updating Documentation with docgen

### requirements:
- javascript - jsdoc and docdash: `npm i -g docdash jsdoc`
- python - 
  - sphinx: `python3 -m pip install sphinx`
  - theme: `python3 -m pip install sphinx-rtd-theme`

### building api docs

1. change to docgen folder: `cd src/docgen`
2. build docgen: `dotnet build docgen.csproj`
3. run docgen: `cd bin/Debug && ./docgen`
4. generate docs:
    1. javascript: `~/.npm-global/bin/jsdoc ./out/js_apidocs/rh3dm_temp.js README.md -c jsdoc.conf -t ~/.npm-global/lib/node_modules/docdash -d ../../docs/javascript/api`
    2. python: 
        - `sphinx-build -M html out/py_apidocs out/py_apidocs/sphinxout`
        - replace the docs in docs/python/api with the docs generated in src/docgen/out/py_apidocs/sphinxout/html
5. commit these changes and merge with `main`


## JavaScript

### Node.js

1. Run a `workflow_release` workflow from the rhino3dm repository Actions: https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml. This will build all of the rhino3dm versions, including the js version.
2. Download and extract the `rhino3dm.js` artifact.
3. cd into the directory you've just extracted
4. From inside this directory, run `npm publish` (see note 2). You might need to run `npm login` prior to publishing.

See https://docs.npmjs.com/creating-and-publishing-unscoped-public-packages for more info.

#### Notes:
1. After creating a user on npm.org, ask Will to add you to the mcneel team!
2. If publishing a pre-release, e.g. `0.4.0-dev`, use `npm publish --tag next` ([source](https://medium.com/@mbostock/prereleases-and-npm-e778fc5e2420))

## dotnet

1. Run a `workflow_release` workflow from the rhino3dm repository Actions: https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml. This will build all of the rhino3dm versions, including a nupkg for rhino3m dotnet for linux, macos, and windows. 
2. Download the `rhino3dm.net nupkg` artifact generated from running the `workflow_release` workflow. This will result in a `rhino3dm.net nupkg.zip` downloaded to your computer.
3. Unzip the `rhino3dm.net nupkg.zip` file. This will result in a new folder named `rhino3dm.net nupkg` that will contain a `Rhino3dm.*.*.*.nupkg` numbered according to the current version.
4. Open a terminal and direct it to the `rhino3dm.net nupkg` folder created from unzipping the file in step 3.
5. Push the package to NuGet with `dotnet nuget push...`, replacing the wildcards with the version number, and entering your API Key from NuGet.org (see note 1). 

```bash
dotnet nuget push Rhino3dm.*.*.*.nupkg -k <APIKEY> -s https://api.nuget.org/v3/index.json
```

6. If all went well you should see something similar in the terminal: 

```
Pushing Rhino3dm.7.7.0.nupkg to 'https://www.nuget.org/api/v2/package'...
  PUT https://www.nuget.org/api/v2/package/
  Created https://www.nuget.org/api/v2/package/ 3801ms
Your package was pushed.
```

7. The newly created package will take a few minutes to validate on NuGet.org. You can check the status at the Rhino3dm page: https://www.nuget.org/packages/Rhino3dm/


See https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#publish-with-dotnet-nuget-push for more info.

#### Notes:

1. To create an API Key for NuGet, see https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#create-api-keys 

## Python

Python packages can be uploaded to the corresponding `pypi.org` project: https://pypi.org/project/rhino3dm.

1. Run a `workflow_release` workflow from the rhino3dm repository Actions: https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml. This will build all of the rhino3dm versions, including all Python packages.
2. Download all of the `.whl` and `*.tar.gz` (source distribution) artifacts to a folder called `dist`.
3. Extract all of the `.zip` files and delete them. For the tar.gz.zip, you can run `tar -xvzf rhino3dm-8.0.0b3.tar.gz.zip` to get a tar.gz file. You should be left with many `.whl` files and one `.tar.gz` file.
4. From the `dist` parent folder, upload all Python packages with `twine`

```bash
python3 -m twine upload dist/*
```
5. when prompted for the username, enter in `__token__`
6. When prompted for password, use API key obtained from pypi

#### Requirements

1. Have an account on pypi.org.
2. Be a maintainer or owner for the [rhino3dm package](https://pypi.org/project/rhino3dm).
3. Ensure `twine` is installed

```bash
python3 -m pip install --upgrade twine
```

4. Acquire an API token at https://pypi.org/manage/account/token/
