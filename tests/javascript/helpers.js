// Shared helpers for the JS tests.
//
//   const { raises, gcSettle, readModel, curve, approx } = require('./helpers')
//
// gcSettle needs node's --expose-gc (`npm test` passes it).
const fs = require('fs')
const path = require('path')

const MODELS = path.join(__dirname, '..', 'models')

// the path of a file in tests/models
const model = name => path.join(MODELS, name)

// a File3dm read from tests/models (WASM has no host file system: through bytes)
const readModel = (rhino, name) => rhino.File3dm.fromByteArray(new Uint8Array(fs.readFileSync(model(name))))

// fn throws (a C++ exception or a JS error) and, when given, its message matches `re`.
// A caught C++ exception is refcounted in wasm memory, so it is released here.
function raises(rhino, fn, re) {
  let threw = false, msg = ''
  try { fn() } catch (e) {
    threw = true
    try { msg = rhino.getExceptionMessage ? String(rhino.getExceptionMessage(e)) : String(e) } catch (e2) { msg = String(e) }
    try { if (rhino.decrementExceptionRefcount && e instanceof WebAssembly.Exception) rhino.decrementExceptionRefcount(e) } catch (e3) { /* not a C++ exception */ }
  }
  if (!threw) throw new Error('expected a throw')
  if (re && !re.test(msg)) throw new Error('message ' + msg + ' does not match ' + re)
}

// collect garbage and let FinalizationRegistry callbacks run: dropping the last reference
async function gcSettle() {
  if (typeof global.gc !== 'function') throw new Error('run node with --expose-gc')
  for (let i = 0; i < 3; i++) { global.gc(); await new Promise(res => setTimeout(res, 10)) }
}

// |a - b| <= tol
function approx(a, b, tol = 1e-9) {
  if (!(Math.abs(a - b) <= tol)) throw new Error(`${a} != ${b} (tol ${tol})`)
}

// a degree-3 control-point curve through four points in the XY plane
const curve = rhino => rhino.Curve.createControlPointCurve(
  [[0, 0, 0], [1, 2, 0], [4, 1, 0], [6, -2, 0]].map(([X, Y, Z]) => ({ X, Y, Z })), 3)

// a 1 x 2 x 3 box brep at the origin
const boxBrep = rhino => rhino.Brep.createFromBox(rhino.BoundingBox.create({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 }))

// a mesh with one quad face on the unit square
function quadMesh(rhino) {
  const m = new rhino.Mesh()
  for (const [x, y, z] of [[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0]]) m.vertices.add(x, y, z)
  m.faces.addFace(0, 1, 2, 3)
  return m
}

// the first object geometry of type `cls` in File3dm `f`, or null
function firstGeometry(f, cls) {
  for (const o of f.objects) {
    const g = o.geometry
    if (g instanceof cls) return g
  }
  return null
}

module.exports = { MODELS, model, readModel, raises, gcSettle, approx, curve, boxBrep, quadMesh, firstGeometry }
