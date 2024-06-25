import unittest

from write3dm import writeFile

class TestWrite(unittest.TestCase):
    def test_write(self):
        """
        Test that it we can write a 3dm file
        """
        result = writeFile()
        self.assertTrue(result)

if __name__ == '__main__':
    unittest.main()