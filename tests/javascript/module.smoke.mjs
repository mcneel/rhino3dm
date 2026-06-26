// Smoke test for the ES6 module build (rhino3dm.module.js).
//
// The jest suite loads the CommonJS build (require('rhino3dm')); the ES6 module
// variant is shipped but was otherwise never loaded in CI. This standalone
// script imports the module build and exercises a couple of basic APIs so a
// broken EXPORT_ES6 output is caught. Run with: node module.smoke.mjs
// Exits non-zero on failure (CI-friendly).

import rhino3dm from './lib/rhino3dm.module.js'

function assert(cond, msg) {
  if (!cond) {
    console.error('FAIL:', msg)
    process.exit(1)
  }
}

const rhino = await rhino3dm()

assert(typeof rhino.Sphere === 'function', 'Sphere constructor missing from module build')

const sphere = new rhino.Sphere([0, 0, 0], 5)
assert(sphere.radius === 5, `sphere.radius expected 5, got ${sphere.radius}`)

// exercise a tuple-returning method (the embind raw-pointer path) through the module build
const brep = sphere.toBrep()
assert(brep !== null && brep.faces().count === 1, 'sphere.toBrep() did not produce a 1-face brep')

console.log('ES6 module build smoke test OK (rhino3dm', rhino.Version || '', ')')
