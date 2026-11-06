const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

// a File3dm with layers top > mid > leaf: [file, top, leaf]
function layerThreeLevels(rhino) {
  const f = new rhino.File3dm()
  const t = f.allLayers
  const black = { r: 0, g: 0, b: 0, a: 255 }
  t.addLayer('top', black)
  const top = [...t][0]
  t.addLayer('mid', black, top.id)
  const mid = [...t].find(l => l.name === 'mid')
  t.addLayer('leaf', black, mid.id)
  const leaf = [...t].find(l => l.name === 'leaf')
  return [f, top, leaf]
}

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('layerFullPathJoinsTheParentNames', () => {
  // fullPath is the layer's name after its parents', joined by namePathSeparator, and toString returns it
  const [f, top, leaf] = layerThreeLevels(rhino)
  const sep = rhino.ModelComponent.namePathSeparator
  expect(top.fullPath).toBe('top')
  expect(leaf.fullPath).toBe(['top', 'mid', 'leaf'].join(sep))
  expect(leaf.toString()).toBe(leaf.fullPath)
})

test('layerNameIsWritable', () => {
  // setting name renames the layer in its file's full path
  const [f, top, leaf] = layerThreeLevels(rhino)
  const sep = rhino.ModelComponent.namePathSeparator
  leaf.name = 'leaf2'
  expect([...f.allLayers].find(l => l.id === leaf.id).fullPath.endsWith(sep + 'leaf2')).toBe(true)
})

test('layerFullPathOutsideAFileIsItsName', () => {
  // a layer in no file has its name as fullPath
  expect(new rhino.Layer().fullPath).toBe(new rhino.Layer().name)
})
