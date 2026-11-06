const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino

let crvFromList, crvFromArray

beforeAll(async() => {

  rhino = await rhino3dm()

  const pointArray = [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }, { X: 3, Y: -1, Z: 0 }, { X: 4, Y: 0, Z: 0 }]

  const pointList = new rhino.Point3dList(5)
  pointList.add(0, 0, 0)
  pointList.add(1, 1, 0)
  pointList.add(2, 0, 0)
  pointList.add(3, -1, 0)
  pointList.add(4, 0, 0)

  crvFromList = rhino.Curve.createControlPointCurve(pointList, 3)
  crvFromArray = rhino.Curve.createControlPointCurve(pointArray, 3)


});
beforeEach( async() => {
    
  })

test('createControlPointCurve', async () => {

    const r1 = crvFromArray.pointAt(0.5)
    const r2 = crvFromList.pointAt(0.5)
    
    const result = r1.X === r2.X && r1.Y === r2.Y && r1.Z === r2.Z

    expect(result).toBe(true)

})

test('frameAt', async () => {

  const result = crvFromArray.frameAt(0.5)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "boolean")
  expect(typeof result[1] == "object")
  expect(Object.keys(result[1]).length).toBe(4) //this is a Plane
  // D11: plain object with RhinoCommon's field names
  expect(Object.keys(result[1])[0]).toBe("Origin")
  expect(Object.keys(result[1])[1]).toBe("XAxis")
  expect(Object.keys(result[1])[2]).toBe("YAxis")
  expect(Object.keys(result[1])[3]).toBe("ZAxis")
  expect(result[0]).toBe(true)

})

test('getCurveParameterFromNurbsFormParameter', async () => {

  const result = crvFromArray.getCurveParameterFromNurbsFormParameter(0.5)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "boolean")
  expect(typeof result[1] == "number")
  expect(result[0]).toBe(true)

})

test('getNurbsFormParameterFromCurveParameter', async () => {

  const result = crvFromArray.getNurbsFormParameterFromCurveParameter(0.5)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "boolean")
  expect(typeof result[1] == "number")
  expect(result[0]).toBe(true)

})

test('split', async () => {

  const result = crvFromArray.split(0.1)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "object")
  expect(result[0].constructor.name).toBe("NurbsCurve")
  expect(typeof result[1] == "object")
  expect(result[1].constructor.name).toBe("NurbsCurve")

})

test('deriativeAt', async () => {

  const result = crvFromArray.derivativeAt(0.5, 2)

  expect(Array.isArray(result)).toBe(true)
  result.forEach( item => {
    // D11: {X, Y, Z} objects (were [x, y, z] arrays)
    expect(Object.keys(item)).toEqual(['X', 'Y', 'Z'])
  })

})

test('deriativeAt2', async () => {

  // derivativeAtSide -> derivativeAt(t, count, side)
  const result = crvFromArray.derivativeAt(0.5, 2, rhino.CurveEvaluationSide.Below)

  expect(Array.isArray(result)).toBe(true)
  result.forEach( item => {
    // D11: {X, Y, Z} objects (were [x, y, z] arrays)
    expect(Object.keys(item)).toEqual(['X', 'Y', 'Z'])
  })

})

// an arc curve on a circle of radius 2 and a line curve of length 10, both from the origin
const curveCircleAndLine = rhino => [
  new rhino.ArcCurve(new rhino.Circle(new rhino.Point3d(0, 0, 0), 2.0)),
  new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(10, 0, 0))]

test('curveSpanDomainRaisesForANegativeIndex', () => {
  // spanDomain works in range and throws for a negative span index, as RhinoCommon does (D9)
  const x = helpers.curve(rhino)
  expect(typeof x.spanDomain(0).T0).toBe('number')
  helpers.raises(rhino, () => x.spanDomain(-1), /out_of_range,spanIndex must be larger than or equal to zero/)
})

test('curveSpanVectorListsTheSpanParameters', () => {
  // spanVector returns the span parameters; a one-span curve gives its domain ends
  const c = helpers.curve(rhino)
  expect(c.spanVector()).toEqual([c.domain.T0, c.domain.T1])
})

test('curveDuplicateReturnsTheMostDerivedClass', () => {
  // Curve.duplicate on a NurbsCurve returns a NurbsCurve
  const nc = rhino.NurbsCurve.createFromLine(new rhino.Line(new rhino.Point3d(0, 0, 0), new rhino.Point3d(0, 1, 0)))
  expect(rhino.Curve.prototype.duplicate.call(nc)).toBeInstanceOf(rhino.NurbsCurve)
})

test('curveSplitReturnsTheTwoHalves', () => {
  // split(t) returns the two curves on either side of t
  const c = new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(2, 0, 0))
  const parts = c.split(1.0)
  expect(parts.length).toBe(2)
  expect(parts.every(p => p instanceof rhino.Curve)).toBe(true)
  expect(parts[0].pointAtEnd).toEqual(new rhino.Point3d(1, 0, 0))
  expect(parts[1].pointAtStart).toEqual(new rhino.Point3d(1, 0, 0))
})

test('curveSplitOutsideTheDomainReturnsNone', () => {
  // split at a parameter outside the domain returns null
  const c = new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(2, 0, 0))
  expect(c.split(5.0) == null).toBe(true)
})

