const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('planeRotateTurnsTheAxes', () => {
  // rotate by a quarter turn about Z turns the plane's X axis onto world Y
  const p = rhino.Plane.worldXY
  expect(rhino.Plane.rotate(p, Math.PI / 2, new rhino.Vector3d(0, 0, 1))).toBe(true)
  expect(p.XAxis.Y).toBeCloseTo(1.0, 12)
})

test('planeDecodeRoundTripsEncode', () => {
  // Plane.decode(encode()) gives back the same plane
  const tilted = new rhino.Plane(new rhino.Point3d(1, 2, 3), new rhino.Vector3d(1, 1, 1))
  expect(rhino.Plane.encode(rhino.Plane.decode(rhino.Plane.encode(tilted)))).toEqual(rhino.Plane.encode(tilted))
})
