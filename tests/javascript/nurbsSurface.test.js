const rhino3dm = require('rhino3dm')

let rhino
let ns
beforeAll(async () => {
  rhino = await rhino3dm()

  ns = new rhino.Sphere([0, 0, 0], 5).toNurbsSurface()

})

test('knotsToList', async () => {

  const knotsU = ns.knotsU().toList()

  expect(Array.isArray(knotsU)).toBe(true)
  expect(typeof knotsU[0] === 'number').toBe(true)
  expect(knotsU.length == ns.knotsU().count).toBe(true)

  const knotsV = ns.knotsV().toList()

  expect(Array.isArray(knotsV)).toBe(true)
  expect(typeof knotsV[0] === 'number').toBe(true)
  expect(knotsV.length == ns.knotsV().count).toBe(true)

})