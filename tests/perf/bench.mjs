// rhino3dm.js performance tasks. The same script runs on the reference release (8.35) and on this
// tree's build; the API differences live in the adapter `A`. Prints one JSON object on stdout.
// usage: node --expose-gc bench.mjs <rhino3dm.js> <models dir> <model.3dm> [repeats]
// Each task runs once to warm up, then `repeats` times (2 when the warm-up took over a second); the
// median is reported.
import { readFileSync, readdirSync } from 'fs'
import { createRequire } from 'module'
import { performance } from 'perf_hooks'

const [modPath, MODELS, MODEL] = process.argv.slice(2)
const REPEATS = +(process.argv[5] || 5)
const t0 = performance.now()
const rhino = await createRequire(import.meta.url)(modPath)()
const initMs = performance.now() - t0
const V9 = !!rhino.Point3d.distanceTo     // false: the reference release's API
const results = { 'module.init': { median_ms: initMs, min_ms: initMs, n: 1 } }

function bench(name, fn, setup, repeats = REPEATS) {
  const times = []
  if (process.env.BENCH_TRACE) process.stderr.write(name + '\n')
  try {
    for (let i = 0; i <= repeats; i++) {
      const arg = setup ? setup() : undefined
      if (global.gc) global.gc()
      const t = performance.now()
      fn(arg)
      times.push(performance.now() - t)
      if (i === 0 && times[0] > 1000) repeats = Math.min(repeats, 2)   // a slow task: 2 repeats are enough
    }
  } catch (e) {
    const msg = e instanceof WebAssembly.Exception && rhino.getExceptionMessage ? rhino.getExceptionMessage(e).join(': ') : String(e && e.message || e)
    results[name] = { error: msg.slice(0, 300) }
    process.stderr.write('ERROR ' + name + ': ' + msg.slice(0, 300) + '\n')
    return
  }
  times.shift()
  times.sort((a, b) => a - b)
  results[name] = { median_ms: times[times.length >> 1], min_ms: times[0], n: times.length }
}
const del = o => { if (o && o.delete && !o.isDeleted()) o.delete() }

// ---- adapters: the one place the APIs differ --------------------------------------------------
const A = V9 ? {
  P: (x, y, z) => ({ X: x, Y: y, Z: z }),
  objects: d => d.objects,
  each: (d, f) => { for (const o of d.objects) f(o) },
  geom: o => o.geometry, attrs: o => o.attributes,
  layers: d => d.layers,
  eachLayer: (d, f) => { for (const l of d.layers) f(l) },
  verts: m => m.vertices, faces: m => m.faces, normals: m => m.normals,
  addQuad: (fs, a, b, c, d) => fs.addFace(a, b, c, d),
  dom: c => { const d = c.domain; return [d.T0, d.T1] },
  write8: d => { const o = new rhino.File3dmWriteOptions(); o.version = 8; const b = d.toByteArray(o); o.delete(); return b },
  sphere: (c, r) => new rhino.Sphere(c, r),
  translation: (x, y, z) => rhino.Transform.translation(x, y, z),
  xmul: (a, b) => rhino.Transform.multiply(a, b),
  xformPoint: (p, x) => rhino.Point3d.transform(p, x),
  brepFaces: b => b.faces, brepEdges: b => b.edges, brepVerts: b => b.vertices,
  eachSubD: (s, f) => { for (const x of s.faces) f.face(x); for (const x of s.edges) f.edge(x); for (const x of s.vertices) f.vert(x) },
  faceA: f => f.A,
} : {
  P: (x, y, z) => [x, y, z],
  objects: d => d.objects(),
  each: (d, f) => { const t = d.objects(); for (let i = 0; i < t.count; i++) { const o = t.get(i); f(o); o.delete() } },
  geom: o => o.geometry(), attrs: o => o.attributes(),
  layers: d => d.layers(),
  eachLayer: (d, f) => { const t = d.layers(); for (let i = 0; i < t.count; i++) { const l = t.get(i); f(l); l.delete() } },
  verts: m => m.vertices(), faces: m => m.faces(), normals: m => m.normals(),
  addQuad: (fs, a, b, c, d) => fs.addQuadFace(a, b, c, d),
  dom: c => c.domain,
  write8: d => { const o = new rhino.File3dmWriteOptions(); o.version = 8; const b = d.toByteArrayOptions(o); o.delete(); return b },
  sphere: (c, r) => new rhino.Sphere(c, r),
  translation: (x, y, z) => rhino.Transform.translationXYZ(x, y, z),
  xmul: (a, b) => rhino.Transform.multiply(a, b),
  xformPoint: (p, x) => rhino.Point3d.transform(p, x),
  brepFaces: b => b.faces(), brepEdges: b => b.edges(), brepVerts: b => b.vertices(),
  eachSubD: (s, f) => {
    for (const [tab, cb] of [[s.faces(), f.face], [s.edges(), f.edge], [s.vertices(), f.vert]]) {
      const n = tab.count; let x = tab.first()
      for (let i = 0; i < n; i++) { cb(x); x.delete(); x = tab.next() }
      del(x)
    }
  },
  faceA: f => f[0],
}
const prop = (o, name) => typeof o[name] === 'function' ? o[name]() : o[name]

