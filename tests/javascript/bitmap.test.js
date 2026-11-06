const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('bitmapUserStrings', () => {
  // A Bitmap keeps user strings: set, get, count and list them (D62)
  const o = new rhino.Bitmap()
  expect(o.userStringCount).toBe(0)
  expect(o.setUserString('k', 'v')).toBe(true)
  expect(o.getUserString('k')).toBe('v')
  expect(Array.from(o.getUserStrings(), p => Array.from(p))).toEqual([['k', 'v']])
})
