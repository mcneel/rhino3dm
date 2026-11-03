const rhino3dm = require('rhino3dm')

let rhino

let crvFromList, crvFromArray

beforeAll(async() => {

  rhino = await rhino3dm()

  const pointArray = [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }, { X: 3, Y: -1, Z: 0 }, { X: 4, Y: 0, Z: 0 }]

  const pointList = new rhino.Point3dList(5)
  pointList.add(0, 0, 0)
  pointList.add(1, 1, 0)
  pointList.add(2, 0, 0)
  pointList.add(3, -1, 0)
  pointList.add(4, 0, 0)

  crvFromList = rhino.Curve.createControlPointCurve(pointList, 3)
  crvFromArray = rhino.Curve.createControlPointCurve(pointArray, 3)


});
beforeEach( async() => {
    
  })

test('createControlPointCurve', async () => {

    const r1 = crvFromArray.pointAt(0.5)
    const r2 = crvFromList.pointAt(0.5)
    
    const result = r1.X === r2.X && r1.Y === r2.Y && r1.Z === r2.Z

    expect(result).toBe(true)

})

test('frameAt', async () => {

  const result = crvFromArray.frameAt(0.5)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "boolean")
  expect(typeof result[1] == "object")
  expect(Object.keys(result[1]).length).toBe(4) //this is a Plane
  // D11: plain object with RhinoCommon's field names
  expect(Object.keys(result[1])[0]).toBe("Origin")
  expect(Object.keys(result[1])[1]).toBe("XAxis")
  expect(Object.keys(result[1])[2]).toBe("YAxis")
  expect(Object.keys(result[1])[3]).toBe("ZAxis")
  expect(result[0]).toBe(true)

})

test('getCurveParameterFromNurbsFormParameter', async () => {

  const result = crvFromArray.getCurveParameterFromNurbsFormParameter(0.5)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "boolean")
  expect(typeof result[1] == "number")
  expect(result[0]).toBe(true)

})

test('getNurbsFormParameterFromCurveParameter', async () => {

  const result = crvFromArray.getNurbsFormParameterFromCurveParameter(0.5)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "boolean")
  expect(typeof result[1] == "number")
  expect(result[0]).toBe(true)

})

test('split', async () => {

  const result = crvFromArray.split(0.1)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "object")
  expect(result[0].constructor.name).toBe("NurbsCurve")
  expect(typeof result[1] == "object")
  expect(result[1].constructor.name).toBe("NurbsCurve")

})

test('deriativeAt', async () => {

  const result = crvFromArray.derivativeAt(0.5, 2)

  expect(Array.isArray(result)).toBe(true)
  result.forEach( item => {
    // D11: {X, Y, Z} objects (were [x, y, z] arrays)
    expect(Object.keys(item)).toEqual(['X', 'Y', 'Z'])
  })

})

test('deriativeAt2', async () => {

  // derivativeAtSide -> derivativeAt(t, count, side)
  const result = crvFromArray.derivativeAt(0.5, 2, rhino.CurveEvaluationSide.Below)

  expect(Array.isArray(result)).toBe(true)
  result.forEach( item => {
    // D11: {X, Y, Z} objects (were [x, y, z] arrays)
    expect(Object.keys(item)).toEqual(['X', 'Y', 'Z'])
  })

})


