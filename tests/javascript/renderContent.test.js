const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

const renderContentAll = f => [...f.renderMaterials, ...f.renderEnvironments, ...f.renderTextures]
const renderContentWithChildren = f => renderContentAll(f).find(c => [...c.children].length > 0)

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('renderContentChildSlotOnAndAmount', () => {
  // childSlotOn / setChildSlotOn and childSlotAmount / setChildSlotAmount take the child slot name first (D64)
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const top = renderContentWithChildren(f)
  const slot = [...top.children][0].childSlotName
  expect([String(top.id), slot]).toEqual(['660433b2-4c4f-459e-962c-8f70942c5ccd', 'bitmap-texture'])
  expect(top.childSlotOn(slot)).toBe(false)
  expect(top.setChildSlotOn(slot, true)).toBe(true)
  expect(top.childSlotOn(slot)).toBe(true)
  expect(top.setChildSlotAmount(slot, 42.0)).toBe(true)
  expect(top.childSlotAmount(slot)).toBe(42.0)
})

test('renderContentChildSlotAmountOfAnUnknownSlotIsZero', () => {
  // childSlotAmount of a slot that has no amount is 0.0, as in RhinoCommon (D64)
  const top = renderContentWithChildren(helpers.readModel(rhino, 'file3dm_stuff.3dm'))
  expect(top.childSlotAmount('no-such-slot')).toBe(0.0)
})

test('renderContentSetChildAddsAChild', () => {
  // setChild(content, slot) adds a child that findChild finds by its slot name
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const top = renderContentWithChildren(f)
  const n = [...top.children].length
  const other = renderContentAll(f).find(c => c.id !== top.id)
  expect(top.setChild(other, 'new-slot')).toBe(true)
  expect([...top.children].length).toBe(n + 1)
  expect(top.findChild('new-slot')).not.toBeNull()
})

test('renderContentChildSlotsSurviveAFileRoundTrip', () => {
  // a child slot amount and a child set with setChild read back from the file's bytes
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const top = renderContentWithChildren(f)
  const slot = [...top.children][0].childSlotName
  expect(top.setChildSlotAmount(slot, 42.0)).toBe(true)
  const other = renderContentAll(f).find(c => c.id !== top.id)
  expect(top.setChild(other, 'new-slot')).toBe(true)
  const back = rhino.File3dm.fromByteArray(f.toByteArray(null))
  const btop = renderContentAll(back).find(c => c.id === top.id)
  expect(btop.childSlotAmount(slot)).toBe(42.0)
  expect(btop.findChild('new-slot')).not.toBeNull()
})

test('renderContentSetChildGivesTheCopyItsOwnId', () => {
  // setChild adds a copy with an id of its own, a child in the given slot
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const top = renderContentWithChildren(f)
  const other = renderContentAll(f).find(c => c.id !== top.id)
  expect(top.setChild(other, 'new-slot')).toBe(true)
  const kid = top.findChild('new-slot')
  expect(kid).not.toBeNull()
  expect(kid.id).not.toBe(other.id)
  expect(kid.isChild).toBe(true)
  expect(kid.childSlotName).toBe('new-slot')
})
