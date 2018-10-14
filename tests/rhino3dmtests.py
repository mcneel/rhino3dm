import unittest
import rhino3dm

class TestRhino3dm(unittest.TestCase):
    def setUp(self):
        pass

    def test_readmodel(self):
        path = 'example_3dm_files_20181010/V6/my_curves.3dm'
        self.assertTrue(rhino3dm.File3dm._TestRead(path))


if __name__ == '__main__':
    unittest.main()