const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('environmentBackgroundImageIsANewTexturePerAccess', () => {
  // a constructed Environment has a background projection, and backgroundImage returns a new Texture per access
  const e = new rhino.Environment()
  expect(e.backgroundProjection).not.toBeNull()
  expect(e.backgroundProjection).not.toBeUndefined()
  const tex = e.backgroundImage
  expect(tex instanceof rhino.Texture).toBe(true)
  expect(e.backgroundImage).not.toBe(tex)
})
