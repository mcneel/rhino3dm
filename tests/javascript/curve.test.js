const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('createControlPointCurve', async () => {

    const pointArray = [ [ 0, 0, 0 ], [ 1, 1, 0 ], [ 2, 0, 0 ], [ 3, -1, 0 ], [ 4, 0, 0 ] ]

    const pointList = new rhino.Point3dList(5)
    pointList.add(0, 0, 0)
    pointList.add(1, 1, 0)
    pointList.add(2, 0, 0)
    pointList.add(3, -1, 0)
    pointList.add(4, 0, 0)

    const crvFromList = rhino.Curve.createControlPointCurve(pointList, 3)
    const crvFromArray = rhino.Curve.createControlPointCurve(pointArray, 3)

    const r1 = crvFromArray.pointAt(0.5)
    const r2 = crvFromList.pointAt(0.5)
    
    const result = r1[0] === r2[0] && r1[1] === r2[1] && r1[2] === r2[2]

    expect(result).toBe(true)

})