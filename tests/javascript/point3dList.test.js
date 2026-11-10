const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

// the X coordinates of a Point3dList's points
const point3dListXs = pl => [...pl].map(p => p.X)

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('point3dListEditsLikeAList', () => {
  // Point3dList supports count, get / set, iteration, add, insert, removeAt, indexOf and contains, and get raises past the end
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pl = new rhino.Point3dList([P(3, 0, 0), P(1, 0, 0), P(2, 0, 0)])
  expect(pl.count).toBe(3)
  expect(pl.get(1)).toEqual(P(1, 0, 0))
  expect(point3dListXs(pl)).toEqual([3, 1, 2])
  pl.set(0, P(0, 0, 0))
  pl.add(P(5, 0, 0))
  pl.add(6, 0, 0)
  pl.insert(1, P(9, 0, 0))
  pl.removeAt(0)
  expect(point3dListXs(pl)).toEqual([9, 1, 2, 5, 6])
  expect(pl.indexOf(P(2, 0, 0))).toBe(2)
  expect(pl.contains(P(5, 0, 0))).toBe(true)
  expect(pl.contains(P(7, 0, 0))).toBe(false)
  helpers.raises(rhino, () => pl.get(5), /out_of_range/)
})

test('point3dListReverseSortAndBinarySearch', () => {
  // reverse reverses the points, sort orders them by X, then Y, then Z, and binarySearch returns the index or its complement
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pl = new rhino.Point3dList([P(9, 0, 0), P(1, 0, 0), P(2, 0, 0), P(5, 0, 0), P(6, 0, 0)])
  pl.reverse()
  expect(point3dListXs(pl)).toEqual([6, 5, 2, 1, 9])
  pl.sort()
  expect(point3dListXs(pl)).toEqual([1, 2, 5, 6, 9])
  expect(pl.binarySearch(P(5, 0, 0))).toBe(2)
  expect(pl.binarySearch(P(3, 0, 0))).toBe(~2)
})

test('point3dListCapacityGrowsLikeRhinoList', () => {
  // an empty Point3dList has capacity 0, then 4 after the first add, then doubles
  const e = new rhino.Point3dList()
  expect(e.capacity).toBe(0)
  e.add(new rhino.Point3d(0, 0, 0))
  expect(e.capacity).toBe(4)
  for (let i = 0; i < 4; i++) e.add(new rhino.Point3d(i, 0, 0))
  expect(e.capacity).toBe(8)
  expect(e.count).toBe(5)
})

test('point3dListDuplicateToArrayAndBoundingBox', () => {
  // duplicate is an independent Point3dList, toArray holds the same points and boundingBox spans them
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pl = new rhino.Point3dList([P(1, 0, 0), P(2, 0, 0), P(5, 0, 0), P(6, 0, 0), P(9, 0, 0)])
  const d = pl.duplicate()
  d.set(0, P(42, 0, 0))
  expect(pl.get(0)).toEqual(P(1, 0, 0))
  expect(d).toBeInstanceOf(rhino.Point3dList)
  expect(pl.toArray()).toEqual([...pl])
  const bb = pl.boundingBox
  expect(bb.Min).toEqual(P(1, 0, 0))
  expect(bb.Max).toEqual(P(9, 0, 0))
})

test('point3dListAddCoordinates', () => {
  // Point3dList.add(x, y, z) appends a point
  const pl = new rhino.Point3dList()
  pl.add(1.0, 2.0, 3.0)
  expect([pl.get(0).X, pl.get(0).Z]).toEqual([1, 3])
})
