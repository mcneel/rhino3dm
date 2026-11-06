const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const THREE = require('three')

let rhino
beforeEach(async () => {
    rhino = await rhino3dm()
})

test('ctor', async () => {

    const pointArray = []
    const pointList = new rhino.Point3dList(15)

    for (let i = 0; i < 15; i++) {
        pointArray.push({ X: i, Y: i, Z: 0 })
        pointList.add(i, i, 0)
    }

    const pcFromArray = new rhino.PointCloud(pointArray)
    const pcFromList = new rhino.PointCloud(pointList)

    // closestPoint returns the index of the closest point
    const r1 = pcFromArray.closestPoint({ X: 0, Y: 0, Z: 0 })
    const r2 = pcFromList.closestPoint({ X: 0, Y: 0, Z: 0 })

    const result = r1 === r2

    expect(result).toBe(true)

})

test('createFromThreejsJSON', async () => {

    const geometry = new THREE.BufferGeometry();

    const num = 100;

    const verts = []
    const colors3 = []
    const colors4 = []

    for (let i = 0; i < num; i++) {

        const x = Math.random()
        const y = Math.random()
        const z = Math.random()

        verts.push(x, y, z)

        colors3.push(x, y, z)

    }

    geometry.setAttribute('position', new THREE.Float32BufferAttribute(verts, 3))
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors3, 3))

    let pc = rhino.PointCloud.createFromThreejsJSON({ data: geometry })

    expect(pc instanceof rhino.PointCloud).toBe(true)
    expect(pc.containsColors).toBe(true)
    expect(pc.getPoints().length === geometry.attributes.position.count).toBe(true)
    expect(pc.getColors().length === geometry.attributes.color.count).toBe(true)

    //assign random color to verts
    for (let i = 0; i < num; i++) {

        const r = geometry.attributes.color.getX(i)
        const g = geometry.attributes.color.getY(i)
        const b = geometry.attributes.color.getZ(i)
        const a = Math.random()

        colors4.push(r, g, b, a)

    }

    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors4, 4))

    pc = rhino.PointCloud.createFromThreejsJSON({ data: geometry })

    expect(pc instanceof rhino.PointCloud).toBe(true)
    expect(pc.containsColors).toBe(true)
    expect(pc.getPoints().length === geometry.attributes.position.count).toBe(true)
    expect(pc.getColors().length === geometry.attributes.color.count).toBe(true)

    const r = geometry.attributes.color.array[0]
    const g = geometry.attributes.color.array[1]
    const b = geometry.attributes.color.array[2]
    const a = geometry.attributes.color.array[3]

    const rn = r * 255
    const gn = g * 255
    const bn = b * 255
    const an = a * 255

    expect(pc.getColors()[0].r === ~~rn).toBe(true)
    expect(pc.getColors()[0].g === ~~gn).toBe(true)
    expect(pc.getColors()[0].b === ~~bn).toBe(true)
    expect(pc.getColors()[0].a === ~~an).toBe(true)

})

test('members', async () => {

    const pc = new rhino.PointCloud()

    // addPointNormalColorValue -> add(point, normal, color, value)
    pc.add({ X: 0, Y: 0, Z: 0 }, { X: 0, Y: 1, Z: 1 }, { 'r': 255, 'g': 0, 'b': 0, 'a': 0 }, 1.234)
    pc.add({ X: 0, Y: 0, Z: 0 }, { X: 0, Y: 1, Z: 2 }, { 'r': 10, 'g': 0, 'b': 27, 'a': 0 }, 1.999)
    pc.add({ X: 0, Y: 0, Z: 0 }, { X: 0, Y: 13, Z: 1 }, { 'r': 255, 'g': 90, 'b': 0, 'a': 0 }, 1.333)
    pc.add({ X: 0, Y: 0, Z: 0 }, { X: 0, Y: 1, Z: 4 }, { 'r': 44, 'g': 0, 'b': 0, 'a': 0 }, 1.111)

    // D11: points and normals are {X, Y, Z} objects
    const pts = pc.getPoints()
    expect(pts.length > 0).toBe(true)
    expect(Array.isArray(pts)).toBe(true)
    expect(Object.keys(pts[0])).toEqual(['X', 'Y', 'Z'])

    const nrmls = pc.getNormals()
    expect(nrmls.length > 0).toBe(true)
    expect(Array.isArray(nrmls)).toBe(true)
    expect(Object.keys(nrmls[0])).toEqual(['X', 'Y', 'Z'])

    const cols = pc.getColors()
    expect(cols.length > 0).toBe(true)
    expect(Array.isArray(cols)).toBe(true)
    expect(typeof cols[0] === 'object').toBe(true)
    expect(cols[0].r).toBeDefined()
    expect(cols[0].g).toBeDefined()
    expect(cols[0].b).toBeDefined()
    expect(cols[0].a).toBeDefined()
    expect(Number.isInteger(cols[0].r)).toBe(true)
    expect(Number.isInteger(cols[0].g)).toBe(true)
    expect(Number.isInteger(cols[0].b)).toBe(true)
    expect(Number.isInteger(cols[0].a)).toBe(true)

    // getValues -> getPointValues
    const vals = pc.getPointValues()
    expect(vals.length > 0).toBe(true)
    expect(Array.isArray(vals)).toBe(true)
    expect(typeof vals[0] === 'number').toBe(true)

})

