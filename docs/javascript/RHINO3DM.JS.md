# rhino3dm.js

[![](https://data.jsdelivr.com/v1/package/npm/rhino3dm/badge)](https://www.jsdelivr.com/package/npm/rhino3dm)

**Read, write and create Rhino 3D models (`.3dm` files) in JavaScript.**

rhino3dm brings [openNURBS](https://www.rhino3d.com/opennurbs), the library behind Rhino's file
format, to JavaScript with [RhinoCommon](https://developer.rhino3d.com/api/rhinocommon/)'s API (Rhino's
.NET SDK). Use it in scripts, notebooks and web services on Windows, macOS and Linux.

The library is compiled to WebAssembly (`rhino3dm.wasm`) and runs in every current browser, Safari and
iOS included, and in Node.js 22+. The package also ships a 64-bit-memory build in `wasm64/` for models
that need more than 4 GB. It runs in Chrome 133+, Firefox 134+ and Node.js 24+ (in Safari, only Technology Preview 252+) and
is slower, so use it only when you need the memory.

For browser based applications, we also develop a 3dmLoader for the popular three.js WebGL library: https://threejs.org/examples/?q=3dm#webgl_loader_3dm

## Coming from rhino3dm.js 8.x?

Most 8.x scripts need changes. For example, points and other values are objects (`{ X: 1, Y: 2, Z: 3 }`, not `[1, 2, 3]`), properties that were functions are properties, and editing an object's geometry no longer changes the file.

- [Migrating to rhino3dm.js 9.2](https://mcneel.github.io/rhino3dm/javascript/api/documents/Migrating_to_9_2.html): what changed, with before-and-after code
- [Renamed and removed members](https://mcneel.github.io/rhino3dm/javascript/api/documents/Migrating__renamed_and_removed_members.html): every 8.x name and what replaces it

## Install

```
npm install rhino3dm
```

Pre-releases, such as betas, install with `npm install rhino3dm@next`. In a browser you can also load
the library from a CDN without installing anything (see [Usage](#usage)).

## Usage

The easiest way to get started is to reference a specific version of the library using jsDelivr.

```html
<!DOCTYPE html>

<body>

  <!-- Import maps polyfill -->
  <!-- Remove this when import maps will be widely supported -->
  <script async src="https://unpkg.com/es-module-shims@1.10.0/dist/es-module-shims.js"></script>

  <script type="importmap">
      {
          "imports": {
            "rhino3dm":"https://cdn.jsdelivr.net/npm/rhino3dm@9.2.0-beta.0/rhino3dm.module.min.js"
          }
      }
  </script>

  <script type="module">

    import rhino3dm from 'rhino3dm'
    const rhino = await rhino3dm()
    const sphere = new rhino.Sphere({ X: 1, Y: 2, Z: 3 }, 12)
    console.log(rhino.Sphere.diameter(sphere))

  </script>

</body>

</html>
```

You can also [download the files](https://www.jsdelivr.com/package/npm/rhino3dm) if you want to bake them into your site or application. You'll need the `.wasm` web assembly along with the `.js` (or `.min.js`) wrapper.

### Bundlers: telling rhino3dm where `rhino3dm.wasm` lives

`rhino3dm.js` loads `rhino3dm.wasm` from the same directory it was itself loaded from. Bundlers (Vite, webpack, Next.js, ...) often move or rename the `.js`, so the `.wasm` is no longer found. Rather than copying the `.wasm` around, pass a `locateFile` callback to the module factory and return the URL the `.wasm` is served from:

```js
import rhino3dm from 'rhino3dm'

// e.g. the .wasm is copied to /public/wasm/ by your build
const rhino = await rhino3dm({ locateFile: (file) => `/wasm/${file}` })
```

The argument is the standard [Emscripten module configuration object](https://emscripten.org/docs/api_reference/module.html), so other members such as `wasmBinary` (supply the bytes yourself) or `print`/`printErr` may be passed too. See `RhinoModuleOptions` in `rhino3dm.d.ts`.


### Node.js


Example node.js script (an ES module, e.g. `example.mjs`):

```js
import { readFileSync, writeFileSync } from 'node:fs'
import rhino3dm from 'rhino3dm'

const rhino = await rhino3dm()

// build a model
let doc = new rhino.File3dm()
doc.objects.addSphere(new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 5))
doc.objects.addCircle(new rhino.Circle({ X: 20, Y: 0, Z: 0 }, 3))
doc.objects.addPoint({ X: 10, Y: 10, Z: 10 })
writeFileSync('example.3dm', doc.toByteArray())

// read it back
doc = rhino.File3dm.fromByteArray(readFileSync('example.3dm'))
for (const obj of doc.objects) {
  const geometry = obj.geometry
  const bbox = geometry.getBoundingBox(true)
  console.log(geometry.constructor.name, bbox.Min, bbox.Max)
}

// RevSurface { X: -5, Y: -5, Z: -5 } { X: 5, Y: 5, Z: 5 }
// ArcCurve { X: 17, Y: -3, Z: 0 } { X: 23, Y: 3, Z: 0 }
// Point { X: 10, Y: 10, Z: 10 } { X: 10, Y: 10, Z: 10 }
```

### React.js

Similar to plain html, **rhino3dm.js** can be added as a script in `index.js`

```js
// index.js
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import App from "./App";

const rootElement = document.getElementById("root");

const script = document.createElement("script");
script.src = "https://cdn.jsdelivr.net/npm/rhino3dm@9.2.0-beta.0/rhino3dm.min.js";
script.addEventListener("load", () => {
  createRoot(rootElement).render(
    <StrictMode>
      <App />
    </StrictMode>
  );
});
document.body.appendChild(script);

```

Once loaded, `rhino3dm` can be referenced in any component with `window.rhino3dm`, keep in mind that the wasm file loads asycnhronously so any any calls to wasm methods need to happen inside `.then()` or using `await`

```js
// App.js
import React, { useEffect, useState } from "react";
import "./styles.css";

export default function App() {
  const [diameter, setDiameter] = useState(null);

  useEffect(() => {
    window.rhino3dm().then((Module) => {
      //creating a sphere using rhino3dm
      const sphere = new Module.Sphere({ X: 1, Y: 2, Z: 3 }, 16);
      setDiameter(Module.Sphere.diameter(sphere));
    });
  }, []);

  return <div className="App">{diameter !== null && <p>{`sphere diameter is: ${diameter}`}</p>}</div>;
}
```

CodeSandbox of above react implementation can be found [here](https://codesandbox.io/s/rhino3dm-react-p3gr7?file=/src/App.js:0-428)


## Documentation

* [API reference](https://mcneel.github.io/rhino3dm/javascript/api/index.html), and the
  [8.x API reference](https://mcneel.github.io/rhino3dm/8/javascript/api/index.html) for the last 8.x
  release
* [RhinoCommon documentation](https://developer.rhino3d.com/api/rhinocommon/): rhino3dm follows its
  names and behaviour
* [Samples](https://github.com/mcneel/rhino-developer-samples/tree/9/rhino3dm/js)

## Platforms

| | `rhino3dm.wasm` (default) | `wasm64/rhino3dm.wasm` (memory64) |
|---|---|---|
| Browsers | every current browser: Chrome, Edge, Firefox, Safari (macOS and iOS) | Chrome and Edge 133+, Firefox 134+, Safari Technology Preview 252+ (not yet released Safari) |
| Node.js | 22+ | 24+ |
| Model size | up to 4 GB of memory | more than 4 GB |

## Build from source

If the pre-compiled libraries above do not work in your situation, you can compile the libraries from their source. For detailed instructions go to [rhino3dm.js and rhino3dm.wasm](https://github.com/mcneel/rhino3dm/blob/HEAD/docs/javascript/RHINO3DM-BUILD.JS.md)
