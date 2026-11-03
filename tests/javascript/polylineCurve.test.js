const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('ctor', async () => {

    const pointArray = []
    const pointList = new rhino.Point3dList(15)

    for (let i = 0; i < 15; i++) {
        pointArray.push({ X: i, Y: i, Z: 0 })
        pointList.add(i, i, 0)
    }

    const polyline1 = new rhino.PolylineCurve(pointArray)
    const polyline2 = new rhino.PolylineCurve(pointList)

    const r1 = polyline1.point(3)
    const r2 = polyline2.point(3)

    const result = r1.X === r2.X && r1.Y === r2.Y && r1.Z === r2.Z

    expect(result).toBe(true)


})