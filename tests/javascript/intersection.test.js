const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('intersectLineLine', async () => {

  const lineA = new rhino.LineCurve({ X: 0, Y: 0, Z: 0 }, { X: 10, Y: 10, Z: 0 }).line
  const lineB = new rhino.LineCurve({ X: 10, Y: 0, Z: 0 }, { X: 0, Y: 10, Z: 0 }).line

  const resultLineLine = rhino.Intersection.lineLine(lineA, lineB)

  expect(Array.isArray(resultLineLine)).toBe(true)
  expect(resultLineLine.length === 3).toBe(true)
  expect(typeof resultLineLine[0] === 'boolean').toBe(true)
  expect(typeof resultLineLine[1] === 'number').toBe(true)
  expect(typeof resultLineLine[2] === 'number').toBe(true)
  expect(resultLineLine[0]).toBe(true)
  expect(resultLineLine[1] === 0.5).toBe(true)
  expect(resultLineLine[2] === 0.5).toBe(true)

  // lineLineTolerance -> lineLine(a, b, tolerance, finiteSegments)
  const resultLineLine2 = rhino.Intersection.lineLine(lineA, lineB, 0.0, false)

  expect(Array.isArray(resultLineLine2)).toBe(true)
  expect(typeof resultLineLine2[0] === 'boolean').toBe(true)
  expect(typeof resultLineLine2[1] === 'number').toBe(true)
  expect(typeof resultLineLine2[2] === 'number').toBe(true)
  expect(resultLineLine2[0]).toBe(true)
  expect(resultLineLine2[1] === 0.5).toBe(true)
  expect(resultLineLine2[2] === 0.5).toBe(true)

})

test('intersectPlanes', async () => {

  // D51: static properties are getters
  const planeA = rhino.Plane.worldXY
  const planeB = rhino.Plane.worldYZ
  const planeC = rhino.Plane.worldZX
  const line = new rhino.LineCurve({ X: 0, Y: 0, Z: -10 }, { X: 10, Y: 10, Z: 10 }).line
  const sphere = rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5)

  // line-plane
  const resultLinePlane = rhino.Intersection.linePlane(line, planeA)

  expect(resultLinePlane.length === 2).toBe(true)
  expect(Array.isArray(resultLinePlane)).toBe(true)
  expect(typeof resultLinePlane[0] === 'boolean').toBe(true)
  expect(typeof resultLinePlane[1] === 'number').toBe(true)
  expect(resultLinePlane[0]).toBe(true)
  expect(resultLinePlane[1] === 0.5).toBe(true)

  // plane plane

  const resultPlanePlane = rhino.Intersection.planePlane(planeA, planeB)

  expect(resultPlanePlane.length === 2).toBe(true)
  expect(Array.isArray(resultPlanePlane)).toBe(true)
  expect(typeof resultPlanePlane[0] === 'boolean').toBe(true)
  // D11: a Line is a plain object {From, To}
  expect(Object.keys(resultPlanePlane[1])).toEqual(['From', 'To'])
  expect(resultPlanePlane[0]).toBe(true)
  expect(rhino.Line.isValid(resultPlanePlane[1])).toBe(true)

  // sphere plane

  const resultSpherePlane = rhino.Intersection.planeSphere(planeA, sphere)

  expect(resultSpherePlane.length === 2).toBe(true)
  expect(Array.isArray(resultSpherePlane)).toBe(true)
  expect(resultSpherePlane[0] === rhino.PlaneSphereIntersection.Circle).toBe(true)
  // D11: a Circle is a plain object {Plane, Radius}
  expect(Object.keys(resultSpherePlane[1])).toEqual(['Plane', 'Radius'])
  expect(rhino.Circle.isValid(resultSpherePlane[1])).toBe(true)

  // plane plane plane

  const resultPlanePlanePlane = rhino.Intersection.planePlanePlane(planeA, planeB, planeC)

  expect(resultPlanePlanePlane.length === 2).toBe(true)
  expect(Array.isArray(resultLinePlane)).toBe(true)
  expect(typeof resultPlanePlanePlane[0] === 'boolean').toBe(true)
  // D11: a point is {X, Y, Z}
  expect(Object.keys(resultPlanePlanePlane[1])).toEqual(['X', 'Y', 'Z'])
  expect(resultPlanePlanePlane[0] === true)
  expect(resultPlanePlanePlane[1].X === 0)
  expect(resultPlanePlanePlane[1].Y === 0)
  expect(resultPlanePlanePlane[1].Z === 0)

})

