const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('fontOfOneFamilyIsShared', () => {
  // two Fonts of one family describe the same font, and deleting one leaves the other usable
  const a = new rhino.Font('Arial'), b = new rhino.Font('Arial')
  expect(a.familyName).toBe(b.familyName)
  expect(a.description).toBe(b.description)
  a.delete()
  expect(b.familyName).toBeTruthy()
  expect(new rhino.Font('Arial').description).toBe(b.description)
})

test('fontFromQuartetProperties', () => {
  // Font.fromQuartetProperties returns a font with a family name, or null
  const q = rhino.Font.fromQuartetProperties('Arial', false, false)
  expect(q == null || !!q.familyName).toBe(true)
})
