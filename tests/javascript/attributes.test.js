const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs')

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})

test('attributesGroupList', async () => { 

    // read model 
    const model = '../models/groups.3dm'

    const buffer = fs.readFileSync(model)
    const arr = new Uint8Array(buffer)
    const doc = rhino.File3dm.fromByteArray(arr)

    expect(doc !== null).toBe(true)

    // D17: no index on the object table; iterate
    const obj = [...doc.objects][0]

    groupList = obj.attributes.getGroupList()

    expect(Array.isArray(groupList)).toBe(true)
    expect(typeof groupList[0] === "number").toBe(true)

})

const attributesRgb = c => [c.r, c.g, c.b]

function attributesDrawColorFile(rhino) {
  const f = new rhino.File3dm()
  for (const c of [{ r: 200, g: 0, b: 0, a: 255 }, { r: 0, g: 0, b: 200, a: 255 }]) {
    const m = new rhino.Material()
    m.diffuseColor = c
    f.materials.add(m)
  }
  const layer = new rhino.Layer()
  layer.name = 'L'
  layer.color = { r: 10, g: 20, b: 30, a: 255 }
  layer.renderMaterialIndex = 1
  f.layers.add(layer)
  const pbr = new rhino.Material()
  pbr.diffuseColor = { r: 9, g: 9, b: 9, a: 255 }
  pbr.toPhysicallyBased()
  pbr.physicallyBased.baseColor = new rhino.Color4f(1.0, 0.5, 0.0, 1.0)
  f.materials.add(pbr)
  return f
}

const attributesUuid = () => require('crypto').randomUUID()

const crypto = require('crypto')

test('attributesMeshModifiersAreNoneWhenAbsent', () => {
  // a new ObjectAttributes has no displacement, edge softening, thickening, curve piping or shut lining
  const mm = new rhino.ObjectAttributes().file3dmMeshModifiers
  expect(mm.displacement).toBeNull()
  expect(mm.edgeSoftening).toBeNull()
  expect(mm.thickening).toBeNull()
  expect(mm.curvePiping).toBeNull()
  expect(mm.shutLining).toBeNull()
})

test('attributesMeshModifiersOfObjectsReadFromFiles', () => {
  // the mesh modifiers of objects read from files are reachable, and a present displacement reads back
  for (const name of ['subd.3dm', 'subd_creases.3dm', 'mesh.3dm']) {
    const f = helpers.readModel(rhino, name)
    for (const o of f.objects) {
      const d = o.attributes.file3dmMeshModifiers.displacement
      if (d !== null) expect(typeof d.on).toBe('boolean')
    }
  }
})

test('attributesCustomMeshingParametersIsNoneUntilSet', () => {
  // customMeshingParameters is null on new attributes and returns the parameters once set
  const attrs = new rhino.ObjectAttributes()
  expect(attrs.customMeshingParameters).toBeNull()
  const quality = rhino.MeshingParameters.qualityRenderMesh
  attrs.customMeshingParameters = quality
  const got = attrs.customMeshingParameters
  expect(got).not.toBeNull()
  expect(got.relativeTolerance).toBe(quality.relativeTolerance)
})

test('attributesGetGroupListIsNoneWithoutGroups', () => {
  // getGroupList returns null when the object is in no group
  expect(new rhino.ObjectAttributes().getGroupList()).toBeNull()
})

test('attributesUserStringsSetAndGet', () => {
  // setUserString stores a key/value that getUserString and userStringCount see
  const a = new rhino.ObjectAttributes()
  expect(a.setUserString('k', 'v')).toBe(true)
  expect(a.getUserString('k')).toBe('v')
  expect(a.userStringCount).toBe(1)
})

test('objectAttributesMeshModifiersCreateThickening', () => {
  // file3dmMeshModifiers.thickening is null until createThickening makes one
  const mm = new rhino.ObjectAttributes().file3dmMeshModifiers
  expect(mm.thickening == null).toBe(true)
  expect(mm.createThickening() != null).toBe(true)
  expect(mm.thickening != null).toBe(true)
})

test('shutLiningCurvesSurviveAWriteAndRead', () => {
  // a shut lining's curves and their settings, keyed by curve id, read back from the file's bytes
  const cid = require('crypto').randomUUID()
  const wf = new rhino.File3dm()
  const attrs = new rhino.ObjectAttributes()
  const sl0 = attrs.file3dmMeshModifiers.createShutLining()
  expect(sl0.addCurve(cid)).toBe(cid)
  sl0.setCurveRadius(cid, 2.5)
  sl0.setCurveProfile(cid, 3)
  sl0.setCurvePull(cid, true)
  sl0.setCurveIsBump(cid, true)
  sl0.setCurveEnabled(cid, false)
  wf.objects.addPoint({ X: 0, Y: 0, Z: 0 }, attrs)
  const f = rhino.File3dm.fromByteArray(wf.toByteArray())
  const sl = [...f.objects][0].attributes.file3dmMeshModifiers.shutLining
  expect(sl != null).toBe(true)
  expect([...sl.getCurves()].map(String)).toEqual([String(cid)])
  expect([sl.curveRadius(cid), sl.curveProfile(cid), sl.curvePull(cid), sl.curveIsBump(cid), sl.curveEnabled(cid)])
    .toEqual([2.5, 3, true, true, false])
})

