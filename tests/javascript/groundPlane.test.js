const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('groundPlaneConstructedOwnsItsValues', () => {
  // a constructed GroundPlane keeps the values set on it
  const gp = new rhino.GroundPlane()
  gp.altitude = 7.0
  expect(gp.altitude).toBe(7.0)
})