// 32-bit LCG, identical in bench.py and dotnet/Program.cs
function lcg(seed) { let s = seed >>> 0; return () => { s = (Math.imul(s, 1664525) + 1013904223) >>> 0; return s / 4294967296 } }
function shuffled(arr, seed) {   // Fisher-Yates with the LCG: the same order in every language
  const a = arr.slice(), u = lcg(seed)
  for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(u() * (i + 1)); [a[i], a[j]] = [a[j], a[i]] }
  return a
}
function gridMesh(n, regular = false) {
  const m = new rhino.Mesh(), vs = A.verts(m), fs = A.faces(m), u = lcg(n)
  for (let j = 0; j <= n; j++) for (let i = 0; i <= n; i++) {
    if (regular) vs.add(i, j, 0); else { const x = i + 0.3 * u(), y = j + 0.3 * u(); vs.add(x, y, u()) }
  }
  for (let j = 0; j < n; j++) for (let i = 0; i < n; i++) { const a = j * (n + 1) + i; A.addQuad(fs, a, a + 1, a + n + 2, a + n + 1) }
  return m
}
const controlPoints = (k, seed) => Array.from({ length: k }, (_, i) => A.P(i, (i * 7 + seed) % 11, (i * 3 + seed) % 5))

// ---- file IO -----------------------------------------------------------------------------------
const models = readdirSync(MODELS).filter(f => f.endsWith('.3dm')).sort().map(f => new Uint8Array(readFileSync(MODELS + '/' + f)))
const modelBytes = new Uint8Array(readFileSync(MODEL))
bench('io.from_byte_array_model', () => { const d = rhino.File3dm.fromByteArray(modelBytes); d.delete() })
bench('io.read_test_models', () => { for (const b of models) rhino.File3dm.fromByteArray(b).delete() })
const big = rhino.File3dm.fromByteArray(modelBytes)
bench('io.write_model_file_v8', () => A.write8(big), undefined, 2)

// ---- iterate a loaded file ---------------------------------------------------------------------
bench('table.iterate_objects_geometry_attrs', () => {
  let n = 0
  A.each(big, o => { const g = A.geom(o), a = A.attrs(o); n += a.layerIndex; g.objectType; g.delete(); a.delete() })
  return n
})
bench('table.iterate_objects_bbox', () => {
  A.each(big, o => { const g = A.geom(o); const b = V9 ? g.getBoundingBox(false) : g.getBoundingBox(); del(b); g.delete() })
})
bench('table.iterate_layers', () => { const out = []; A.eachLayer(big, l => out.push([l.name, l.index])); return out.length })

// ---- building a model --------------------------------------------------------------------------
function populate() {
  const m = new rhino.File3dm(), L = A.layers(m), O = A.objects(m)
  for (let i = 0; i < 1000; i++) L.addLayer('L' + i, { r: 255, g: 0, b: 0, a: 255 })
  for (let i = 0; i < 3000; i++) O.addPoint(A.P(i, i, i))
  for (let i = 0; i < 3000; i++) { const c = rhino.Curve.createControlPointCurve(controlPoints(20, i), 3); O.addCurve(c, null); c.delete() }
  for (let i = 0; i < 300; i++) { const s = A.sphere(A.P(i, 0, 0), 1); O.addSphere(s, null); del(s) }
  const mesh = gridMesh(30)
  for (let i = 0; i < 300; i++) O.addMesh(mesh, null)
  mesh.delete()
  return m
}
bench('create.populate_model', () => populate().delete())
const pop = populate()
bench('create.write_populated_v8', () => A.write8(pop))
const reg = new rhino.File3dm(); { const g = gridMesh(30, true), O = A.objects(reg); for (let i = 0; i < 50; i++) O.addMesh(g, null); g.delete() }
bench('create.write_50_regular_grid_meshes_v8', () => A.write8(reg))
bench('create.write_populated_default_version', () => pop.toByteArray())

// ---- value types -------------------------------------------------------------------------------
const N = 100000
const xf = A.translation(1, 2, 3)
bench('values.transform_multiply_100k', () => { for (let i = 0; i < N; i++) del(A.xmul(xf, xf)) })
const pts = Array.from({ length: N }, (_, i) => A.P(i, i, i))
bench('values.point3d_transform_100k', () => { for (const p of pts) A.xformPoint(p, xf) })

