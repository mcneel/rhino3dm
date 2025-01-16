import rhino3dm
import unittest

class TestSubD(unittest.TestCase):

    #objective: to read a SubD from file and check the number of faces, edges and vertices
    def test_readSubDFromFile(self):
        file = rhino3dm.File3dm.Read('../models/subd.3dm')
        subd = file.Objects[0].Geometry
        #self.assertTrue(len(subd.Faces) == 235)
        
        #self.assertTrue(len(subd.Edges) == 434)
        #self.assertTrue(len(subd.Vertices) == 201)

        #face = subd.Faces.Find(10)
        #print(face.Index)
        #print(face.EdgeCount) # <--- segfault

        self.assertTrue(len(subd.FaceIterator) == 235)
        self.assertTrue(subd.FaceIterator.FaceCount == len(subd.FaceIterator))

        firstface = subd.FaceIterator.FirstFace()
        self.assertTrue(firstface.EdgeCount == 3)
        self.assertTrue(firstface.Index == 1)

        #print(subd.FaceIterator.CurrentFaceIndex)

        lastface = subd.FaceIterator.LastFace()
        self.assertTrue(lastface.EdgeCount == 4)
        self.assertTrue(lastface.Index == subd.FaceIterator.FaceCount)

        #print(subd.FaceIterator.CurrentFaceIndex)

        nextface = subd.FaceIterator.NextFace()
        self.assertTrue(nextface.EdgeCount == 3)
        self.assertTrue(nextface.Index == 2)

        print(subd.FaceIterator.CurrentFaceIndex)

        nextface1 = subd.FaceIterator.NextFace()
        print(nextface1.Index)

        nextface2 = subd.FaceIterator.NextFace()
        print(subd.FaceIterator.CurrentFaceIndex)

        nextface3 = subd.FaceIterator.NextFace()
        print(subd.FaceIterator.CurrentFaceIndex)
        #print(subd.FaceIterator.CurrentFaceIndex)

        currface = subd.FaceIterator.CurrentFace()
        #print(currface.Index)

        #print(subd.FaceIterator.CurrentFaceIndex)

        #while subd.FaceIterator.NextFace():
            #print(subd.FaceIterator.NextFace().Index)

        self.assertTrue(len(subd.EdgeIterator) == 434)
        self.assertTrue(subd.EdgeIterator.EdgeCount == len(subd.EdgeIterator))

        firstedge = subd.EdgeIterator.FirstEdge()
        self.assertTrue(firstedge.VertexCount == 2)
        self.assertTrue(firstedge.Index == 1)

        lastedge = subd.EdgeIterator.LastEdge()
        self.assertTrue(lastedge.VertexCount == 2)
        self.assertTrue(lastedge.Index == subd.EdgeIterator.EdgeCount)

        nextedge = subd.EdgeIterator.NextEdge()
        self.assertTrue(nextedge.VertexCount == 2)
        self.assertTrue(nextedge.Index == 2)

        self.assertTrue(len(subd.VertexIterator) == 201)
        self.assertTrue(subd.VertexIterator.VertexCount == len(subd.VertexIterator))

        #self.assertTrue(nextface.EdgeCount == 4)
        #self.assertTrue(nextface.Index == 3)

        




if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")