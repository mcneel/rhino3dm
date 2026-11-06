const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

// RH3DM-188: GetTightBoundingBox exposed on GeometryBase (Py/JS parity with .NET)
test('getTightBoundingBox on sphere brep', async () => {
    const brep = rhino.Sphere.toBrep(rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5))
    // getTightBoundingBox -> getBoundingBox(true)
    const tight = brep.getBoundingBox(true)
    expect(rhino.BoundingBox.isValid(tight)).toBe(true)
    expect(tight.Min.X).toBeCloseTo(-5, 3)
    expect(tight.Min.Y).toBeCloseTo(-5, 3)
    expect(tight.Min.Z).toBeCloseTo(-5, 3)
    expect(tight.Max.X).toBeCloseTo(5, 3)
    expect(tight.Max.Y).toBeCloseTo(5, 3)
    expect(tight.Max.Z).toBeCloseTo(5, 3)
})

test('getTightBoundingBox on mesh', async () => {
    const mesh = new rhino.Mesh()
    const v = mesh.vertices
    v.add(0, 0, 0); v.add(2, 0, 0); v.add(2, 3, 0); v.add(0, 3, 0)
    mesh.faces.addFace(0, 1, 2, 3)
    const tight = mesh.getBoundingBox(true)
    expect(rhino.BoundingBox.isValid(tight)).toBe(true)
    expect(tight.Min.X).toBeCloseTo(0, 6)
    expect(tight.Max.X).toBeCloseTo(2, 6)
    expect(tight.Max.Y).toBeCloseTo(3, 6)
})

test('brepTightBoundingBoxOfABox', () => {
  // getBoundingBox(true) of a box brep is the box
  const b = helpers.boxBrep(rhino).getBoundingBox(true)
  expect([b.Min.X, b.Max.Y, b.Max.Z]).toEqual([0, 2, 3])
})
