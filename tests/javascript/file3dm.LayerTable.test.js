const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
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
  layer1.name = 'layer1'
  layer1.color = { r: 255, g: 0, b: 255, a: 255 }

  const layer2 = new rhino.Layer()
  layer2.name = 'layer2'

  file3dm.layers.add(layer1)
  file3dm.layers.add(layer2)

  const qtyLayers = file3dm.layers.count

  const bufferWrite = file3dm.toByteArray()
  fs.writeFileSync('test_createFileWithLayers.3dm', bufferWrite)

  const bufferRead = fs.readFileSync('test_createFileWithLayers.3dm')
  const arr = new Uint8Array(bufferRead)
  const file3dmRead = rhino.File3dm.fromByteArray(arr)

  const qtyLayers2 = file3dmRead.layers.count

  expect(qtyLayers === 2 && qtyLayers2 === 2).toBe(true)

})

test('DeleteLayer', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-layerTable-deleteLayer'
  file3dm.applicationUrl = 'https://rhino3d.com'

  //create layers
  const layer1 = new rhino.Layer()
  layer1.name = 'layer1'
  layer1.color = { r: 255, g: 0, b: 255, a: 255 }

  const layer2 = new rhino.Layer()
  layer2.name = 'layer2'

  // add returns nothing; read the item back
  file3dm.layers.add(layer1)
  file3dm.layers.add(layer2)

  const qtyLayers = file3dm.layers.count

  const item1 = file3dm.layers.findIndex(0)

  // D14: delete(item), the id form is gone
  file3dm.layers.delete(item1)

  const qtyLayers2 = file3dm.layers.count

  expect(qtyLayers === 2 && qtyLayers2 === 1).toBe(true)

})

test('LayerIndex', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-layerTable-layerIndex'
  file3dm.applicationUrl = 'https://rhino3d.com'

  //create layers
  const layer1 = new rhino.Layer()
  layer1.name = 'layer1'
  layer1.color = { r: 255, g: 0, b: 255, a: 255 }

  const layer2 = new rhino.Layer()
  layer2.name = 'layer2'

  // add returns nothing; read the items back
  file3dm.layers.add(layer1)
  file3dm.layers.add(layer2)

  const _index1 = file3dm.layers.findIndex(0).layerIndex
  const _index2 = file3dm.layers.findIndex(1).layerIndex

  expect(_index1 === 0 && _index2 === 1).toBe(true)

})

test('readFileWithLayers', async () => {

  // read model 
  const model = '../models/file3dm_stuff.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  expect(doc.layers.count === 6).toBe(true)

})

const LAYERTABLE_BLACK = { r: 0, g: 0, b: 0, a: 255 }
const LAYERTABLE_ZERO_ID = '00000000-0000-0000-0000-000000000000'

// a file with layers a, b, c, and b deleted (indices 0 and 2 remain)
function layerTableAbc(rhino) {
  const f = new rhino.File3dm()
  for (const n of 'abc') f.allLayers.addLayer(n, LAYERTABLE_BLACK)
  f.allLayers.delete(f.allLayers.findIndex(1))
  return f
}

const crypto = require('crypto')

test('layerTableAddLayerReturnsTheIndex', () => {
  // addLayer returns the new layer's index, and findIndex finds it by that index
  const layers = new rhino.File3dm().allLayers
  expect(layers.count).toBe(0)
  const i0 = layers.addLayer('a', { r: 255, g: 0, b: 0, a: 255 })
  const i1 = layers.addLayer('b', { r: 0, g: 255, b: 0, a: 255 })
  expect([i0, i1, layers.count]).toEqual([0, 1, 2])
  expect([0, 1].map(i => layers.findIndex(i).name)).toEqual(['a', 'b'])
})

test('layerTableFindIndexReturnsNoneForAMissingIndex', () => {
  // findIndex returns null for an index with no layer, as RhinoCommon does (D7)
  const layers = new rhino.File3dm().allLayers
  layers.addLayer('a', LAYERTABLE_BLACK)
  expect(layers.findIndex(7)).toBeNull()
})

test('layersAndAllLayersAreTheSameTable', () => {
  // file.layers and file.allLayers are views of the same table (D16)
  const f = new rhino.File3dm()
  f.allLayers.addLayer('a', LAYERTABLE_BLACK)
  f.allLayers.addLayer('b', LAYERTABLE_BLACK)
  expect(f.layers.count).toBe(2)
})

