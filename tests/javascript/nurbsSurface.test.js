const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
let ns
beforeAll(async () => {
  rhino = await rhino3dm()

  ns = rhino.Sphere.toNurbsSurface(rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5))

})

test('knotsToList', async () => {

  // toList -> iteration
  const knotsU = [...ns.knotsU]

  expect(Array.isArray(knotsU)).toBe(true)
  expect(typeof knotsU[0] === 'number').toBe(true)
  expect(knotsU.length == ns.knotsU.count).toBe(true)

  const knotsV = [...ns.knotsV]

  expect(Array.isArray(knotsV)).toBe(true)
  expect(typeof knotsV[0] === 'number').toBe(true)
  expect(knotsV.length == ns.knotsV.count).toBe(true)

})

function nurbsSurfaceSphere(rhino) {
  return rhino.NurbsSurface.createFromSphere(new rhino.Sphere(new rhino.Point3d(0, 0, 0), 2.0))
}

test('nurbsSurfaceIsoCurveReturnsANurbsCurve', () => {
  // isoCurve of a NURBS surface comes back as its most-derived class, NurbsCurve (D8)
  const srf = rhino.Sphere.toNurbsSurface(rhino.Sphere.create(new rhino.Point3d(0, 0, 0), 5))
  const iso = srf.isoCurve(1, 0.5)
  expect(iso).toBeInstanceOf(rhino.NurbsCurve)
  expect(iso.constructor.name).toBe('NurbsCurve')
})

test('nurbsSurfaceKnotListsCountTheKnots', () => {
  // knotsU and knotsV of a NURBS sphere have knots
  const ns = nurbsSurfaceSphere(rhino)
  const ku = ns.knotsU
  expect(ku.count).toBeGreaterThan(0)
  expect(typeof ku.clampedAtStart).toBe('boolean')
  expect(ns.knotsV.count).toBeGreaterThan(0)
})

test('nurbsSurfaceDomainTreatsAnyNonzeroDirectionAsV', () => {
  // Surface.domain(direction) returns the V domain for any nonzero direction, as in RhinoCommon
  const ns = nurbsSurfaceSphere(rhino)
  const d1 = ns.domain(1), d7 = ns.domain(7)
  expect([d1.T0, d1.T1]).toEqual([d7.T0, d7.T1])
})

test('nurbsSurfaceKnotListSuperfluousKnotAtStartAndEnd', () => {
  // NurbsSurfaceKnotList.superfluousKnot(start) returns the start or end superfluous knot
  const ku = nurbsSurfaceSphere(rhino).knotsU
  expect(typeof ku.superfluousKnot(true)).toBe('number')
  expect(ku.superfluousKnot(true)).toBeLessThanOrEqual(ku.superfluousKnot(false))
})

test('nurbsSurfaceGetSpanVectorHasSpanCountPlusOneSortedValues', () => {
  // getSpanVector returns spanCount + 1 increasing span parameters
  const ns = nurbsSurfaceSphere(rhino)
  const sv = Array.from(ns.getSpanVector(0))
  expect(sv.length).toBe(ns.spanCount(0) + 1)
  expect(sv).toEqual([...sv].sort((x, y) => x - y))
})

test('nurbsSurfacePointListGetPointRaisesOutsideTheGrid', () => {
  // points.getPoint returns [true, point] inside the grid and throws at countU
  const ns = nurbsSurfaceSphere(rhino)
  const res = ns.points.getPoint(0, 0)
  expect(res.length).toBe(2)
  const [ok, p] = res
  expect(ok).toBe(true)
  expect([typeof p.X, typeof p.Y, typeof p.Z]).toEqual(['number', 'number', 'number'])
  helpers.raises(rhino, () => ns.points.getPoint(ns.points.countU, 0), /out_of_range,u must be less than CountU/)
})

test('nurbsSurfaceSetPointAndGetControlPoint', () => {
  // points.setPoint(u, v, x, y, z) sets a control point that getControlPoint reads back with weight 1
  const ns = rhino.NurbsSurface.create(3, false, 2, 2, 2, 2)
  ns.points.setPoint(1, 1, 1.0, 1.0, 0.0)
  const cp = ns.points.getControlPoint(1, 1)
  expect(rhino.ControlPoint.location(cp)).toEqual(new rhino.Point3d(1, 1, 0))
  expect(rhino.ControlPoint.weight(cp)).toBe(1.0)
})

test('nurbsSurfaceCreateRuledSurface', () => {
  // createRuledSurface between two lines returns a NurbsSurface
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const a = new rhino.LineCurve(P(0, 0, 0), P(1, 0, 0))
  const b = new rhino.LineCurve(P(0, 1, 0), P(1, 1, 0))
  expect(rhino.NurbsSurface.createRuledSurface(a, b)).toBeInstanceOf(rhino.NurbsSurface)
})

test('nurbsSurfacePointListIteratesUOutermost', () => {
  // nurbsSurface.points iterates over all countU * countV control points, u outermost
  const ns = rhino.NurbsSurface.create(3, false, 2, 2, 3, 2)
  const pts = ns.points
  pts.setPoint(2, 1, 5, 6, 7)
  const cps = [...pts]
  expect(cps.length).toBe(pts.countU * pts.countV)
  expect(cps.length).toBe(6)
  const p = cps[2 * pts.countV + 1]
  expect([p.X, p.Y, p.Z]).toEqual([5, 6, 7])
})

test('nurbsSurfaceSphereKnotsU', () => {
  // A sphere's NurbsSurface has the knots of a rational circle in U (D35)
  const pi = Math.PI
  const s = rhino.NurbsSurface.createFromSphere(new rhino.Sphere(new rhino.Point3d(0, 0, 0), 2))
  expect([...s.knotsU]).toEqual([0, 0, pi / 2, pi / 2, pi, pi, 4.71238898038469, 4.71238898038469, 2 * pi, 2 * pi])
})

test('nurbsSurfaceKnotsClampedAndMakeNonRational', () => {
  // A sphere's U knots are clamped at both ends, and makeNonRational succeeds
  const s = rhino.NurbsSurface.createFromSphere(new rhino.Sphere(new rhino.Point3d(0, 0, 0), 2))
  expect([s.knotsU.clampedAtStart, s.knotsU.clampedAtEnd]).toEqual([true, true])
  expect(s.makeNonRational()).toBe(true)
})

test('nurbsSurfacePointsGetPoint4d', () => {
  // points.getPoint4d(u, v) gives the homogeneous control point
  const [ok, q4] = rhino.NurbsSurface.create(3, false, 2, 2, 3, 2).points.getPoint4d(0, 0)
  expect(ok).toBe(true)
  expect(q4.W).toBe(1)
})
