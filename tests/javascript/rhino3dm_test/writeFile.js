const rhino3dm = require('rhino3dm')
const fs = require('fs')

async function writeFile() {

  const rhino = await rhino3dm()

  const file3dm = new rhino.File3dm()
  //const points = [ [ 0, 0, 0 ], [ 1, 1, 0 ], [ 2, 0, 0 ], [ 3, -1, 0 ], [ 4, 0, 0 ] ]

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

  const buffer = file3dm.toByteArray()
  fs.writeFileSync('spline.3dm', buffer)
  return fs.existsSync('spline.3dm')

}

module.exports = writeFile