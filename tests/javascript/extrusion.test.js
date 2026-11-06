const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

// objective: to test that creating and extrusion vs creating an extrusion with a place yield similar extrusions in different places
test('createExtrusion', async () => {

    const circle1 = rhino.Circle.create(1.0)
    const extrusion1 = rhino.Extrusion.create(rhino.Circle.toNurbsCurve(circle1), 10.0, true)
    const plane = rhino.Plane.worldXY
    plane.Origin = { X: 10, Y: 10, Z: 10 }
    const circle2 = rhino.Circle.create(1.0)
    circle2.Plane = plane
    // createWithPlane -> create(curve, plane, height, cap)
    const extrusion2 = rhino.Extrusion.create(rhino.Circle.toNurbsCurve(circle2), plane, 10.0, true)

    const bb1 = extrusion1.getBoundingBox(false)
    const bb2 = extrusion2.getBoundingBox(false)

    expect(rhino.BoundingBox.volume(bb1)).toBeCloseTo(rhino.BoundingBox.volume(bb2), 5)
    expect(rhino.Point3d.equals(rhino.BoundingBox.center(bb1), rhino.BoundingBox.center(bb2))).toBe(false)

})

test('extrusionCapsOfACylinder', () => {
  // a cylinder extrusion reports the bottom and top caps it was made with, and no mitered ends
  const cyl = new rhino.Cylinder(new rhino.Circle(new rhino.Point3d(0, 0, 0), 1.0), 4.0)
  for (const [bottom, top] of [[false, false], [true, false], [false, true], [true, true]]) {
    const e = rhino.Extrusion.createCylinderExtrusion(cyl, bottom, top)
    expect([e.isCappedAtBottom, e.isCappedAtTop]).toEqual([bottom, top])
    expect([e.isMiteredAtStart, e.isMiteredAtEnd]).toEqual([false, false])
  }
})

test('extrusionCreateBoxExtrusionIsACappedSolid', () => {
  // createBoxExtrusion(box, true) returns a solid Extrusion with two caps
  const I = (a, b) => new rhino.Interval(a, b)
  const box = new rhino.Box(rhino.Plane.worldXY, I(0, 1), I(0, 2), I(0, 3))
  const ext = rhino.Extrusion.createBoxExtrusion(box, true)
  expect(ext).toBeInstanceOf(rhino.Extrusion)
  expect(ext.isSolid).toBe(true)
  expect(ext.capCount).toBe(2)
})

test('extrusionCreateFromCurveAndPlane', () => {
  // Extrusion.create(curve, plane, height, cap) extrudes the profile to the height
  const e = rhino.Extrusion.create(rhino.Circle.toNurbsCurve(new rhino.Circle(2)), rhino.Plane.worldXY, 3.0, true)
  expect(e).toBeTruthy()
  expect(e.getBoundingBox(rhino.Transform.identity).Max.Z).toBeCloseTo(3.0, 9)
})