test('curveClosedCurveOrientation', () => {
  // closedCurveOrientation is CounterClockwise for a circle seen from +Z, Clockwise from -Z and Undefined for an open curve
  const [circle, line] = curveCircleAndLine(rhino)
  const O = rhino.CurveOrientation
  expect(circle.closedCurveOrientation()).toBe(O.CounterClockwise)
  expect(circle.closedCurveOrientation(new rhino.Vector3d(0, 0, -1))).toBe(O.Clockwise)
  expect(line.closedCurveOrientation()).toBe(O.Undefined)
})

test('curveTrimByParametersOrInterval', () => {
  // trim(t0, t1) and trim(interval) return the piece of the curve between the parameters
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const [, line] = curveCircleAndLine(rhino)
  const piece = line.trim(2.0, 5.0)
  expect(piece).toBeInstanceOf(rhino.Curve)
  expect(piece.pointAtStart).toEqual(P(2, 0, 0))
  expect(piece.pointAtEnd).toEqual(P(5, 0, 0))
  expect(line.trim(new rhino.Interval(1.0, 3.0)).pointAtEnd).toEqual(P(3, 0, 0))
})

test('curveIsCircleAndTryGetCircle', () => {
  // isCircle is true for an arc curve on a circle; tryGetCircle returns [true, circle] for it and false for a line
  const [circle, line] = curveCircleAndLine(rhino)
  expect(circle.isCircle()).toBe(true)
  expect(line.isCircle()).toBe(false)
  const [ok, c] = circle.tryGetCircle()
  expect(ok).toBe(true)
  expect(c.Radius).toBeCloseTo(2.0, 12)
  expect(line.tryGetCircle()[0]).toBe(false)
})

test('curveDerivativeAtReturnsThePointAndDerivatives', () => {
  // derivativeAt(t, count, side) returns the point and count derivatives; a negative count raises
  const [, line] = curveCircleAndLine(rhino)
  const d = line.derivativeAt(4.0, 1, rhino.CurveEvaluationSide.Default)
  expect(d.length).toBe(2)
  expect(d[0]).toEqual(new rhino.Vector3d(4, 0, 0))     // the line's domain is [0, length]
  expect(d[1]).toEqual(new rhino.Vector3d(1, 0, 0))
  helpers.raises(rhino, () => line.derivativeAt(4.0, -1, rhino.CurveEvaluationSide.Default),
    /runtime_error,The derivativeCount must be larger than or equal to zero/)
})

test('curveCreateControlPointCurveReturnsTheSimplestType', () => {
  // createControlPointCurve returns a LineCurve for two points, a PolylineCurve for degree 1 and a NurbsCurve otherwise
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pts = [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)]
  expect(rhino.Curve.createControlPointCurve(pts.slice(0, 2), 3)).toBeInstanceOf(rhino.LineCurve)
  expect(rhino.Curve.createControlPointCurve(pts, 1)).toBeInstanceOf(rhino.PolylineCurve)
  expect(rhino.Curve.createControlPointCurve(pts, 3)).toBeInstanceOf(rhino.NurbsCurve)
})

test('curveTryGetPolylineReturnsAPolylineOnlyForPolylines', () => {
  // tryGetPolyline returns [true, polyline] for a polyline curve and [false, null] for a circle
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const crv = new rhino.PolylineCurve([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)])
  let [ok, pl] = crv.tryGetPolyline()
  expect(ok).toBe(true)
  expect(pl).toBeInstanceOf(rhino.Polyline)
  expect(pl.count).toBe(3);
  [ok, pl] = new rhino.ArcCurve(new rhino.Circle(P(0, 0, 0), 1.0)).tryGetPolyline()
  expect(ok).toBe(false)
  expect(pl == null).toBe(true)
})

test('curveClosedCurveOrientationInAPlane', () => {
  // closedCurveOrientation(plane): a circle in world XY is counter-clockwise in it (D19)
  const c = rhino.Circle.toNurbsCurve(new rhino.Circle(5))
  expect(c.closedCurveOrientation(rhino.Plane.worldXY).value).toBe(1)
})

test('curveDerivativeAtTakesASide', () => {
  // derivativeAt(t, count, side) returns the point and its derivatives as vectors (D78)
  const c = rhino.Circle.toNurbsCurve(new rhino.Circle(5))
  const d = c.derivativeAt(0.5, 1, rhino.CurveEvaluationSide.Below)
  expect(Array.from(d).map(v => [v.X, v.Y, v.Z])).toEqual([[4.419622907229764, 2.338147420050742, 0], [-2.4116096580979542, 4.558483009593529, 0]])
})

test('curveToNurbsCurveOverASubDomain', () => {
  // toNurbsCurve(subdomain) returns the part of the curve over that interval: half a circle ends at (-5, 0, 0) (D19)
  const c = rhino.Circle.toNurbsCurve(new rhino.Circle(5))
  const dom = c.domain
  expect([dom.T0, dom.T1]).toEqual([0, 6.283185307179586])
  const half = c.toNurbsCurve(new rhino.Interval(dom.T0, (dom.T0 + dom.T1) / 2))
  expect(half.points.count).toBe(5)
  const e = half.pointAtEnd
  expect([e.X, e.Y, e.Z].map(x => +x.toFixed(9))).toEqual([-5, 0, 0])
})
