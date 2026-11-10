const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

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

test('polyCurveAppendCopiesTheSegment', () => {
  // append copies the segment: the polycurve keeps it after the argument is deleted
  const pc = new rhino.PolyCurve()
  const seg = new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 0, 0))
  expect(pc.append(seg)).toBe(true)
  seg.delete()
  expect(pc.segmentCount).toBe(1)
  expect(pc.pointAtEnd).toEqual({ X: 1, Y: 0, Z: 0 })
})

test('polyCurveAppendNoneReturnsFalse', () => {
  // append(null) returns false, as RhinoCommon's null check does (D10)
  expect(new rhino.PolyCurve().append(null)).toBe(false)
})

test('polyCurveSegmentIndexesReturnsTheCountFirst', () => {
  // PolyCurve.segmentIndexes returns [count, start index, end index] (D13)
  const seg = new rhino.PolyCurve().segmentIndexes(new rhino.Interval(0, 1))
  expect(seg.length).toBe(3)
  expect(Number.isInteger(seg[0])).toBe(true)
})

test('polyCurveSegmentCurveWritesThroughToTheSegment', () => {
  // segmentCurve returns the segment itself: reversing it reverses the polycurve's segment; past the end it is null
  const P = (X, Y, Z) => new rhino.Point3d(X, Y, Z)
  const pc = new rhino.PolyCurve()
  pc.append(new rhino.LineCurve(P(0, 0, 0), P(2, 0, 0)))
  pc.append(new rhino.LineCurve(P(2, 0, 0), P(2, 3, 0)))
  const seg = pc.segmentCurve(0)
  expect(seg).toBeInstanceOf(rhino.LineCurve)
  seg.reverse()
  const s = pc.segmentCurve(0).pointAtStart
  expect([s.X, s.Y]).toEqual([2, 0])
  expect(pc.segmentCurve(5)).toBeNull()
})

test('polyCurveDuplicateIsAPolyCurve', () => {
  // duplicate of a PolyCurve is a PolyCurve with the same segments
  const pc = new rhino.PolyCurve()
  pc.append(new rhino.LineCurve(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 0, 0)))
  const pd = pc.duplicate()
  expect(pd).toBeInstanceOf(rhino.PolyCurve)
  expect(pd.segmentCount).toBe(1)
})

test('polyCurveExplodeReturnsCopiesOfTheSegments', () => {
  // explode returns each segment as its own curve, and an empty array for an empty polycurve
  const P = (X, Y, Z) => new rhino.Point3d(X, Y, Z)
  const pc = new rhino.PolyCurve()
  pc.append(new rhino.LineCurve(P(0, 0, 0), P(1, 0, 0)))
  pc.append(new rhino.LineCurve(P(1, 0, 0), P(1, 1, 0)))
  const segs = pc.explode()
  expect(segs.length).toBe(2)
  expect(segs.every(s => s instanceof rhino.LineCurve)).toBe(true)
  expect(segs[1].pointAtEnd).toEqual(P(1, 1, 0))
  expect(new rhino.PolyCurve().explode()).toEqual([])
})

test('polyCurveAppendTakesAnArcOrACurve', () => {
  // PolyCurve.append takes an arc or a curve as the next segment (D20)
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pc = new rhino.PolyCurve()
  expect(pc.append(new rhino.Arc(P(0, 0, 0), P(1, 1, 0), P(2, 0, 0)))).toBe(true)
  expect(pc.append(new rhino.LineCurve(P(2, 0, 0), P(4, 0, 0)))).toBe(true)
  expect(pc.segmentCount).toBe(2)
  const e = pc.pointAtEnd
  expect([e.X, e.Y, e.Z]).toEqual([4, 0, 0])
})

test('polyCurveAppendTakesALine', () => {
  // PolyCurve.append(line) adds the line as the next segment (D20)
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const pc = new rhino.PolyCurve()
  expect(pc.append(new rhino.Line(P(0, 0, 0), P(2, 0, 0)))).toBe(true)
  expect(pc.append(new rhino.Line(P(2, 0, 0), P(2, 3, 0)))).toBe(true)
  expect(pc.segmentCount).toBe(2)
  expect([pc.pointAtEnd.X, pc.pointAtEnd.Y]).toEqual([2, 3])
})
