const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('point3dDistanceToAnUnsetPointIsZero', () => {
  // distanceTo is 0 when either point is unset, as in RhinoCommon (D2)
  expect(rhino.Point3d.distanceTo(rhino.Point3d.unset, new rhino.Point3d(4, 6, 3))).toBe(0.0)
})
