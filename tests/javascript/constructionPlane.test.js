const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('constructionPlaneDefaults', () => {
  // a new ConstructionPlane has RhinoCommon's defaults: spacing 1, 70 lines, every 5th thick, depth buffered, world XY, no name
  const cp = new rhino.ConstructionPlane()
  expect(cp.gridSpacing).toBe(1.0)
  expect(cp.gridLineCount).toBe(70)
  expect(cp.thickLineFrequency).toBe(5)
  expect(cp.depthBuffered).toBe(true)
  expect(cp.plane.Origin).toEqual(new rhino.Point3d(0, 0, 0))
  expect(cp.name).toBe('')
})

test('constructionPlaneNameIsSettable', () => {
  // ConstructionPlane.name reads back as set
  const cp = new rhino.ConstructionPlane()
  cp.name = 'top'
  expect(cp.name).toBe('top')
})
