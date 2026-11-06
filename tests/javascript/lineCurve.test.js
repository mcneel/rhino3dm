const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('lineCurveTransformMovesTheCurve', () => {
  // transform(xform) applies the given transform to the curve
  const c = new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 0, 0))
  const xf = rhino.Transform.translation(new rhino.Vector3d(0, 0, 5))
  expect(c.transform(xf)).toBe(true)
  expect(c.pointAtStart).toEqual({ X: 0, Y: 0, Z: 5 })
})

test('lineCurveTransformByIdentityReturnsTrue', () => {
  // transform(identity) returns true, as RhinoCommon's early return does
  const c = new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 0, 0))
  expect(c.transform(rhino.Transform.identity)).toBe(true)
})

test('lineCurveConstructsFromPoint3dOrPoint2d', () => {
  // new LineCurve(Point3d, Point3d) and new LineCurve(Point2d, Point2d) both construct, each with its own points
  const a = new rhino.LineCurve(new rhino.Point3d(1, 2, 3), new rhino.Point3d(4, 5, 6)).line
  const b = new rhino.LineCurve(new rhino.Point2d(1, 2), new rhino.Point2d(4, 5)).line
  expect([a.To.Z, b.To.Z]).toEqual([6.0, 0.0])
  expect([a.To.X, a.To.Y, b.To.X, b.To.Y]).toEqual([4, 5, 4, 5])
})

test('lineCurveDomainSetterWritesThrough', () => {
  // setting domain changes the curve's domain
  const c = new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(2, 0, 0))
  c.domain = new rhino.Interval(1, 5)
  expect([c.domain.T0, c.domain.T1]).toEqual([1.0, 5.0])
})
