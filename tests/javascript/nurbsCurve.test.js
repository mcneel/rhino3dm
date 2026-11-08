const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
let crvFromList, crvFromArray
beforeAll(async () => {
  rhino = await rhino3dm()

  const pointArray = [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }, { X: 3, Y: -1, Z: 0 }, { X: 4, Y: 0, Z: 0 }]

  const pointList = new rhino.Point3dList(5)
  pointList.add(0, 0, 0)
  pointList.add(1, 1, 0)
  pointList.add(2, 0, 0)
  pointList.add(3, -1, 0)
  pointList.add(4, 0, 0)

  crvFromList = rhino.NurbsCurve.create(false, 3, pointList)
  crvFromArray = rhino.NurbsCurve.create(false, 3, pointArray)

})

test('NurbsCurve_create', async () => {

  const result = crvFromArray.points.count === crvFromList.points.count && crvFromArray.points.controlPolygonLength === crvFromList.points.controlPolygonLength

  expect(result).toBe(true)

})

test('knotsToList', async () => {

  // toList -> iteration
  const knotsList = [...crvFromList.knots]

  expect(Array.isArray(knotsList)).toBe(true)
  expect(typeof knotsList[0] === 'number').toBe(true)
  expect(knotsList.length == crvFromList.knots.count).toBe(true)

})

const nurbsCurveCircle = rhino => rhino.NurbsCurve.createFromCircle(new rhino.Circle(new rhino.Point3d(0, 0, 0), 1.0))

test('nurbsCurveConvertSpanToBezierReturnsABezierOrNone', () => {
  // convertSpanToBezier returns a BezierCurve for a valid span index and null past the last span
  const nc = nurbsCurveCircle(rhino)
  expect(nc.convertSpanToBezier(0)).toBeInstanceOf(rhino.BezierCurve)
  expect(nc.convertSpanToBezier(99)).toBeNull()
})

test('nurbsCurvePointListGetWeightRaisesOutsideTheList', () => {
  // points.getWeight returns a number for a valid index and throws for -1 and count
  const pts = nurbsCurveCircle(rhino).points
  expect(typeof pts.getWeight(0)).toBe('number')
  for (const bad of [-1, pts.count]) helpers.raises(rhino, () => pts.getWeight(bad), /out_of_range,Index must be (greater|less)/)
})

test('nurbsCurveCreateFromEllipseStartsOnTheMajorAxis', () => {
  // createFromEllipse returns a NurbsCurve starting at the end of the first radius
  const nc = rhino.NurbsCurve.createFromEllipse(new rhino.Ellipse(rhino.Plane.worldXY, 2.0, 1.0))
  expect(nc).toBeInstanceOf(rhino.NurbsCurve)
  expect(nc.pointAtStart.X).toBeCloseTo(2.0, 9)
})

test('nurbsCurveCreateFromLine', () => {
  // createFromLine returns a NurbsCurve along the line, or null for a zero-length line
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const nc = rhino.NurbsCurve.createFromLine(new rhino.Line(P(0, 0, 0), P(3, 0, 0)))
  expect(nc).toBeInstanceOf(rhino.NurbsCurve)
  expect(nc.pointAtEnd).toEqual(P(3, 0, 0))
  expect(rhino.NurbsCurve.createFromLine(new rhino.Line(P(1, 1, 1), P(1, 1, 1)))).toBeNull()
})

test('nurbsCurveCreateFromPoints', () => {
  // NurbsCurve.create(periodic, degree, points) returns a curve through the end points, or null for fewer than two points
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pts = [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)]
  const nc = rhino.NurbsCurve.create(false, 3, pts)
  expect(nc).toBeInstanceOf(rhino.NurbsCurve)
  expect(nc.pointAtEnd).toEqual(P(3, 1, 0))
  expect(rhino.NurbsCurve.create(false, 3, [P(0, 0, 0)])).toBeNull()
})

test('nurbsCurvePointListIndexAndIterate', () => {
  // nurbsCurve.points has a count, iterates over the control points, and throws for a negative index
  const P = (X, Y, Z) => ({ X, Y, Z })
  const nc = rhino.NurbsCurve.create(false, 3, [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)])
  const cps = nc.points
  expect(cps.count).toBe(4)
  expect([...cps].map(c => c.X)).toEqual([0, 1, 2, 3])
  helpers.raises(rhino, () => cps.get(-1), /greater than or equal to zero/)
})

test('nurbsCurvePointsGetPoint4dReturnsTheWeightedPoint', () => {
  // points.getPoint gives the Euclidean point and getPoint4d the homogeneous one
  const c = rhino.NurbsCurve.create(false, 2, [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }])
  const [ok4, p4] = c.points.getPoint4d(1)
  expect(ok4).toBe(true)
  expect([p4.X, p4.Y, p4.W]).toEqual([1, 1, 1])
  const [ok, p3] = c.points.getPoint(1)
  expect(ok).toBe(true)
  expect(p3.W).toBeUndefined()
  expect(p3.X).toBe(1)
})

test('nurbsCurveConstructorsFromDegreeAndCounts', () => {
  // new NurbsCurve(degree, pointCount) and new NurbsCurve(dimension, rational, order, pointCount)
  const c = new rhino.NurbsCurve(3, 5)
  expect([c.degree, c.order, c.points.count, c.dimension, c.isRational]).toEqual([3, 4, 5, 3, false])
  expect(c.knots.count).toBe(7)
  const r = new rhino.NurbsCurve(2, true, 3, 4)
  expect([r.dimension, r.isRational, r.order, r.points.count]).toEqual([2, true, 3, 4])
})
