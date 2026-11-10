import os
import sys
import unittest
import rhino3dm
import helpers
import json


def _common_object_json_forms():
    with open(helpers.model('old_oracle/json_forms.json')) as fh:
        return json.load(fh)


def _common_object_first_geometry():
    return next(iter(helpers.read_model('file3dm_stuff.3dm').Objects)).Geometry


def _common_object_box(g):
    b = g.GetBoundingBox(False)
    return [round(c, 9) for p in (b.Min, b.Max) for c in (p.X, p.Y, p.Z)]


class TestCommonObject(unittest.TestCase):
    def test_commonObjectIsTheBaseOfGeometryAndAttributes(self):
        """Geometry, object attributes and CachedTextureCoordinates are CommonObjects."""
        self.assertIsInstance(helpers.box_brep(), rhino3dm.CommonObject)
        self.assertIsInstance(rhino3dm.ObjectAttributes(), rhino3dm.CommonObject)
        self.assertTrue(issubclass(rhino3dm.CachedTextureCoordinates, rhino3dm.CommonObject))

    def test_commonObjectMembersOfCachedTextureCoordinatesRaise(self):
        """CachedTextureCoordinates wraps no ON_Object, so its CommonObject members raise; its own members work."""
        mesh = helpers.quad_mesh()
        I = rhino3dm.Interval
        tm = rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.WorldXY, I(0, 1), I(0, 1), I(0, 1))
        mesh.SetCachedTextureCoordinates(tm, rhino3dm.Transform.Identity)
        tc = mesh.GetCachedTextureCoordinates(tm.Id)
        self.assertEqual(tc.Count, 4)
        for member in (lambda: tc.IsValid, lambda: tc.IsValidWithLog(), lambda: tc.HasUserData):
            with self.assertRaisesRegex(RuntimeError, 'not an ON_Object'):
                member()

    def test_commonObjectIsValidAndIsValidWithLog(self):
        """IsValid is True for a box and IsValidWithLog returns (True, log text)."""
        b = helpers.box_brep()
        self.assertIs(b.IsValid, True)
        ok, log = b.IsValidWithLog()
        self.assertIs(ok, True)
        self.assertIsInstance(log, str)

    def test_commonObjectComputeMemoryEstimateIsPositive(self):
        """CommonObject.ComputeMemoryEstimate is positive for a brep."""
        self.assertGreater(rhino3dm.CommonObject.ComputeMemoryEstimate(helpers.box_brep()), 0)

    def test_commonObjectEncodeWritesThe8xJsonKeys(self):
        """CommonObject.Encode writes the 8.x JSON dict's keys, and Decode reads it back as the most derived class."""
        forms = _common_object_json_forms()
        g = _common_object_first_geometry()
        d = g.Encode()
        old = forms['common_object_encode']
        self.assertEqual(set(d), {'version', 'archive3dm', 'opennurbs', 'data'})
        self.assertEqual(set(d), set(old))
        self.assertEqual(d['archive3dm'], 60)
        self.assertEqual(d['archive3dm'], old['archive3dm'])
        self.assertEqual(d['version'], old['version'])
        back = rhino3dm.CommonObject.Decode(d)
        self.assertEqual(type(g).__name__, forms['common_object_type'])
        self.assertEqual(type(back).__name__, type(g).__name__)
        self.assertEqual(back.Encode(), d)

    def test_commonObjectDecodeReadsAn8xPayload(self):
        """CommonObject.Decode reads a payload 8.x's Encode wrote: the same class, the same geometry."""
        old = _common_object_json_forms()['common_object_encode']
        g = _common_object_first_geometry()
        from_old = rhino3dm.CommonObject.Decode(old)
        self.assertEqual(type(from_old).__name__, type(g).__name__)
        self.assertEqual(_common_object_box(from_old), _common_object_box(g))

    def test_commonObjectRdkXmlIsEmptyWithoutRenderData(self):
        """RdkXml of a geometry with no render data is an empty string."""
        self.assertEqual(_common_object_first_geometry().RdkXml(), '')


if __name__ == '__main__':
    unittest.main()
