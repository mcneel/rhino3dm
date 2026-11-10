const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('getSegments', async () => {

  const lt = rhino.Linetype.dashed

  const segment = lt.getSegment(0)

  expect(Array.isArray(segment)).toBe(true)
  expect(segment.length === 2)
  expect(typeof segment[0] === 'number')
  expect(typeof segment[1] === 'boolean')

})

test('linetypeCopyConstructorMakesAnIndependentCopy', () => {
  // new Linetype(other) is a deep copy: segments added to the copy leave the original alone
  const a = new rhino.Linetype()
  a.appendSegment(1.5, true)
  const b = new rhino.Linetype(a)
  b.appendSegment(2.0, false)
  expect([a.patternLength, b.patternLength]).toEqual([1.5, 3.5])
})

test('linetypeIsFreedCleanly', () => {
  // deleting a Linetype does not crash (it once freed its pattern twice)
  const lt = new rhino.Linetype()
  lt.delete()
})

test('linetypeIsNotModifiedOrPatternLocked', () => {
  // a new Linetype reports isModified and isPatternLocked as false, as RhinoCommon does outside Rhino
  const lt = new rhino.Linetype()
  expect(lt.isModified).toBe(false)
  expect(lt.isPatternLocked).toBe(false)
})

test('linetypeBuiltIns', () => {
  // The built-in linetypes have their names, negative indices and segment counts
  const expected = [['continuous', 'Continuous', -1, 1], ['byLayer', 'By Layer', -2, 1], ['byParent', 'By Parent', -3, 1],
                    ['hidden', 'Hidden', -4, 2], ['dashed', 'Dashed', -5, 2], ['dashDot', 'DashDot', -6, 4],
                    ['center', 'Center', -7, 4], ['border', 'Border', -8, 6], ['dots', 'Dots', -9, 2]]
  expected.forEach(([prop, ...o], i) => {
    const n = rhino.Linetype[prop]
    expect(n).toBeInstanceOf(rhino.Linetype)
    expect([prop, n.name, n.index, n.segmentCount]).toEqual([prop, ...o])
    expect(n.index).toBe(-(i + 1))
  })
})

test('linetypeClearPattern', () => {
  // clearPattern removes a linetype's segments
  const lt = new rhino.Linetype()
  lt.appendSegment(1.0, true)
  lt.appendSegment(0.5, false)
  expect(lt.clearPattern()).toBe(true)
  expect(lt.segmentCount).toBe(0)
})

test('linetypeSetSegmentsDashesAndGaps', () => {
  // setSegments takes lengths: >= 0 a dash, < 0 a gap; getSegment(i) returns [length, isSolid]
  const lt = new rhino.Linetype()
  expect(lt.setSegments([1.0, -0.5])).toBe(true)
  expect([0, 1].map(i => Array.from(lt.getSegment(i)))).toEqual([[1.0, true], [0.5, false]])
})
