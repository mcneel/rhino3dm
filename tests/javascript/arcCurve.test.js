const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

function arcCurveCheck(c, t0, t1, end) {
  helpers.approx(c.domain.T0, t0, 1e-12)
  helpers.approx(c.domain.T1, t1, 1e-12)
  const b = c.pointAtEnd
  helpers.approx(b.X, end[0], 1e-9)
  helpers.approx(b.Y, end[1], 1e-9)
  helpers.approx(b.Z, end[2], 1e-9)
}

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('arcCurveConstructsFromNone', () => {
  // new ArcCurve(null) constructs an empty ArcCurve, as RhinoCommon's new ArcCurve(null) (D10)
  expect(new rhino.ArcCurve(null)).toBeInstanceOf(rhino.ArcCurve)
})

test('arcCurveConstructorsFromArcAndCircle', () => {
  // the ArcCurve constructors take an arc or a circle, with an optional domain
  const arc = new rhino.Arc({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 })
  const circle = new rhino.Circle(3.0)
  arcCurveCheck(new rhino.ArcCurve(arc), 0, 3.141592653589793, [2, 0, 0])
  arcCurveCheck(new rhino.ArcCurve(arc, 2.0, 5.0), 2, 5, [2, 0, 0])
  arcCurveCheck(new rhino.ArcCurve(circle), 0, 18.84955592153876, [3, 0, 0])
  arcCurveCheck(new rhino.ArcCurve(circle, 1.0, 4.0), 1, 4, [3, 0, 0])
})
