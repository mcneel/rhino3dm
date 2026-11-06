import os
import sys
import unittest
import rhino3dm
import helpers


def _bindings_world_plane(origin):
    return {'Origin': origin, 'XAxis': {'X': 1.0, 'Y': 0.0, 'Z': 0.0}, 'YAxis': {'X': 0.0, 'Y': 1.0, 'Z': 0.0},
            'ZAxis': {'X': 0.0, 'Y': 0.0, 'Z': 1.0}}


class TestBindings(unittest.TestCase):
    def test_enumArgumentsAreTypeChecked(self):
        """An enum parameter accepts the enum value and raises TypeError for a raw int."""
        x = helpers.curve()
        self.assertIs(x.IsContinuous(rhino3dm.Continuity.C2_continuous, 0.5), True)
        with self.assertRaises(TypeError):
            x.IsContinuous(3, 0.5)

    def test_abstractBasesAreNotConstructible(self):
        """Abstract RhinoCommon bases (GeometryBase, ModelComponent) cannot be constructed."""
        for base in (rhino3dm.GeometryBase, rhino3dm.ModelComponent):
            with self.assertRaises(TypeError):
                base()

    def test_subclassesReachBaseClassMembers(self):
        """A derived class is an instance of its bases and reaches their members."""
        pc = rhino3dm.PolyCurve()
        pc.Append(rhino3dm.LineCurve((0, 0, 0), (2, 0, 0)))
        self.assertIsInstance(pc, rhino3dm.Curve)
        self.assertIsInstance(pc, rhino3dm.GeometryBase)
        mid = pc.PointAt(1.0)                       # declared on Curve
        self.assertEqual((mid.X, mid.Y, mid.Z), (1.0, 0.0, 0.0))

    def test_readOnlyPropertiesHaveNoSetter(self):
        """A property that is read-only in RhinoCommon raises AttributeError when assigned."""
        c = rhino3dm.LineCurve(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(2, 0, 0))
        with self.assertRaises(AttributeError):
            c.PointAtStart = rhino3dm.Point3d(1, 1, 1)

    def test_classStaticPropertiesAreProperties(self):
        """A class's static properties are Python properties, not methods, as on the value types."""
        self.assertIsInstance(rhino3dm.MeshingParameters.Default, rhino3dm.MeshingParameters)
        self.assertEqual(rhino3dm.Material.MaxShine, 255.0)
        self.assertIsInstance(rhino3dm.ModelComponent.NamePathSeparator, str)
        self.assertIsInstance(rhino3dm.ObjectAttributes.OCSMappingChannelId, int)
        self.assertIsInstance(rhino3dm.SubD.Empty, rhino3dm.SubD)
        self.assertFalse(callable(rhino3dm.MeshingParameters.Default))

    def test_valueTypesEncodeToThe8xJsonForm(self):
        """Every value type's Encode writes the 8.x JSON dict, key for key."""
        r = rhino3dm
        plane = {'Origin': {'X': 1.0, 'Y': 2.0, 'Z': 3.0}, 'XAxis': {'X': 1.0, 'Y': 0.0, 'Z': -0.0},
                 'YAxis': {'X': -0.0, 'Y': 1.0, 'Z': 0.0}, 'ZAxis': {'X': 0.0, 'Y': 0.0, 'Z': 1.0}}
        pairs = [(r.Point2d(1, 2), {'X': 1.0, 'Y': 2.0}), (r.Point2f(1.5, 2.5), {'X': 1.5, 'Y': 2.5}),
                 (r.Point3d(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}), (r.Point3f(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}),
                 (r.Point4d(1, 2, 3, 4), {'X': 1.0, 'Y': 2.0, 'Z': 3.0, 'W': 4.0}), (r.Vector2d(1, 2), {'X': 1.0, 'Y': 2.0}),
                 (r.Vector3d(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}), (r.Vector3f(1, 2, 3), {'X': 1.0, 'Y': 2.0, 'Z': 3.0}),
                 (r.Circle(2.5), {'radius': 2.5, 'plane': _bindings_world_plane({'X': 0.0, 'Y': 0.0, 'Z': 0.0})}),
                 (r.Plane(r.Point3d(1, 2, 3), r.Vector3d(0, 0, 1)), plane),
                 (r.Sphere(r.Point3d(1, 2, 3), 4),
                  {'Radius': 4.0, 'EquatorialPlane': _bindings_world_plane({'X': 1.0, 'Y': 2.0, 'Z': 3.0})})]
        for value, expected in pairs:
            with self.subTest(type(value).__name__):
                self.assertEqual(value.Encode(), expected)

    def test_strOfAValueIsRhinoCommonsToString(self):
        """str() of a value type is RhinoCommon's ToString, distinct from repr (D75)."""
        r, P = rhino3dm, rhino3dm.Point3d
        self.assertEqual(str(P(1, 2, 3)), '1,2,3')
        self.assertEqual(P(1, 2, 3).ToString(), '1,2,3')
        self.assertEqual(str(r.Point2d(1, 2)), '1,2')
        self.assertEqual(str(r.Vector2d(4, 5)), '4,5')
        self.assertEqual(str(r.Vector3d(1, 2, 3)), '1,2,3')
        self.assertEqual(str(P(1.5, 2.25, 3.75)), '1.5,2.25,3.75')
        self.assertTrue(repr(P(1, 2, 3)).startswith('Point3d('))
        self.assertNotEqual(str(P(1, 2, 3)), repr(P(1, 2, 3)))
        self.assertEqual(str(r.Point4d(1, 2, 3, 4)), '1,2,3,4')
        self.assertEqual(str(r.Interval(0, 1.5)), '0,1.5')
        self.assertEqual(str(r.Line(P(0, 0, 0), P(1, 2, 3))), '0,0,0,1,2,3')
        self.assertEqual(str(r.BoundingBox(0, 0, 0, 1, 1, 1)), '0,0,0 - 1,1,1')
        self.assertEqual(str(r.Plane.WorldXY), 'Origin=0,0,0 XAxis=1,0,0, YAxis=0,1,0, ZAxis=0,0,1')
        self.assertEqual(str(r.Transform.Identity), 'R0=(1,0,0,0), R1=(0,1,0,0), R2=(0,0,1,0), R3=(0,0,0,1)')
        self.assertEqual(str(r.MeshFace(0, 1, 2)), 'T(0, 1, 2)')
        self.assertEqual(str(r.MeshFace(0, 1, 2, 3)), 'Q(0, 1, 2, 3)')

    def test_strUsesDotNetNumberForms(self):
        """str() prints numbers as .NET does: shortest round-trip, scientific from 1E+17 and below 1E-04, -0, NaN, Infinity (D75)."""
        P = rhino3dm.Point3d
        self.assertEqual(str(P(0.1 + 0.2, 1e15, 1e-05)), '0.30000000000000004,1000000000000000,1E-05')
        self.assertEqual(str(P(1e16, 1e17, -1.5e17)), '10000000000000000,1E+17,-1.5E+17')
        self.assertEqual(str(P(1e14, 0.0001, -1.5e-7)), '100000000000000,0.0001,-1.5E-07')
        self.assertEqual(str(P(-0.0, float('nan'), float('inf'))), '-0,NaN,Infinity')
        self.assertEqual(str(P(1.2345678901234567e20, -float('inf'), 123.456)), '1.2345678901234567E+20,-Infinity,123.456')
        # a float: its own shortest digits, scientific from 1E+09
        self.assertEqual(str(rhino3dm.Point3f(1.1, 1e8, 1e9)), '1.1,100000000,1E+09')

    def test_strOfAClassIsItsToString(self):
        """str() of a class instance is its ToString (a Layer's is its full path) (D75)."""
        layer = rhino3dm.Layer()
        layer.Name = 'Walls'
        self.assertEqual(str(layer), 'Walls')
        self.assertEqual(layer.ToString(), 'Walls')

    def test_arrayResultsAreLists(self):
        """Every C# array result is a Python list, on classes as on value types (D87)."""
        r = rhino3dm
        pts = [r.Point3d(0, 0, 0), r.Point3d(1, 0, 0), r.Point3d(1, 1, 0)]
        pc = r.PointCloud(pts)
        self.assertIsInstance(pc.GetPoints(), list)
        self.assertEqual(pc.GetPoints(), pts)
        seg = r.PolyCurve()
        seg.Append(r.PolylineCurve(pts))
        self.assertIsInstance(seg.Explode(), list)
        self.assertEqual(len(seg.Explode()), 1)
        self.assertIsNone(r.ObjectAttributes().GetGroupList())       # `if (count < 1) return null;`
        self.assertIsInstance(r.Mesh().Vertices.ToPoint3dArray(), list)
        self.assertIsInstance(r.Layer().GetUserStrings(), list)

    def test_nullArrayResultsAreNone(self):
        """Where RhinoCommon returns a null array, Python gets None; an empty array is an empty list (D35)."""
        r, P = rhino3dm, rhino3dm.Point3d
        self.assertIsNone(r.BoundingBox.Unset.GetCorners())
        self.assertIsNone(r.BoundingBox.Unset.GetEdges())
        bb = r.BoundingBox(P(0, 0, 0), P(1, 2, 3))
        self.assertEqual(len(bb.GetCorners()), 8)
        self.assertEqual(len(bb.GetEdges()), 12)
        self.assertIsNone(r.Polyline([P(0, 0, 0)]).GetSegments())               # fewer than 2 points
        self.assertEqual(len(r.Polyline([P(0, 0, 0), P(1, 0, 0)]).GetSegments()), 1)
        self.assertEqual(r.Box.Unset.GetCorners(), [])                         # C#: Array.Empty, not null

    def test_enumValueNoneHasAWritableAlias(self):
        """An enum's None value is also E.None_ (None is a Python keyword); its name stays None."""
        e = rhino3dm.ActiveSpace
        self.assertIs(e.None_, e['None'])
        self.assertIs(e.None_, getattr(e, 'None'))
        self.assertIs(e.None_, e(0))
        self.assertEqual(e.None_.name, 'None')
        self.assertEqual(repr(e.None_), 'ActiveSpace.None')
        self.assertNotIn('None_', [m.name for m in e])                         # iteration lists it once
        self.assertIs(rhino3dm.DimensionStyle.LeaderCurveStyle.None_, rhino3dm.DimensionStyle.LeaderCurveStyle['None'])


if __name__ == '__main__':
    unittest.main()
