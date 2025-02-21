const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})

test('attributesGroupList', async () => { 

    // read model 
    const model = '../models/groups.3dm'

    const buffer = fs.readFileSync(model)
    const arr = new Uint8Array(buffer)
    const doc = rhino.File3dm.fromByteArray(arr)

    expect(doc !== null).toBe(true)

    const objects = doc.objects()

    const obj = objects.get(0)

    groupList = obj.attributes().getGroupList()

    expect(Array.isArray(groupList)).toBe(true)
    expect(Number.isFinite(groupList[0])).toBe(true)

})