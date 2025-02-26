const rhino3dm = require('rhino3dm')

let rhino

let crvFromList, crvFromArray

beforeAll(async() => {

  rhino = await rhino3dm()

  const pointArray = [ [ 0, 0, 0 ], [ 1, 1, 0 ], [ 2, 0, 0 ], [ 3, -1, 0 ], [ 4, 0, 0 ] ]

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
    
    const result = r1[0] === r2[0] && r1[1] === r2[1] && r1[2] === r2[2]

    expect(result).toBe(true)

})

test('frameAt', async () => {

  const result = crvFromArray.frameAt(0.5)

  expect(result.length).toBe(2)
  expect(typeof result[0] == "boolean")
  expect(typeof result[1] == "object")
  expect(Object.keys(result[1]).length).toBe(4) //this is a Plane
  expect(Object.keys(result[1])[0]).toBe("origin")
  expect(Object.keys(result[1])[1]).toBe("xAxis")
  expect(Object.keys(result[1])[2]).toBe("yAxis")
  expect(Object.keys(result[1])[3]).toBe("zAxis")
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
    //these should x,y,z arrays
    expect(Array.isArray(item)).toBe(true)
    expect(item.length).toBe(3)
  })

})

test('deriativeAt2', async () => {

  const result = crvFromArray.derivativeAtSide(0.5, 2, rhino.CurveEvaluationSide.Below)

  expect(Array.isArray(result)).toBe(true)
  result.forEach( item => {
    //these should x,y,z arrays
    expect(Array.isArray(item)).toBe(true)
    expect(item.length).toBe(3)
  })

})


