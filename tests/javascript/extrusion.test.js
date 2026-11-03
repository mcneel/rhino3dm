const rhino3dm = require('rhino3dm')

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