const pointCloudOf = (rhino, pts) => {
  const pc = new rhino.PointCloud()
  for (const [x, y, z] of pts) pc.add(new rhino.Point3d(x, y, z))
  return pc
}

const pointCloudRange = (a, b) => Array.from({ length: b - a }, (_, j) => a + j)
const pointCloudX = x => ({ X: x, Y: 0, Z: 0 })

// a point cloud's channels as arrays: points, normals, colors, point values
function pointCloudChannels(c) {
  const xyz = v => [v.X, v.Y, v.Z]
  return [c.count, Array.from(c.getPoints()).map(xyz), Array.from(c.getNormals()).map(xyz),
    Array.from(c.getColors()), Array.from(c.getPointValues())]
}

test('pointCloudItemsAreViewsIntoTheCloud', () => {
  // cloud.get(i) is the i-th PointCloudItem; setting its location moves the cloud's point; past the end throws
  const cloud = new rhino.PointCloud()
  for (let i = 0; i < 4; i++) cloud.add(new rhino.Point3d(i, 2 * i, 0))
  const item = cloud.get(2)
  expect(item.index).toBe(2)
  expect([item.location.X, item.location.Y]).toEqual([2, 4])
  item.location = new rhino.Point3d(9, 9, 9)
  expect(cloud.get(2).location.Z).toBe(9)
  helpers.raises(rhino, () => cloud.get(4), /out_of_range,index must be smaller than the Number of points/)
})

test('pointCloudPointAtGuardsTheIndex', () => {
  // PointCloud.pointAt returns the i-th point and throws for a negative or too large index (D9)
  const cloud = new rhino.PointCloud()
  for (let i = 0; i < 3; i++) cloud.add(new rhino.Point3d(i, 0, 0))
  expect(cloud.pointAt(1).X).toBe(1)
  for (const bad of [-1, 3]) helpers.raises(rhino, () => cloud.pointAt(bad), /out_of_range,index must be (larger|smaller)/)
})

test('pointCloudGetPointsReturnsThePoints', () => {
  // getPoints returns the cloud's points in order
  const cloud = pointCloudOf(rhino, [[0, 0, 0], [1, 2, 0], [2, 4, 0]])
  expect(Array.from(cloud.getPoints(), p => [p.X, p.Y])).toEqual([[0, 0], [1, 2], [2, 4]])
})

test('pointCloudGetNormalsAndColorsAreEmptyWithoutThem', () => {
  // getNormals and getColors return empty arrays when the cloud has no normals or colors
  const cloud = pointCloudOf(rhino, [[0, 0, 0]])
  expect(Array.from(cloud.getNormals())).toEqual([])
  expect(Array.from(cloud.getColors())).toEqual([])
})

test('pointCloudItemColorHasFourComponents', () => {
  // a point cloud item's color is a four-component (a, r, g, b) color
  const c = pointCloudOf(rhino, [[0, 0, 0]]).get(0).color
  expect(Object.keys(c).length).toBe(4)
})

test('pointCloudItemXYZChainThroughLocation', () => {
  // a point cloud item's x, y and z read its location, and setting x writes the point
  const pc = pointCloudOf(rhino, [[1, 2, 3]])
  const item = pc.get(0)
  expect([item.x, item.y, item.z]).toEqual([1.0, 2.0, 3.0])
  item.x = 7.5
  expect(item.location).toEqual(new rhino.Point3d(7.5, 2, 3))
  expect(pc.get(0).x).toBe(7.5)
})

