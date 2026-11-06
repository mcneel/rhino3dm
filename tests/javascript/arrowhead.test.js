const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('arrowheadGetPoints', () => {
  // Arrowhead.getPoints gives a solid triangle's outline
  const pts = [...rhino.Arrowhead.getPoints(rhino.DimensionStyleArrowType.SolidTriangle)].map(p => [p.X, p.Y])
  expect(pts).toEqual([[0, 0], [-1, 0.25], [-1, -0.25]])
})
