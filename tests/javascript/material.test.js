const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

// Regression guard for the SetTextureHelper bug where the Texture-object setters
// forced every texture to the Bitmap type instead of their own channel.
test('setBumpTextureKeepsBumpType', async () => {

  const material = new rhino.Material()
  const texture = new rhino.Texture()
  texture.fileName = 'bump.png'
  material.setBumpTexture(texture)

  const bump = material.getBumpTexture()
  const bitmap = material.getBitmapTexture()

  expect(bump).not.toBeNull()
  expect(bump.textureType).toBe(rhino.TextureType.Bump)
  // must not have leaked in as a Bitmap texture
  expect(bitmap).toBeNull()

  if (bump) bump.delete()
})

// The generic setTexture assigns using the texture's own TextureType, so any
// channel can be set - here a PBR roughness map.
test('setTextureHonorsTextureType', async () => {

  const material = new rhino.Material()
  const texture = new rhino.Texture()
  texture.fileName = 'roughness.png'
  // D55: enum arguments take the enum object
  texture.textureType = rhino.TextureType.PBR_Roughness
  // setTexture(texture) -> setTexture(texture, texture.textureType)
  material.setTexture(texture, texture.textureType)

  const roughness = material.getTexture(rhino.TextureType.PBR_Roughness)

  expect(roughness).not.toBeNull()
  expect(roughness.textureType).toBe(rhino.TextureType.PBR_Roughness)

  if (roughness) roughness.delete()
})

test('materialPhysicallyBasedIsNoneUntilConverted', () => {
  // Material.physicallyBased is null until toPhysicallyBased(), then a view onto the material
  const m = new rhino.Material()
  expect(m.physicallyBased).toBeNull()
  m.toPhysicallyBased()
  const pbr = m.physicallyBased
  pbr.metallic = 0.25
  expect(m.physicallyBased.metallic).toBe(0.25)
})

test('materialMaxShineIs255', () => {
  // Material.maxShine is 255
  expect(rhino.Material.maxShine).toBe(255.0)
})

test('materialBitmapTextureIsACopyOnWriteView', () => {
  // getBitmapTexture returns the material's texture; changing it does not change the material until setBitmapTexture
  const m = new rhino.Material()
  expect(m.getBitmapTexture()).toBeNull()
  const t = new rhino.Texture()
  t.fileName = 'a.png'
  expect(m.setBitmapTexture(t)).toBeTruthy()
  const v = m.getBitmapTexture()
  expect(v.fileName).toBe('a.png')
  v.fileName = 'c.png'
  expect(v.fileName).toBe('c.png')
  expect(m.getBitmapTexture().fileName).toBe('a.png')
  // to change the material's texture, set it back, as in RhinoCommon
  expect(m.setBitmapTexture(v)).toBeTruthy()
  expect(m.getBitmapTexture().fileName).toBe('c.png')
})

test('materialPhysicallyBasedBaseColorIsAColor4f', () => {
  // a physically based material's baseColor reads back the Color4f it was set to
  const c = new rhino.Color4f(0.25, 0.5, 0.75, 1.0)
  const m = new rhino.Material()
  m.toPhysicallyBased()
  const pbr = m.physicallyBased
  pbr.baseColor = c
  expect(pbr.baseColor).toEqual(c)
})

test('materialToPhysicallyBasedEmission', () => {
  // toPhysicallyBased makes a material physically based
  const m = new rhino.Material()
  m.toPhysicallyBased()
  expect(m.isPhysicallyBased).toBe(true)
})

test('materialSetTextureByType', () => {
  // setTexture(texture, type) sets the texture, and getTexture(type) reads it back
  const m = new rhino.Material(), t = new rhino.Texture()
  t.fileName = '/tmp/bump.png'
  t.textureType = rhino.TextureType.Bump
  expect(m.setTexture(t, t.textureType)).toBe(true)
  expect(m.getTexture(rhino.TextureType.Bump).fileName).toBe('/tmp/bump.png')
})

test('materialRenderMaterialInstanceId', () => {
  // renderMaterialInstanceId starts nil and can be set; setting it makes the universal render engine the plug-in
  const m = new rhino.Material()
  expect(m.renderMaterialInstanceId).toBe('00000000-0000-0000-0000-000000000000')
  const uid = '3f2504e0-4f89-11d3-9a0c-0305e82c3301'
  m.renderMaterialInstanceId = uid
  expect(m.renderMaterialInstanceId).toBe(uid)
  expect(m.renderPlugInId).toBe('99999999-9999-9999-9999-999999999999')
})

test('materialCompareAppearance', () => {
  // compareAppearance ignores names
  const a = new rhino.Material(), b = new rhino.Material()
  a.name = 'one'; b.name = 'two'
  expect(rhino.Material.compareAppearance(a, b)).toBe(0)
})

test('materialSetTextureFilenamesReportSuccess', () => {
  // set{Bitmap,Bump,Environment,Transparency}Texture(filename) return true, the first texture included, and set the file name
  const m = new rhino.Material()
  for (const k of ['Bitmap', 'Bump', 'Environment', 'Transparency']) {
    expect(m['set' + k + 'Texture']('/tmp/' + k + '.png')).toBe(true)
    expect(m['get' + k + 'Texture']().fileName).toBe('/tmp/' + k + '.png')
  }
})
