const rhino3dm = require('rhino3dm')

let rhino
let crvFromList, crvFromArray
beforeAll(async () => {
  rhino = await rhino3dm()

  const pointArray = [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }, { X: 3, Y: -1, Z: 0 }, { X: 4, Y: 0, Z: 0 }]

  const pointList = new rhino.Point3dList(5)
  pointList.add(0, 0, 0)
  pointList.add(1, 1, 0)
  pointList.add(2, 0, 0)
  pointList.add(3, -1, 0)
  pointList.add(4, 0, 0)

  crvFromList = rhino.NurbsCurve.create(false, 3, pointList)
  crvFromArray = rhino.NurbsCurve.create(false, 3, pointArray)

})

test('NurbsCurve_create', async () => {

  const result = crvFromArray.points.count === crvFromList.points.count && crvFromArray.points.controlPolygonLength === crvFromList.points.controlPolygonLength

  expect(result).toBe(true)

})

test('knotsToList', async () => {

  // toList -> iteration
  const knotsList = [...crvFromList.knots]

  expect(Array.isArray(knotsList)).toBe(true)
  expect(typeof knotsList[0] === 'number').toBe(true)
  expect(knotsList.length == crvFromList.knots.count).toBe(true)

})