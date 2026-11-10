import os
import sys
import unittest
import rhino3dm
import helpers
import uuid


def _texturemapping_all():
    I = rhino3dm.Interval(0, 1)
    TM = rhino3dm.TextureMapping
    return [TM.CreateSurfaceParameterMapping(),
            TM.CreatePlaneMapping(rhino3dm.Plane.WorldXY, I, I, I),
            TM.CreateCylinderMapping(rhino3dm.Cylinder(rhino3dm.Circle(1.0), 2.0), True),
            TM.CreateCylinderMapping(rhino3dm.Cylinder(rhino3dm.Circle(1.0), 2.0), False),
            TM.CreateSphereMapping(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 2.0)),
            TM.CreateBoxMapping(rhino3dm.Plane.WorldXY, I, I, I, True)]


def _texturemapping_matrix(t):
    return [getattr(t, 'M%d%d' % (i, j)) for i in range(4) for j in range(4)]


class TestTextureMapping(unittest.TestCase):
    def test_textureMappingFactoriesReturnNoneOnFailure(self):
        """TextureMapping's Create* factories return a mapping, or None when it cannot be made."""
        i = rhino3dm.Interval(0, 1)
        tm = rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.WorldXY, i, i, i, True)
        self.assertIsInstance(tm, rhino3dm.TextureMapping)
        self.assertIs(tm.Capped, True)
        self.assertIsInstance(rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.WorldXY, i, i, i), rhino3dm.TextureMapping)
        self.assertIsNone(rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.Unset, i, i, i, False))   # origin unset
        self.assertIsInstance(rhino3dm.TextureMapping.CreateSurfaceParameterMapping(), rhino3dm.TextureMapping)
        self.assertIsInstance(rhino3dm.TextureMapping.CreateSphereMapping(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 2)), rhino3dm.TextureMapping)

    def test_textureMappingEvaluateReturnsTheResultAndPoint(self):
        """TextureMapping.Evaluate(p, n) returns (result, texture coordinate)."""
        tm = rhino3dm.TextureMapping.CreateSurfaceParameterMapping()
        t = tm.Evaluate(rhino3dm.Point3d(0.25, 0.5, 0), rhino3dm.Vector3d(0, 0, 1))
        self.assertEqual(len(t), 2)
        self.assertIsInstance(t[0], int)

    def test_textureMappingIdIsAUuid(self):
        """A texture mapping's Id is a UUID."""
        self.assertIsInstance(rhino3dm.TextureMapping.CreateSurfaceParameterMapping().Id, uuid.UUID)

    def test_textureMappingInheritsModelComponentMembers(self):
        """A plane TextureMapping has ModelComponent's HasId."""
        I = rhino3dm.Interval
        tm = rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.WorldXY, I(0, 1), I(0, 1), I(0, 1))
        self.assertIn(tm.HasId, (True, False))

    def test_textureMappingTypeOfEachCreate(self):
        """MappingType is the kind of mapping each Create method makes."""
        T = rhino3dm.TextureMappingType
        self.assertEqual([n.MappingType for n in _texturemapping_all()],
                         [T.SurfaceParameters, T.PlaneMapping, T.CylinderMapping, T.CylinderMapping, T.SphereMapping, T.BoxMapping])

    def test_textureMappingPeriodicAndVertexNormalsFromItsProperties(self):
        """Sphere and cylinder mappings are periodic; ray projection, box and capped cylinder mappings need vertex normals (D49)."""
        T = rhino3dm.TextureMappingType
        # (periodic, requires vertex normals) of each mapping in _texturemapping_all
        expected = [(False, False), (False, False), (True, True), (True, False), (True, False), (False, True)]
        for n, (periodic, normals) in zip(_texturemapping_all(), expected):
            k = n.MappingType
            self.assertEqual(periodic, k in (T.SphereMapping, T.CylinderMapping))
            derived = k != T.SurfaceParameters and (n.Projection == rhino3dm.Projection.Ray or k == T.BoxMapping
                                                    or (k == T.CylinderMapping and n.Capped))
            self.assertEqual(normals, derived, k)

    def test_textureMappingUvwTransformComposes(self):
        """Left-multiplying UvwTransform reverses, swaps or tiles a plane mapping's texture coordinates."""
        I = rhino3dm.Interval(0, 1)
        cases = (
            ('reverse', lambda x: (setattr(x, 'M11', -1.0), setattr(x, 'M13', 1.0)),
             [1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]),
            ('swap', lambda x: (setattr(x, 'M00', 0.0), setattr(x, 'M22', 0.0), setattr(x, 'M02', 1.0), setattr(x, 'M20', 1.0)),
             [0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0]),
            ('tile', lambda x: (setattr(x, 'M00', 3.0), setattr(x, 'M03', 0.25)),
             [3.0, 0.0, 0.0, 0.25, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]))
        for name, build, want in cases:
            x = rhino3dm.Transform(1.0)
            build(x)
            n = rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.WorldXY, I, I, I)
            n.UvwTransform = x * n.UvwTransform
            self.assertEqual(_texturemapping_matrix(n.UvwTransform), want, name)

    def test_textureMappingUserStrings(self):
        """A TextureMapping keeps user strings: set, get, count and list them. (D62)"""
        I = rhino3dm.Interval
        o = rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.WorldXY, I(0, 1), I(0, 1), I(0, 1))
        self.assertEqual(o.UserStringCount, 0)
        self.assertEqual(o.GetUserStrings(), [])
        self.assertTrue(o.SetUserString('k', 'v'))
        self.assertEqual(o.GetUserString('k'), 'v')
        self.assertEqual(o.UserStringCount, 1)
        self.assertEqual(o.GetUserStrings(), [['k', 'v']])

    def test_textureMappingTypeWcsProjectionValues(self):
        """TextureMappingType has the WCS projection values RhinoCommon gives them."""
        T = rhino3dm.TextureMappingType
        self.assertEqual((int(T.WcsProjection), int(T.WcsBoxProjection)), (11, 12))

    def test_textureMappingTryGetMappingCapped(self):
        """TryGetMappingBoxCapped / CylinderCapped / PlaneCapped add the capped flag to TryGetMapping*'s results."""
        r = rhino3dm
        I, xy = r.Interval, r.Plane.WorldXY
        for capped in (True, False):
            with self.subTest(capped=capped):
                box = r.TextureMapping.CreateBoxMapping(xy, I(0, 1), I(0, 2), I(0, 3), capped)
                res = box.TryGetMappingBoxCapped()
                self.assertEqual(len(res), 6)
                self.assertIs(res[5], capped)
                self.assertEqual(res[:5], box.TryGetMappingBox())
                cyl = r.TextureMapping.CreateCylinderMapping(r.Cylinder(r.Circle(xy, 1.0), 2.0), capped)
                ok, cy, cap = cyl.TryGetMappingCylinderCapped()
                self.assertTrue(ok)
                self.assertIs(cap, capped)
                self.assertIsInstance(cy, r.Cylinder)
                self.assertTrue(cyl.TryGetMappingCylinder()[0])
                pl = r.TextureMapping.CreatePlaneMapping(xy, I(0, 1), I(0, 2), I(0, 3), capped)
                res = pl.TryGetMappingPlaneCapped()
                self.assertEqual(len(res), 6)
                self.assertIs(res[5], capped)
                self.assertEqual(res[:5], pl.TryGetMappingPlane())


if __name__ == '__main__':
    unittest.main()
