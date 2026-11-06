const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

// Bounded coordinates keep float32 rounding (buffers path) well under the tolerances below,
// while still covering negative/fractional values.
function makeCloud(rhino, n, { withColor = false, withNormal = false } = {}) {
    const pc = new rhino.PointCloud()
    for (let i = 0; i < n; i++) {
        const p = { X: (i % 512) * 0.25 - 64, Y: ((i * 3) % 512) * 0.25 - 64, Z: Math.sin(i * 0.05) * 10 }
        const color = { r: i % 256, g: 0, b: 255 - (i % 256), a: 0 }
        const normal = { X: 0, Y: 0, Z: 1 }
        // addPointNormalColor / addPointColor / addPointNormal -> add overloads
        if (withColor && withNormal) pc.add(p, normal, color)
        else if (withColor) pc.add(p, color)
        else if (withNormal) pc.add(p, normal)
        else pc.add(p)
    }
    return pc
}

// RH3DM-191 (8.32.1): zero-copy point-cloud export. PointCloud.toThreejsBuffers() returns typed
// arrays built with a single bulk copy per attribute; the data must match the existing
// element-by-element toThreejsJSON(). Point clouds are non-indexed (no index/faces).
test('toThreejsBuffers returns typed arrays matching toThreejsJSON (points only)', async () => {
    const pc = makeCloud(rhino, 12)
    const json = pc.toThreejsJSON()
    const buf = pc.toThreejsBuffers(false)

    expect(buf.position instanceof Float32Array).toBe(true)
    expect(buf.pointCount).toBe(12)

    const jsonPos = json.data.attributes.position.array
    expect(buf.position.length).toBe(jsonPos.length)
    for (let i = 0; i < jsonPos.length; i++) expect(buf.position[i]).toBeCloseTo(jsonPos[i], 4)

    // point clouds have no index, and no color/normal unless added
    expect(buf.index).toBeUndefined()
    expect(buf.color).toBeUndefined()
    expect(buf.normal).toBeUndefined()
})

test('toThreejsBuffers includes color (normalized 0..1) when present', async () => {
    const pc = makeCloud(rhino, 12, { withColor: true })
    const json = pc.toThreejsJSON()
    const buf = pc.toThreejsBuffers(false)

    expect(buf.color instanceof Float32Array).toBe(true)
    const jsonCol = json.data.attributes.color.array
    expect(buf.color.length).toBe(jsonCol.length)
    for (let i = 0; i < jsonCol.length; i++) expect(buf.color[i]).toBeCloseTo(jsonCol[i], 5)
    for (const c of buf.color) {
        expect(c).toBeGreaterThanOrEqual(0)
        expect(c).toBeLessThanOrEqual(1)
    }
})

test('toThreejsBuffers includes normal when present', async () => {
    const pc = makeCloud(rhino, 8, { withNormal: true })
    const json = pc.toThreejsJSON()
    const buf = pc.toThreejsBuffers(false)

    expect(buf.normal instanceof Float32Array).toBe(true)
    const jsonNrm = json.data.attributes.normal.array
    expect(buf.normal.length).toBe(jsonNrm.length)
    for (let i = 0; i < jsonNrm.length; i++) expect(buf.normal[i]).toBeCloseTo(jsonNrm[i], 4)
})

test('toThreejsBuffers(true) rotates Z-up to Y-up', async () => {
    const pc = new rhino.PointCloud()
    pc.add({ X: 1, Y: 2, Z: 3 })
    const buf = pc.toThreejsBuffers(true)
    // Rhino is Z-up, three.js is Y-up: (x, y, z) -> (x, z, -y)
    expect(buf.position[0]).toBeCloseTo(1, 4)
    expect(buf.position[1]).toBeCloseTo(3, 4)
    expect(buf.position[2]).toBeCloseTo(-2, 4)
})