test('intersectLines', async () => {

  const line = new rhino.LineCurve({ X: 0, Y: 0, Z: 0 }, { X: 10, Y: 10, Z: 0 }).line
  const circle = rhino.Circle.create({ X: 0, Y: 0, Z: 0 }, 5)
  const sphere = rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5)
  const cylinder = rhino.Cylinder.create(circle, 5)
  const box = rhino.Sphere.toBrep(sphere).getBoundingBox(false)

  // line circle

  const resultLineCircle = rhino.Intersection.lineCircle(line, circle)

  expect(resultLineCircle.length === 5).toBe(true)
  expect(Array.isArray(resultLineCircle)).toBe(true)
  expect(resultLineCircle[0] === rhino.LineCircleIntersection.Multiple).toBe(true)
  expect(typeof resultLineCircle[1] === 'number').toBe(true)
  // D11: points are {X, Y, Z}
  expect(Object.keys(resultLineCircle[2])).toEqual(['X', 'Y', 'Z'])
  expect(typeof resultLineCircle[3] === 'number').toBe(true)

  // line sphere

  const resultLineSphere = rhino.Intersection.lineSphere(line, sphere)

  expect(resultLineSphere.length === 3).toBe(true)
  expect(Array.isArray(resultLineSphere)).toBe(true)
  expect(resultLineSphere[0] === rhino.LineSphereIntersection.Multiple).toBe(true)
  expect(Object.keys(resultLineSphere[1])).toEqual(['X', 'Y', 'Z'])
  expect(Object.keys(resultLineSphere[2])).toEqual(['X', 'Y', 'Z'])

  // line cylinder

  const resultLineCylinder = rhino.Intersection.lineCylinder(line, cylinder)

  expect(resultLineCylinder.length === 3).toBe(true)
  expect(Array.isArray(resultLineCylinder)).toBe(true)
  expect(resultLineCylinder[0] === rhino.LineCylinderIntersection.Multiple).toBe(true)
  expect(Object.keys(resultLineCylinder[1])).toEqual(['X', 'Y', 'Z'])
  expect(Object.keys(resultLineCylinder[2])).toEqual(['X', 'Y', 'Z'])

  // line box

  const resultLineBox = rhino.Intersection.lineBox(line, box, 0.01)

  expect(resultLineBox.length === 2).toBe(true)
  expect(Array.isArray(resultLineBox)).toBe(true)
  expect(typeof resultLineBox[0] === 'boolean').toBe(true)
  // D11: an Interval is {T0, T1}
  expect(Object.keys(resultLineBox[1])).toEqual(['T0', 'T1'])

})

test('intersectSphereSphere', async () => {

  const sphereA = rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5)
  const sphereB = rhino.Sphere.create({ X: 2.5, Y: 0, Z: 0 }, 5)

  const resultSphereSphere = rhino.Intersection.sphereSphere(sphereA, sphereB)

  expect(resultSphereSphere.length === 2).toBe(true)
  expect(Array.isArray(resultSphereSphere)).toBe(true)
  expect(resultSphereSphere[0] === rhino.SphereSphereIntersection.Circle).toBe(true)
  // D11: plain objects {Plane, Radius}
  expect(Object.keys(resultSphereSphere[1])).toEqual(['Plane', 'Radius'])
  expect(rhino.Circle.isValid(resultSphereSphere[1])).toBe(true)
  expect(resultSphereSphere[1].Radius < sphereA.Radius).toBe(true)

})

