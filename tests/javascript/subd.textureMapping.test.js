const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

// A SubD whose UVs come from a cylinder mapping on channel 1 (Rhino's projection mapping,
// not face texture points). The control net carries no texture coordinates of its own; the
// mapping is read from the file and applied to the control-net mesh.
// https://discourse.mcneel.com/t/rhino3dm-js-subds-uv-map-information/223518

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function subdObject(doc) {
    for (const obj of doc.objects)
        if (obj.geometry instanceof rhino.SubD) return obj
    return null
}

test('subdUVsComeFromTheMapping', async () => {
    const doc = helpers.readModel(rhino, 'subduv.3dm')
    const obj = subdObject(doc)
    expect(obj).not.toBeNull()
    const subd = obj.geometry
    expect(subd.textureCoordinateType).toBe(rhino.SubDTextureCoordinateType.FromMapping)

    // no face texture points: the control net alone has no UVs
    const net = rhino.Mesh.createFromSubDControlNetWithTextureCoordinates(subd)
    expect(net.textureCoordinates.count).toBe(0)
})

test('cylinderMappingAppliesToControlNetMesh', async () => {
    const doc = helpers.readModel(rhino, 'subduv.3dm')
    const obj = subdObject(doc)

    expect(obj.getTextureMapping(0)[0]).toBeNull()
    const [mapping, xform] = obj.getTextureMapping(1)
    expect(mapping).not.toBeNull()
    expect(mapping.mappingType).toBe(rhino.TextureMappingType.CylinderMapping)

    const mesh = rhino.Mesh.createFromSubDControlNet(obj.geometry)
    mesh.setTextureCoordinates(mapping, xform, false)
    const tc = mesh.textureCoordinates
    // the seam splits vertices, so the count is the mesh's after the call
    expect(tc.count).toBeGreaterThan(0)
    expect(tc.count).toBe(mesh.vertices.count)
    for (let i = 0; i < tc.count; i++) {
        const p = tc.get(i)
        expect(p.X).toBeGreaterThanOrEqual(0)
        expect(p.X).toBeLessThanOrEqual(1)
        expect(p.Y).toBeGreaterThanOrEqual(0)
        expect(p.Y).toBeLessThanOrEqual(1)
    }
})
