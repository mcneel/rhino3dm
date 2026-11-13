const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

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

function objectTableFileWithLine(rhino) {
  const f = new rhino.File3dm()
  f.objects.addLine(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 0, 0), null)
  return f
}

test('objectTableKeepsTheFileAlive', async () => {
  // file.objects keeps its file alive after the last reference to the file is dropped; null attributes mean defaults (D52, D10)
  const objs = (() => new rhino.File3dm().objects)()
  expect(objs.__rh3dm_parent).toBeInstanceOf(rhino.File3dm)
  await helpers.gcSettle()
  objs.addPoint(new rhino.Point3d(1, 2, 3), null)
  objs.addPoint(new rhino.Point3d(-1, 0, 5), null)
  const bb = objs.getBoundingBox()
  expect([bb.Min.X, bb.Max.Z]).toEqual([-1.0, 5.0])
})

test('objectTableIteratesAsFile3dmObjects', () => {
  // iterating objects yields File3dmObject items whose geometry is the most-derived class (D8)
  const f = objectTableFileWithLine(rhino)
  f.objects.addPoint(new rhino.Point3d(5, 5, 5), null)
  const objs = [...f.objects]
  expect(f.objects.count).toBe(2)
  expect(objs.length).toBe(2)
  expect(objs[0]).toBeInstanceOf(rhino.File3dmObject)
  expect(objs[0].geometry).toBeInstanceOf(rhino.LineCurve)
  expect(objs[1].geometry).toBeInstanceOf(rhino.Point)
})

test('file3dmObjectGeometryIsCopyOnWrite', () => {
  // editing an object's geometry changes a private copy, not the file; delete + add changes the file (D1)
  const f = objectTableFileWithLine(rhino)
  const obj = [...f.objects][0]
  const g = obj.geometry
  expect(g.pointAtStart.X).toBe(0.0)
  expect(g.transform(rhino.Transform.translation(new rhino.Vector3d(10, 0, 0)))).toBe(true)
  expect(g.pointAtStart.X).toBe(10.0)             // the private copy moved ...
  expect(obj.geometry.pointAtStart.X).toBe(0.0)   // ... the file did not
  // the RhinoCommon way to change the file: delete + add
  f.objects.delete(obj.id)
  f.objects.addCurve(g, null)
  expect([...f.objects].map(o => o.geometry.pointAtStart.X)).toEqual([10.0])
})

test('file3dmObjectAttributesWriteThroughAndNameChainsToThem', () => {
  // an object's attributes write into the file, and File3dmObject.name is attributes.name
  const f = objectTableFileWithLine(rhino)
  const obj = [...f.objects][0]
  obj.attributes.name = 'via attributes'
  expect(obj.name).toBe('via attributes')
  obj.name = 'via name'
  expect([...f.objects][0].attributes.name).toBe('via name')
})

test('file3dmObjectGeometryOfADeletedObjectRaises', () => {
  // reading the geometry of an object deleted from the file throws (D9)
  const f = objectTableFileWithLine(rhino)
  const obj = [...f.objects][0]
  const g = obj.geometry
  expect(g.pointAtStart.X).toBe(0.0)
  f.objects.delete(obj.id)
  expect(f.objects.count).toBe(0)
  helpers.raises(rhino, () => g.pointAtStart, /runtime_error,.*no longer in the File3dm/)
})

test('file3dmObjectViewsFollowTheFileAsItChanges', () => {
  // geometry and attributes read before the file changes still read their object after an add, and throw once it is deleted
  const f = objectTableFileWithLine(rhino)
  const obj = [...f.objects][0]
  const g = obj.geometry, a = obj.attributes
  a.name = 'first'
  expect(g.pointAtStart.X).toBe(0.0)
  expect(a.name).toBe('first')
  f.objects.addPoint(1, 2, 3)
  expect(g.pointAtStart.X).toBe(0.0)
  expect(a.name).toBe('first')
  f.objects.delete(obj.id)
  expect(f.objects.count).toBe(1)
  helpers.raises(rhino, () => g.pointAtStart, /runtime_error,.*no longer in the File3dm/)
  helpers.raises(rhino, () => a.name, /runtime_error,.*no longer in the File3dm/)
})

test('objectTableAddPolylineAddsACurve', () => {
  // addPolyline adds an object and returns its id; an empty point list adds nothing and returns the empty id
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const ZERO_ID = '00000000-0000-0000-0000-000000000000'
  const f = new rhino.File3dm()
  const pid = f.objects.addPolyline([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)], null)
  expect(f.objects.count).toBe(1)
  expect(pid).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
  expect(pid).not.toBe(ZERO_ID)
  expect(f.objects.addPolyline([], null)).toBe(ZERO_ID)
  expect(f.objects.count).toBe(1)
})

test('objectTableAddInstanceObjectReturnsAnId', () => {
  // addInstanceObject(index, xform, null) returns a uuid
  const f = new rhino.File3dm()
  const iid = f.objects.addInstanceObject(0, rhino.Transform.identity, null)
  expect(iid).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
})

test('objectTableFindByGroupListsTheGroupsObjects', () => {
  // findByGroup(group) returns the objects in that group (D35)
  const f = new rhino.File3dm()
  const g0 = f.allGroups.addGroup()
  const g1 = f.allGroups.addGroup()
  const ids = [[], [g0], [g0, g1]].map(groups => {
    const a = new rhino.ObjectAttributes()
    for (const g of groups) a.addToGroup(g)
    return f.objects.addPoint(new rhino.Point3d(groups.length, 0, 0), a)
  })
  const grp = f.allGroups.findIndex(g1)
  expect(f.objects.findByGroup(grp).map(o => String(o.id))).toEqual([String(ids[2])])
})

test('objectTableAddFile3dmObject', () => {
  // objects.add(File3dmObject) adds one more object
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const n0 = [...f.objects].length
  f.objects.add([...f.objects][0])
  expect([...f.objects].length).toBe(n0 + 1)
})

test('objectTableAddGeometryWithoutAttributes', () => {
  // objects.add(geometry, null) adds the geometry with default attributes
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const nid = f.objects.add([...f.objects][0].geometry, null)
  expect(f.objects.findId(nid).geometry).toBeInstanceOf(rhino.GeometryBase)
})

test('objectTableAddAnnotations', () => {
  // objects.add takes a leader, which RhinoCommon's Add does not (D71)
  const f = new rhino.File3dm()
  const le = rhino.Leader.create('x', rhino.Plane.worldXY, new rhino.DimensionStyle(),
    [new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 0, 0)])
  const lid = f.objects.add(le, null)
  expect(f.objects.findId(lid).geometry).toBeInstanceOf(rhino.Leader)
})

test('objectTableAddPointTakesXYZ', () => {
  // File3dmObjectTable.addPoint(x, y, z) adds a point object at that location
  const f = new rhino.File3dm()
  f.objects.addPoint(1, 2, 3)
  const loc = [...f.objects][0].geometry.location
  expect([loc.X, loc.Y, loc.Z]).toEqual([1, 2, 3])
})