test('objectAttributesDrawColorByLayerAndObject', () => {
  // drawColor(file) is the layer's color when colored by layer and objectColor when colored by object
  const f = attributesDrawColorFile(rhino)
  const a = new rhino.ObjectAttributes()
  a.layerIndex = 0
  a.colorSource = rhino.ObjectColorSource.ColorFromLayer
  expect(attributesRgb(a.drawColor(f))).toEqual(attributesRgb(f.layers.findIndex(0).color))
  expect(attributesRgb(a.drawColor(f))).toEqual([10, 20, 30])
  a.colorSource = rhino.ObjectColorSource.ColorFromObject
  a.objectColor = { r: 1, g: 2, b: 3, a: 255 }
  expect(attributesRgb(a.drawColor(f))).toEqual([1, 2, 3])
})

test('objectAttributesDrawColorByMaterial', () => {
  // colored by material, drawColor(file) is the selected material's diffuse color, or its PBR base color (D63)
  const f = attributesDrawColorFile(rhino)
  const a = new rhino.ObjectAttributes()
  a.layerIndex = 0
  a.colorSource = rhino.ObjectColorSource.ColorFromMaterial
  a.materialSource = rhino.ObjectMaterialSource.MaterialFromObject
  a.materialIndex = 0
  expect(attributesRgb(a.drawColor(f))).toEqual([200, 0, 0])
  a.materialSource = rhino.ObjectMaterialSource.MaterialFromLayer      // the layer's render material (1)
  expect(attributesRgb(a.drawColor(f))).toEqual([0, 0, 200])
  a.materialSource = rhino.ObjectMaterialSource.MaterialFromObject
  a.materialIndex = 2                                                  // physically based
  expect([[255, 127, 0], [255, 128, 0]]).toContainEqual(attributesRgb(a.drawColor(f)))
})

test('objectAttributesDrawColorWithEmptyViewportId', () => {
  // drawColor(file, the empty id) is drawColor(file)
  const f = attributesDrawColorFile(rhino)
  const a = new rhino.ObjectAttributes()
  a.colorSource = rhino.ObjectColorSource.ColorFromMaterial
  a.materialSource = rhino.ObjectMaterialSource.MaterialFromObject
  a.materialIndex = 2
  expect(a.drawColor(f, '00000000-0000-0000-0000-000000000000')).toEqual(a.drawColor(f))
})

test('shutLiningCurveSettingsById', () => {
  // A shut lining's curves are listed by id, and each curve's radius, profile, enabled, pull and bump settings read and write by id
  const a = new rhino.ObjectAttributes()
  const created = a.file3dmMeshModifiers.createShutLining()
  created.on = true
  const cid = attributesUuid()
  expect(created.addCurve(cid)).toBe(cid)
  const sl = a.file3dmMeshModifiers.shutLining
  expect(sl.on).toBe(true)
  expect([...sl.getCurves()]).toEqual([cid])
  sl.setCurveRadius(cid, 0.75); sl.setCurveProfile(cid, 2); sl.setCurveEnabled(cid, true)
  sl.setCurvePull(cid, true); sl.setCurveIsBump(cid, true)
  expect([sl.curveRadius(cid), sl.curveProfile(cid), sl.curveEnabled(cid), sl.curvePull(cid), sl.curveIsBump(cid)])
    .toEqual([0.75, 2, true, true, true])
  sl.setCurveRadius(cid, 1.25)
  expect(sl.curveRadius(cid)).toBe(1.25)
})

test('meshModifiersCreateKeepsAnExistingOne', () => {
  // Each file3dmMeshModifiers.create* makes the modifier when there is none and returns the existing one otherwise
  const mm = new rhino.ObjectAttributes().file3dmMeshModifiers
  expect(mm.shutLining == null).toBe(true)                // none until created
  for (const n of ['Displacement', 'EdgeSoftening', 'Thickening', 'CurvePiping', 'ShutLining']) {
    const m = mm['create' + n]()
    m.on = true
    expect([n, mm['create' + n]().on]).toEqual([n, true])
  }
})

test('shutLiningAddCurveOncePerId', () => {
  // File3dmShutLining.addCurve adds one curve per id, each with its own settings
  const sl = new rhino.ObjectAttributes().file3dmMeshModifiers.createShutLining()
  const a = attributesUuid(), b = attributesUuid()
  expect(sl.addCurve(a)).toBe(a)
  expect(sl.addCurve(b)).toBe(b)
  expect(sl.addCurve(a)).toBe(a)                          // no second curve for an id
  expect([...sl.getCurves()]).toEqual([a, b])
  sl.setCurveRadius(a, 2.0)
  expect([sl.curveRadius(a), sl.curveRadius(b)]).toEqual([2.0, 1.0])
})

test('objectAttributesRdkXmlIsTheObjectsRenderXml', () => {
  // ObjectAttributes.rdkXml is the object's RDK XML (here its decals)
  const na = [...helpers.readModel(rhino, 'sphereDecals.3dm').objects][0].attributes
  expect(na.rdkXml().length).toBeGreaterThan(0)
  expect(crypto.createHash('sha256').update(na.rdkXml()).digest('hex'))
    .toBe('be505faefb52c2faacf3dbf1b766542bbdb034017bd6f349edb0e8e326f200b8')
})
