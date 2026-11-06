const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('manifestTableCounts', () => {
  // manifest count and longCount agree, its componentType is Mixed and activeObjectCount counts one type
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const m = f.manifest
  const T = rhino.ModelComponentType
  expect(Number.isInteger(m.count)).toBe(true)
  expect(m.count).toBeGreaterThan(0)
  expect(Number(m.longCount)).toBe(m.count)          // a BigInt (C# long)
  expect(m.componentType).toBe(T.Mixed)
  expect([...f.allLayers].length).toBeGreaterThan(0)
  expect(m.activeObjectCount(T.Layer)).toBe(f.allLayers.count)
})

test('manifestTableGetEnumeratorWalksOneType', () => {
  // getEnumerator(type) yields that type's components: layers as Layer, materials as Material, objects as File3dmObject
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const m = f.manifest
  const T = rhino.ModelComponentType
  const viaenum = [...m.getEnumerator(T.Layer)]
  expect(viaenum.every(x => x instanceof rhino.Layer)).toBe(true)
  expect(viaenum.map(x => String(x.id))).toEqual([...f.allLayers].map(l => String(l.id)))
  expect([...m.getEnumerator(T.RenderMaterial)].every(x => x instanceof rhino.Material)).toBe(true)
  const objs = [...m.getEnumerator(T.ModelGeometry)]
  expect(objs.every(x => x instanceof rhino.File3dmObject)).toBe(true)
  expect(objs.length).toBe([...f.objects].length)
})

test('manifestTableFindByIdIndexAndName', () => {
  // findId, findIndex and findName return the component as its own class; an unknown id gives null
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const m = f.manifest
  const T = rhino.ModelComponentType
  const l0 = [...f.allLayers][0]
  for (const found of [m.findId(l0.id), m.findId(l0.id, T.Layer), m.findIndex(l0.index, T.Layer),
    m.findName(l0.name, T.Layer, '00000000-0000-0000-0000-000000000000')]) {
    expect(found).toBeInstanceOf(rhino.Layer)
    expect(found.id).toBe(l0.id)
    expect(found.name).toBe(l0.name)
  }
  expect(m.findId(require('crypto').randomUUID())).toBeNull()
})

test('manifestTableFindIndexRejectsUnsetType', () => {
  // findIndex with ModelComponentType.Unset raises out_of_range (ArgumentOutOfRangeException in .NET)
  const m = helpers.readModel(rhino, 'file3dm_stuff.3dm').manifest
  helpers.raises(rhino, () => m.findIndex(0, rhino.ModelComponentType.Unset), /out_of_range,.*Parameter 'type'/)
})

test('manifestTableIteratesEveryComponent', () => {
  // iterating the manifest yields ModelComponents, and raises for a type it cannot make, as RhinoCommon does
  const m = helpers.readModel(rhino, 'file3dm_stuff.3dm').manifest
  const T = rhino.ModelComponentType
  const unmade = [T.TextureMapping, T.HistoryRecord, T.TextStyle, T.SectionStyle].filter(x => m.activeObjectCount(x))
  if (unmade.length) helpers.raises(rhino, () => [...m], /runtime_error/)
  else expect([...m].every(x => x instanceof rhino.ModelComponent)).toBe(true)
})

test('manifestTableItemsKeepTheFileAlive', async () => {
  // components from the manifest stay usable after the file is released
  let f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const viaenum = [...f.manifest.getEnumerator(rhino.ModelComponentType.Layer)]
  const name = [...f.allLayers][0].name
  f = null
  await helpers.gcSettle()
  expect(viaenum[0].name).toBe(name)
})

test('manifestTableImagesAreBitmaps', () => {
  // the manifest's images are Bitmap objects with the image's path and id (D44)
  const f = new rhino.File3dm()
  const b = new rhino.Bitmap()
  b.fileFullPath = '/tmp/rh3dm_manifest.png'
  const added = f.bitmaps.add(b)
  const imgs = [...f.manifest.getEnumerator(rhino.ModelComponentType.Image)]
  expect(imgs.length).toBe(1)
  expect(imgs[0]).toBeInstanceOf(rhino.Bitmap)
  expect(imgs[0].fileFullPath).toBe('/tmp/rh3dm_manifest.png')
  expect(imgs[0].id).toBe(added.id)
  expect(f.manifest.findId(added.id).fileFullPath).toBe('/tmp/rh3dm_manifest.png')
})

test('manifestFindIndexRenderContent', () => {
  // manifest.findIndex(i, RenderContent) returns the file's render content by index, as a File3dmRenderContent
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const ids = ['ab776f2b-7763-4c53-ac15-d02d3040ff26', '660433b2-4c4f-459e-962c-8f70942c5ccd', '16421078-5d0e-6851-2d34-062d266c3949']
  ids.forEach((id, i) => {
    const c = f.manifest.findIndex(i, rhino.ModelComponentType.RenderContent)
    expect(c).toBeInstanceOf(rhino.File3dmRenderContent)
    expect(c.id).toBe(id)
  })
})
