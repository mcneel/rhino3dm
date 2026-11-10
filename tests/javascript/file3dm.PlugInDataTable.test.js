const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('plugInDataTableIterates', () => {
  // plugInData iterates over count File3dmPlugInData records, each with its plug-in id
  const g = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const pd = [...g.plugInData]
  expect(pd.length).toBe(g.plugInData.count)
  expect(pd.every(p => p instanceof rhino.File3dmPlugInData && /^[0-9a-f-]{36}$/i.test(p.plugInId))).toBe(true)
})
