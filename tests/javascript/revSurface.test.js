const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('revSurfaceCreateFromSphereIsAFullRevolution', () => {
  // RevSurface.createFromSphere returns a surface of revolution through 2 pi with a coplanar axis
  const s = rhino.RevSurface.createFromSphere(new rhino.Sphere(new rhino.Point3d(0, 0, 0), 2.0))
  expect(s).toBeInstanceOf(rhino.RevSurface)
  expect(s).toBeInstanceOf(rhino.Surface)
  expect(s.angle.T1 - s.angle.T0).toBeCloseTo(2 * Math.PI, 9)
  expect(s.axisAndCurveCoplanar()).toBe(true)
})
