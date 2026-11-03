const rhino3dm = require('rhino3dm')

let rhino
let ns
beforeAll(async () => {
  rhino = await rhino3dm()

  ns = rhino.Sphere.toNurbsSurface(rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5))

})

test('knotsToList', async () => {

  // toList -> iteration
  const knotsU = [...ns.knotsU]

  expect(Array.isArray(knotsU)).toBe(true)
  expect(typeof knotsU[0] === 'number').toBe(true)
  expect(knotsU.length == ns.knotsU.count).toBe(true)

  const knotsV = [...ns.knotsV]

  expect(Array.isArray(knotsV)).toBe(true)
  expect(typeof knotsV[0] === 'number').toBe(true)
  expect(knotsV.length == ns.knotsV.count).toBe(true)

})