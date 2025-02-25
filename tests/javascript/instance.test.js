const rhino3dm = require('rhino3dm')
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

  const instanceDefinition = file3dm.instanceDefinitions().get(0)
  const ids = instanceDefinition.getObjectIds()

  expect(Array.isArray(ids)).toBe(true)
  expect(typeof ids[0] === 'string').toBe(true)

})