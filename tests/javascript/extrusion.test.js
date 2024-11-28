const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

// objective: to test that creating and extrusion vs creating an extrusion with a place yield similar extrusions in different places
test('createExtrusion', async () => {

    const circle1 = new rhino.Circle(1.0);
    const extrusion1 = rhino.Extrusion.create(circle1.toNurbsCurve(), 10.0, true)
    const plane = new rhino.Plane.worldXY()
    plane.origin = [10,10,10]
    const circle2 = new rhino.Circle(1.0);
    circle2.plane = plane
    const extrusion2 = rhino.Extrusion.createWithPlane(circle2.toNurbsCurve(), plane, 10.0, true)

    const bb1 = extrusion1.getBoundingBox()
    const bb2 = extrusion2.getBoundingBox()

    expect(bb1.volume).toBeCloseTo(bb2.volume, 5)
    expect(bb1.center === bb2.center).toBe(false)

})