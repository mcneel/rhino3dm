const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('read3dmTables', async () => {

  // read model 
  const model = '../models/file3dm_stuff.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  const objectTableCnt = doc.objects().count
  const layerTableCnt = doc.layers().count
  const materialsTableCnt = doc.materials().count
  const linetypeTableCnt = doc.linetypes().count
  const bitmapsTableCnt = doc.bitmaps().count
  const groupTableCnt = doc.groups().count
  const idTableCnt = doc.instanceDefinitions().count
  const viewTableCnt = doc.views().count
  const namedViewsTableCnt = doc.namedViews().count
  const pluginDataTableCnt = doc.plugInData().count
  const stringTableCnt = doc.strings().count
  const embeddedFileTableCnt = doc.embeddedFiles().count
  const renderContentTableCnt = doc.renderContent().count

  expect(objectTableCnt === 22).toBe(true)
  expect(layerTableCnt === 6).toBe(true)
  expect(materialsTableCnt === 2).toBe(true)
  expect(linetypeTableCnt === 6).toBe(true)
  expect(bitmapsTableCnt === 0).toBe(true)
  expect(groupTableCnt === 2).toBe(true)
  expect(idTableCnt === 1).toBe(true)
  expect(viewTableCnt === 4).toBe(true)
  expect(namedViewsTableCnt === 2).toBe(true)
  expect(pluginDataTableCnt === 1).toBe(true)
  expect(stringTableCnt === 1).toBe(true)
  expect(embeddedFileTableCnt === 1).toBe(true)
  expect(renderContentTableCnt === 3).toBe(true)

})

test('embeddedFilePaths', async () => {

  // read model 
  const model = '../models/file3dm_stuff.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  const ef = doc.embeddedFilePaths()

  expect(Array.isArray(ef)).toBe(true)
  expect(typeof ef[0] === 'string').toBe(true)

})