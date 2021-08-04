# Publishing new versions

## JavaScript

### Node.js

1. Build rhino3dm.js, rhino3dm.wasm, and rhino3dm.module.js (or get them from the [Github Repo Actions Artifacts](https://github.com/mcneel/rhino3dm/actions) )
2. Build and run docgen (for type definitions). See `src/docgen/make_javascript_docs.bat`. This will be in the `src/docgen/out/js_tsdef` directory
3. Create a new directory and copy in rhino3dm.js, rhino3dm.wasm, rhino3dm.module.js and rhino3dm.d.ts
4. Copy in RHINO3DM.JS.md from `docs/javascript` and rename to README.md
6. Update the version number in package.json and copy that in too
7. From inside the new directory, run `npm publish` (see note 2). You might need to run `npm login` prior to publishing.

See https://docs.npmjs.com/creating-and-publishing-unscoped-public-packages for more info.

#### Notes:
1. After creating a user on npm.org, ask Will to add you to the mcneel team!
2. If publishing a pre-release, e.g. `0.4.0-dev`, use `npm publish --tag next` ([source](https://medium.com/@mbostock/prereleases-and-npm-e778fc5e2420))
