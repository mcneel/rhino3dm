const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('arcAngleIsTheSweepInRadians', () => {
  // Arc.angle of a half circle through three points is pi
  const a = new rhino.Arc({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 })
  helpers.approx(rhino.Arc.angle(a), Math.PI, 1e-12)
})
