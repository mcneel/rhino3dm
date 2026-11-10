import os
import sys
import unittest
import rhino3dm
import helpers


def _arccurve_check(test, c, t0, t1, end):
    test.assertAlmostEqual(c.Domain.T0, t0, delta=1e-12)
    test.assertAlmostEqual(c.Domain.T1, t1, delta=1e-12)
    b = c.PointAtEnd
    for got, want in zip((b.X, b.Y, b.Z), end):
        test.assertAlmostEqual(got, want, delta=1e-9)


class TestArcCurve(unittest.TestCase):
    def test_arcCurveConstructsFromNone(self):
        """ArcCurve(None) constructs an empty ArcCurve, as RhinoCommon's new ArcCurve(null). (D10)"""
        self.assertIsInstance(rhino3dm.ArcCurve(None), rhino3dm.ArcCurve)

    def test_arcCurveConstructorsFromArcAndCircle(self):
        """The ArcCurve constructors take an arc or a circle, with an optional domain."""
        arc = rhino3dm.Arc(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 1, 0), rhino3dm.Point3d(2, 0, 0))
        _arccurve_check(self, rhino3dm.ArcCurve(arc), 0.0, 3.141592653589793, (2.0, 0.0, 0.0))
        _arccurve_check(self, rhino3dm.ArcCurve(arc, 2.0, 5.0), 2.0, 5.0, (2.0, 0.0, 0.0))
        _arccurve_check(self, rhino3dm.ArcCurve(rhino3dm.Circle(3.0)), 0.0, 18.84955592153876, (3.0, 0.0, 0.0))
        _arccurve_check(self, rhino3dm.ArcCurve(rhino3dm.Circle(3.0), 1.0, 4.0), 1.0, 4.0, (3.0, 0.0, 0.0))


if __name__ == '__main__':
    unittest.main()
