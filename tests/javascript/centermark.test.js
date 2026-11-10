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

test('dimensionConstructorsFromPoints', () => {
  // new Centermark(plane, center, radius), AngularDimension(plane, horizontal, center, def1, def2, dimline)
  // and RadialDimension(type, plane, center, radiusPoint, dimlinePoint) build the annotation
  const P = (x, y, z) => ({ X: x, Y: y, Z: z })
  const xy = rhino.Plane.worldXY
  const cm = new rhino.Centermark(xy, P(1, 2, 0), 3)
  expect(cm.radius).toBe(3)
  const ad = new rhino.AngularDimension(xy, P(1, 0, 0), P(0, 0, 0), P(1, 0, 0), P(0, 1, 0), P(2, 2, 0))
  expect(ad.radius).toBeCloseTo(Math.sqrt(8))
  const rd = new rhino.RadialDimension(rhino.AnnotationType.Radius, xy, P(0, 0, 0), P(5, 0, 0), P(6, 1, 0))
  expect(rd.annotationType).toBe(rhino.AnnotationType.Radius)
  expect([rd.centerPoint.X, rd.centerPoint.Y]).toEqual([0, 0])
})
