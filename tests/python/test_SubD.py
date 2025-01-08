import rhino3dm
import unittest

class TestSubD(unittest.TestCase):

    #objective: to read a SubD from file and check the number of faces, edges and vertices
    def test_readSubDFromFile(self):
        file = rhino3dm.File3dm.Read('../models/subd.3dm')
        subd = file.Objects[0].Geometry
        self.assertTrue(len(subd.Faces) == 235)
        self.assertTrue(len(subd.Edges) == 434)
        self.assertTrue(len(subd.Vertices) == 201)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")