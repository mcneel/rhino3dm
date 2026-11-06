const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

const fs = require('fs')

const commonObjectJsonForms = () => JSON.parse(fs.readFileSync(helpers.model('old_oracle/json_forms.json'), 'utf8'))
const commonObjectFirstGeometry = rhino => [...helpers.readModel(rhino, 'file3dm_stuff.3dm').objects][0].geometry
const commonObjectBox = g => {
  const b = g.getBoundingBox(false)
  return [b.Min.X, b.Min.Y, b.Min.Z, b.Max.X, b.Max.Y, b.Max.Z].map(c => Math.round(c * 1e9) / 1e9)
}

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('commonObjectIsTheBaseOfGeometryAndAttributes', () => {
  // geometry, object attributes and CachedTextureCoordinates are CommonObjects
  expect(helpers.boxBrep(rhino)).toBeInstanceOf(rhino.CommonObject)
  expect(new rhino.ObjectAttributes()).toBeInstanceOf(rhino.CommonObject)
  expect(rhino.CachedTextureCoordinates.prototype instanceof rhino.CommonObject).toBe(true)
})

test('commonObjectMembersOfCachedTextureCoordinatesRaise', () => {
  // CachedTextureCoordinates wraps no ON_Object, so its CommonObject members throw; its own members work
  const mesh = helpers.quadMesh(rhino)
  const I = rhino.Interval
  const tm = rhino.TextureMapping.createPlaneMapping(rhino.Plane.worldXY, new I(0, 1), new I(0, 1), new I(0, 1))
  mesh.setCachedTextureCoordinates(tm, rhino.Transform.identity)
  const tc = mesh.getCachedTextureCoordinates(tm.id)
  expect(tc.count).toBe(4)
  for (const member of [() => tc.isValid, () => tc.isValidWithLog(), () => tc.hasUserData])
    helpers.raises(rhino, member, /not an ON_Object/)
})

test('commonObjectIsValidAndIsValidWithLog', () => {
  // isValid is true for a box and isValidWithLog returns [true, log text]
  const b = helpers.boxBrep(rhino)
  expect(b.isValid).toBe(true)
  const [ok, log] = b.isValidWithLog()
  expect(ok).toBe(true)
  expect(typeof log).toBe('string')
})

test('commonObjectComputeMemoryEstimateIsPositive', () => {
  // CommonObject.computeMemoryEstimate is positive for a brep
  expect(rhino.CommonObject.computeMemoryEstimate(helpers.boxBrep(rhino))).toBeGreaterThan(0)
})

test('commonObjectEncodeWritesThe8xJsonKeys', () => {
  // CommonObject.encode writes the 8.x JSON object's keys, and decode reads it back as the most derived class
  const forms = commonObjectJsonForms()
  const g = commonObjectFirstGeometry(rhino)
  const d = g.encode()
  const old = forms.common_object_encode
  expect(Object.keys(d).sort()).toEqual(['archive3dm', 'data', 'opennurbs', 'version'])
  expect(Object.keys(old).sort()).toEqual(Object.keys(d).sort())
  expect([d.archive3dm, d.version]).toEqual([old.archive3dm, old.version])
  expect(d.archive3dm).toBe(60)
  const back = rhino.CommonObject.decode(d)
  expect(g.constructor.name).toBe(forms.common_object_type)
  expect(back.constructor.name).toBe(g.constructor.name)
  expect(back.encode()).toEqual(d)
})

test('commonObjectDecodeReadsAn8xPayload', () => {
  // CommonObject.decode reads a payload 8.x's encode wrote: the same class, the same geometry
  const old = commonObjectJsonForms().common_object_encode
  const g = commonObjectFirstGeometry(rhino)
  const fromOld = rhino.CommonObject.decode(old)
  expect(fromOld.constructor.name).toBe(g.constructor.name)
  expect(commonObjectBox(fromOld)).toEqual(commonObjectBox(g))
})

test('commonObjectToJsonIsItsEncode', () => {
  // JSON.stringify of a CommonObject writes its encode(), with 8.x's keys
  const J = v => JSON.parse(JSON.stringify(v))
  const g = commonObjectFirstGeometry(rhino)
  const old = commonObjectJsonForms().common_object_encode
  expect(J(g)).toEqual(J(g.encode()))
  expect(Object.keys(J(g)).sort()).toEqual(Object.keys(old).sort())
  expect([J(g).archive3dm, J(g).version]).toEqual([old.archive3dm, old.version])
})
