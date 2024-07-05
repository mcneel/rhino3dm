const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('ctor', async () => {

    const pointArray = []
    const pointList = new rhino.Point3dList(15)

    for (let i = 0; i < 15; i++) {
        pointArray.push([i, i, 0])
        pointList.add(i, i, 0)
    }

    const pcFromArray = new rhino.PointCloud(pointArray)
    const pcFromList = new rhino.PointCloud(pointList)
    
    const r1 = pcFromArray.closestPoint([0,0,0]) 
    const r2 = pcFromList.closestPoint([0,0,0])
    
    const result = r1[0] === r2[0] && r1[1] === r2[1] && r1[2] === r2[2]

    expect(result).toBe(true)

})