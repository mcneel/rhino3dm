const rhino3dm = require('rhino3dm')
const fs = require('fs');

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})

//objective: test the shape of the group table get group members method
test('createExtrusion', async () => {

  // read model 
  const model = '../models/groups.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  const members = doc.groups().groupMembers(0)

  expect(Array.isArray(members)).toBe(true)
  expect(members[0].constructor.name).toBe('File3dmObject')

})