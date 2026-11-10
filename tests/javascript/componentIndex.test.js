const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('componentIndexHoldsTypeAndIndex', () => {
  // new ComponentIndex(type, index) stores both and is set
  const CI = rhino.ComponentIndex
  const ci = new CI(rhino.ComponentIndexType.BrepFace, 3)
  expect(ci.Index).toBe(3)
  expect(CI.componentIndexType(ci)).toBe(rhino.ComponentIndexType.BrepFace)
  expect(CI.isSet(ci)).toBe(true)
})

test('componentIndexUnsetIsUnset', () => {
  // ComponentIndex.unset has index -1 and isUnset; a real index is not unset
  const CI = rhino.ComponentIndex
  const u = CI.unset
  expect(CI.isUnset(u)).toBe(true)
  expect(u.Index).toBe(-1)
  expect(CI.isUnset(new CI(rhino.ComponentIndexType.BrepFace, 3))).toBe(false)
})

test('componentIndexComparesByValue', () => {
  // component indices are equal by value and unset orders before a set index (equals / compareTo: JS has no operators)
  const CI = rhino.ComponentIndex
  const ci = new CI(rhino.ComponentIndexType.BrepFace, 3)
  const u = CI.unset
  expect(CI.equals(ci, new CI(rhino.ComponentIndexType.BrepFace, 3))).toBe(true)
  expect(CI.equals(ci, u)).toBe(false)
  expect(CI.compareTo(u, ci)).toBeLessThan(0)
})
