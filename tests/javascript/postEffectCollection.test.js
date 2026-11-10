const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

// [id, localName, on, shown, type] of the post effects in tests/models/posteffects.3dm
const postEffectsOfTheModel = [
  ['57baba9e-fc02-40d2-9682-5f61b9049eaa', 'Despeckle', true, true, 0], ['ce2f1c40-7f25-4c2d-b1d3-271e118d843b', 'Bloom', true, false, 0],
  ['7243f003-c4f1-415c-8228-35e5efaabd60', 'Glow', true, false, 0], ['b4d7b2f3-fa0d-47fb-8063-6869e96dbf35', 'Fog', true, false, 0],
  ['ab3b31ab-7d40-4a59-aa5c-5df833a81e8f', 'Depth of field', true, false, 0], ['f8e993bf-3407-4121-b71a-6876c2541cc7', 'Multiplier', true, false, 0],
  ['1ec87a84-f61f-43dc-b331-92d8b81ed468', 'Denoise', true, true, 0], ['acb8d258-c1d6-499d-aa23-02dcdea2b0a2', 'Clamp', false, false, 1],
  ['64e53275-0583-4ec4-9bde-b9413df25f08', 'Black/White point', false, false, 1], ['a51d4149-3685-463e-b32f-d9ac986b7c1b', 'Logarithmic', false, false, 1],
  ['ec302d96-402e-4244-bf25-5d0859ebf37c', 'Filmic', false, false, 1], ['4723b167-50a6-42ce-bba6-ce4f8013495b', 'False color', false, false, 1],
  ['c65eb6ba-ee66-4c48-9457-95f0936eca08', 'Hue/Saturation/Luminance', true, false, 2], ['0034b235-c351-4674-be3f-afec691187b3', 'Brightness/Contrast', true, false, 2],
  ['4398d28e-ed29-4974-9304-d56d0e61cc15', 'Noise', true, false, 2], ['38986982-3eab-4988-b8cb-32dd4ad0baa3', 'Gaussian blur', true, false, 2]]

const postEffectCollectionOf = (rhino, name) => helpers.readModel(rhino, name).settings.renderSettings.postEffects

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('postEffectCollectionItemsOfAFile', () => {
  // iterating renderSettings.postEffects gives each post effect's id, name, on, shown and type (D50)
  const coll = postEffectCollectionOf(rhino, 'posteffects.3dm')
  expect([...coll].map(p => [String(p.id), p.localName, p.on, p.shown, p.type.value])).toEqual(postEffectsOfTheModel)
})

test('postEffectCollectionOfFilesWithoutPostEffectsIsEmpty', () => {
  // a file with no post effects has an empty postEffects collection
  for (const name of ['file3dm_stuff.3dm', 'textEntities_r8.3dm', 'sphereDecals.3dm']) {
    expect([...postEffectCollectionOf(rhino, name)]).toEqual([])
  }
})

test('postEffectCollectionPostEffectDataFromId', () => {
  // postEffectDataFromId finds each post effect by its id
  const coll = postEffectCollectionOf(rhino, 'posteffects.3dm')
  const items = [...coll]
  postEffectsOfTheModel.forEach(([oid, oname], i) => {
    expect([String(items[i].id), items[i].localName]).toEqual([oid, oname])
    const d = coll.postEffectDataFromId(oid)
    expect(String(d.id)).toBe(oid)
    expect(d.localName).toBe(oname)
  })
})

test('postEffectDataFromAnUnknownIdRaisesOnUse', () => {
  // the PostEffectData of an id not in the collection throws when used
  for (const name of ['posteffects.3dm', 'file3dm_stuff.3dm']) {
    const ghost = postEffectCollectionOf(rhino, name).postEffectDataFromId(require('crypto').randomUUID())
    expect(ghost).not.toBeUndefined()
    helpers.raises(rhino, () => ghost.localName, /runtime_error,.*no longer in its owner/)
  }
})

test('postEffectDataWritesReachTheFile', () => {
  // setting on on a PostEffectData changes the post effect in the file's collection
  const coll = postEffectCollectionOf(rhino, 'posteffects.3dm')
  const oid = postEffectsOfTheModel[0][0]
  const d = coll.postEffectDataFromId(oid)
  const on = d.on
  d.on = !on
  expect([...coll].find(p => String(p.id) === oid).on).toBe(!on)
})

test('postEffectCollectionNewIsEmpty', () => {
  // a new PostEffectCollection is empty
  expect([...new rhino.PostEffectCollection()]).toEqual([])
})