// ---- curves ------------------------------------------------------------------------------------
const curve = rhino.Curve.createControlPointCurve(controlPoints(200, 1), 3)
const [d0, d1] = A.dom(curve)
const ts = Array.from({ length: N }, (_, i) => d0 + (d1 - d0) * i / (N - 1))
bench('curve.point_at_100k', () => { for (const t of ts) curve.pointAt(t) })
bench('curve.tangent_at_100k', () => { for (const t of ts) curve.tangentAt(t) })
// the realistic case: 1,000 curves, parameters in random order (no warm cache, no predictable span)
const curves1k = Array.from({ length: 1000 }, (_, s) => rhino.Curve.createControlPointCurve(controlPoints(200, s), 3))
const calls = shuffled(Array.from({ length: N }, (_, i) => i / (N - 1)), 7).map((u, i) => {
  const c = curves1k[i % 1000], [a, b] = A.dom(c); return [c, a + (b - a) * u]
})
bench('curve.point_at_100k_shuffled_1k_curves', () => { for (const [c, t] of calls) c.pointAt(t) })
bench('curve.create_control_point_5k', () => { for (let i = 0; i < 5000; i++) rhino.Curve.createControlPointCurve(controlPoints(20, i), 3).delete() })

// ---- meshes ------------------------------------------------------------------------------------
bench('mesh.build_grid_200', () => gridMesh(200).delete())
const gm = gridMesh(200)
bench('mesh.compute_normals_200', m => { const n = A.normals(m); n.computeNormals(); m.delete() }, () => gm.duplicate())
bench('mesh.vertices_index_loop_40k', () => {
  const vs = A.verts(gm), n = vs.count; let s = 0
  for (let i = 0; i < n; i++) { const p = vs.get(i); s += V9 ? p.X : p[0] }
  return s
})
bench('mesh.faces_index_loop_40k', () => {
  const fs = A.faces(gm), n = fs.count; let s = 0
  for (let i = 0; i < n; i++) s += A.faceA(fs.get(i))
  return s
})
gm.normals && A.normals(gm).computeNormals()
bench('mesh.toThreejsJSON_40k', () => gm.toThreejsJSON())
bench('mesh.toThreejsBuffers_40k', () => gm.toThreejsBuffers(false))
if (V9) bench('mesh.vertices_toFloatArray_40k', () => gm.vertices.toFloatArray())

// ---- breps -------------------------------------------------------------------------------------
bench('brep.create_from_sphere_1k', () => { for (let i = 0; i < 1000; i++) { const s = A.sphere(A.P(i, 0, 0), 2); rhino.Brep.createFromSphere(s).delete(); del(s) } })
const breps = []
A.each(big, o => { const g = A.geom(o); if (g instanceof rhino.Brep) breps.push(g); else g.delete() })
bench('brep.walk_faces_edges', () => {
  let n = 0
  for (const b of breps) {
    const F = A.brepFaces(b), E = A.brepEdges(b), Vt = A.brepVerts(b)
    for (let i = 0; i < F.count; i++) { const f = F.get(i); const s = f.underlyingSurface(); n += s.degree(0); s.delete(); f.delete() }
    for (let i = 0; i < E.count; i++) { const e = E.get(i); prop(e, 'pointAtStart'); e.delete() }
    n += Vt.count
  }
  return n
})

// ---- SubD --------------------------------------------------------------------------------------
const subdDoc = rhino.File3dm.fromByteArray(models[readdirSync(MODELS).filter(f => f.endsWith('.3dm')).sort().indexOf('subd.3dm')])
const subds = []
A.each(subdDoc, o => { const g = A.geom(o); if (g instanceof rhino.SubD) subds.push(g); else g.delete() })
bench('subd.walk_components', () => {
  let n = 0
  for (const s of subds) A.eachSubD(s, {
    face: f => { n += f.edgeCount },
    edge: e => { n += e.id },
    vert: v => { const p = v.controlNetPoint; del(p) },
  })
  return n
})
bench('subd.subdivide_1', s => { s.subdivide(1); s.delete() }, () => subds[0].duplicate())

// ---- serialization helpers ---------------------------------------------------------------------
bench('draco.compress_grid_200', () => { const c = rhino.DracoCompression.compress(gm); c.toBase64String(); c.delete() })
const dc = rhino.DracoCompression.compress(gm); const b64 = dc.toBase64String(); dc.delete()
bench('draco.decompress_grid_200', () => rhino.DracoCompression.decompressBase64String(b64).delete())
const curves = Array.from({ length: 1000 }, (_, i) => rhino.Curve.createControlPointCurve(controlPoints(20, i), 3))
bench('json.encode_1k_curves', () => curves.map(c => c.encode()))
const encs = curves.map(c => c.encode())
bench('json.decode_1k_curves', () => { for (const e of encs) rhino.CommonObject.decode(e).delete() })

// ---- attributes / user strings -----------------------------------------------------------------
bench('attrs.user_strings_10k', () => {
  const a = new rhino.ObjectAttributes()
  for (let i = 0; i < 10000; i++) a.setUserString('k' + i, 'v' + i)
  const out = []; for (let i = 0; i < 10000; i++) out.push(a.getUserString('k' + i))
  a.delete(); return out.length
})

console.log(JSON.stringify({ lib: 'javascript', version: typeof rhino.Version === 'string' ? rhino.Version : (V9 ? 'current' : '8.35'), runtime: 'node ' + process.version, module: modPath, results }))
