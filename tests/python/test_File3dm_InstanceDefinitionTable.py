import os
import sys
import unittest
import rhino3dm
import helpers


class TestFile3dmInstanceDefinitionTable(unittest.TestCase):
    def test_instanceDefinitionTableAddWithGeometryAndAttributes(self):
        """Add(name, description, basePoint, geometry, attributes) adds one instance definition."""
        f = rhino3dm.File3dm()
        n0 = len(f.AllInstanceDefinitions)
        circle = rhino3dm.ArcCurve(rhino3dm.Circle(2.0))
        idx = f.AllInstanceDefinitions.Add('blk', 'desc', rhino3dm.Point3d(0, 0, 0), [circle], [rhino3dm.ObjectAttributes()])
        self.assertGreaterEqual(idx, 0)
        self.assertEqual(len(f.AllInstanceDefinitions), n0 + 1)

    def test_instanceDefinitionTableFindIndex(self):
        """AllInstanceDefinitions.FindIndex returns the definition added at an index, None for an index past the end."""
        f = rhino3dm.File3dm()
        circle = rhino3dm.ArcCurve(rhino3dm.Circle(rhino3dm.Plane.WorldXY, 1.0))
        idx = f.AllInstanceDefinitions.Add('blk', 'desc', rhino3dm.Point3d(0, 0, 0), [circle], [rhino3dm.ObjectAttributes()])
        idef = f.AllInstanceDefinitions.FindIndex(idx)
        self.assertIsInstance(idef, rhino3dm.InstanceDefinitionGeometry)
        self.assertEqual(idef.Name, 'blk')
        self.assertIsNone(f.AllInstanceDefinitions.FindIndex(idx + 1))

    def test_instanceDefinitionTableAdd(self):
        """AllInstanceDefinitions.Add(definition) adds the definition to the file."""
        f = rhino3dm.File3dm()
        idef = rhino3dm.InstanceDefinitionGeometry()
        idef.Name = 'blk'
        n0 = len(f.AllInstanceDefinitions)
        f.AllInstanceDefinitions.Add(idef)
        self.assertEqual(len(f.AllInstanceDefinitions), n0 + 1)
        self.assertEqual(f.AllInstanceDefinitions.FindIndex(n0).Name, 'blk')


if __name__ == '__main__':
    unittest.main()
