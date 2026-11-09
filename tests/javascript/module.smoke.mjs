// Smoke test for the ES6 module build (rhino3dm.module.js).
//
// The jest suite loads the CommonJS build (require('rhino3dm')); this script imports the
// module build and exercises a couple of basic APIs so a broken EXPORT_ES6 output is caught.
// The module is built for browsers and workers (no Node file system), so the wasm bytes are
// handed to it here, as a bundler or a page may do with `wasmBinary`.
// Run with: node module.smoke.mjs  (exits non-zero on failure)

import { existsSync, readFileSync } from 'node:fs'
import rhino3dm from './lib/rhino3dm.module.js'

function assert(cond, msg) {
  if (!cond) {
    console.error('FAIL:', msg)
    process.exit(1)
  }
}

// CI builds the module as released and rhino3dm.js as debug, so their wasm differ there: CI keeps
// the module's own as rhino3dm.module.wasm. A package has one rhino3dm.wasm for both.
const own = new URL('./lib/rhino3dm.module.wasm', import.meta.url)
const wasmBinary = readFileSync(existsSync(own) ? own : new URL('./lib/rhino3dm.wasm', import.meta.url))
const rhino = await rhino3dm({ wasmBinary })

assert(typeof rhino.Sphere === 'function', 'Sphere missing from module build')

const sphere = new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 5)
assert(sphere.Radius === 5, `sphere.Radius expected 5, got ${sphere.Radius}`)
assert(rhino.Sphere.diameter(sphere) === 10, 'Sphere.diameter(sphere) expected 10')

// a class object made by a value type's function, through the module build
const brep = rhino.Sphere.toBrep(sphere)
assert(brep !== null && brep.faces.count === 1, 'Sphere.toBrep(sphere) did not produce a 1-face brep')

console.log('ES6 module build smoke test OK')
