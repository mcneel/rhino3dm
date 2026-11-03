const rhino3dm = require('rhino3dm')

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
