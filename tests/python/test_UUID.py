import unittest
import rhino3dm

class TestUuid(unittest.TestCase):

    def test_uuid(self):
        id = rhino3dm.UUID.Create()
        self.assertTrue(type(id) == str)
        self.assertTrue(len(id) == 36)