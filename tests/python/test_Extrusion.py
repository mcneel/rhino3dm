import rhino3dm
import unittest

#objective: to test that creating and extrusion vs creating an extrusion with a place yield similar extrusions in different places
class TestExtrusion(unittest.TestCase):
    def test_createExtrusion(self):

        circle1 = rhino3dm.Circle( 1 )
        extrusion1 = rhino3dm.Extrusion.Create(circle1.ToNurbsCurve(), 10, True)
        plane = rhino3dm.Plane.WorldXY()
        plane.Origin = rhino3dm.Point3d(10, 10, 10)
        circle2 = rhino3dm.Circle( 1 )
        circle2.Plane = plane
        extrusion2 = rhino3dm.Extrusion.CreateWithPlane(circle2.ToNurbsCurve(), plane, 10, True)

        bb1 = extrusion1.GetBoundingBox()
        bb2 = extrusion2.GetBoundingBox()

        self.assertAlmostEqual( bb1.Volume, bb2.Volume, 5 )
        self.assertFalse( bb1.Center == bb2.Center )

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")