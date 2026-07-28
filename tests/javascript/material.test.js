const rhino3dm = require('rhino3dm')

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
  // the textureType setter is bound as int, so assign the enum's numeric .value
  texture.textureType = rhino.TextureType.PBR_Roughness.value
  material.setTexture(texture)

  const roughness = material.getTexture(rhino.TextureType.PBR_Roughness)

  expect(roughness).not.toBeNull()
  expect(roughness.textureType).toBe(rhino.TextureType.PBR_Roughness)

  if (roughness) roughness.delete()
})
