const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

// the top-level render contents of file3dm_stuff.3dm: [id, kind, name], and each one's children
const renderContentStuff = [['16421078-5d0e-6851-2d34-062d266c3949', 'environment', 'Studio'],
  ['660433b2-4c4f-459e-962c-8f70942c5ccd', 'material', 'hops'],
  ['ab776f2b-7763-4c53-ac15-d02d3040ff26', 'material', 'Metal']]
const renderContentChildren = {
  'ab776f2b-7763-4c53-ac15-d02d3040ff26': [],
  '660433b2-4c4f-459e-962c-8f70942c5ccd': ['f3785eac-0099-4df4-9666-81c5158011a9'],
  '16421078-5d0e-6851-2d34-062d266c3949': ['63bae601-b151-4a2a-bf62-9e5df2dc5293'] }
const renderContentTops = f => [...f.renderMaterials, ...f.renderEnvironments, ...f.renderTextures]

const renderContentKinds = f => [f.renderMaterials, f.renderEnvironments, f.renderTextures]
const renderContentCopyAll = (src, dst) => {
  const into = renderContentKinds(dst)
  renderContentKinds(src).forEach((have, i) => { for (const c of [...have]) into[i].add(c) })
}

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('file3dmRenderContentCollectionsKeepTheirKind', () => {
  // renderMaterials, renderEnvironments and renderTextures list the file's top-level contents of their own kind
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const mats = [...f.renderMaterials], envs = [...f.renderEnvironments], texs = [...f.renderTextures]
  expect(mats.every(m => m instanceof rhino.File3dmRenderMaterial)).toBe(true)
  expect(envs.every(e => e instanceof rhino.File3dmRenderEnvironment)).toBe(true)
  expect(texs.every(x => x instanceof rhino.File3dmRenderTexture)).toBe(true)
  const key = ([id, kind]) => id + ' ' + kind
  const found = [...mats, ...envs, ...texs].map(c => key([String(c.id), c.kind])).sort()
  expect(found).toEqual(renderContentStuff.map(key).sort())
  expect(mats.every(m => m.kind === 'material')).toBe(true)
  expect(envs.every(e => e.kind === 'environment')).toBe(true)
  for (const m of mats) {
    expect(m.toMaterial()).toBeInstanceOf(rhino.Material)
    expect(m.isTopLevel).toBe(true)
    expect(m.name).toBe(renderContentStuff.find(([id]) => id === String(m.id))[2])
  }
})

test('renderMaterialsFindById', () => {
  // renderMaterials.find(id) returns the material with that id, or null
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const mats = [...f.renderMaterials]
  expect(mats.length).toBeGreaterThan(0)
  const m = f.renderMaterials.find(mats[0].id)
  expect(m).not.toBeNull()
  expect(m.id).toBe(mats[0].id)
  expect(f.renderMaterials.find(require('crypto').randomUUID())).toBeNull()
})

test('renderContentParametersRoundTripByType', () => {
  // setParameter / getParameter round-trip strings, booleans, numbers, uuids and 2-4 number arrays with their types; a missing one is null (D40)
  // a uuid is a string in JS, so it round-trips as a string; an integer number is an int, any other a double
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const m = [...f.renderMaterials][0]
  expect(m.getParameter('rh3dm-no-such-parameter')).toBeNull()
  const values = { s: 'hello', b: true, i: 7, d: 0.25, u: require('crypto').randomUUID(),
    p2: [1.0, 2.0], p3: [1.0, 2.0, 3.0], p4: [1.0, 2.0, 3.0, 4.0] }
  for (const [k, v] of Object.entries(values)) expect(m.setParameter('rh3dm-' + k, v)).toBe(true)
  for (const [k, v] of Object.entries(values)) {
    const got = m.getParameter('rh3dm-' + k)
    expect(got).toEqual(v)
    expect(typeof got).toBe(typeof v)
    expect(Array.isArray(got)).toBe(Array.isArray(v))
  }
  helpers.raises(rhino, () => m.setParameter('rh3dm-bad', {}), /invalid_argument,Variant: unsupported value type/)
})

test('renderContentChildrenLinkToTheirParent', () => {
  // children lists a content's children; each child's parent and topLevel are the top content and findChild finds it by slot name
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const tops = renderContentTops(f)
  expect(tops.filter(c => c.children.length > 0).length).toBeGreaterThan(0)
  for (const top of tops) {
    expect(top.parent).toBeNull()
    expect(top.topLevel.isAliasOf(top)).toBe(true)
    const kids = top.children
    expect(Array.isArray(kids)).toBe(true)
    expect(kids.map(c => String(c.id))).toEqual(renderContentChildren[String(top.id)])
    for (const c of kids) {
      expect(c).toBeInstanceOf(rhino.File3dmRenderContent)
      expect(c.isChild).toBe(true)
      expect(c.isTopLevel).toBe(false)
      expect(c.parent.id).toBe(top.id)
      expect(c.parent.isAliasOf(c.parent)).toBe(true)     // the same parent object every time
      expect(c.topLevel.id).toBe(top.id)
      const found = top.findChild(c.childSlotName)
      expect(found).not.toBeNull()
      expect(found.id).toBe(c.id)
    }
    expect(top.findChild('rh3dm-no-such-slot')).toBeNull()
  }
})

test('renderContentChildKeepsTheFileAlive', async () => {
  // a child content stays usable, parent included, after the file and its top-level contents are released
  const kid = (() => {
    const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
    return renderContentTops(f).filter(c => c.children.length > 0)[0].children[0]
  })()
  const pid = kid.parent.id
  await helpers.gcSettle()
  expect(kid.parent.id).toBe(pid)
  expect(kid.name == null).toBe(false)
})

test('renderContentAddCopiesTheContentIntoTheModel', () => {
  // add on renderMaterials / renderEnvironments / renderTextures copies a content in, keeping its id
  const src = helpers.readModel(rhino, 'file3dm_stuff.3dm'), dst = new rhino.File3dm()
  renderContentCopyAll(src, dst)
  const got = renderContentKinds(dst).flatMap(k => [...k].map(c => `${c.id} ${c.kind}`)).sort()
  expect(got).toEqual(['16421078-5d0e-6851-2d34-062d266c3949 environment', '660433b2-4c4f-459e-962c-8f70942c5ccd material',
                       'ab776f2b-7763-4c53-ac15-d02d3040ff26 material'])
})

test('renderContentAddTwiceMakesACopyWithANewId', () => {
  // adding a content whose id is already in the model adds a copy with a new id, which a write keeps
  const src = helpers.readModel(rhino, 'file3dm_stuff.3dm'), dst = new rhino.File3dm()
  renderContentCopyAll(src, dst)
  dst.renderMaterials.add([...src.renderMaterials][0])
  const ids = [...dst.renderMaterials].map(c => c.id)
  expect(new Set(ids).size).toBe([...src.renderMaterials].length + 1)
  const back = rhino.File3dm.fromByteArray(dst.toByteArray(null))
  expect([...back.renderMaterials].map(c => c.id).sort()).toEqual(ids.slice().sort())
})
