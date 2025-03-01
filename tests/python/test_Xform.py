import unittest
import rhino3dm

class TestXform(unittest.TestCase):

    def test_xformToFloatArray(self):

        xform = rhino3dm.Transform.Identity()
        fa = xform.ToFloatArray2(False)

        self.assertEqual(len(fa), 16)
        self.assertTrue( type(fa) == list )
        self.assertTrue( type(fa[0]) == float )

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")