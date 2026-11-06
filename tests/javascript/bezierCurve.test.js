const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('bezierSplit', async () => {

    const pointArray = [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }, { X: 3, Y: -1, Z: 0 }, { X: 4, Y: 0, Z: 0 }]

    const ncurve = rhino.NurbsCurve.create(true, 3, pointArray)

    const bezier = ncurve.convertSpanToBezier(0)
    const result = bezier.split(0.5)
    
    expect(result[0]).toBe(true)
    expect(result.length).toBe(3)
    expect(result[1].constructor.name).toBe('BezierCurve')
    expect(result[2].constructor.name).toBe('BezierCurve')

})

test('bezierCurveSplitReturnsSuccessAndTheHalves', () => {
  // BezierCurve.split returns [true, left, right] meeting at t, and [false, null, null] outside the domain
  const nc = rhino.NurbsCurve.createFromCircle(new rhino.Circle(new rhino.Point3d(0, 0, 0), 1.0))
  const bz = nc.convertSpanToBezier(0)
  const [ok, left, right] = bz.split(0.5)
  expect(ok).toBe(true)
  expect(left).toBeInstanceOf(rhino.BezierCurve)
  expect(right).toBeInstanceOf(rhino.BezierCurve)
  expect(left.pointAt(1.0)).toEqual(right.pointAt(0.0))
  expect(right.pointAt(0.0)).toEqual(bz.pointAt(0.5))
  const bad = bz.split(2.0)
  expect(bad.length).toBe(3)
  expect(bad[0]).toBe(false)
  expect(bad[1] == null && bad[2] == null).toBe(true)
})
