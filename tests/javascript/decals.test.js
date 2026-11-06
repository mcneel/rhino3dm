const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

// RH3DM-159: decal table holds shared_ptr<ON_Decal>; exercise the read path + wrapper lifetime.
// The table is read-only by design — opennurbs only reflects decals committed to RDK user data and
// GetDecalArray() repopulates from it every call (RH-86089), so in-memory authoring doesn't
// round-trip (tracked: RH3DM-159 / RH3DM-194). Reading a Rhino-authored .3dm works.
// D31: RhinoCommon's Decals is iteration only, each item its own copy; there is no Decal()
// constructor (Decal.Create / Decals.Add are not generated yet), so 'decalProperties' is gone.

test('emptyTableIsCrashSafe', async () => {
    // repeated count/get (which rebuild the decal-array cache) must never dangle/crash —
    // the path RH3DM-159 made memory-safe via shared ownership.
    // D31: iteration only (no count / get)
    const attr = new rhino.ObjectAttributes()
    expect([...attr.decals]).toEqual([])
    for (let n = 0; n < 50; n++) {
        expect([...attr.decals].length).toBe(0)
    }
})

const fs = require('fs')

test('readDecalsFromFile', async () => {
    const buffer = fs.readFileSync('../models/sphereDecals.3dm')
    const model = rhino.File3dm.fromByteArray(new Uint8Array(buffer))
    expect(model.objects.count).toBe(1)
    // D17 / D31: iterate the object table and the decals
    const decals = [...[...model.objects][0].attributes.decals]
    expect(decals.length).toBe(2)
    const ids = new Set([decals[0].textureInstanceId, decals[1].textureInstanceId])
    expect(ids.size).toBe(2)
})

test('readDecalLifetime_RH3DM_159', async () => {
    const buffer = fs.readFileSync('../models/sphereDecals.3dm')
    const model = rhino.File3dm.fromByteArray(new Uint8Array(buffer))
    // D17 / D31: iterate the object table and the decals
    const decals = [...model.objects][0].attributes.decals
    const held = [...decals]
    const expected = [held[0].textureInstanceId, held[1].textureInstanceId]

    for (let n = 0; n < 50; n++) {
        [...decals]
    }

    expect([held[0].textureInstanceId, held[1].textureInstanceId]).toEqual(expected)
})

test('decalsIterateCopiesThatOutliveTheFile', () => {
  // iterating decals yields copies of the decals that stay valid after the file is gone
  const f = helpers.readModel(rhino, 'sphereDecals.3dm')
  const objs = [...f.objects]
  const decals = [...objs[0].attributes.decals]
  expect(decals.length).toBe(2)
  for (const d of decals) expect(d.mapping).toBe(rhino.DecalMapping.UV)
  const ids = decals.map(d => d.textureInstanceId)
  expect(new Set(ids).size).toBe(2)
  // the file and its objects are deleted explicitly
  for (const o of objs) o.delete()
  f.delete()
  expect(decals.map(d => d.textureInstanceId)).toEqual(ids)
})

test('decalsOfNewAttributesAreEmptyAndWritable', () => {
  // new attributes have no decals; removeAllDecals and clear work, and remove(null) returns false
  const a = new rhino.ObjectAttributes()
  expect([...a.decals]).toEqual([])
  a.decals.removeAllDecals()
  a.decals.clear()
  expect(a.decals.remove(null)).toBe(false)
})

test('decalSweepsAndUVBounds', () => {
  // A decal's horzSweep, vertSweep and getUVBounds read the values stored in the file
  const decals = []
  for (const o of helpers.readModel(rhino, 'sphereDecals.3dm').objects) decals.push(...o.attributes.decals)
  const expected = [[[0, 0], [0, 0], [0.6, 0.6, 0.7, 0.7]],
                    [[0, 0], [0, 0], [0.2, 0.2, 0.300000001490116, 0.300000001490116]]]
  expect(decals.length).toBe(expected.length)
  expected.forEach(([horz, vert, bounds], k) => {
    const d = decals[k]
    expect(Array.from(d.horzSweep())).toEqual(horz)
    expect(Array.from(d.vertSweep())).toEqual(vert)
    expect(Array.from(d.getUVBounds())).toEqual(bounds)
  })
})

test('objectAttributesDecalsListsTheVisibleDecals', () => {
  // iterating an object's attributes.decals yields its visible decals, in order (D74)
  const f = helpers.readModel(rhino, 'sphereDecals.3dm')
  const decals = [...[...f.objects][0].attributes.decals]
  expect(decals.map(d => String(d.textureInstanceId)))
    .toEqual(['84eda676-69e1-4fa5-b81e-412676e88b31', '7339f997-0433-4591-811f-3cf66b6d41fe'])
})
