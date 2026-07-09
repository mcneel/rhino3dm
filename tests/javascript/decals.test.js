const rhino3dm = require('rhino3dm')

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function makeDecal(rhino, transparency = 0.25) {
    const d = new rhino.Decal()
    d.mapping = rhino.Mappings.Planar
    d.projection = rhino.Projections.Forward
    d.origin = [1, 2, 3]
    d.transparency = transparency
    d.height = 4.0
    d.radius = 2.0
    return d
}

// RH3DM-159: decal table holds shared_ptr<ON_Decal>; exercise the read path + wrapper lifetime.
// The table is read-only by design — opennurbs only reflects decals committed to RDK user data and
// GetDecalArray() repopulates from it every call (RH-86089), so in-memory authoring doesn't
// round-trip (tracked: RH3DM-159 / RH3DM-194). Reading a Rhino-authored .3dm works.
test('decalProperties', async () => {
    const d = makeDecal(rhino, 0.4)
    expect(d.projection).toBe(rhino.Projections.Forward)
    expect(d.origin[0]).toBeCloseTo(1.0, 5)
    expect(d.transparency).toBeCloseTo(0.4, 5)
    expect(d.height).toBeCloseTo(4.0, 5)
    expect(d.radius).toBeCloseTo(2.0, 5)
})

test('emptyTableIsCrashSafe', async () => {
    // repeated count/get (which rebuild the decal-array cache) must never dangle/crash —
    // the path RH3DM-159 made memory-safe via shared ownership.
    const attr = new rhino.ObjectAttributes()
    expect(attr.decals().count).toBe(0)
    for (let n = 0; n < 50; n++) {
        expect(attr.decals().count).toBe(0)
        const d = attr.decals().get(5) // out of range
        expect(d).toBeNull()
    }
})

const fs = require('fs')

test('readDecalsFromFile', async () => {
    const buffer = fs.readFileSync('../models/sphereDecals.3dm')
    const model = rhino.File3dm.fromByteArray(new Uint8Array(buffer))
    expect(model.objects().count).toBe(1)
    const decals = model.objects().get(0).attributes().decals()
    expect(decals.count).toBe(2)
    const ids = new Set([decals.get(0).textureInstanceId, decals.get(1).textureInstanceId])
    expect(ids.size).toBe(2)
})

test('readDecalLifetime_RH3DM_159', async () => {
    const buffer = fs.readFileSync('../models/sphereDecals.3dm')
    const model = rhino.File3dm.fromByteArray(new Uint8Array(buffer))
    const decals = model.objects().get(0).attributes().decals()
    const held = [decals.get(0), decals.get(1)]
    const expected = [held[0].textureInstanceId, held[1].textureInstanceId]

    for (let n = 0; n < 50; n++) {
        decals.get(0); decals.get(1)
    }

    expect([held[0].textureInstanceId, held[1].textureInstanceId]).toEqual(expected)
})
