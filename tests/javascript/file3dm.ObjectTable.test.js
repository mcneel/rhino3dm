const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })
//TODO
test('AddPolyline', async () => {

  const file3dm = new rhino.File3dm()

  const pointArray = [ [ 0, 0, 0 ], [ 1, 1, 0 ], [ 2, 0, 0 ], [ 3, -1, 0 ], [ 4, 0, 0 ] ]

  const pointList = new rhino.Point3dList(5)
  pointList.add(0, 0, 0)
  pointList.add(1, 1, 0)
  pointList.add(2, 0, 0)
  pointList.add(3, -1, 0)
  pointList.add(4, 0, 0)

  const id1 = file3dm.objects().addPolyline(pointArray, null)
  const id2 =file3dm.objects().addPolyline(pointList, null)

  const objqty = file3dm.objects().count
  const o1 = file3dm.objects().get(0)
  const o2 = file3dm.objects().get(1)

  const isCurve1 = o1.geometry.objectType == rhino.ObjectType.PolylineCurve
  const isCurve2 = o2.geometry.objectType == rhino.ObjectType.PolylineCurve
  //const len1 = file3dm.objects().get(0).geometry.pointCount
  //const len2 = file3dm.objects().get(1).geometry.pointCount

  expect((objqty === 2) && isCurve1 && isCurve2 && (id1 !== id2) ).toBe(true)

} )