const rhino3dm = require('rhino3dm')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('unitScale', () => {
  // RhinoMath.unitScale converts between unit systems
  const U = rhino.UnitSystem
  expect(rhino.RhinoMath.unitScale(U.Meters, U.Millimeters)).toBe(1000)
  expect(rhino.RhinoMath.unitScale(U.Inches, U.Millimeters)).toBeCloseTo(25.4)
  expect(rhino.RhinoMath.unitScale(U.CustomUnits, 2, U.Meters, 1)).toBe(2)
  expect(rhino.RhinoMath.metersPerUnit(U.Inches)).toBeCloseTo(0.0254)
})

test('constants', () => {
  // RhinoMath's constants are static getters with RhinoCommon's values
  expect(rhino.RhinoMath.unsetValue).toBe(-1.23432101234321e+308)
  expect(rhino.RhinoMath.zeroTolerance).toBe(2.3283064365386962890625e-10)
  expect(rhino.RhinoMath.twoPI).toBe(2 * Math.PI)
  expect(rhino.RhinoMath.unsetIntIndex).toBe(-2147483647)
})

test('helpers', () => {
  // toRadians, clamp (a double: JS numbers are doubles), wrap and epsilonEquals
  const M = rhino.RhinoMath
  expect(M.toRadians(180)).toBe(Math.PI)
  expect(M.toDegrees(Math.PI)).toBe(180)
  expect(M.clamp(5, 0, 3)).toBe(3)
  expect(M.clamp(0.5, 0, 1)).toBe(0.5)
  expect(M.wrap(370, 0, 360)).toBe(10)
  expect(M.epsilonEquals(1, 1 + 1e-12, 1e-9)).toBe(true)
  expect(M.isValidDouble(M.unsetValue)).toBe(false)
})
