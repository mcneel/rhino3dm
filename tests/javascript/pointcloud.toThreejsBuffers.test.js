const rhino3dm = require('rhino3dm')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

// Bounded coordinates keep float32 rounding (buffers path) well under the tolerances below,
// while still covering negative/fractional values.
function makeCloud(rhino, n, { withColor = false, withNormal = false } = {}) {
    const pc = new rhino.PointCloud()
    for (let i = 0; i < n; i++) {
        const p = [ (i % 512) * 0.25 - 64, ((i * 3) % 512) * 0.25 - 64, Math.sin(i * 0.05) * 10 ]
        const color = { r: i % 256, g: 0, b: 255 - (i % 256), a: 0 }
        const normal = [ 0, 0, 1 ]
        if (withColor && withNormal) pc.addPointNormalColor(p, normal, color)
        else if (withColor) pc.addPointColor(p, color)
        else if (withNormal) pc.addPointNormal(p, normal)
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
    pc.add([ 1, 2, 3 ])
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
