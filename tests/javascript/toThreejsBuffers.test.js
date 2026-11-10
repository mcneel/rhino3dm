const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

function quadMesh(rhino) {
    const m = new rhino.Mesh()
    const v = m.vertices
    v.add(0, 0, 0); v.add(1, 0, 0); v.add(1, 1, 0); v.add(0, 1, 0)
    m.faces.addFace(0, 1, 2, 3)
    return m
}

// RH3DM-191: zero-copy mesh export. toThreejsBuffers() returns typed arrays built with a single
// bulk copy per attribute; the data must match the existing element-by-element toThreejsJSON().
test('toThreejsBuffers returns typed arrays matching toThreejsJSON', async () => {
    const mesh = quadMesh(rhino)
    const json = mesh.toThreejsJSON()
    const buf = mesh.toThreejsBuffers(false)

    expect(buf.position instanceof Float32Array).toBe(true)
    expect(buf.normal instanceof Float32Array).toBe(true)
    expect(buf.index instanceof Uint32Array).toBe(true)
    expect(buf.vertexCount).toBe(4)

    // position matches the JSON path exactly
    const jsonPos = json.data.attributes.position.array
    expect(buf.position.length).toBe(jsonPos.length)
    for (let i = 0; i < jsonPos.length; i++) expect(buf.position[i]).toBeCloseTo(jsonPos[i], 5)

    // normal matches
    const jsonNrm = json.data.attributes.normal.array
    expect(buf.normal.length).toBe(jsonNrm.length)
    for (let i = 0; i < jsonNrm.length; i++) expect(buf.normal[i]).toBeCloseTo(jsonNrm[i], 5)

    // index matches (quad -> 2 triangles = 6 indices)
    expect(buf.index.length).toBe(6)
    expect(Array.from(buf.index)).toEqual(Array.from(json.data.index.array))

    // no uv/color on a plain mesh
    expect(buf.uv).toBeUndefined()
    expect(buf.color).toBeUndefined()
})

test('toThreejsBuffers includes uv when texture coordinates are present', async () => {
    const mesh = quadMesh(rhino)
    const tc = mesh.textureCoordinates
    tc.add(0, 0); tc.add(1, 0); tc.add(1, 1); tc.add(0, 1)

    const buf = mesh.toThreejsBuffers(false)
    expect(buf.uv instanceof Float32Array).toBe(true)
    expect(buf.uv.length).toBe(8)
    expect(Array.from(buf.uv)).toEqual([0, 0, 1, 0, 1, 1, 0, 1])
})

test('toThreejsBuffers(true) rotates to Y-up like toThreejsJSONRotate(true)', async () => {
    const mesh = quadMesh(rhino)
    const json = mesh.toThreejsJSONRotate(true)
    const buf = mesh.toThreejsBuffers(true)

    const jsonPos = json.data.attributes.position.array
    expect(buf.position.length).toBe(jsonPos.length)
    for (let i = 0; i < jsonPos.length; i++) expect(buf.position[i]).toBeCloseTo(jsonPos[i], 5)
})

// builds a G x G grid of quads -> (G+1)^2 vertices, G^2 quads
function gridMesh(rhino, G) {
    const m = new rhino.Mesh()
    const v = m.vertices
    for (let y = 0; y <= G; y++)
        for (let x = 0; x <= G; x++)
            v.add(x, y, Math.sin(x * 0.1) * Math.cos(y * 0.1))
    const f = m.faces
    const stride = G + 1
    for (let y = 0; y < G; y++) {
        for (let x = 0; x < G; x++) {
            const a = y * stride + x
            f.addFace(a, a + 1, a + stride + 1, a + stride)
        }
    }
    return m
}

