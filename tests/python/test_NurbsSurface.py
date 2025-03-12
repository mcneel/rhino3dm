import unittest
import rhino3dm

class TestNurbsSurface(unittest.TestCase):

    ns = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5).ToNurbsSurface()

    def test_NurbsSurfaceKnotList_ToList(self):

        knotsU = self.ns.KnotsU.ToList2()

        self.assertTrue( type(knotsU) == list)
        self.assertTrue( type(knotsU[0]) == float)
        self.assertTrue( len(knotsU) == len(self.ns.KnotsU))

        knotsV = self.ns.KnotsV.ToList2()

        self.assertTrue( type(knotsV) == list)
        self.assertTrue( type(knotsV[0]) == float)
        self.assertTrue( len(knotsV) == len(self.ns.KnotsV))



if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")

        