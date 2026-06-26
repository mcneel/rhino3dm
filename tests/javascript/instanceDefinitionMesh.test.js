const rhino3dm = require('rhino3dm')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

function makeMesh(rhino, n) {
    const m = new rhino.Mesh()
    const v = m.vertices()
    for (let i = 0; i < n; i++) v.add((i % 100) * 0.1, Math.floor(i / 100) * 0.1, (i % 5) * 0.05)
    const f = m.faces()
    for (let i = 0; i + 3 < n; i += 4) f.addQuadFace(i, i + 1, i + 2, i + 3)
    return m
}

// RH-86691: adding instance definitions whose objects carry meshes used to throw
// "memory access out of bounds" in WASM — a by-value copy of ObjectAttributes freed the
// ON_3dmObjectAttributes the JS wrapper still owned, leaving the mesh-modifier back-pointer
// dangling. The crash is data-size dependent (the freed region must be recycled to fault),
// so this uses large meshes: ~20k vertices reliably reproduces it on the pre-fix build.
// Fixed by the deep-copy copy ctor of BND_3dmObjectAttributes.
test('add instance definitions with large meshes does not crash (RH-86691)', async () => {
    const file = new rhino.File3dm()
    const idefs = file.instanceDefinitions()
    for (let k = 0; k < 3; k++) {
        const mesh = makeMesh(rhino, 20000)
        const attr = new rhino.ObjectAttributes()
        const idx = idefs.add('idef' + k, '', '', '', [0, 0, 0], [mesh], [attr])
        expect(idx).toBeGreaterThanOrEqual(0)
    }
    expect(idefs.count).toBe(3)
})
