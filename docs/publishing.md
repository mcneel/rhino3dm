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

- python - 
  - sphinx: `python3 -m pip install sphinx`
  - theme: `python3 -m pip install sphinx-rtd-theme`

### building api docs

1. change to docgen folder: `cd src/docgen`
2. build docgen: `dotnet build docgen.csproj`
3. run docgen: `cd bin/Debug && ./docgen`
4. generate docs:
    1. javascript: 
        - New System: 
          - change to `src/js/docs` directory
          - install dependencies (only first time): `npm i`
          - generate docs: `npm run docs`. This also handles copying to `docs/javscript/api`
        - Old system: 
          - istall dependencies: `npm i -g docdash jsdoc`
          - `~/.npm-global/bin/jsdoc ./out/js_apidocs/rh3dm_temp.js README.md -c jsdoc.conf -t ~/.npm-global/lib/node_modules/docdash -d ../../docs/javascript/api`
    2. python: 
        - install dependencies in venv:
          - change to rhino3dm root directory
          - `python3 -m venv .venv`
          - `source .venv/bin/activate`
          - `pip install sphinx`
          - `pip install sphinx-rtd-theme`
        - `sphinx-build -M html src/docgen/out/py_apidocs src/docgen/out/py_apidocs/sphinxout`
        - replace the docs in docs/python/api with the docs generated in src/docgen/out/py_apidocs/sphinxout/html
5. commit these changes and merge with `main`

## JavaScript

### Node.js

`rhino3dm.js` is published to npm automatically by the `workflow_release` workflow.

1. Ensure `docs/javascript/RHINO3DM.JS.md` is updated and committed to reflect the latest changes (version numbers, etc).
2. Go to the `workflow_release` workflow in the rhino3dm repository Actions: https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml.
3. Click **Run workflow** and check the **Publish rhino3dm.js to npm** box. For a pre-release (e.g. `8.x-beta`), also set the **npm dist-tag** field to `next` (see note 2); leave it as `latest` for a normal release. Run the workflow.
4. The `publish_npm` job waits for the JS build and its tests to pass, then downloads the assembled `rhino3dm.js` artifact and runs `npm publish` for you. No manual download, extract, or `npm login` is required.

Leaving the box unchecked builds the `rhino3dm.js` artifact without publishing — use that to test a release before pushing it to npm.

See https://docs.npmjs.com/creating-and-publishing-unscoped-public-packages for more info.

#### Requirements

