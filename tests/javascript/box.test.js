const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('boxContainsAndGetCorners', () => {
  // a box from a bounding box contains its center and has eight corners
  const box = new rhino.Box(new rhino.BoundingBox(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 1, 1)))
  expect(rhino.Box.contains(box, new rhino.Point3d(0.5, 0.5, 0.5))).toBe(true)
  expect(rhino.Box.getCorners(box).length).toBe(8)
})

test('boxGetCornersOfUnsetIsEmpty', () => {
  // getCorners of an unset Box is an empty array, as RhinoCommon returns an empty array, not null (D35)
  expect(rhino.Box.getCorners(rhino.Box.unset)).toEqual([])
})
