import rhino3dm
import unittest
import os
import helpers


class TestFile3dmNotes(unittest.TestCase):

    def test_notesDefaultsAreEmpty(self):
        """A new model has empty, hidden, plain-text notes."""
        notes = rhino3dm.File3dm().Notes
        self.assertEqual(notes.Notes, '')
        self.assertFalse(notes.IsVisible)
        self.assertFalse(notes.IsHtml)

    def test_notesSurviveWriteAndRead(self):
        """The text, flags and window rectangle set on the notes are in the written file (RH3DM-208)."""
        w = rhino3dm.File3dm()
        w.Notes.Notes = 'Revision B\nlicence: CC-BY, café'
        w.Notes.IsVisible = True
        w.Notes.IsHtml = True
        w.Notes.WindowRectangle = (10, 20, 300, 200)
        with helpers.TempDir() as d:
            path = os.path.join(d, 'notes.3dm')
            self.assertTrue(w.Write(path, 8))
            self.assertEqual(rhino3dm.File3dm.ReadNotes(path), 'Revision B\nlicence: CC-BY, café')
            f = rhino3dm.File3dm.Read(path)
        self.assertEqual(f.Notes.Notes, 'Revision B\nlicence: CC-BY, café')
        self.assertTrue(f.Notes.IsVisible)
        self.assertTrue(f.Notes.IsHtml)
        self.assertEqual(tuple(f.Notes.WindowRectangle), (10, 20, 300, 200))

    def test_notesSetterKeepsTheOtherValues(self):
        """Setting one value of the notes leaves the others as they were."""
        notes = rhino3dm.File3dm().Notes
        notes.Notes = 'text'
        notes.IsVisible = True
        notes.WindowRectangle = (1, 2, 3, 4)
        notes.IsHtml = True
        self.assertEqual(notes.Notes, 'text')
        self.assertTrue(notes.IsVisible)
        self.assertEqual(tuple(notes.WindowRectangle), (1, 2, 3, 4))


if __name__ == '__main__':
    unittest.main()
