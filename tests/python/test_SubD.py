import rhino3dm
import unittest
import os 

class TestSubD(unittest.TestCase):

    #objective: to read a SubD from file and check the number of faces, edges and vertices
    def test_readSubDFromFile(self):
        print(os.getcwd())
        file = rhino3dm.File3dm.Read('/Users/luis.fraguada/dev/rhino3dm/tests/models/subd.3dm')
        subd = file.Objects[0].Geometry

        self.assertTrue(len(subd.FaceIterator) == 235)
        self.assertTrue(subd.FaceIterator.FaceCount == len(subd.FaceIterator))

        for face in subd.FaceIterator:
            print(face.Index)

        self.assertTrue(len(subd.EdgeIterator) == 434)
        self.assertTrue(subd.EdgeIterator.EdgeCount == len(subd.EdgeIterator))

        for edge in subd.EdgeIterator:
            print(edge.Index)

        self.assertTrue(len(subd.VertexIterator) == 201)
        self.assertTrue(subd.VertexIterator.VertexCount == len(subd.VertexIterator))

        for vertex in subd.VertexIterator:
            print(vertex.Index)
            #print(vertex.Next().Index) #segfault
            #print(vertex.Previous().Index) #segfault
            print(vertex.ControlNetPoint)
            print(vertex.SurfacePoint)
            for i in range(vertex.EdgeCount):
                print(vertex.EdgeAt(i).Index)
            print('-------')

        

        




if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")