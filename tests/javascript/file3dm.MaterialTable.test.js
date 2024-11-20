const rhino3dm = require('rhino3dm')

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
  material1.physicallyBased().baseColor = { r: 1, g: 0, b: 0, a: 0 }
  material1.physicallyBased().metallic = 0.7
  material1.physicallyBased().roughness = 0.5

  const material2 = new rhino.Material()
  material2.toPhysicallyBased()
  material2.physicallyBased().baseColor = { r: 1, g: 0, b: 0, a: 0 }
  material2.physicallyBased().metallic = 0.7
  material2.physicallyBased().roughness = 0.5

  const index1 = file3dm.materials().add(material1)
  const index2 = file3dm.materials().add(material2)

  const qtyMaterials1 = file3dm.materials().count

  const id1 = file3dm.materials().get(index1).id

  file3dm.materials().delete(id1)

  const qtyMaterials2 = file3dm.materials().count

  expect(qtyMaterials1 === 2 && qtyMaterials2 === 1).toBe(true)

})