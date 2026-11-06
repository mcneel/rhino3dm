import os
import sys
import unittest
import rhino3dm
import helpers


class TestComponentIndex(unittest.TestCase):
    def test_componentIndexHoldsTypeAndIndex(self):
        """ComponentIndex(type, index) stores both and is set."""
        ci = rhino3dm.ComponentIndex(rhino3dm.ComponentIndexType.BrepFace, 3)
        self.assertEqual(ci.Index, 3)
        self.assertEqual(ci.ComponentIndexType, rhino3dm.ComponentIndexType.BrepFace)
        self.assertTrue(ci.IsSet)

    def test_componentIndexUnsetIsUnset(self):
        """ComponentIndex.Unset has index -1 and IsUnset; a real index is not unset."""
        u = rhino3dm.ComponentIndex.Unset
        self.assertTrue(u.IsUnset())
        self.assertEqual(u.Index, -1)
        self.assertFalse(rhino3dm.ComponentIndex(rhino3dm.ComponentIndexType.BrepFace, 3).IsUnset())

    def test_componentIndexComparesByValue(self):
        """Component indices are equal by value and Unset orders before a set index."""
        ci = rhino3dm.ComponentIndex(rhino3dm.ComponentIndexType.BrepFace, 3)
        u = rhino3dm.ComponentIndex.Unset
        self.assertEqual(ci, rhino3dm.ComponentIndex(rhino3dm.ComponentIndexType.BrepFace, 3))
        self.assertNotEqual(ci, u)
        self.assertLess(u, ci)


if __name__ == '__main__':
    unittest.main()
