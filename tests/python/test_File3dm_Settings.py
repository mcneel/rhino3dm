import os
import tempfile
import unittest
import rhino3dm


# RH3DM-195 / RH-87934: the page absolute/relative tolerance setters must be independent.
# The bug was .NET-only; this is a parity guard for the shared setter surface.
class TestFile3dmSettings(unittest.TestCase):

    def test_page_absolute_tolerance_sticks_and_leaves_relative(self):
        file3dm = rhino3dm.File3dm()
        rel_before = file3dm.Settings.PageRelativeTolerance

        file3dm.Settings.PageAbsoluteTolerance = 0.01

        self.assertEqual(file3dm.Settings.PageAbsoluteTolerance, 0.01)
        self.assertEqual(file3dm.Settings.PageRelativeTolerance, rel_before)

    def test_page_relative_tolerance_sticks_and_leaves_absolute(self):
        file3dm = rhino3dm.File3dm()
        abs_before = file3dm.Settings.PageAbsoluteTolerance

        file3dm.Settings.PageRelativeTolerance = 0.5

        self.assertEqual(file3dm.Settings.PageRelativeTolerance, 0.5)
        self.assertEqual(file3dm.Settings.PageAbsoluteTolerance, abs_before)

    def test_page_tolerances_round_trip(self):
        path = os.path.join(tempfile.gettempdir(), "rh3dm195_pagetol_py.3dm")
        try:
            file3dm = rhino3dm.File3dm()
            file3dm.Settings.PageAbsoluteTolerance = 0.01
            file3dm.Settings.PageRelativeTolerance = 0.25
            self.assertTrue(file3dm.Write(path, 8))

            read = rhino3dm.File3dm.Read(path)
            self.assertEqual(read.Settings.PageAbsoluteTolerance, 0.01)
            self.assertEqual(read.Settings.PageRelativeTolerance, 0.25)
        finally:
            if os.path.exists(path):
                os.remove(path)


if __name__ == "__main__":
    unittest.main()
