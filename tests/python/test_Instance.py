import unittest
import rhino3dm
import uuid
import os
import sys
import helpers


def _instance_translation(m03, m13):
    return [1.0, 0.0, 0.0, m03, 0.0, 1.0, 0.0, m13, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]

#objective


class TestInstance(unittest.TestCase):

    def test_instanceGetObjectIDs(self):
        file3dm = rhino3dm.File3dm.Read('../models/blocks.3dm')
        instanceDefinition = file3dm.InstanceDefinitions[0]
        ids = instanceDefinition.GetObjectIds()
        self.assertTrue(type(ids) == list)
        self.assertTrue(type(ids[0]) == uuid.UUID)

    def test_instanceDefinitionGeometryGetObjectIdsIsEmptyForANewDefinition(self):
        """GetObjectIds of a new InstanceDefinitionGeometry is an empty list. (D87)"""
        self.assertEqual(rhino3dm.InstanceDefinitionGeometry().GetObjectIds(), [])

    def test_instanceReferenceGeometryParentIdefIdAndXform(self):
        """The block instances of a file are InstanceReferenceGeometry with their definition's id and their transform."""
        expected = {'blocks.3dm': [('388f436f-d108-4b1a-9a98-bc48f9593dc4', _instance_translation(-78.83008444266686, 0.0)),
                                   ('388f436f-d108-4b1a-9a98-bc48f9593dc4', _instance_translation(42.29887373318087, 0.0))],
                    'file3dm_stuff.3dm': [('a6a736ea-89f8-4d26-badc-d7556278f055', _instance_translation(0.0, 0.0)),
                                          ('a6a736ea-89f8-4d26-badc-d7556278f055', _instance_translation(0.0, -190.71648594710535))]}
        cells = ['M%d%d' % (i, j) for i in range(4) for j in range(4)]
        for name, want in expected.items():
            refs = [o.Geometry for o in helpers.read_model(name).Objects if isinstance(o.Geometry, rhino3dm.InstanceReferenceGeometry)]
            self.assertEqual(len(refs), len(want), name)
            for ref, (idef, xform) in zip(refs, want):
                self.assertEqual(str(ref.ParentIdefId), idef)
                self.assertEqual([getattr(ref.Xform, c) for c in cells], xform)

    def test_instanceDefinitionIsInstanceGeometryId(self):
        """IsInstanceGeometryId is true for the definition's own object ids, false for nil, unknown ids and an empty definition."""
        f = rhino3dm.File3dm()
        circle = rhino3dm.ArcCurve(rhino3dm.Circle(rhino3dm.Plane.WorldXY, 1.0))
        idx = f.AllInstanceDefinitions.Add('blk', 'desc', rhino3dm.Point3d(0, 0, 0), [circle], [rhino3dm.ObjectAttributes()])
        idef = f.AllInstanceDefinitions.FindIndex(idx)
        ids = idef.GetObjectIds()
        self.assertEqual(len(ids), 1)
        self.assertTrue(idef.IsInstanceGeometryId(ids[0]))
        self.assertFalse(idef.IsInstanceGeometryId(uuid.UUID(int=0)))
        self.assertFalse(idef.IsInstanceGeometryId(uuid.uuid4()))
        self.assertFalse(rhino3dm.InstanceDefinitionGeometry().IsInstanceGeometryId(uuid.uuid4()))

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
