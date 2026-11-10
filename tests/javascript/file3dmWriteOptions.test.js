const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('file3dmWriteOptionsDefaults', () => {
  // new write options save user data and render meshes, with version 0
  const o = new rhino.File3dmWriteOptions()
  expect(o.saveUserData).toBe(true)
  expect(o.saveRenderMeshes).toBe(true)
  expect(o.version).toBe(0)
})

test('file3dmWriteOptionsEnableRenderMeshesOff', () => {
  // enableRenderMeshes(AnyObject, false) turns saveRenderMeshes off
  const o = new rhino.File3dmWriteOptions()
  o.enableRenderMeshes(rhino.ObjectType.AnyObject, false)
  expect(o.saveRenderMeshes).toBe(false)
})
