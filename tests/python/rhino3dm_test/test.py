import unittest

from write3dm import writeFile
from createPolylines import createPolylines

class TestWrite(unittest.TestCase):
    def test_write(self):
        """
        Test that it we can write a 3dm file
        """
        result = writeFile()
        self.assertTrue(result)

class TestWrite(unittest.TestCase):
    def test_polyline(self):
        """
        Test that it we can make polylines
        """
        result = createPolylines()
        self.assertTrue(result)

if __name__ == '__main__':
    unittest.main()