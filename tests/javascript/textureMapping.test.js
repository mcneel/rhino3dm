const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

function textureMappingAll(rhino) {
  const I = { T0: 0, T1: 1 }
  const TM = rhino.TextureMapping
  return [TM.createSurfaceParameterMapping(),
    TM.createPlaneMapping(rhino.Plane.worldXY, I, I, I),
    TM.createCylinderMapping(new rhino.Cylinder(new rhino.Circle(1.0), 2.0), true),
    TM.createCylinderMapping(new rhino.Cylinder(new rhino.Circle(1.0), 2.0), false),
    TM.createSphereMapping(new rhino.Sphere({ X: 0, Y: 0, Z: 0 }, 2.0)),
    TM.createBoxMapping(rhino.Plane.worldXY, I, I, I, true)]
}

function textureMappingMatrix(m) {
  const a = []
  for (let i = 0; i < 4; i++) for (let j = 0; j < 4; j++) a.push(m['M' + i + j])
  return a
}

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('textureMappingFactoriesReturnNoneOnFailure', () => {
  // TextureMapping's create* factories return a mapping, or null when it cannot be made
  const i = new rhino.Interval(0, 1)
  const tm = rhino.TextureMapping.createPlaneMapping(rhino.Plane.worldXY, i, i, i, true)
  expect(tm instanceof rhino.TextureMapping).toBe(true)
  expect(tm.capped).toBe(true)
  expect(rhino.TextureMapping.createPlaneMapping(rhino.Plane.worldXY, i, i, i) instanceof rhino.TextureMapping).toBe(true)
  expect(rhino.TextureMapping.createPlaneMapping(rhino.Plane.unset, i, i, i, false)).toBeNull()   // origin unset
  expect(rhino.TextureMapping.createSurfaceParameterMapping() instanceof rhino.TextureMapping).toBe(true)
  expect(rhino.TextureMapping.createSphereMapping(new rhino.Sphere(new rhino.Point3d(0, 0, 0), 2)) instanceof rhino.TextureMapping).toBe(true)
})

test('textureMappingEvaluateReturnsTheResultAndPoint', () => {
  // TextureMapping.evaluate(p, n) returns [result, texture coordinate]
  const tm = rhino.TextureMapping.createSurfaceParameterMapping()
  const res = tm.evaluate(new rhino.Point3d(0.25, 0.5, 0), new rhino.Vector3d(0, 0, 1))
  expect(res.length).toBe(2)
  expect(Number.isInteger(res[0])).toBe(true)
})

test('textureMappingIdIsAUuid', () => {
  // a texture mapping's id is a UUID string
  expect(rhino.TextureMapping.createSurfaceParameterMapping().id).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
})

test('textureMappingInheritsModelComponentMembers', () => {
  // a plane TextureMapping has ModelComponent's hasId
  const I = (a, b) => new rhino.Interval(a, b)
  const tm = rhino.TextureMapping.createPlaneMapping(rhino.Plane.worldXY, I(0, 1), I(0, 1), I(0, 1))
  expect(typeof tm.hasId).toBe('boolean')
})

test('textureMappingTypeOfEachCreate', () => {
  // mappingType is the kind of mapping each create method makes
  const T = rhino.TextureMappingType
  expect(textureMappingAll(rhino).map(n => n.mappingType.value))
    .toEqual([T.SurfaceParameters, T.PlaneMapping, T.CylinderMapping, T.CylinderMapping, T.SphereMapping, T.BoxMapping].map(k => k.value))
})

test('textureMappingPeriodicAndVertexNormalsFromItsProperties', () => {
  // sphere and cylinder mappings are periodic; ray projection, box and capped cylinder mappings need vertex normals (D49)
  const T = rhino.TextureMappingType
  // [periodic, requires vertex normals] of each mapping in textureMappingAll
  const expected = [[false, false], [false, false], [true, true], [true, false], [true, false], [false, true]]
  textureMappingAll(rhino).forEach((n, i) => {
    const k = n.mappingType
    expect(k === T.SphereMapping || k === T.CylinderMapping).toBe(expected[i][0])
    const derived = k !== T.SurfaceParameters && (n.projection === rhino.Projection.Ray || k === T.BoxMapping
      || (k === T.CylinderMapping && n.capped))
    expect(derived).toBe(expected[i][1])
  })
})

test('textureMappingUvwTransformComposes', () => {
  // left-multiplying uvwTransform reverses, swaps or tiles a plane mapping's texture coordinates
  const I = { T0: 0, T1: 1 }
  for (const [name, want, build] of [
    ['reverse', [1, 0, 0, 0, 0, -1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1], x => { x.M11 = -1.0; x.M13 = 1.0 }],
    ['swap', [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1], x => { x.M00 = 0.0; x.M22 = 0.0; x.M02 = 1.0; x.M20 = 1.0 }],
    ['tile', [3, 0, 0, 0.25, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1], x => { x.M00 = 3.0; x.M03 = 0.25 }]]) {
    const x = new rhino.Transform(1.0)
    build(x)
    const n = rhino.TextureMapping.createPlaneMapping(rhino.Plane.worldXY, I, I, I)
    n.uvwTransform = rhino.Transform.multiply(x, n.uvwTransform)
    expect([name, textureMappingMatrix(n.uvwTransform)]).toEqual([name, want])
  }
})

test('textureMappingUserStrings', () => {
  // A TextureMapping keeps user strings: set, get, count and list them (D62)
  const I = (a, b) => new rhino.Interval(a, b)
  const o = rhino.TextureMapping.createPlaneMapping(rhino.Plane.worldXY, I(0, 1), I(0, 1), I(0, 1))
  expect(o.userStringCount).toBe(0)
  expect(o.setUserString('k', 'v')).toBe(true)
  expect(o.getUserString('k')).toBe('v')
  expect(Array.from(o.getUserStrings(), p => Array.from(p))).toEqual([['k', 'v']])
})

test('textureMappingTryGetMappingCapped', () => {
  // tryGetMappingBoxCapped / CylinderCapped / PlaneCapped add the capped flag to tryGetMapping*'s results
  const I = (T0, T1) => ({ T0, T1 })
  const xy = rhino.Plane.worldXY
  const box = rhino.TextureMapping.createBoxMapping(xy, I(0, 1), I(0, 2), I(0, 3), true)
  const res = box.tryGetMappingBoxCapped()
  expect(res.length).toBe(6)
  expect([res[0], res[5]]).toEqual([true, true])
  expect(box.tryGetMappingBox().length).toBe(5)
  const pl = rhino.TextureMapping.createPlaneMapping(xy, I(0, 1), I(0, 2), I(0, 3), false)
  expect(pl.tryGetMappingPlaneCapped()[5]).toBe(false)
  const cyl = rhino.TextureMapping.createCylinderMapping(new rhino.Cylinder(new rhino.Circle(xy, 1.0), 2.0), true)
  const [ok, , cap] = cyl.tryGetMappingCylinderCapped()
  expect([ok, cap]).toEqual([true, true])
})
