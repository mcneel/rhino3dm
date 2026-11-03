const rhino3dm = require('rhino3dm')

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})
//TODO
test('AddPolyline', async () => {

  const file3dm = new rhino.File3dm()

  const pointArray = [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }, { X: 3, Y: -1, Z: 0 }, { X: 4, Y: 0, Z: 0 }]

  const pointList = new rhino.Point3dList(5)
  pointList.add(0, 0, 0)
  pointList.add(1, 1, 0)
  pointList.add(2, 0, 0)
  pointList.add(3, -1, 0)
  pointList.add(4, 0, 0)

  const id1 = file3dm.objects.addPolyline(pointArray, null)
  const id2 = file3dm.objects.addPolyline(pointList, null)

  const objqty = file3dm.objects.count
  // D17: no index on the object table; iterate
  const [o1, o2] = [...file3dm.objects]

  // ObjectType has no PolylineCurve member (it never had); D8: most-derived class
  const isCurve1 = o1.geometry instanceof rhino.PolylineCurve && o1.geometry.objectType == rhino.ObjectType.Curve
  const isCurve2 = o2.geometry instanceof rhino.PolylineCurve && o2.geometry.objectType == rhino.ObjectType.Curve
  //const len1 = o1.geometry.pointCount
  //const len2 = o2.geometry.pointCount

  expect((objqty === 2) && isCurve1 && isCurve2 && (id1 !== id2)).toBe(true)

})

test('DeleteObject', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-objectTable-deleteObject'
  file3dm.applicationUrl = 'https://rhino3d.com'

  const circle1 = rhino.Circle.create(5)
  const id1 = file3dm.objects.addCircle(circle1, null)
  // addPointXYZ -> addPoint(x, y, z)
  file3dm.objects.addPoint(0, 0, 0)

  const qtyObjects1 = file3dm.objects.count

  file3dm.objects.delete(id1)

  const qtyObjects2 = file3dm.objects.count

  expect(qtyObjects1 === 2 && qtyObjects2 === 1).toBe(true)

})

test('AddPoint with Attributes', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-objectTable-addPointWithAttributes'
  file3dm.applicationUrl = 'https://rhino3d.com'

  // create layers
  file3dm.layers.addLayer("layer1", { 'r': 30, 'g': 144, 'b': 255, 'a': 255 })
  file3dm.layers.addLayer("layer2", { 'r': 255, 'g': 215, 'b': 0, 'a': 255 })

  // points added without attributes are added to the current layer, i.e., the first
  // layer added to the model
  file3dm.objects.addPoint({ X: 0, Y: 0, Z: 0 })
  // D17: iterate the object table
  expect([...file3dm.objects][0].attributes.layerIndex).toBe(0)

  // add point with attributes
  oa = new rhino.ObjectAttributes()
  oa.layerIndex = 1
  file3dm.objects.addPoint({ X: 1, Y: 1, Z: 0 }, oa)
  expect([...file3dm.objects][1].attributes.layerIndex).toBe(1)

})

test('AddLine with Attributes', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-objectTable-addPointWithAttributes'
  file3dm.applicationUrl = 'https://rhino3d.com'

  // create layers
  file3dm.layers.addLayer("layer1", { 'r': 30, 'g': 144, 'b': 255, 'a': 255 })
  file3dm.layers.addLayer("layer2", { 'r': 255, 'g': 215, 'b': 0, 'a': 255 })

  // points added without attributes are added to the current layer, i.e., the first
  // layer added to the model
  file3dm.objects.addLine({ X: 0, Y: 0, Z: 0 }, { X: 10, Y: 0, Z: 10 })
  // D17: iterate the object table
  expect([...file3dm.objects][0].attributes.layerIndex).toBe(0)

  // add point with attributes
  oa = new rhino.ObjectAttributes()
  oa.layerIndex = 1
  file3dm.objects.addLine({ X: 1, Y: 1, Z: 0 }, { X: 5, Y: 5, Z: 5 }, oa)
  expect([...file3dm.objects][1].attributes.layerIndex).toBe(1)

})