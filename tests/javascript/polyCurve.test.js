const rhino3dm = require('rhino3dm')

let rhino
let pc
beforeAll(async () => {
  rhino = await rhino3dm()

  pc = new rhino.PolyCurve()
  const line = new rhino.LineCurve({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 1 }).line
  const pointArray = []
  pointArray.push({ X: 1, Y: 1, Z: 1 })
  pointArray.push({ X: 2, Y: 2, Z: 2 })
  pointArray.push({ X: 3, Y: -2, Z: 1 })
  pointArray.push({ X: 4, Y: 2, Z: 1 })
  pointArray.push({ X: 5, Y: -2, Z: 1 })

  const curve = rhino.Curve.createControlPointCurve(pointArray, 3)

  // appendLine / appendCurve -> append
  pc.append(line)
  pc.append(curve)

})

test('explode', async () => {

  explodedCurves = pc.explode()
  expect(explodedCurves.length > 0).toBe(true)
  expect(Array.isArray(explodedCurves)).toBe(true)
  expect(explodedCurves[0]).toBeInstanceOf(rhino.LineCurve)
  expect(explodedCurves[1]).toBeInstanceOf(rhino.NurbsCurve)

})