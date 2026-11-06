import rhino3dm
import unittest
import os
import sys
import helpers
import gc


class TestBrep(unittest.TestCase):

    #objective: 
    def test_brepBrepVertex_GetEdgeIndices(self):
        
        sphere = rhino3dm.Sphere( rhino3dm.Point3d(0,0,0), 5)
        bbox = sphere.ToBrep().GetBoundingBox(False)
        brep = rhino3dm.Brep.CreateFromBox(bbox)
        vertex = brep.Vertices[0]
        get_edge_indices = vertex.EdgeIndices()

        self.assertTrue(type(get_edge_indices) == list)
        self.assertEqual(len(get_edge_indices), 3)
        self.assertTrue(type(get_edge_indices[0]) == int)

    def test_brepTranslateByComponents(self):
        """Translate(x, y, z) moves the brep and returns True."""
        b = helpers.box_brep()
        self.assertIs(b.Translate(1.0, 2.0, 3.0), True)
        self.assertEqual(b.GetBoundingBox(True).Min, rhino3dm.Point3d(1, 2, 3))

    def test_brepDuplicateIsAnIndependentBrep(self):
        """Duplicate of a Brep is a new Brep with the same faces."""
        b = helpers.box_brep()
        d = b.Duplicate()
        self.assertIsInstance(d, rhino3dm.Brep)
        self.assertIsNot(d, b)
        self.assertEqual(d.Faces.Count, b.Faces.Count)

    def test_brepDefaultConstructorIsEmpty(self):
        """A new Brep has no faces."""
        self.assertEqual(rhino3dm.Brep().Faces.Count, 0)

    def test_brepDuplicateBrepIsABrep(self):
        """DuplicateBrep returns a Brep with the same faces."""
        d = helpers.box_brep().DuplicateBrep()
        self.assertIsInstance(d, rhino3dm.Brep)
        self.assertEqual(d.Faces.Count, 6)

    def test_brepTryConvertBrepOfAFileBrepIsANewBrep(self):
        """TryConvertBrep of a brep read from a file returns a new Brep; editing it leaves the argument alone (D42)."""
        f = helpers.read_model('file3dm_stuff.3dm')
        g = helpers.first_geometry(f, rhino3dm.Brep)
        b = rhino3dm.Brep.TryConvertBrep(g)
        self.assertIsInstance(b, rhino3dm.Brep)
        self.assertIsNot(b, g)
        self.assertEqual(b.Faces.Count, g.Faces.Count)
        identity = rhino3dm.Transform.Identity
        x0 = g.GetBoundingBox(identity).Min.X
        self.assertTrue(b.Transform(rhino3dm.Transform.Translation(rhino3dm.Vector3d(100, 0, 0))))
        self.assertAlmostEqual(b.GetBoundingBox(identity).Min.X - x0, 100, places=6)
        self.assertEqual(g.GetBoundingBox(identity).Min.X, x0)
        del f, g
        gc.collect()
        self.assertGreater(b.Faces.Count, 0)        # the result keeps the file alive

    def test_brepTryConvertBrepOfABrepIsACopy(self):
        """TryConvertBrep of a Brep made in memory returns another Brep with the same faces (D42)."""
        own = helpers.box_brep()
        c = rhino3dm.Brep.TryConvertBrep(own)
        self.assertIsNot(c, own)
        self.assertEqual(c.Faces.Count, own.Faces.Count)

    def test_brepCreateFromBoxBoundingBoxAndBox(self):
        """Brep.CreateFromBox takes a BoundingBox or a Box and makes a six-face brep."""
        bb = rhino3dm.BoundingBox(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3))
        self.assertEqual(rhino3dm.Brep.CreateFromBox(bb).Faces.Count, 6)
        self.assertEqual(rhino3dm.Brep.CreateFromBox(rhino3dm.Box(bb)).Faces.Count, 6)

    def test_brepCreateTrimmedPlaneFromACurveOrAList(self):
        """Brep.CreateTrimmedPlane takes one boundary curve or a list of them."""
        circle = rhino3dm.ArcCurve(rhino3dm.Circle(2.0))
        one = rhino3dm.Brep.CreateTrimmedPlane(rhino3dm.Plane.WorldXY, circle)
        many = rhino3dm.Brep.CreateTrimmedPlane(rhino3dm.Plane.WorldXY, [circle])
        self.assertEqual(one.Faces.Count, 1)
        self.assertEqual(many.Faces.Count, 1)
        self.assertEqual(len(one.Edges), 1)

    def test_brepSurfaceListIndexAndIterate(self):
        """Brep.Surfaces has a length, an indexer that raises IndexError past the end, and iterates."""
        srfs = helpers.box_brep().Surfaces
        self.assertEqual(len(srfs), 6)
        items = list(srfs)
        self.assertEqual(len(items), 6)
        self.assertTrue(all(isinstance(s, rhino3dm.Surface) and s.IsPlanar() for s in items))
        with self.assertRaises(IndexError):
            srfs[6]

    def test_brepSurfaceKeepsItsBrepAlive(self):
        """A surface taken from Brep.Surfaces stays usable after the brep and the list are dropped."""
        b = helpers.box_brep()
        srfs = b.Surfaces
        s0 = srfs[0]
        del b, srfs
        gc.collect()
        self.assertTrue(s0.IsPlanar())

    def test_brepGetBoundingBoxOfABox(self):
        """GetBoundingBox(False) of a box brep is the box."""
        b = helpers.box_brep().GetBoundingBox(False)
        self.assertEqual((b.Min.X, b.Min.Y, b.Max.Y, b.Max.Z), (0, 0, 2, 3))

    def test_brepDuplicateSubBrepKeepsTheChosenFaces(self):
        """DuplicateSubBrep(faceIndices) is a valid brep of those faces only; the original keeps all."""
        box = helpers.box_brep()
        sub = box.DuplicateSubBrep([0, 1])
        self.assertEqual(sub.Faces.Count, 2)
        self.assertTrue(sub.IsValid)
        self.assertEqual(box.Faces.Count, 6)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
