const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('ctor', async () => {

    const pointArray = []
    const pointList = new rhino.Point3dList(15)

    for (let i = 0; i < 15; i++) {
        pointArray.push({ X: i, Y: i, Z: 0 })
        pointList.add(i, i, 0)
    }

    const polyline1 = new rhino.PolylineCurve(pointArray)
    const polyline2 = new rhino.PolylineCurve(pointList)

    const r1 = polyline1.point(3)
    const r2 = polyline2.point(3)

    const result = r1.X === r2.X && r1.Y === r2.Y && r1.Z === r2.Z

    expect(result).toBe(true)


})

test('polylineCurveToPolylineReturnsACopy', () => {
  // toPolyline returns a new Polyline of the curve's points; changing it leaves the curve alone
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const crv = new rhino.PolylineCurve([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
  const pl = crv.toPolyline()
  expect(pl).toBeInstanceOf(rhino.Polyline)
  expect(pl.count).toBe(3)
  expect(pl.get(2)).toEqual(P(3, 4, 0))
  expect(pl.length).toBeCloseTo(7.0, 12)
  pl.set(0, P(1, 0, 0))
  expect(crv.toPolyline().get(0)).toEqual(P(0, 0, 0))
})

test('polylineCurveConstructsFromPoints', () => {
  // new PolylineCurve(points) takes an array of points or a Polyline; an empty array makes an empty curve
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const crv = new rhino.PolylineCurve([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
  expect(crv.pointCount).toBe(3)
  expect(crv.pointAtEnd).toEqual(P(3, 4, 0))
  expect(new rhino.PolylineCurve(new rhino.Polyline([P(0, 0, 0), P(1, 0, 0)])).pointCount).toBe(2)
  expect(new rhino.PolylineCurve([]).pointCount).toBe(0)
})