test('pointCloudDefaultConstructorIsEmpty', () => {
  // a new PointCloud has no points
  expect(new rhino.PointCloud().count).toBe(0)
})

test('pointCloudAppendNewAndInsertNewReturnTheNewItem', () => {
  // appendNew adds a point at the origin and returns its item; insertNew returns the item at the index
  const pc = new rhino.PointCloud()
  const item = pc.appendNew()
  expect(item).toBeInstanceOf(rhino.PointCloudItem)
  expect(pc.count).toBe(1)
  expect(item.location).toEqual(new rhino.Point3d(0, 0, 0))
  pc.add(new rhino.Point3d(1, 1, 1))
  expect(pc.insertNew(1).index).toBe(1)
  expect(pc.count).toBe(3)
})

test('pointCloudAddRangeTakesAnySequenceOfPoints', () => {
  // addRange takes an array of points or a Polyline
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pc = new rhino.PointCloud()
  pc.addRange([P(0, 0, 0), P(1, 0, 0)])
  pc.addRange(new rhino.Polyline([P(2, 0, 0), P(3, 0, 0)]))
  expect(pc.count).toBe(4)
  expect(pc.getPoints().map(p => p.X)).toEqual([0, 1, 2, 3])
})

test('pointCloudAddRangeWithNormals', () => {
  // addRange(points, normals) adds points with normals, ignores empty normals and rejects unequal counts
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const V = (x, y, z) => new rhino.Vector3d(x, y, z)
  const n = new rhino.PointCloud()
  n.addRange([P(0, 0, 0)], [V(0, 0, 1)])
  expect(n.count).toBe(1)
  expect(n.containsNormals).toBe(true)
  n.addRange([P(0, 0, 0)], [])              // no normals: nothing is added, as in RhinoCommon
  expect(n.count).toBe(1)
  helpers.raises(rhino, () => n.addRange([P(0, 0, 0), P(1, 0, 0)], [V(0, 0, 1)]),
    /invalid_argument,Must supply equal number of points and vectors/)
})

test('pointCloudInsertRangeKeepsOrderAndNormals', () => {
  // insertRange puts the new points at the index, the following points after them, and their normals stay with them
  const range = pointCloudRange
  for (const [n, i, k] of [[4, 1, 1], [4, 0, 2], [4, 3, 1], [2, 0, 5], [5, 2, 3], [6, 1, 2], [3, 3, 2]]) {
    const pc = new rhino.PointCloud()
    // each normal tagged z = j + 1; the inserted points get zero normals
    pc.addRange(range(0, n).map(pointCloudX), range(0, n).map(j => ({ X: 0, Y: 0, Z: j + 1 })))
    pc.insertRange(i, range(0, k).map(j => pointCloudX(90 + j)))
    expect(Array.from(pc.getPoints()).map(p => p.X)).toEqual([...range(0, i), ...range(0, k).map(j => 90 + j), ...range(i, n)])
    expect(Array.from(pc.getNormals()).map(v => v.Z)).toEqual([...range(0, i).map(j => j + 1), ...range(0, k).map(() => 0), ...range(i, n).map(j => j + 1)])
    expect(pc.count).toBe(n + k)
    pc.delete()
  }
})

test('pointCloudInsertRangeOfNothingChangesNothing', () => {
  // insertRange with an empty list leaves the point cloud as it was
  const pc = new rhino.PointCloud()
  pc.addRange([pointCloudX(0), pointCloudX(1)])
  pc.insertRange(1, [])
  expect(Array.from(pc.getPoints()).map(p => p.X)).toEqual([0, 1])
  pc.delete()
})

test('pointCloudInsertRangeOutOfRangeRaises', () => {
  // insertRange at a negative index or past count throws
  const pc = new rhino.PointCloud()
  pc.addRange([pointCloudX(0), pointCloudX(1)])
  helpers.raises(rhino, () => pc.insertRange(-1, [pointCloudX(9)]), /larger than or equal to zero/)
  helpers.raises(rhino, () => pc.insertRange(3, [pointCloudX(9)]), /smaller than or equal to Count/)
  pc.delete()
})

