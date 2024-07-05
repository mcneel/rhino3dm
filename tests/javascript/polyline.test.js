const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('append', async () => {

    const polyline1 = new rhino.Polyline()
    polyline1.add(21, 21, 21)

    const polyline2 = new rhino.Polyline()
    polyline2.add(21, 21, 21)

    const pointArray = []
    const pointList = new rhino.Point3dList(15)

    for (let i = 0; i < 15; i++) {
        pointArray.push([i, i, 0])
        pointList.add(i, i, 0)
    }

    polyline1.append(pointArray)
    polyline2.append(pointList)

    const result = polyline1.count === polyline2.count && polyline1[10] === polyline2[10]

    expect(result).toBe(true)

})

test('createFromPoints', async () => {

    const pointArray = []
    const pointList = new rhino.Point3dList(15)

    for (let i = 0; i < 15; i++) {
        pointArray.push([i, i, 0])
        pointList.add(i, i, 0)
    }

    const polyline1 = rhino.Polyline.createFromPoints(pointArray)
    const polyline2 = rhino.Polyline.createFromPoints(pointList)

    const result = polyline1.count === polyline2.count && polyline1[10] === polyline2[10]

    expect(result).toBe(true)


})