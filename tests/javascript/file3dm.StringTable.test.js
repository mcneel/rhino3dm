const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('stringTableDocumentUserTextCountCountsSectionKeys', () => {
  // after a write and read, documentUserTextCount counts the section\key strings and getValue(section, key) reads them
  const w = new rhino.File3dm()
  w.strings.setString('plain', 'v')
  w.strings.setString('sec', 'key', 'v2')
  const f = rhino.File3dm.fromByteArray(w.toByteArray())
  expect(f.strings.documentUserTextCount).toBe(1)
  expect(f.strings.getValue('sec', 'key')).toBe('v2')
  expect(f.strings.getValue('', 'key')).toBe('')
})
