const rhino3dm = require('rhino3dm')

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