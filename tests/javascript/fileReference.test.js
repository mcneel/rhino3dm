const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('fileReferenceKeepsFullAndRelativePaths', () => {
  // FileReference.createFromFullAndRelativePaths keeps both paths and is set
  const fr = rhino.FileReference.createFromFullAndRelativePaths('/tmp/textures/wood.png', 'textures/wood.png')
  expect(fr).not.toBeNull()
  expect(fr.isSet).toBe(true)
  expect(fr.fullPath).toBe('/tmp/textures/wood.png')
  expect(fr.relativePath).toBe('textures/wood.png')
  fr.delete()
})

test('fileReferenceCreateFromFullPath', () => {
  // createFromFullPath makes a FileReference whose fullPath is the given path
  const fr = rhino.FileReference.createFromFullPath('/tmp/a.png')
  expect(fr.fullPath.endsWith('a.png')).toBe(true)
})
