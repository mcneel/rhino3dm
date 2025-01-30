import rhino3dm
import unittest

#objective: 
class TestCircle(unittest.TestCase):
    def test_Circle(self):

        circle = rhino3dm.Circle(5)
        result = circle.ClosestParameter(rhino3dm.Point3d(0, 0, 0))
        self.assertTrue(result[0] == True)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")