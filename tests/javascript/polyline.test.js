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
        const point = [i, i, i]
        pointList.add(i, i, i)
        pointArray.push(point)
    }

    polyline1.append(pointArray)
    polyline2.append(pointList)

})

test('append', async () => {

    polyline1.add(21, 21, 21)
    polyline2.add(21, 21, 21)

    polyline1.append(pointArray)
    polyline2.append(pointList)

    const result = polyline1.count === polyline2.count && polyline1[10] === polyline2[10]

    expect(result).toBe(true)

})

test('createFromPoints', async () => {

    const polyline3 = rhino.Polyline.createFromPoints(pointArray)
    const polyline4 = rhino.Polyline.createFromPoints(pointList)

    const result = polyline3.count === polyline4.count && polyline3[10] === polyline4[10]

    expect(result).toBe(true)

})

test('explode', async () => {

    const explodedCurves = polyline1.getSegments()
    expect(explodedCurves.length > 0).toBe(true)
    expect(Array.isArray(explodedCurves)).toBe(true)
    expect(explodedCurves[0].constructor.name === 'LineCurve').toBe(true)

})