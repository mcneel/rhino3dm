const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('boundingBoxGetCornersReturnsEightPoints', () => {
  // getCorners returns the eight corners as an array, and null for an unset box (D35)
  const B = rhino.BoundingBox
  const bb = new B(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 2, 3))
  const corners = B.getCorners(bb)
  expect(Array.isArray(corners)).toBe(true)
  expect(corners.length).toBe(8)
  expect(corners[0]).toEqual(new rhino.Point3d(0, 0, 0))
  expect(corners[6]).toEqual(new rhino.Point3d(1, 2, 3))
  expect(B.getCorners(B.unset)).toBeNull()
})

test('boundingBoxTransformMovesTheBox', () => {
  // transform moves the box passed in and returns true, as in C#
  const B = rhino.BoundingBox
  const bb = new B(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 2, 3))
  expect(B.transform(bb, rhino.Transform.translation(new rhino.Vector3d(1, 0, 0)))).toBe(true)
  expect(bb.Min).toEqual(new rhino.Point3d(1, 0, 0))
})

test('boundingBoxClosestPoint', () => {
  // closestPoint returns the nearest point on the box
  const B = rhino.BoundingBox
  const bb = new B(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 2, 3))
  expect(B.closestPoint(bb, new rhino.Point3d(-5, 1, 1))).toEqual(new rhino.Point3d(0, 1, 1))
})

test('boundingBoxGetCornersAndEdgesOfUnsetAreNull', () => {
  // getCorners and getEdges of an unset box are null, where RhinoCommon returns a null array (D35)
  const B = rhino.BoundingBox
  expect(B.getCorners(B.unset)).toBeNull()
  expect(B.getEdges(B.unset)).toBeNull()
  expect(B.getEdges(new B({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 })).length).toBe(12)
})

test('boundingBoxEncodeDecodeUseThe8xJsonForm', () => {
  // BoundingBox.encode writes the 8.x JSON object, and decode reads it back
  const enc = { Min: { X: 1, Y: 2, Z: 3 }, Max: { X: 4, Y: 5.5, Z: 6 } }
  expect(rhino.BoundingBox.encode(new rhino.BoundingBox(1, 2, 3, 4, 5.5, 6))).toEqual(enc)
  const back = rhino.BoundingBox.decode(enc)
  expect([back.Min.X, back.Min.Y, back.Min.Z, back.Max.X, back.Max.Y, back.Max.Z]).toEqual([1, 2, 3, 4, 5.5, 6])
})

test('boundingBoxDecodeRaisesOnAMissingKey', () => {
  // BoundingBox.decode throws when a corner is missing
  helpers.raises(rhino, () => rhino.BoundingBox.decode({ Min: { X: 0, Y: 0, Z: 0 } }), /missing key: Max/)
})

test('boundingBoxInflateXYZInflatesTheBoxInPlace', () => {
  // BoundingBox.inflate(box, x, y, z) grows the box passed in by each amount on both sides and returns nothing (D19)
  const b = new rhino.BoundingBox(new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 1, 1))
  expect(rhino.BoundingBox.inflate(b, 1, 2, 3)).toBeUndefined()
  const xyz = v => [v.X, v.Y, v.Z]
  expect([xyz(b.Min), xyz(b.Max)]).toEqual([[-1, -2, -3], [2, 3, 4]])
})
