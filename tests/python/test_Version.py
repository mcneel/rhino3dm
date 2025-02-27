import rhino3dm
import unittest

class TestVersion(unittest.TestCase):

    def test_Version(self):
        print(rhino3dm.Version)
        self.assertTrue(type(rhino3dm.Version) == str)
        self.assertTrue(len(rhino3dm.Version) > 0)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")