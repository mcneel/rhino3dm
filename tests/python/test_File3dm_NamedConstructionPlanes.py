import os
import sys
import unittest
import rhino3dm
import helpers


class TestFile3dmNamedConstructionPlanes(unittest.TestCase):
    def test_namedConstructionPlanesOfANewFileIsEmpty(self):
        """A new file has no named construction planes."""
        self.assertEqual(len(rhino3dm.File3dm().NamedConstructionPlanes), 0)

    def test_namedConstructionPlanesAddAndIndex(self):
        """Add stores a construction plane that the indexer returns, and it survives a write and read."""
        f = rhino3dm.File3dm()
        cp = rhino3dm.ConstructionPlane()
        cp.Name, cp.GridSpacing = 'top', 2.5
        f.NamedConstructionPlanes.Add(cp)
        self.assertEqual(len(f.NamedConstructionPlanes), 1)
        got = f.NamedConstructionPlanes[0]
        self.assertEqual(got.Name, 'top')
        self.assertEqual(got.GridSpacing, 2.5)
        with helpers.TempDir() as d:
            path = os.path.join(d, 'v.3dm')
            self.assertIs(f.Write(path, 7), True)
            g = rhino3dm.File3dm.Read(path)
        self.assertEqual(g.NamedConstructionPlanes[0].Name, 'top')


if __name__ == '__main__':
    unittest.main()
