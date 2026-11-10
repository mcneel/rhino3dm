const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

test('append', async () => {

    const sphere = rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5)

    const result = rhino.Sphere.closestParameter(sphere, { X: 0, Y: 0, Z: 5 })

    expect(Array.isArray(result)).toBe(true)
    expect(result.length === 3).toBe(true)
    expect(typeof result[0] === 'boolean').toBe(true)
    expect(typeof result[1] === 'number').toBe(true)
    expect(typeof result[2] === 'number').toBe(true)

    expect(result[0]).toBe(true)

})

const SPHERE_8X_JSON = { Radius: 4, EquatorialPlane: { Origin: { X: 1, Y: 2, Z: 3 }, XAxis: { X: 1, Y: 0, Z: 0 },
  YAxis: { X: 0, Y: 1, Z: 0 }, ZAxis: { X: 0, Y: 0, Z: 1 } } }

test('sphereToRevSurfaceReturnsARevSurface', () => {
  // Sphere.toRevSurface returns a RevSurface
  expect(rhino.Sphere.toRevSurface(new rhino.Sphere(new rhino.Point3d(0, 0, 0), 1.0))).toBeInstanceOf(rhino.RevSurface)
})

test('sphereDecodeKeepsTheWholeEquatorialPlane', () => {
  // Sphere.decode(encode()) gives back the same sphere, a tilted equatorial plane included
  const tilted = new rhino.Plane(new rhino.Point3d(1, 2, 3), new rhino.Vector3d(1, 1, 1))
  const s = new rhino.Sphere(tilted, 4)
  expect(rhino.Sphere.encode(rhino.Sphere.decode(rhino.Sphere.encode(s)))).toEqual(rhino.Sphere.encode(s))
})

test('sphereDecodeReadsThe8xJsonForm', () => {
  // Sphere.decode reads the 8.x JSON object
  expect(rhino.Sphere.decode(SPHERE_8X_JSON).Radius).toBe(4)
})
