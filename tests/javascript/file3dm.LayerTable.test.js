const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})
//TODO
test('CreateFileWithLayers', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests'
  file3dm.applicationUrl = 'https://rhino3d.com'

  //create layers
  const layer1 = new rhino.Layer()
  layer1.Name = 'layer1'
  layer1.Color = { r: 255, g: 0, b: 255, a: 255 }

  const layer2 = new rhino.Layer()
  layer2.Name = 'layer2'

  file3dm.layers().add(layer1)
  file3dm.layers().add(layer2)

  const qtyLayers = file3dm.layers().count

  const bufferWrite = file3dm.toByteArray()
  fs.writeFileSync('test_createFileWithLayers.3dm', bufferWrite)

  const bufferRead = fs.readFileSync('test_createFileWithLayers.3dm')
  const arr = new Uint8Array(bufferRead)
  const file3dmRead = rhino.File3dm.fromByteArray(arr)

  const qtyLayers2 = file3dmRead.layers().count

  expect(qtyLayers === 2 && qtyLayers2 === 2).toBe(true)

})

test('DeleteLayer', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-layerTable-deleteLayer'
  file3dm.applicationUrl = 'https://rhino3d.com'

  //create layers
  const layer1 = new rhino.Layer()
  layer1.Name = 'layer1'
  layer1.Color = { r: 255, g: 0, b: 255, a: 255 }

  const layer2 = new rhino.Layer()
  layer2.Name = 'layer2'

  const index1 = file3dm.layers().add(layer1)
  const index2 = file3dm.layers().add(layer2)

  const qtyLayers = file3dm.layers().count

  const id1 = file3dm.layers().findIndex(index1).id

  file3dm.layers().delete(id1)

  const qtyLayers2 = file3dm.layers().count

  expect(qtyLayers === 2 && qtyLayers2 === 1).toBe(true)

})