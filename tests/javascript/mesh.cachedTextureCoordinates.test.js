const rhino3dm = require('rhino3dm')
const fs = require('fs')
const path = require('path')

// RH3DM-170: headless (ONX_Model based) cached texture coordinates.
//   mesh.setCachedTextureCoordinatesFromMaterial(file, objectId, material)
//   mesh.getCachedTextureCoordinatesFromTexture(file, objectId, texture)
// The RhinoCommon overloads take a RhinoObject/RhinoDoc (RHINO_SDK only); the
// rhino3dm variants resolve the object's texture mappings from the File3dm.

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function makeMesh() {
    const mesh = new rhino.Mesh()
    mesh.vertices().add(0, 0, 0)
    mesh.vertices().add(10, 0, 0)
    mesh.vertices().add(10, 10, 0)
    mesh.vertices().add(0, 10, 0)
    mesh.faces().addQuadFace(0, 1, 2, 3)
    return mesh
}

function makeTexturedMaterial() {
    const material = new rhino.Material()
    const texture = new rhino.Texture()
    texture.fileName = 'fake_texture.png'
    material.setBitmapTexture(texture)
    return material
}

function fixturePath() {
    const candidate = path.join(__dirname, '..', 'models', 'meshWithTexture.3dm')
    return fs.existsSync(candidate) ? candidate : null
}

test('methods are callable and null-safe', async () => {
    const file3dm = new rhino.File3dm()
    const mesh = makeMesh()
    const material = makeTexturedMaterial()
    file3dm.materials().add(material)

    const attributes = new rhino.ObjectAttributes()
    attributes.materialSource = rhino.ObjectMaterialSource.MaterialFromObject
    const objectId = file3dm.objects().addMesh(mesh, attributes)

    // Should run without throwing regardless of whether coordinates get cached.
    const result = mesh.setCachedTextureCoordinatesFromMaterial(file3dm, objectId, material)
    expect(typeof result).toBe('boolean')

    const texture = material.getBitmapTexture()
    expect(texture).not.toBeNull()

    const tc = mesh.getCachedTextureCoordinatesFromTexture(file3dm, objectId, texture)
    // Without object-level texture mappings there is nothing to cache -> null.
    // If something was cached it must be a well-formed set over all vertices.
    if (tc !== null) {
        expect(tc.count).toBe(mesh.vertices().count)
        expect([2, 3]).toContain(tc.dimension)
    }
})

test('get with unrelated texture returns null', async () => {
    const file3dm = new rhino.File3dm()
    const mesh = makeMesh()
    const objectId = file3dm.objects().addMesh(mesh, null)

    const unrelated = new rhino.Texture()
    unrelated.fileName = 'unrelated.png'

    const tc = mesh.getCachedTextureCoordinatesFromTexture(file3dm, objectId, unrelated)
    expect(tc).toBeNull()
})

test('positive path from fixture', async () => {
    const fixture = fixturePath()
    if (fixture === null) {
        console.warn('meshWithTexture.3dm fixture not present - skipping positive path')
        return
    }

    const buffer = fs.readFileSync(fixture)
    const file3dm = rhino.File3dm.fromByteArray(new Uint8Array(buffer))

    let found = false
    const objects = file3dm.objects()
    for (let i = 0; i < objects.count; i++) {
        const obj = objects.get(i)
        const geometry = obj.geometry()
        if (!(geometry instanceof rhino.Mesh)) continue

        const material = file3dm.materials().findIndex(obj.attributes().materialIndex)
        if (material === null) continue
        const texture = material.getBitmapTexture()
        if (texture === null) continue

        const objectId = obj.attributes().id
        geometry.setCachedTextureCoordinatesFromMaterial(file3dm, objectId, material)
        const tc = geometry.getCachedTextureCoordinatesFromTexture(file3dm, objectId, texture)
        if (tc !== null) {
            found = true
            expect(tc.count).toBe(geometry.vertices().count)
            expect([2, 3]).toContain(tc.dimension)
        }
    }

    expect(found).toBe(true)
})
