const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('textureUvwTransformDefaultsToIdentity', () => {
  // a new Texture's uvwTransform is the identity
  const xf = new rhino.Texture().uvwTransform
  expect([xf.M00, xf.M11, xf.M22, xf.M33]).toEqual([1.0, 1.0, 1.0, 1.0])
})

test('textureWrapURoundTripsClampAndRepeat', () => {
  // Texture.wrapU reads back Clamp and Repeat as set
  const t = new rhino.Texture()
  t.wrapU = rhino.TextureUvwWrapping.Clamp
  expect(t.wrapU).toBe(rhino.TextureUvwWrapping.Clamp)
  t.wrapU = rhino.TextureUvwWrapping.Repeat
  expect(t.wrapU).toBe(rhino.TextureUvwWrapping.Repeat)
})

test('textureFileReferenceIsNoneOrAFileReference', () => {
  // Texture.fileReference is null or a FileReference
  const fr = new rhino.Texture().fileReference
  expect(fr == null || fr instanceof rhino.FileReference).toBe(true)
})

test('textureProjectionMode', () => {
  // a new Texture's projectionMode is MappingChannel, and the setter changes it
  const t = new rhino.Texture()
  const m = rhino.TextureProjectionModes
  expect(t.projectionMode).toBe(m.MappingChannel)
  t.projectionMode = m.Wcs
  expect(t.projectionMode).toBe(m.Wcs)
})