test('pointCloudPointValues', () => {
  // containsPointValues and getPointValues: the values in point order
  const pc = new rhino.PointCloud()
  pc.addPointValue(new rhino.Point3d(0, 0, 0), 2.5)
  pc.addPointValue(new rhino.Point3d(1, 0, 0), -1.0)
  expect(pc.containsPointValues).toBe(true)
  expect(Array.from(pc.getPointValues())).toEqual([2.5, -1.0])
})

test('pointCloudConstructFromPoints', () => {
  // new PointCloud(points) holds those points; an empty array makes an empty cloud
  const pc = new rhino.PointCloud([{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 }])
  expect(pc.count).toBe(2)
  expect(pc.pointAt(1)).toEqual({ X: 1, Y: 2, Z: 3 })
  expect(new rhino.PointCloud([]).count).toBe(0)
})

test('pointCloudCopyConstructorIsADeepCopy', () => {
  // new PointCloud(other) is an independent copy
  const pc = new rhino.PointCloud([{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 }])
  const cp = new rhino.PointCloud(pc)
  pc.add({ X: 9, Y: 9, Z: 9 })
  expect([cp.count, pc.count]).toEqual([2, 3])
})

test('pointCloudConstructorRejectsANumber', () => {
  // new PointCloud(7) throws: no constructor takes a number
  helpers.raises(rhino, () => new rhino.PointCloud(7), /no constructor matches/)
})

test('pointCloudAddAndInsertTakeNormalsColorsAndValues', () => {
  // add / insert take a point with a normal, then optionally a color and a point value (D19)
  const P = (x, y, z) => new rhino.Point3d(x, y, z), V = (x, y, z) => new rhino.Vector3d(x, y, z)
  const red = { r: 255, g: 0, b: 0, a: 255 }, black = { r: 0, g: 0, b: 0, a: 255 }
  const c = new rhino.PointCloud()
  c.add(P(1, 0, 0), V(0, 0, 1))
  c.add(P(2, 0, 0), V(0, 1, 0), red)
  c.add(P(3, 0, 0), V(1, 0, 0), red, 2.5)
  c.insert(3, P(4, 0, 0), V(0, 0, 1))
  c.insert(4, P(5, 0, 0), V(0, 0, 1), red)
  c.insert(5, P(6, 0, 0), V(0, 0, 1), red, 1.5)
  expect(pointCloudChannels(c)).toEqual([6, [[1, 0, 0], [2, 0, 0], [3, 0, 0], [4, 0, 0], [5, 0, 0], [6, 0, 0]],
    [[0, 0, 1], [0, 1, 0], [1, 0, 0], [0, 0, 1], [0, 0, 1], [0, 0, 1]],
    [black, red, red, black, red, red], [0, 0, 2.5, 0, 0, 1.5]])
})

test('pointCloudAddAndInsertTakeAColor', () => {
  // add / insert take a point with a color only: the cloud gets colors and no normals (D19)
  const red = { r: 255, g: 0, b: 0, a: 255 }
  const c = new rhino.PointCloud()
  c.add(new rhino.Point3d(1, 2, 3), red)
  c.insert(1, new rhino.Point3d(4, 5, 6), red)
  expect(pointCloudChannels(c)).toEqual([2, [[1, 2, 3], [4, 5, 6]], [], [red, red], []])
})

test('pointCloudAddRangeTakesPointsOrPointsAndNormals', () => {
  // addRange takes the points alone, or the points and their normals (D19)
  const pts = [[0, 0, 0], [1, 1, 1]], nrm = [[0, 0, 1], [0, 1, 0]]
  const a = new rhino.PointCloud()
  a.addRange(pts.map(q => new rhino.Point3d(...q)))
  expect(pointCloudChannels(a)).toEqual([2, pts, [], [], []])
  const b = new rhino.PointCloud()
  b.addRange(pts.map(q => new rhino.Point3d(...q)), nrm.map(q => new rhino.Vector3d(...q)))
  expect(pointCloudChannels(b)).toEqual([2, pts, nrm, [], []])
})

