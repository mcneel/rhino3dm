const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

test('toFloatArray', async () => {

    const xform = rhino.Transform.identity
    const fa = rhino.Transform.toFloatArray(xform, false)

    expect(fa.length === 16).toBe(true)
    expect(Array.isArray(fa)).toBe(true)
    expect(typeof fa[0] === 'number').toBe(true)

})

// a transform's top three rows, row by row
function transformRows(x) {
  return [x.M00, x.M01, x.M02, x.M03, x.M10, x.M11, x.M12, x.M13, x.M20, x.M21, x.M22, x.M23]
}

test('transformTransformListReturnsTheMovedPoints', () => {
  // Transform.transformList returns the transformed points as an array
  const xf = rhino.Transform.translation(new rhino.Vector3d(1, 0, 0))
  const moved = rhino.Transform.transformList(xf, [new rhino.Point3d(0, 0, 0), new rhino.Point3d(1, 1, 1)])
  expect(moved).toEqual([new rhino.Point3d(1, 0, 0), new rhino.Point3d(2, 1, 1)])
})

test('transformToFloatArrayHasSixteenValues', () => {
  // toFloatArray returns the sixteen matrix values
  const xf = rhino.Transform.translation(new rhino.Vector3d(1, 0, 0))
  expect(rhino.Transform.toFloatArray(xf, true).length).toBe(16)
})

test('transformDecomposeAffineFull', () => {
  // decomposeAffineFull gives the translation and the diagonal of the scale
  const [ok, t, , , diag] = rhino.Transform.decomposeAffineFull(rhino.Transform.scale({ X: 1, Y: 2, Z: 3 }, 2.0))
  expect(ok).toBe(true)
  expect(t).toEqual({ X: -1, Y: -2, Z: -3 })
  expect([diag.X, diag.Y, diag.Z].map(Math.abs)).toEqual([2, 2, 2])
})

test('transformMirrorReflectsAcrossAPlane', () => {
  // Transform.mirror(plane) reflects across the plane: across world XY it flips Z (D19)
  expect(transformRows(rhino.Transform.mirror(rhino.Plane.worldXY))).toEqual([1, -0, -0, 0, -0, 1, -0, 0, -0, -0, -1, 0])
})

test('transformRotationTurnsOneVectorOntoAnotherAboutAPoint', () => {
  // Transform.rotation(start, end, center) turns X onto Y about the center (D19)
  const P = (x, y, z) => new rhino.Point3d(x, y, z)
  const x = rhino.Transform.rotation(new rhino.Vector3d(1, 0, 0), new rhino.Vector3d(0, 1, 0), P(1, 2, 3))
  expect(transformRows(x)).toEqual([0, -1, 0, 3, 1, 0, 0, 1, 0, 0, 1, -0])
})

test('transformTranslationTakesAVectorOrXYZ', () => {
  // Transform.translation takes a vector or its three components, with the same result (D19)
  const expected = [1, 0, 0, 1, 0, 1, 0, 2, 0, 0, 1, 3]
  expect(transformRows(rhino.Transform.translation(new rhino.Vector3d(1, 2, 3)))).toEqual(expected)
  expect(transformRows(rhino.Transform.translation(1, 2, 3))).toEqual(expected)
})
