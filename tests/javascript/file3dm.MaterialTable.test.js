const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })
//TODO

test('DeleteMaterial', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-materialTable-deleteMaterial'
  file3dm.applicationUrl = 'https://rhino3d.com'

  const material1 = new rhino.Material()
  material1.toPhysicallyBased()
  // D34: baseColor is a Color4f {R, G, B, A}
  material1.physicallyBased.baseColor = { R: 1, G: 0, B: 0, A: 0 }
  material1.physicallyBased.metallic = 0.7
  material1.physicallyBased.roughness = 0.5

  const material2 = new rhino.Material()
  material2.toPhysicallyBased()
  material2.physicallyBased.baseColor = { R: 1, G: 0, B: 0, A: 0 }
  material2.physicallyBased.metallic = 0.7
  material2.physicallyBased.roughness = 0.5

  // add returns nothing; read the item back
  file3dm.materials.add(material1)
  file3dm.materials.add(material2)

  const qtyMaterials1 = file3dm.materials.count

  const item1 = file3dm.materials.get(0)

  // D14: delete(item), the id form is gone
  file3dm.materials.delete(item1)

  const qtyMaterials2 = file3dm.materials.count

  expect(qtyMaterials1 === 2 && qtyMaterials2 === 1).toBe(true)

})

test('materialTableFindIdSkipsDeletedMaterials', () => {
  // materials.findId finds a material by id, and returns null once it is deleted
  const f = new rhino.File3dm()
  f.materials.add(new rhino.Material())
  const mi = f.materials.count - 1
  const mid = f.materials.findIndex(mi).id
  expect(f.materials.findId(mid)).not.toBeNull()
  f.materials.delete(mi)
  expect(f.materials.findId(mid)).toBeNull()
})

test('materialTableFindFromAttributes', () => {
  // findFromAttributes resolves an object's material: the default material by default, a model material through the object
  const f = new rhino.File3dm()
  const a = new rhino.ObjectAttributes()
  const m = f.allMaterials.findFromAttributes(a)
  expect(m).toBeInstanceOf(rhino.Material)
  expect(String(m.id).toLowerCase()).toBe('cf2be1de-b81c-4d69-9418-dde91d266086')   // ON_Material::Default
  const mat = new rhino.Material()
  mat.name = 'red'
  const mi = f.allMaterials.addMaterial(mat)
  a.materialSource = rhino.ObjectMaterialSource.MaterialFromObject
  a.materialIndex = mi
  expect(f.allMaterials.findFromAttributes(a).name).toBe('red')
})
