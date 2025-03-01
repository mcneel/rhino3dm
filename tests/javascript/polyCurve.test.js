const rhino3dm = require('rhino3dm')

let rhino
let pc
beforeAll(async () => {
  rhino = await rhino3dm()

  pc = new rhino.PolyCurve()
  const line = new rhino.LineCurve([0, 0, 0], [1, 1, 1]).line
  const pointArray = []
  pointArray.push([1, 1, 1])
  pointArray.push([2, 2, 2])
  pointArray.push([3, -2, 1])
  pointArray.push([4, 2, 1])
  pointArray.push([5, -2, 1])

  const curve = rhino.Curve.createControlPointCurve(pointArray, 3)

  pc.appendLine(line)
  pc.appendCurve(curve)

})

test('explode', async () => {

  explodedCurves = pc.explode()
  expect(explodedCurves.length > 0).toBe(true)
  expect(Array.isArray(explodedCurves)).toBe(true)
  expect(explodedCurves[0]).toBeInstanceOf(rhino.LineCurve)
  expect(explodedCurves[1]).toBeInstanceOf(rhino.NurbsCurve)

})