import rhino3dm
import unittest
import os
import sys
import helpers

#objective: to test that creating and extrusion vs creating an extrusion with a place yield similar extrusions in different places


class TestExtrusion(unittest.TestCase):
    def test_createExtrusion(self):

        circle1 = rhino3dm.Circle( 1 )
        extrusion1 = rhino3dm.Extrusion.Create(circle1.ToNurbsCurve(), 10, True)
        plane = rhino3dm.Plane.WorldXY
        plane.Origin = rhino3dm.Point3d(10, 10, 10)
        circle2 = rhino3dm.Circle( 1 )
        circle2.Plane = plane
        extrusion2 = rhino3dm.Extrusion.Create(circle2.ToNurbsCurve(), plane, 10, True)   # 8.x: CreateWithPlane

        bb1 = extrusion1.GetBoundingBox(False)
        bb2 = extrusion2.GetBoundingBox(False)

        self.assertAlmostEqual( bb1.Volume, bb2.Volume, 5 )
        self.assertFalse( bb1.Center == bb2.Center )

    def test_extrusionCapsOfACylinder(self):
        """A cylinder extrusion reports the bottom and top caps it was made with, and no mitered ends."""
        cyl = rhino3dm.Cylinder(rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 1.0), 4.0)
        for bottom, top in ((False, False), (True, False), (False, True), (True, True)):
            e = rhino3dm.Extrusion.CreateCylinderExtrusion(cyl, bottom, top)
            self.assertEqual((e.IsCappedAtBottom, e.IsCappedAtTop), (bottom, top))
            self.assertEqual((e.IsMiteredAtStart, e.IsMiteredAtEnd), (False, False))

    def test_extrusionCreateBoxExtrusionIsACappedSolid(self):
        """CreateBoxExtrusion(box, True) returns a solid Extrusion with two caps."""
        box = rhino3dm.Box(rhino3dm.Plane.WorldXY, rhino3dm.Interval(0, 1), rhino3dm.Interval(0, 2), rhino3dm.Interval(0, 3))
        ext = rhino3dm.Extrusion.CreateBoxExtrusion(box, True)
        self.assertIsInstance(ext, rhino3dm.Extrusion)
        self.assertTrue(ext.IsSolid)
        self.assertEqual(ext.CapCount, 2)

    def test_extrusionCreateFromCurveAndPlane(self):
        """Extrusion.Create(curve, plane, height, cap) extrudes the profile to the height."""
        e = rhino3dm.Extrusion.Create(rhino3dm.Circle(2).ToNurbsCurve(), rhino3dm.Plane.WorldXY, 3.0, True)
        self.assertIsNotNone(e)
        self.assertAlmostEqual(e.GetBoundingBox(rhino3dm.Transform.Identity).Max.Z, 3.0, delta=1e-9)

    def test_extrusionToBrepTakesTheBaseOverloads(self):
        """Extrusion.ToBrep() and ToBrep(splitKinkyFaces) both return a Brep."""
        e = rhino3dm.Extrusion.Create(rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 1.0).ToNurbsCurve(), 2.0, True)
        self.assertIsInstance(e.ToBrep(), rhino3dm.Brep)
        self.assertIsInstance(e.ToBrep(False), rhino3dm.Brep)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