test('pointCloudAddRangeTakesNormalsColorsAndValues', () => {
  // addRange(points, normals, colors[, values]) sets every channel it is given
  const red = { r: 255, g: 0, b: 0, a: 255 }, blue = { r: 0, g: 0, b: 255, a: 255 }
  const pts = [[0, 0, 0], [1, 0, 0]].map(q => new rhino.Point3d(...q))
  const nrm = [[0, 0, 1], [0, 1, 0]].map(q => new rhino.Vector3d(...q))
  const a = new rhino.PointCloud()
  a.addRange(pts, nrm, [red, blue])
  expect(pointCloudChannels(a).slice(2)).toEqual([[[0, 0, 1], [0, 1, 0]], [red, blue], []])
  const b = new rhino.PointCloud()
  b.addRange(pts, nrm, [red, blue], [1.5, 2.5])
  expect(Array.from(b.getPointValues())).toEqual([1.5, 2.5])
})

test('pointCloudAddRangeTakesPointsAndColors', () => {
  // addRange(points, colors) gives each point its color
  const red = { r: 255, g: 0, b: 0, a: 255 }, blue = { r: 0, g: 128, b: 255, a: 255 }
  const c = new rhino.PointCloud()
  c.addRange([new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 0, 0)], [red, blue])
  expect(Array.from(c.getColors()).map(k => [k.r, k.g, k.b])).toEqual([[255, 0, 0], [0, 128, 255]])
})

test('pointCloudAddAndInsertTakeAPointValue', () => {
  // addPointValue, addRangePointsValues and insertPointValue give each point its value, in order
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const c = new rhino.PointCloud()
  c.addPointValue(P(1, 0, 0), 1.5)
  c.addRangePointsValues([P(2, 0, 0), P(3, 0, 0)], [2.5, 3.5])
  c.insertPointValue(0, P(0, 0, 0), 0.5)
  expect(Array.from(c.getPointValues())).toEqual([0.5, 1.5, 2.5, 3.5])
  expect(Array.from(c.getPoints()).map(q => q.X)).toEqual([0, 1, 2, 3])
})

test('pointCloudInsertPointValueKeepsTheNormalsAligned', () => {
  // insertPointValue into a cloud with normals gives the new point a zero normal and the others keep theirs
  const c = new rhino.PointCloud()
  c.add(new rhino.Point3d(1, 0, 0), new rhino.Vector3d(0, 0, 1))
  c.add(new rhino.Point3d(2, 0, 0), new rhino.Vector3d(0, 1, 0))
  c.insertPointValue(0, new rhino.Point3d(0, 0, 0), 9)
  expect(Array.from(c.getNormals()).map(q => [q.X, q.Y, q.Z])).toEqual([[0, 0, 0], [0, 0, 1], [0, 1, 0]])
  expect(Array.from(c.getPointValues())).toEqual([9, 0, 0])
})

test('pointCloudInsertKeepsEveryChannelWithItsPoint', () => {
  // insert puts the new point's normal, color and value at its index; the other points keep theirs
  const red = { r: 255, g: 0, b: 0, a: 255 }, lime = { r: 0, g: 255, b: 0, a: 255 }, blue = { r: 0, g: 0, b: 255, a: 255 }
  const P = (x, y, z) => new rhino.Point3d(x, y, z), V = (x, y, z) => new rhino.Vector3d(x, y, z)
  const c = new rhino.PointCloud()
  c.add(P(1, 0, 0), V(0, 0, 1), red, 1)
  c.add(P(2, 0, 0), V(0, 1, 0), lime, 2)
  c.insert(0, P(0, 0, 0), V(1, 0, 0), blue, 9)
  expect(Array.from(c.getPointValues())).toEqual([9, 1, 2])
  expect(Array.from(c.getNormals()).map(q => [q.X, q.Y, q.Z])).toEqual([[1, 0, 0], [0, 0, 1], [0, 1, 0]])
  expect(Array.from(c.getColors()).map(k => [k.r, k.g, k.b])).toEqual([[0, 0, 255], [255, 0, 0], [0, 255, 0]])
})

test('pointCloudAddRangePutsANewChannelOnTheNewPoints', () => {
  // addRange with normals on a cloud without them gives the earlier points zero normals
  const P = (x, y, z) => new rhino.Point3d(x, y, z), V = (x, y, z) => new rhino.Vector3d(x, y, z)
  const d = new rhino.PointCloud()
  d.add(P(1, 0, 0)); d.add(P(2, 0, 0))
  d.addRange([P(3, 0, 0), P(4, 0, 0)], [V(0, 0, 1), V(0, 0, 1)])
  expect(Array.from(d.getNormals()).map(q => [q.X, q.Y, q.Z])).toEqual([[0, 0, 0], [0, 0, 0], [0, 0, 1], [0, 0, 1]])
})
