const rhino3dm = require('rhino3dm')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('notesDefaultsAreEmpty', () => {
  // a new model has empty, hidden, plain-text notes
  const notes = new rhino.File3dm().notes
  expect(notes.notes).toBe('')
  expect(notes.isVisible).toBe(false)
  expect(notes.isHtml).toBe(false)
})

test('notesSurviveWriteAndRead', () => {
  // the text, flags and window rectangle set on the notes are in the written file (RH3DM-208)
  const w = new rhino.File3dm()
  w.notes.notes = 'Revision B\nlicence: CC-BY, café'
  w.notes.isVisible = true
  w.notes.isHtml = true
  w.notes.windowRectangle = [10, 20, 300, 200]
  const f = rhino.File3dm.fromByteArray(w.toByteArray())
  expect(f.notes.notes).toBe('Revision B\nlicence: CC-BY, café')
  expect(f.notes.isVisible).toBe(true)
  expect(f.notes.isHtml).toBe(true)
  expect(Array.from(f.notes.windowRectangle)).toEqual([10, 20, 300, 200])
})

test('notesSetterKeepsTheOtherValues', () => {
  // setting one value of the notes leaves the others as they were
  const notes = new rhino.File3dm().notes
  notes.notes = 'text'
  notes.isVisible = true
  notes.windowRectangle = [1, 2, 3, 4]
  notes.isHtml = true
  expect(notes.notes).toBe('text')
  expect(notes.isVisible).toBe(true)
  expect(Array.from(notes.windowRectangle)).toEqual([1, 2, 3, 4])
})
