const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs')
const path = require('path')

// RH3DM-170: headless (ONX_Model based) cached texture coordinates.
//   mesh.setCachedTextureCoordinatesFromMaterial(file, objectId, material)
//   mesh.getCachedTextureCoordinates(file, objectId, texture)
// The RhinoCommon overloads take a RhinoObject/RhinoDoc (RHINO_SDK only); the
// rhino3dm variants resolve the object's texture mappings from the File3dm.

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function makeMesh() {
    const mesh = new rhino.Mesh()
    mesh.vertices.add(0, 0, 0)
    mesh.vertices.add(10, 0, 0)
    mesh.vertices.add(10, 10, 0)
    mesh.vertices.add(0, 10, 0)
    mesh.faces.addFace(0, 1, 2, 3)
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
    file3dm.materials.add(material)

    const attributes = new rhino.ObjectAttributes()
    attributes.materialSource = rhino.ObjectMaterialSource.MaterialFromObject
    const objectId = file3dm.objects.addMesh(mesh, attributes)

    // Should run without throwing regardless of whether coordinates get cached.
    // D65: the setter returns nothing, as RhinoCommon's
    const result = mesh.setCachedTextureCoordinatesFromMaterial(file3dm, objectId, material)
    expect(result).toBeUndefined()

    const texture = material.getBitmapTexture()
    expect(texture).not.toBeNull()

    const tc = mesh.getCachedTextureCoordinates(file3dm, objectId, texture)
    // Without object-level texture mappings there is nothing to cache -> null.
    // If something was cached it must be a well-formed set over all vertices.
    if (tc !== null) {
        expect(tc.count).toBe(mesh.vertices.count)
        expect([2, 3]).toContain(tc.dim) // dimension -> dim
    }
})

test('get with unrelated texture returns null', async () => {
    const file3dm = new rhino.File3dm()
    const mesh = makeMesh()
    const objectId = file3dm.objects.addMesh(mesh, null)

    const unrelated = new rhino.Texture()
    unrelated.fileName = 'unrelated.png'

    const tc = mesh.getCachedTextureCoordinates(file3dm, objectId, unrelated)
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
    // D17: iterate the object table
    for (const obj of file3dm.objects) {
        const geometry = obj.geometry
        if (!(geometry instanceof rhino.Mesh)) continue

        const material = file3dm.materials.findIndex(obj.attributes.materialIndex)
        if (material === null) continue
        const texture = material.getBitmapTexture()
        if (texture === null) continue

        const objectId = obj.attributes.objectId
        geometry.setCachedTextureCoordinatesFromMaterial(file3dm, objectId, material)
        const tc = geometry.getCachedTextureCoordinates(file3dm, objectId, texture)
        if (tc !== null) {
            found = true
            expect(tc.count).toBe(geometry.vertices.count)
            expect([2, 3]).toContain(tc.dim) // dimension -> dim
        }
    }

    expect(found).toBe(true)
})

const crypto = require('crypto')

test('cachedTextureCoordinatesAreAReadOnlyViewIntoTheMesh', () => {
  // getCachedTextureCoordinates returns null for an unknown mapping id, and a read-only view for a cached one (D27)
  const f = helpers.readModel(rhino, 'meshWithTexture.3dm')
  const objs = [...f.objects]
  const meshes = objs.map(o => o.geometry).filter(g => g instanceof rhino.Mesh)
  expect(meshes.length).toBeGreaterThan(0)
  expect(meshes[0].getCachedTextureCoordinates(crypto.randomUUID())).toBeNull()
  // a cache, when the file carries one under one of its ids
  const ids = new Set(objs.map(o => o.attributes.objectId))
  for (const m of meshes) {
    for (const mid of ids) {
      const tc = m.getCachedTextureCoordinates(mid)
      if (tc !== null) {
        expect(tc.isReadOnly).toBe(true)
        expect(tc.count).toBeGreaterThanOrEqual(0)
        expect([2, 3]).toContain(tc.dim)
        expect(tc.tryGetAt(-1)).toEqual([false, -1.0, -1.0, -1.0])   // RhinoCommon's -1 defaults
        return
      }
    }
  }
})
