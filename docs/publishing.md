# Publishing new versions

## Updating version numbers

There are several places where version numbers should be updated:

- [JavaScript] package.json, line 3
- [.NET] src/dotnet/Rhino3dm.csproj, line 11
- [Python]
  - setup.py, line 113
  - src/rhino3dm/\_\_init\_\_.py, line 7
- src/version.txt, line 1

## JavaScript

### Node.js

1. Build rhino3dm.js, rhino3dm.wasm, and rhino3dm.module.js (or get them from the [Github Repo Actions Artifacts](https://github.com/mcneel/rhino3dm/actions) )
2. Build and run docgen (for type definitions). At the time of writing this needs to be done on windows as building docgen results in an exe. Once docgen is built, you can run it from `src/docgen/bin/docgen.exe`. After running docgen, the `rhino3dm.d.ts` file will be in the `src/docgen/out/js_tsdef` directory
3. Create a new directory and copy in rhino3dm.js, rhino3dm.wasm, rhino3dm.module.js and rhino3dm.d.ts
4. Copy in RHINO3DM.JS.md from `docs/javascript` and rename to README.md
6. Update the version number in package.json and copy that in too
7. From inside the new directory, run `npm publish` (see note 2). You might need to run `npm login` prior to publishing.

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