Publishing uses [npm Trusted Publishing (OIDC)](https://docs.npmjs.com/trusted-publishers), so no npm token or stored secret is needed. This must be configured once on npm:

1. Sign in as a member of the `mcneel` npm team with publish rights to the [rhino3dm package](https://www.npmjs.com/package/rhino3dm).
2. On the package's **Settings → Trusted Publisher** page, add a GitHub Actions publisher:
   - Organization/user: `mcneel`
   - Repository: `rhino3dm`
   - Workflow filename: `workflow_release.yml`

Once registered, every run with the checkbox enabled publishes without further credentials.

#### Notes:
1. After creating a user on npm.org, ask Will to add you to the mcneel team!
2. If publishing a pre-release, e.g. `0.4.0-beta`, set the **npm dist-tag** input to `next` so it does not become the default `latest` install ([source](https://medium.com/@mbostock/prereleases-and-npm-e778fc5e2420)).

#### Manual fallback (npm publish)

If trusted publishing is unavailable, you can still publish by hand:

1. Ensure `docs/javascript/RHINO3DM.JS.md` is updated and committed to reflect the latest changes (version numbers, etc).
2. Run a `workflow_release` workflow (the npm checkbox can be left unchecked). This builds the `rhino3dm.js` artifact.
3. Download and extract the `rhino3dm.js` artifact.
4. `cd` into the directory you've just extracted.
5. From inside this directory, run `npm publish` (for a pre-release, `npm publish --tag next` — see note 2). You might need to run `npm login` prior to publishing.

## dotnet

The `Rhino3dm` .NET package is published to NuGet.org automatically by the `workflow_release` workflow.

1. Go to the `workflow_release` workflow in the rhino3dm repository Actions: https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml.
2. Click **Run workflow**, check the **Publish Rhino3dm .NET package to NuGet.org** box, and run it.
3. The `publish_nuget` job waits for the `pack_dotnet` build and the `test_dotnet` tests to pass, then downloads the `rhino3dm.net nupkg` artifact and pushes the `Rhino3dm.*.nupkg` to NuGet.org. No manual download, unzip, or `dotnet nuget push` is required.
4. The newly created package will take a few minutes to validate on NuGet.org. You can check the status at the Rhino3dm page: https://www.nuget.org/packages/Rhino3dm/

Leaving the box unchecked builds the nupkg artifact without publishing — use that to test a release before pushing it to NuGet.

#### Requirements

Publishing uses [NuGet Trusted Publishing (OIDC)](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), so no long-lived API key is stored. The workflow exchanges a GitHub OIDC token for a short-lived (1-hour) key at run time via the `NuGet/login` action. Configure this once:

1. Sign in to nuget.org as an owner of the [Rhino3dm package](https://www.nuget.org/packages/Rhino3dm/), then open your username menu → **Trusted Publishing** and add a policy:
   - Repository Owner: `mcneel`
   - Repository: `rhino3dm`
   - Workflow File: `workflow_release.yml`
   - Environment: leave blank
2. Add a GitHub Actions repository **variable** (Settings → Secrets and variables → Actions → **Variables** tab) named **`NUGET_USER`** set to your nuget.org **profile name** (not your email). It's a plain variable rather than a secret because the profile name is public. The `NuGet/login` action uses it to request the temporary key.

Once the policy is registered and the secret is set, every run with the checkbox enabled publishes without further credentials.

See https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#publish-with-dotnet-nuget-push for more info.

#### Manual fallback (dotnet nuget push)

If trusted publishing is unavailable, you can still publish by hand with a long-lived API key:

1. Run a `workflow_release` workflow (the nuget checkbox can be left unchecked). This builds the nupkg for linux, macos, and windows.
2. Download the `rhino3dm.net nupkg` artifact. This will result in a `rhino3dm.net nupkg.zip` downloaded to your computer.
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

#### Notes:

1. To create an API Key for NuGet (only needed for the manual fallback), see https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#create-api-keys 

## Python

Python packages are published to the corresponding `pypi.org` project automatically by the `workflow_release` workflow: https://pypi.org/project/rhino3dm.

1. Go to the `workflow_release` workflow in the rhino3dm repository Actions: https://github.com/mcneel/rhino3dm/actions/workflows/workflow_release.yml.
2. Click **Run workflow**, check the **Upload Python packages (wheels + sdist) to PyPI** box, and run it.
3. The `publish_pypi` job waits for all Python build jobs (sdist, manylinux, and per-OS bdist) to succeed, then collects every `.whl` and the `.tar.gz` sdist into a single `dist/` folder and uploads them to PyPI. No manual download, unzip, or `twine` step is required.

Leaving the box unchecked builds all packages as artifacts without publishing — use that to test a release before pushing it to PyPI.

#### Requirements

Publishing uses [PyPI Trusted Publishing (OIDC)](https://docs.pypi.org/trusted-publishers/), so no API token or stored secret is needed. This must be configured once on PyPI:

1. Sign in as a maintainer or owner of the [rhino3dm package](https://pypi.org/project/rhino3dm).
2. Under the project's **Publishing** settings, add a GitHub Actions trusted publisher:
   - Owner: `mcneel`
   - Repository: `rhino3dm`
   - Workflow name: `workflow_release.yml`
   - Environment name: leave blank (or set to `pypi` if you also add a matching `environment: pypi` to the `publish_pypi` job for an extra approval gate).

Once the trusted publisher is registered, every run with the checkbox enabled publishes without further credentials.

#### Manual fallback (twine)

If trusted publishing is unavailable (or you need to push a build that was made with the checkbox unchecked), you can still upload the artifacts by hand:

1. Run the `workflow_release` workflow (the checkbox can be left unchecked). This builds all of the Python packages as artifacts.
2. Download all of the `.whl` and `*.tar.gz` (source distribution) artifacts to a folder called `dist`.
3. Extract all of the `.zip` files and delete them. For the tar.gz.zip, you can run `tar -xvzf rhino3dm-8.17.0.tar.gz.zip` to get a tar.gz file. You should be left with many `.whl` files and one `.tar.gz` file.
4. From the `dist` parent folder, upload all Python packages with `twine`:

```bash
python3 -m twine upload dist/*
```
5. When prompted for the username, enter `__token__`.
6. When prompted for the password, use an API token obtained from pypi.

Requirements for the manual path:

1. Have an account on pypi.org.
2. Be a maintainer or owner for the [rhino3dm package](https://pypi.org/project/rhino3dm).
3. Ensure `twine` is installed:

```bash
python3 -m pip install --upgrade twine
```

4. Acquire an API token at https://pypi.org/manage/account/token/