// RH3DM-191: stress test. The element-by-element toThreejsJSON() crosses the embind boundary
// once per scalar; toThreejsBuffers() does one bulk copy per attribute. Verify the new path is
// (a) numerically identical to the old one and (b) measurably faster on a large mesh.
test('toThreejsBuffers matches toThreejsJSON on a large mesh and is faster', async () => {
    const G = 250 // 62,001 vertices, 62,500 quads -> 125,000 triangles
    const mesh = gridMesh(rhino, G)

    const t0 = Date.now()
    const json = mesh.toThreejsJSON()
    const t1 = Date.now()
    const buf = mesh.toThreejsBuffers(false)
    const t2 = Date.now()

    const jsonMs = t1 - t0
    const bufMs = t2 - t1
    console.log(`toThreejsJSON: ${jsonMs}ms  toThreejsBuffers: ${bufMs}ms  speedup: ${(jsonMs / Math.max(bufMs, 1)).toFixed(1)}x`)

    // identical vertex count
    expect(buf.vertexCount).toBe((G + 1) * (G + 1))

    // positions identical
    const jsonPos = json.data.attributes.position.array
    expect(buf.position.length).toBe(jsonPos.length)
    let maxDiff = 0
    for (let i = 0; i < jsonPos.length; i++)
        maxDiff = Math.max(maxDiff, Math.abs(buf.position[i] - jsonPos[i]))
    expect(maxDiff).toBeLessThan(1e-4)

    // index identical
    const jsonIdx = json.data.index.array
    expect(buf.index.length).toBe(jsonIdx.length)
    for (let i = 0; i < jsonIdx.length; i++) expect(buf.index[i]).toBe(jsonIdx[i])

    // the typed-array path should not be slower than the per-element JSON path
    expect(bufMs).toBeLessThanOrEqual(jsonMs)
}, 60000)

// a quad and a triangle over 5 vertices, with normals, texture coordinates and vertex colors
function meshForThreejsBuffers(rhino) {
  const m = new rhino.Mesh()
  for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.vertices.add(x, y, 0)
  m.faces.addFace(0, 1, 2, 3); m.faces.addFace(1, 4, 2)
  m.normals.computeNormals()
  for (const [u, v] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.textureCoordinates.add(u, v)
  for (let i = 0; i < 5; i++) m.vertexColors.add(255, i * 50, 0)
  return m
}

test('meshToThreejsBuffersGivesTypedArraysWithQuadsAsTriangles', () => {
  // toThreejsBuffers(rotate) returns typed arrays, a quad as two triangles; rotate turns Z-up into Y-up
  const m = meshForThreejsBuffers(rhino)
  const position = { false: [0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 2, 0, 0], true: [0, 0, 0, 1, 0, 0, 1, 0, -1, 0, 0, -1, 2, 0, 0] }
  for (const rot of [false, true]) {
    const b = m.toThreejsBuffers(rot)
    expect(b.position).toBeInstanceOf(Float32Array)
    expect(b.index).toBeInstanceOf(Uint32Array)
    expect(b.normal).toBeInstanceOf(Float32Array)
    expect(b.uv).toBeInstanceOf(Float32Array)
    expect(b.color).toBeInstanceOf(Float32Array)
    expect(Array.from(b.index)).toEqual([0, 1, 2, 2, 3, 0, 1, 4, 2])
    expect(Array.from(b.position)).toEqual(position[rot])
  }
})

test('meshCreateFromThreejsBuffersInvertsToThreejsBuffers', () => {
  // createFromThreejsBuffers(buffers, rotate) gives back the vertices, the triangles, the uvs and the colors
  const m = meshForThreejsBuffers(rhino)
  for (const rot of [false, true]) {
    const back = rhino.Mesh.createFromThreejsBuffers(m.toThreejsBuffers(rot), rot)
    expect(back.vertices.count).toBe(5)
    expect(back.faces.count).toBe(3)                        // a quad comes back as two triangles
    for (let i = 0; i < 5; i++) {
      const p = back.vertices.point3dAt(i), q = m.vertices.point3dAt(i)
      expect(p.X).toBeCloseTo(q.X, 6); expect(p.Y).toBeCloseTo(q.Y, 6); expect(p.Z).toBeCloseTo(q.Z, 6)
    }
    expect(Array.from(back.vertexColors.toARGBArray())).toEqual(Array.from(m.vertexColors.toARGBArray()))
    expect(back.textureCoordinates.count).toBe(5)
  }
})

test('meshCreateFromThreejsBuffersReadsNonIndexedBuffers', () => {
  // without an index, every 3 vertices are a triangle
  const tri = rhino.Mesh.createFromThreejsBuffers({ position: new Float32Array([0, 0, 0, 1, 0, 0, 0, 1, 0]) }, false)
  expect([tri.vertices.count, tri.faces.count]).toEqual([3, 1])
})

test('meshCreateFromThreejsBuffersRefusesBadBuffers', () => {
  // a position length that is not a multiple of 3, or an index past the vertices, throws
  helpers.raises(rhino, () => rhino.Mesh.createFromThreejsBuffers({ position: [0, 0, 0, 1] }, false), /'position' has 4 numbers/)
  helpers.raises(rhino, () => rhino.Mesh.createFromThreejsBuffers({ position: [0, 0, 0, 1, 0, 0, 0, 1, 0], index: [0, 1, 7] }, false),
    /outside the vertices/)
})
