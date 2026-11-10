const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('linetypeTableFromAttributesAndLayer', () => {
  // fromAttributes and fromLayerIndex resolve a default object's linetype to Continuous
  const f = new rhino.File3dm()
  const lt = f.allLinetypes.fromAttributes(new rhino.ObjectAttributes())
  expect(lt).toBeInstanceOf(rhino.Linetype)
  expect([lt.name, lt.index]).toEqual(['Continuous', -1])
  expect(f.allLinetypes.fromLayerIndex(0).name).toBe('Continuous')
})
