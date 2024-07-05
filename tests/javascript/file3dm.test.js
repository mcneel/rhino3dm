const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('writeFile', async () => {

  const file3dm = new rhino.File3dm()
  const points2 = [ [ 0, 0, 0 ], [ 1, 1, 0 ], [ 2, 0, 0 ], [ 3, -1, 0 ], [ 4, 0, 0 ] ]

  const points = new rhino.Point3dList(5)
  points.add(0, 0, 0)
  points.add(1, 1, 0)
  points.add(2, 0, 0)
  points.add(3, -1, 0)
  points.add(4, 0, 0)

  for( let i = 1; i < 5; i ++ ) {
    const spline = rhino.Curve.createControlPointCurve( points, i )
    file3dm.objects().addCurve( spline, null )
  }

  const uuid1 = file3dm.objects().addPolyline(points, null)
  const uuid2 = file3dm.objects().addPolyline(points2, null)

  const buffer = file3dm.toByteArray()
  fs.writeFileSync('spline.3dm', buffer)
  const result = fs.existsSync('spline.3dm')

  expect(result).toBe(true) 
  expect(uuid1 === uuid2).toBe(false)

} )