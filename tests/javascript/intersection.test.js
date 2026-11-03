const rhino3dm = require('rhino3dm')

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