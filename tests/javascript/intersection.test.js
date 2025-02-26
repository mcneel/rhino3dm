const rhino3dm = require('rhino3dm')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('intersectLineLine', async () => {

  const lineA = new rhino.LineCurve([0, 0, 0], [10, 10, 0]).line
  const lineB = new rhino.LineCurve([10, 0, 0], [0, 10, 0]).line

  const resultLineLine = rhino.Intersection.lineLine(lineA, lineB)

  expect(Array.isArray(resultLineLine)).toBe(true)
  expect(resultLineLine.length === 3).toBe(true)
  expect(typeof resultLineLine[0] === 'boolean').toBe(true)
  expect(typeof resultLineLine[1] === 'number').toBe(true)
  expect(typeof resultLineLine[2] === 'number').toBe(true)
  expect(resultLineLine[0]).toBe(true)
  expect(resultLineLine[1] === 0.5).toBe(true)
  expect(resultLineLine[2] === 0.5).toBe(true)

  const resultLineLine2 = rhino.Intersection.lineLineTolerance(lineA, lineB, 0.0, false)

  expect(Array.isArray(resultLineLine2)).toBe(true)
  expect(typeof resultLineLine2[0] === 'boolean').toBe(true)
  expect(typeof resultLineLine2[1] === 'number').toBe(true)
  expect(typeof resultLineLine2[2] === 'number').toBe(true)
  expect(resultLineLine2[0]).toBe(true)
  expect(resultLineLine2[1] === 0.5).toBe(true)
  expect(resultLineLine2[2] === 0.5).toBe(true)

})

test('intersectPlanes', async () => {

  const planeA = rhino.Plane.worldXY()
  const planeB = rhino.Plane.worldYZ()
  const planeC = rhino.Plane.worldZX()
  const line = new rhino.LineCurve([0, 0, -10], [10, 10, 10]).line
  const sphere = new rhino.Sphere([0, 0, 0], 5)

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
  expect(resultPlanePlane[1].constructor.name === 'Line').toBe(true)
  expect(resultPlanePlane[0]).toBe(true)
  expect(resultPlanePlane[1].isValid).toBe(true)

  // sphere plane

  const resultSpherePlane = rhino.Intersection.planeSphere(planeA, sphere)

  expect(resultSpherePlane.length === 2).toBe(true)
  expect(Array.isArray(resultSpherePlane)).toBe(true)
  expect(resultSpherePlane[0] === rhino.PlaneSphereIntersection.Circle).toBe(true)
  expect(resultSpherePlane[1].constructor.name === 'Circle').toBe(true)
  expect(resultSpherePlane[1].isValid).toBe(true)

  // plane plane plane

  const resultPlanePlanePlane = rhino.Intersection.planePlanePlane(planeA, planeB, planeC)

  expect(resultPlanePlanePlane.length === 2).toBe(true)
  expect(Array.isArray(resultLinePlane)).toBe(true)
  expect(typeof resultPlanePlanePlane[0] === 'boolean').toBe(true)
  expect(Array.isArray(resultPlanePlanePlane[1])).toBe(true)
  expect(resultPlanePlanePlane[0] === true)
  expect(resultPlanePlanePlane[1][0] === 0)
  expect(resultPlanePlanePlane[1][1] === 0)
  expect(resultPlanePlanePlane[1][2] === 0)

})

test('intersectLines', async () => {

  const line = new rhino.LineCurve([0, 0, 0], [10, 10, 0]).line
  const circle = new rhino.Circle([0, 0, 0], 5)
  const sphere = new rhino.Sphere([0, 0, 0], 5)
  const cylinder = new rhino.Cylinder(circle, 5)
  const box = sphere.toBrep().getBoundingBox()

  // line circle

  const resultLineCircle = rhino.Intersection.lineCircle(line, circle)

  expect(resultLineCircle.length === 5).toBe(true)
  expect(Array.isArray(resultLineCircle)).toBe(true)
  expect(resultLineCircle[0] === rhino.LineCircleIntersection.Multiple).toBe(true)
  expect(typeof resultLineCircle[1] === 'number').toBe(true)
  expect(Array.isArray(resultLineCircle[2])).toBe(true)
  expect(typeof resultLineCircle[3] === 'number').toBe(true)

  // line sphere

  const resultLineSphere = rhino.Intersection.lineSphere(line, sphere)

  expect(resultLineSphere.length === 3).toBe(true)
  expect(Array.isArray(resultLineSphere)).toBe(true)
  expect(resultLineSphere[0] === rhino.LineSphereIntersection.Multiple).toBe(true)
  expect(Array.isArray(resultLineSphere[1])).toBe(true)
  expect(Array.isArray(resultLineSphere[1])).toBe(true)

  // line cylinder

  const resultLineCylinder = rhino.Intersection.lineCylinder(line, cylinder)

  expect(resultLineCylinder.length === 3).toBe(true)
  expect(Array.isArray(resultLineCylinder)).toBe(true)
  expect(resultLineCylinder[0] === rhino.LineCylinderIntersection.Multiple).toBe(true)
  expect(Array.isArray(resultLineCylinder[1])).toBe(true)
  expect(Array.isArray(resultLineCylinder[2])).toBe(true)

  // line box

  const resultLineBox = rhino.Intersection.lineBox(line, box, 0.01)

  expect(resultLineBox.length === 2).toBe(true)
  expect(Array.isArray(resultLineBox)).toBe(true)
  expect(typeof resultLineBox[0] === 'boolean').toBe(true)
  expect(Array.isArray(resultLineBox[1])).toBe(true)

})

test('intersectSphereSphere', async () => {

  const sphereA = new rhino.Sphere([0, 0, 0], 5)
  const sphereB = new rhino.Sphere([2.5, 0, 0], 5)

  const resultSphereSphere = rhino.Intersection.sphereSphere(sphereA, sphereB)

  expect(resultSphereSphere.length === 2).toBe(true)
  expect(Array.isArray(resultSphereSphere)).toBe(true)
  expect(resultSphereSphere[0] === rhino.SphereSphereIntersection.Circle).toBe(true)
  expect(resultSphereSphere[1].constructor.name === 'Circle').toBe(true)
  expect(resultSphereSphere[1].isValid).toBe(true)
  expect(resultSphereSphere[1].radius < sphereA.radius).toBe(true)

})