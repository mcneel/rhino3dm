const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('modelComponentIsValidComponentNameChecksTheName', () => {
  // isValidComponentName accepts an ordinary name and rejects an empty one
  expect(rhino.ModelComponent.isValidComponentName('Layer 01')).toBe(true)
  expect(rhino.ModelComponent.isValidComponentName('')).toBe(false)
})

test('modelComponentNamePathSeparator', () => {
  // namePathSeparator is '::', on ModelComponent and on a derived class such as Layer
  expect(rhino.ModelComponent.namePathSeparator).toBe('::')
  expect(rhino.Layer.namePathSeparator).toBe('::')
})