test('intersectionCircleCircleReturnsTheKindFirst', () => {
  // Intersection.circleCircle returns [kind, point1, point2]; two overlapping circles meet at Multiple (D13)
  const a = new rhino.Circle(new rhino.Point3d(0, 0, 0), 1.0)
  const b = new rhino.Circle(new rhino.Point3d(1, 0, 0), 1.0)
  const res = rhino.Intersection.circleCircle(a, b)
  expect(res.length).toBe(3)
  expect(res[0]).toBe(rhino.CircleCircleIntersection.Multiple)
  expect(res[0].value).toBe(2)
})

test('intersectionLineCircleReturnsTheIntersectionTuple', () => {
  // lineCircle returns [type, t1, point1, t2, point2]: Multiple for a secant, None with zeros for an invalid line
  const P = (X, Y, Z) => ({ X, Y, Z })
  const circle = new rhino.Circle(P(0, 0, 0), 1.0)
  const rc = rhino.Intersection.lineCircle({ From: P(-2, 0, 0), To: P(2, 0, 0) }, circle)
  expect(rc.length).toBe(5)
  expect(rc[0].value).toBe(2)                      // Multiple
  const bad = rhino.Intersection.lineCircle({ From: P(0, 0, 0), To: P(0, 0, 0) }, circle)
  expect(bad[0].value).toBe(0)                     // None
  expect(bad[1]).toBe(0.0)
  expect(bad[3]).toBe(0.0)
})

test('intersectionSphereSphereReturnsTheTypeAndCircle', () => {
  // sphereSphere returns [Circle, circle] for overlapping spheres and None for spheres apart
  const a = new rhino.Sphere(new rhino.Point3d(0, 0, 0), 1.0)
  const b = new rhino.Sphere(new rhino.Point3d(1, 0, 0), 1.0)
  const ss = rhino.Intersection.sphereSphere(a, b)
  expect(ss.length).toBe(2)
  expect(ss[0].value).toBe(2)                      // Circle
  expect(typeof ss[1].Radius).toBe('number')       // a Circle value
  expect(ss[1].Plane).toBeDefined()
  const far = rhino.Intersection.sphereSphere(a, new rhino.Sphere(new rhino.Point3d(10, 0, 0), 1.0))
  expect(far[0].value).toBe(0)                     // None
})

test('intersectionLineLineWithToleranceAndFiniteSegments', () => {
  // lineLine(a, b, tolerance, finiteSegments) returns [ok, a, b]: clamped to the segments when finite, ok within the tolerance
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const L = (a, b) => new rhino.Line(P(...a), P(...b))
  // for (tolerance, finite) = (0, false), (0.001, true), (5, true)
  const cases = [
    [[0, 0, 0], [4, 0, 0], [2, -1, 0], [2, 3, 0], [[true, 0.5, 0.25], [true, 0.5, 0.25], [true, 0.5, 0.25]]],   // crossing inside both
    [[0, 0, 0], [1, 0, 0], [3, -1, 0], [3, 1, 0], [[true, 3, 0.5], [false, 1, 0.5], [true, 1, 0.5]]],         // past the end of the first
    [[0, 0, 0], [1, 0, 0], [0, 0, 1], [1, 0, 1], [[false, 0, 0], [false, 0, 0], [false, 0, 0]]]]              // parallel
  for (const [a0, a1, b0, b1, expected] of cases)
    [[0.0, false], [0.001, true], [5.0, true]].forEach(([tol, finite], k) => {
      expect(Array.from(rhino.Intersection.lineLine(L(a0, a1), L(b0, b1), tol, finite))).toEqual(expected[k])
    })
})

test('intersectionPlaneCircleSecant', () => {
  // planeCircle: a radius 2 circle in world ZX cuts world XY at x = -2 and x = 2
  const circle = new rhino.Circle(rhino.Plane.worldZX, 2.0)
  const [kind, t0, t1] = rhino.Intersection.planeCircle(rhino.Plane.worldXY, circle)
  expect(kind).toBe(rhino.PlaneCircleIntersection.Secant)
  expect([t0, t1].map(t => Math.round(rhino.Circle.pointAt(circle, t).X * 1e9) / 1e9).sort((x, y) => x - y)).toEqual([-2, 2])
})