// Stress test: toThreejsJSON() crosses the embind boundary once per scalar; toThreejsBuffers()
// does one bulk copy per attribute. Verify (a) numerically identical and (b) not slower.
test('toThreejsBuffers matches toThreejsJSON on a large cloud and is faster', async () => {
    const n = 150000
    const pc = makeCloud(rhino, n)

    const t0 = Date.now()
    const json = pc.toThreejsJSON()
    const t1 = Date.now()
    const buf = pc.toThreejsBuffers(false)
    const t2 = Date.now()

    const jsonMs = t1 - t0
    const bufMs = t2 - t1
    console.log(`PointCloud toThreejsJSON: ${jsonMs}ms  toThreejsBuffers: ${bufMs}ms  speedup: ${(jsonMs / Math.max(bufMs, 1)).toFixed(1)}x`)

    expect(buf.pointCount).toBe(n)

    const jsonPos = json.data.attributes.position.array
    expect(buf.position.length).toBe(jsonPos.length)
    let maxDiff = 0
    for (let i = 0; i < jsonPos.length; i++)
        maxDiff = Math.max(maxDiff, Math.abs(buf.position[i] - jsonPos[i]))
    expect(maxDiff).toBeLessThan(1e-3) // float32 (buffers) vs float64 (json), bounded coords

    expect(bufMs).toBeLessThanOrEqual(jsonMs)
}, 60000)

// two points with normals and colors (red, blue)
function pointCloudForThreejsBuffers(rhino) {
  const pc = new rhino.PointCloud()
  pc.add(new rhino.Point3d(1, 2, 3), new rhino.Vector3d(0, 0, 1), { r: 255, g: 0, b: 0, a: 255 })
  pc.add(new rhino.Point3d(4, 5, 6), new rhino.Vector3d(0, 1, 0), { r: 0, g: 0, b: 255, a: 255 })
  return pc
}

test('pointCloudThreejsBuffersRoundTrip', () => {
  // toThreejsBuffers gives positions, colors (0..1) and normals; createFromThreejsBuffers reads them back
  const pc = pointCloudForThreejsBuffers(rhino)
  const b = pc.toThreejsBuffers(false)
  expect(Array.from(b.position)).toEqual([1, 2, 3, 4, 5, 6])
  expect(Array.from(b.color)).toEqual([1, 0, 0, 0, 0, 1])
  const back = rhino.PointCloud.createFromThreejsBuffers(b, false)
  expect(Array.from(back.getPoints()).map(q => [q.X, q.Y, q.Z])).toEqual([[1, 2, 3], [4, 5, 6]])
  expect(Array.from(back.getColors()).map(k => [k.r, k.g, k.b])).toEqual([[255, 0, 0], [0, 0, 255]])
  expect(Array.from(back.getNormals()).map(q => [q.X, q.Y, q.Z])).toEqual([[0, 0, 1], [0, 1, 0]])
})

test('pointCloudToThreejsBuffersRotatesToYUp', () => {
  // toThreejsBuffers(true) maps (x, y, z) to (x, z, -y), the points and their normals alike
  const b = pointCloudForThreejsBuffers(rhino).toThreejsBuffers(true)
  expect(Array.from(b.position)).toEqual([1, 3, -2, 4, 6, -5])
  expect(Array.from(b.normal).map(v => v + 0)).toEqual([0, 1, 0, 0, 0, -1])
})

test('pointCloudThreejsBuffersRoundTripThroughYUp', () => {
  // createFromThreejsBuffers(buffers, true) undoes toThreejsBuffers(true): points and normals come back
  const b = pointCloudForThreejsBuffers(rhino).toThreejsBuffers(true)
  const back = rhino.PointCloud.createFromThreejsBuffers(b, true)
  const round = vs => vs.map(q => [q.X, q.Y, q.Z].map(c => Math.round(c * 1e6) / 1e6 + 0))
  expect(round(Array.from(back.getPoints()))).toEqual([[1, 2, 3], [4, 5, 6]])
  expect(round(Array.from(back.getNormals()))).toEqual([[0, 0, 1], [0, 1, 0]])
})
