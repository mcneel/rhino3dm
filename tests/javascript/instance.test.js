const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('getObjectIDs', async () => {

  // read model 
  const model = '../models/blocks.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const file3dm = rhino.File3dm.fromByteArray(arr)

  expect(file3dm !== null).toBe(true)

  const instanceDefinition = file3dm.instanceDefinitions.get(0)
  const ids = instanceDefinition.getObjectIds()

  expect(Array.isArray(ids)).toBe(true)
  expect(typeof ids[0] === 'string').toBe(true)

})

test('instanceDefinitionGeometryGetObjectIdsIsEmptyForANewDefinition', () => {
  // getObjectIds of a new InstanceDefinitionGeometry is an empty array
  expect(new rhino.InstanceDefinitionGeometry().getObjectIds()).toEqual([])
})

test('instanceDefinitionIsInstanceGeometryId', () => {
  // isInstanceGeometryId is true for the definition's own object ids, false for the nil id
  const f = new rhino.File3dm()
  const circle = new rhino.ArcCurve(new rhino.Circle(1.0))
  const idx = f.allInstanceDefinitions.add('blk', 'desc', new rhino.Point3d(0, 0, 0), [circle], [new rhino.ObjectAttributes()])
  const idef = f.allInstanceDefinitions.findIndex(idx)
  const ids = [...idef.getObjectIds()]
  expect(ids.length).toBe(1)
  expect(idef.isInstanceGeometryId(ids[0])).toBe(true)
  expect(idef.isInstanceGeometryId('00000000-0000-0000-0000-000000000000')).toBe(false)
})
