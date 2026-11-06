const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('centermarkGetDisplayLinesReturnsTheCrossLines', () => {
  // getDisplayLines of a centermark read back from a file gives its two cross lines
  const cm = new rhino.Centermark()
  cm.adjustFromPoints(rhino.Plane.worldXY, { X: 1, Y: 2, Z: 0 })
  cm.radius = 3.0
  const f = new rhino.File3dm()
  f.objects.add(cm, null)
  const back = [...rhino.File3dm.fromByteArray(f.toByteArray(null)).objects][0].geometry
  expect(back).toBeInstanceOf(rhino.Centermark)
  const [ok, lines] = back.getDisplayLines(new rhino.DimensionStyle(), 1.0)
  expect(ok).toBe(true)
  const R = x => Math.round(x * 1e9) / 1e9
  const key = l => [R(l.From.X), R(l.From.Y), R(l.To.X), R(l.To.Y)].join(',')
  expect(Array.from(lines).map(key).sort()).toEqual(['0.5,2,1.5,2', '1,1.5,1,2.5'])
})
