const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('vector3dMultiplyIsTheDotProduct', () => {
  // Vector3d.multiply of two vectors is the dot product
  expect(rhino.Vector3d.multiply({ X: 1, Y: 2, Z: 3 }, { X: -4, Y: 5, Z: 0.5 })).toBe(7.5)
})
