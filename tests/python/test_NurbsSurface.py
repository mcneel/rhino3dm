import unittest
import rhino3dm
import os
import sys
import helpers
import math


try:
    import numpy
except ImportError:
    numpy = None


def _nurbs_surface_sphere():
    return rhino3dm.NurbsSurface.CreateFromSphere(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 2.0))


def _nurbssurface_sphere():
    return rhino3dm.NurbsSurface.CreateFromSphere(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 2.0))


class TestNurbsSurface(unittest.TestCase):

    ns = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5).ToNurbsSurface()

    def test_NurbsSurfaceKnotList_ToList(self):

        knotsU = list(self.ns.KnotsU)

        self.assertTrue( type(knotsU) == list)
        self.assertTrue( type(knotsU[0]) == float)
        self.assertTrue( len(knotsU) == len(self.ns.KnotsU))

        knotsV = list(self.ns.KnotsV)

        self.assertTrue( type(knotsV) == list)
        self.assertTrue( type(knotsV[0]) == float)
        self.assertTrue( len(knotsV) == len(self.ns.KnotsV))

    def test_nurbsSurfaceIsoCurveReturnsANurbsCurve(self):
        """IsoCurve of a NURBS surface comes back as its most-derived class, NurbsCurve. (D8)"""
        srf = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5).ToNurbsSurface()
        self.assertEqual(type(srf.IsoCurve(1, 0.5)).__name__, 'NurbsCurve')

    @unittest.skipIf(numpy is None, 'numpy not installed')
    def test_nurbsSurfacePointsAndKnotsAreNumpyBuffers(self):
        """A NURBS surface's control points (u x v x 3) and both knot lists convert to numpy without a copy."""
        ns = rhino3dm.NurbsSurface.Create(3, False, 2, 2, 3, 2)
        sp = numpy.asarray(ns.Points)
        self.assertEqual(sp.shape, (3, 2, 3))
        self.assertFalse(sp.flags.owndata)
        self.assertEqual(numpy.asarray(ns.KnotsU).shape, (ns.KnotsU.Count,))
        self.assertEqual(numpy.asarray(ns.KnotsV).shape, (ns.KnotsV.Count,))

    def test_nurbsSurfaceKnotListsCountTheKnots(self):
        """KnotsU and KnotsV of a NURBS sphere have knots; len and Count agree."""
        ns = _nurbs_surface_sphere()
        ku = ns.KnotsU
        self.assertGreater(ku.Count, 0)
        self.assertEqual(len(ku), ku.Count)
        self.assertIn(ku.ClampedAtStart, (True, False))
        self.assertGreater(ns.KnotsV.Count, 0)

    def test_nurbsSurfaceDomainTreatsAnyNonzeroDirectionAsV(self):
        """Surface.Domain(direction) returns the V domain for any nonzero direction, as in RhinoCommon."""
        ns = _nurbs_surface_sphere()
        d1, d7 = ns.Domain(1), ns.Domain(7)
        self.assertEqual((d1.T0, d1.T1), (d7.T0, d7.T1))

    def test_nurbsSurfaceKnotListSuperfluousKnotAtStartAndEnd(self):
        """NurbsSurfaceKnotList.SuperfluousKnot(start) returns the start or end superfluous knot."""
        ku = _nurbs_surface_sphere().KnotsU
        self.assertIsInstance(ku.SuperfluousKnot(True), float)
        self.assertLessEqual(ku.SuperfluousKnot(True), ku.SuperfluousKnot(False))

    def test_nurbsSurfaceGetSpanVectorHasSpanCountPlusOneSortedValues(self):
        """GetSpanVector returns SpanCount + 1 increasing span parameters."""
        ns = _nurbssurface_sphere()
        sv = ns.GetSpanVector(0)
        self.assertEqual(len(sv), ns.SpanCount(0) + 1)
        self.assertEqual(list(sv), sorted(sv))

    def test_nurbsSurfacePointListGetPointRaisesOutsideTheGrid(self):
        """Points.GetPoint returns (True, point) inside the grid and raises IndexError at CountU."""
        ns = _nurbssurface_sphere()
        ok, p = ns.Points.GetPoint(0, 0)
        self.assertIs(ok, True)
        self.assertIsInstance(p, rhino3dm.Point3d)
        with self.assertRaisesRegex(IndexError, 'u must be less than CountU'):
            ns.Points.GetPoint(ns.Points.CountU, 0)

    def test_nurbsSurfaceSetPointAndGetControlPoint(self):
        """Points.SetPoint(u, v, x, y, z) sets a control point that GetControlPoint reads back with weight 1."""
        ns = rhino3dm.NurbsSurface.Create(3, False, 2, 2, 2, 2)
        ns.Points.SetPoint(1, 1, 1.0, 1.0, 0.0)
        cp = ns.Points.GetControlPoint(1, 1)
        self.assertEqual(cp.Location, rhino3dm.Point3d(1, 1, 0))
        self.assertEqual(cp.Weight, 1.0)

    def test_nurbsSurfaceCreateRuledSurface(self):
        """CreateRuledSurface between two lines returns a NurbsSurface."""
        P = rhino3dm.Point3d
        a = rhino3dm.LineCurve(P(0, 0, 0), P(1, 0, 0))
        b = rhino3dm.LineCurve(P(0, 1, 0), P(1, 1, 0))
        self.assertIsInstance(rhino3dm.NurbsSurface.CreateRuledSurface(a, b), rhino3dm.NurbsSurface)

    def test_nurbsSurfacePointListIteratesUOutermost(self):
        """NurbsSurface.Points iterates over all CountU * CountV control points, u outermost."""
        ns = rhino3dm.NurbsSurface.Create(3, False, 2, 2, 3, 2)
        pts = ns.Points
        pts.SetPoint(2, 1, 5.0, 6.0, 7.0)
        cps = list(pts)
        self.assertEqual(len(cps), pts.CountU * pts.CountV)
        self.assertEqual(len(cps), 6)
        self.assertEqual(cps[2 * pts.CountV + 1].Location, rhino3dm.Point3d(5, 6, 7))

    def test_nurbsSurfaceSphereKnotsU(self):
        """A sphere's NurbsSurface has the knots of a rational circle in U. (D35)"""
        pi = math.pi
        s = rhino3dm.NurbsSurface.CreateFromSphere(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 2))
        self.assertEqual(list(s.KnotsU),
                         [0.0, 0.0, pi / 2, pi / 2, pi, pi, 4.71238898038469, 4.71238898038469, 2 * pi, 2 * pi])

    def test_nurbsSurfaceKnotsClampedAndMakeNonRational(self):
        """A sphere's U knots are clamped at both ends, and MakeNonRational succeeds."""
        s = rhino3dm.NurbsSurface.CreateFromSphere(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 2))
        self.assertEqual((s.KnotsU.ClampedAtStart, s.KnotsU.ClampedAtEnd), (True, True))
        self.assertIs(s.MakeNonRational(), True)

    def test_nurbsSurfacePointsGetPoint4d(self):
        """Points.GetPoint4d(u, v) gives the homogeneous control point, GetPoint(u, v) the Euclidean one."""
        sp = rhino3dm.NurbsSurface.Create(3, False, 2, 2, 3, 2).Points
        ok4, q4 = sp.GetPoint4d(0, 0)
        self.assertTrue(ok4)
        self.assertIsInstance(q4, rhino3dm.Point4d)
        self.assertIsInstance(sp.GetPoint(0, 0)[1], rhino3dm.Point3d)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")

        
