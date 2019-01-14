# Publishing new versions

## JavaScript

### Node.js

1. Build rhino3dm.js and rhino3dm.wasm
2. Create a new directory and copy in rhino3dm.js and rhino3dm.wasm
3. Copy in RHINO3DM.JS.md and rename to README.md
4. Update the version number in package.json and copy that in too
5. From inside the new directory, run `npm publish`

See https://docs.npmjs.com/creating-and-publishing-unscoped-public-packages for more info.

Note: after creating a user on npm.org, ask Will to add you to the mcneel team!
