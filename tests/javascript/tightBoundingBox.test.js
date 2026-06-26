const rhino3dm = require('rhino3dm')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

// RH3DM-188: GetTightBoundingBox exposed on GeometryBase (Py/JS parity with .NET)
test('getTightBoundingBox on sphere brep', async () => {
    const brep = new rhino.Sphere([0, 0, 0], 5).toBrep()
    const tight = brep.getTightBoundingBox()
    expect(tight.isValid).toBe(true)
    expect(tight.min[0]).toBeCloseTo(-5, 3)
    expect(tight.min[1]).toBeCloseTo(-5, 3)
    expect(tight.min[2]).toBeCloseTo(-5, 3)
    expect(tight.max[0]).toBeCloseTo(5, 3)
    expect(tight.max[1]).toBeCloseTo(5, 3)
    expect(tight.max[2]).toBeCloseTo(5, 3)
})

test('getTightBoundingBox on mesh', async () => {
    const mesh = new rhino.Mesh()
    const v = mesh.vertices()
    v.add(0, 0, 0); v.add(2, 0, 0); v.add(2, 3, 0); v.add(0, 3, 0)
    mesh.faces().addQuadFace(0, 1, 2, 3)
    const tight = mesh.getTightBoundingBox()
    expect(tight.isValid).toBe(true)
    expect(tight.min[0]).toBeCloseTo(0, 6)
    expect(tight.max[0]).toBeCloseTo(2, 6)
    expect(tight.max[1]).toBeCloseTo(3, 6)
})
