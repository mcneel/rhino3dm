const rhino3dm = require('rhino3dm')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

function quadMesh(rhino) {
    const m = new rhino.Mesh()
    const v = m.vertices()
    v.add(0, 0, 0); v.add(1, 0, 0); v.add(1, 1, 0); v.add(0, 1, 0)
    m.faces().addQuadFace(0, 1, 2, 3)
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
    const tc = mesh.textureCoordinates()
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
    const v = m.vertices()
    for (let y = 0; y <= G; y++)
        for (let x = 0; x <= G; x++)
            v.add(x, y, Math.sin(x * 0.1) * Math.cos(y * 0.1))
    const f = m.faces()
    const stride = G + 1
    for (let y = 0; y < G; y++) {
        for (let x = 0; x < G; x++) {
            const a = y * stride + x
            f.addQuadFace(a, a + 1, a + stride + 1, a + stride)
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
