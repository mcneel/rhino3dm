const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
let polyline1, polyline2
let pointArray, pointList
beforeAll(async () => {
    rhino = await rhino3dm()

    polyline1 = new rhino.Polyline()
    polyline2 = new rhino.Polyline()

    pointArray = []
    pointList = new rhino.Point3dList(5)
    for (let i = 0; i < 15; i++) {
        const point = { X: i, Y: i, Z: i }
        pointList.add(i, i, i)
        pointArray.push(point)
    }

    // Point3dList append(points) -> addRange(points)
    polyline1.addRange(pointArray)
    polyline2.addRange(pointList)

})

test('append', async () => {

    polyline1.add(21, 21, 21)
    polyline2.add(21, 21, 21)

    polyline1.addRange(pointArray)
    polyline2.addRange(pointList)

    const result = polyline1.count === polyline2.count && rhino.Point3d.equals(polyline1.get(10), polyline2.get(10))

    expect(result).toBe(true)

})

test('createFromPoints', async () => {

    // createFromPoints -> new Polyline(points)
    const polyline3 = new rhino.Polyline(pointArray)
    const polyline4 = new rhino.Polyline(pointList)

    const result = polyline3.count === polyline4.count && rhino.Point3d.equals(polyline3.get(10), polyline4.get(10))

    expect(result).toBe(true)

})

test('explode', async () => {

    const explodedCurves = polyline1.getSegments()
    expect(explodedCurves.length > 0).toBe(true)
    expect(Array.isArray(explodedCurves)).toBe(true)
    // D89: RhinoCommon's Line[] -- plain {From, To} values, not LineCurve objects
    expect(explodedCurves[0].From !== undefined && explodedCurves[0].To !== undefined).toBe(true)

})

// a polyline (0,0,0) -> (2,0,0) -> (2,2,0)
const polylineCorner = rhino => new rhino.Polyline([[0, 0, 0], [2, 0, 0], [2, 2, 0]].map(([x, y, z]) => new rhino.Point3d(x, y, z)))

test('polylineSegmentsLengthAndClosestPoint', () => {
  // a Polyline is a Point3dList with segments, a length, pointAt, closestParameter and closestPoint
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const poly = polylineCorner(rhino)
  expect(poly).toBeInstanceOf(rhino.Point3dList)
  expect(poly.count).toBe(3)
  expect(poly.segmentCount).toBe(2)
  expect(poly.length).toBeCloseTo(4.0, 12)
  expect(poly.pointAt(1.5)).toEqual(P(2, 1, 0))
  expect(poly.segmentAt(1).From).toEqual(P(2, 0, 0))
  expect(poly.getSegments().map(s => rhino.Line.Length(s))).toEqual([2.0, 2.0])
  expect(poly.closestParameter(P(1, 5, 0))).toBeCloseTo(2.0, 12)
  expect(poly.closestPoint(P(1, -3, 0))).toEqual(P(1, 0, 0))
})

test('polylineTrimReturnsThePartInTheInterval', () => {
  // trim(interval) returns the Polyline between two parameters
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const t = polylineCorner(rhino).trim(new rhino.Interval(0.5, 1.5))
  expect(t).toBeInstanceOf(rhino.Polyline)
  expect(t.toArray()).toEqual([P(1, 0, 0), P(2, 0, 0), P(2, 1, 0)])
})

test('polylineDeleteShortSegmentsAndDuplicate', () => {
  // deleteShortSegments removes a zero-length segment and returns the count; duplicate is a Polyline with the same points
  const poly = polylineCorner(rhino)
  poly.add(new rhino.Point3d(2, 2, 0))
  expect(poly.deleteShortSegments(1e-9)).toBe(1)
  expect(poly.count).toBe(3)
  const c = poly.duplicate()
  expect(c).toBeInstanceOf(rhino.Polyline)
  expect(c.toArray()).toEqual(poly.toArray())
})

test('polylineToPolylineCurve', () => {
  // toPolylineCurve returns a PolylineCurve through the points, or null for fewer than two points
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const poly = new rhino.Polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
  const crv = poly.toPolylineCurve()
  expect(crv).toBeInstanceOf(rhino.PolylineCurve)
  expect(crv.pointCount).toBe(3)
  expect(crv.pointAtEnd).toEqual(P(3, 4, 0))
  expect(new rhino.Polyline([P(0, 0, 0)]).toPolylineCurve()).toBeNull()
  expect(crv.toPolyline().toArray()).toEqual(poly.toArray())
})

test('polylineToNurbsCurve', () => {
  // toNurbsCurve returns a NurbsCurve with the polyline's ends, or null for fewer than two points
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const nc = new rhino.Polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]).toNurbsCurve()
  expect(nc).toBeInstanceOf(rhino.NurbsCurve)
  expect(nc.pointAtEnd).toEqual(P(3, 4, 0))
  expect(nc.pointAtStart).toEqual(P(0, 0, 0))
  expect(new rhino.Polyline([P(0, 0, 0)]).toNurbsCurve()).toBeNull()
})

test('polylineGetSegmentsWithFewerThanTwoPointsIsNull', () => {
  // getSegments of a polyline with fewer than two points is null, where RhinoCommon returns a null array (D35)
  const pl = new rhino.Polyline()
  pl.add({ X: 0, Y: 0, Z: 0 })
  expect(pl.getSegments()).toBeNull()
  pl.add({ X: 1, Y: 0, Z: 0 })
  expect(pl.getSegments().length).toBe(1)
})

test('polylineClosestPoint', () => {
  // closestPoint returns the point on the polyline nearest a test point
  const pl = new rhino.Polyline([[0, 0, 0], [4, 0, 0], [4, 4, 0]].map(([X, Y, Z]) => new rhino.Point3d(X, Y, Z)))
  expect(pl.count).toBe(3)
  const c = pl.closestPoint(new rhino.Point3d(5, 1, 0))
  expect([c.X, c.Y]).toEqual([4, 1])
})
