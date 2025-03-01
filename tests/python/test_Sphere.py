import unittest
import rhino3dm

class TestSphere(unittest.TestCase):
    def test_sphereClosestParameter(self):

        sphere = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5)

        result = sphere.ClosestParameter(rhino3dm.Point3d(0, 0, 5))
        
        self.assertTrue(type(result) == tuple)
        self.assertTrue(len(result) == 3)
        self.assertTrue(type(result[0]) == bool)
        self.assertTrue(type(result[1]) == float)
        self.assertTrue(type(result[2]) == float)

        self.assertTrue(result[0])

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")