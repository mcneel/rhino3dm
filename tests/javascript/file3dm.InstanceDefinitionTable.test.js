const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('instanceDefinitionTableAddWithGeometryAndAttributes', () => {
  // add(name, description, basePoint, geometry, attributes) adds one instance definition
  const f = new rhino.File3dm()
  const n0 = f.allInstanceDefinitions.count
  const circle = new rhino.ArcCurve(new rhino.Circle(2.0))
  const idx = f.allInstanceDefinitions.add('blk', 'desc', { X: 0, Y: 0, Z: 0 }, [circle], [new rhino.ObjectAttributes()])
  expect(idx).toBeGreaterThanOrEqual(0)
  expect(f.allInstanceDefinitions.count).toBe(n0 + 1)
})

test('instanceDefinitionTableFindIndex', () => {
  // allInstanceDefinitions.findIndex returns the definition added at an index
  const f = new rhino.File3dm()
  const circle = new rhino.ArcCurve(new rhino.Circle(1.0))
  const idx = f.allInstanceDefinitions.add('blk', 'desc', new rhino.Point3d(0, 0, 0), [circle], [new rhino.ObjectAttributes()])
  expect(f.allInstanceDefinitions.findIndex(idx)).toBeInstanceOf(rhino.InstanceDefinitionGeometry)
})