test('layerTableDeleteRaisesForABadIndex', () => {
  // delete(index) throws for an index past the end (D14)
  const layers = new rhino.File3dm().allLayers
  layers.addLayer('a', LAYERTABLE_BLACK)
  helpers.raises(rhino, () => layers.delete(7), /out_of_range,/)
})

test('layerTableItemsAreLiveProxies', async () => {
  // a layer from the table edits the file's layer and keeps the file alive (D15, D52)
  const layer = (() => {
    const f = new rhino.File3dm()
    f.allLayers.addLayer('before', LAYERTABLE_BLACK)
    const l = f.allLayers.findIndex(0)
    l.name = 'after'
    expect(f.allLayers.findIndex(0).name).toBe('after')
    return l
  })()
  await helpers.gcSettle()
  expect(layer.name).toBe('after')
})

test('layerTableAddCopiesTheLayer', () => {
  // add stores a copy: later edits to the added layer do not reach the file
  const f = new rhino.File3dm()
  const mine = new rhino.Layer()
  mine.name = 'mine'
  f.allLayers.add(mine)
  mine.name = 'changed later'
  expect(f.allLayers.findIndex(0).name).toBe('mine')
})

test('layerTableDeleteByItem', () => {
  // delete(layer) removes that layer and returns true; delete(null) returns false (D14)
  const f = new rhino.File3dm()
  f.allLayers.addLayer('a', LAYERTABLE_BLACK)
  expect(f.allLayers.delete(null)).toBe(false)
  expect(f.allLayers.delete(f.allLayers.findIndex(0))).toBe(true)
})

test('layerTableFindNameReturnsTheLayer', () => {
  // findName returns the layer with that name, or null when there is none
  const f = new rhino.File3dm()
  f.allLayers.addLayer('walls', LAYERTABLE_BLACK)
  expect(f.allLayers.findName('walls', LAYERTABLE_ZERO_ID).name).toBe('walls')
  expect(f.allLayers.findName('nope', LAYERTABLE_ZERO_ID)).toBeNull()
})

test('layerTableIterationSkipsDeletedLayers', () => {
  // iterating the table after a delete yields the remaining layers with their stable indices (D6)
  const f = new rhino.File3dm()
  const tb = f.allLayers
  for (const n of 'abc') tb.addLayer(n, LAYERTABLE_BLACK)
  expect(tb.delete(tb.findIndex(1))).toBe(true)
  expect(tb.count).toBe(2)
  expect([...tb].map(l => l.name)).toEqual(['a', 'c'])
  expect([...tb].map(l => l.index)).toEqual([0, 2])       // indices are sparse and stable
  tb.addLayer('d', LAYERTABLE_BLACK)
  expect([...tb].map(l => l.name)).toEqual(['a', 'c', 'd'])
  expect([...f.layers].map(l => l.name)).toEqual(['a', 'c', 'd'])
})

test('layerTableIndexerIsTheLayerIndex', () => {
  // get(i) is the layer whose index is i, and null for a hole or an index past the end (D7)
  const tb = layerTableAbc(rhino).allLayers
  const c = tb.get(2)
  expect(c.name).toBe('c')
  expect(c.index).toBe(2)
  expect(tb.findIndex(1)).toBeNull()
  expect(tb.get(1)).toBeNull()
  // JS get(i) is RhinoCommon's indexer itself (Python's t[i] raises IndexError only for its sequence protocol)
  expect(tb.get(7)).toBeNull()
})

test('layerTableIteratorKeepsTheFileAlive', async () => {
  // an iterator over the table, and the layers it yields, keep the file alive
  const it = (() => {
    const f = new rhino.File3dm()
    f.allLayers.addLayer('only', LAYERTABLE_BLACK)
    return f.allLayers.iterator()
  })()
  await helpers.gcSettle()
  const n = it.next()
  expect(n.done).toBe(false)
  const layer = n.value
  if (typeof it.delete === 'function') it.delete()
  expect(layer.name).toBe('only')
})

test('layerTableFindIdReturnsTheLayerOrNone', () => {
  // layers.findId returns the layer with that id, or null for an unknown id
  const f = new rhino.File3dm()
  const layer = new rhino.Layer()
  layer.name = 'walls'
  f.layers.add(layer)
  const found = f.layers.findIndex(f.layers.count - 1)
  const got = f.layers.findId(found.id)
  expect(got).not.toBeNull()
  expect(got.name).toBe('walls')
  expect(f.layers.findId(crypto.randomUUID())).toBeNull()
})
