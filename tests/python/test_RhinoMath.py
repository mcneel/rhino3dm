import math
import unittest

import rhino3dm


class TestRhinoMath(unittest.TestCase):
    def test_unitScale(self):
        """RhinoMath.UnitScale converts between unit systems (8.x: UnitSystem.UnitScale)."""
        U = rhino3dm.UnitSystem
        self.assertEqual(rhino3dm.RhinoMath.UnitScale(U.Meters, U.Millimeters), 1000.0)
        self.assertAlmostEqual(rhino3dm.RhinoMath.UnitScale(U.Inches, U.Millimeters), 25.4)
        self.assertEqual(rhino3dm.RhinoMath.UnitScale(U.CustomUnits, 2.0, U.Meters, 1.0), 2.0)
        self.assertAlmostEqual(rhino3dm.RhinoMath.MetersPerUnit(U.Inches), 0.0254)

    def test_constants(self):
        """RhinoMath's constants are class properties with RhinoCommon's values."""
        self.assertEqual(rhino3dm.RhinoMath.UnsetValue, -1.23432101234321e+308)
        self.assertEqual(rhino3dm.RhinoMath.ZeroTolerance, 2.3283064365386962890625e-10)
        self.assertEqual(rhino3dm.RhinoMath.TwoPI, 2 * math.pi)
        self.assertEqual(rhino3dm.RhinoMath.UnsetIntIndex, -2147483647)

    def test_helpers(self):
        """ToRadians, Clamp (int and double), Wrap and EpsilonEquals."""
        M = rhino3dm.RhinoMath
        self.assertEqual(M.ToRadians(180), math.pi)
        self.assertEqual(M.ToDegrees(math.pi), 180)
        self.assertEqual(M.Clamp(5, 0, 3), 3)
        self.assertEqual(M.Clamp(0.5, 0.0, 1.0), 0.5)
        self.assertEqual(M.Wrap(370.0, 0.0, 360.0), 10.0)
        self.assertTrue(M.EpsilonEquals(1.0, 1.0 + 1e-12, 1e-9))
        self.assertFalse(M.IsValidDouble(M.UnsetValue))


if __name__ == '__main__':
    unittest.main()
