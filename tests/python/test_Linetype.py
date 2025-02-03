import unittest
import rhino3dm

class TestLinetype(unittest.TestCase):
    def test_linetypeGetSegments(self):
        lt = rhino3dm.Linetype.Dashed
        segment = lt.GetSegment(0)

        self.assertTrue(type(segment) == tuple)
        self.assertTrue(len(segment) == 2)
        self.assertTrue(type(segment[0]) == float)
        self.assertTrue(type(segment[1]) == bool)
