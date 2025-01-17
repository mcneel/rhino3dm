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
            print("Edge Index: " + str(edge.Index))
            print("Edge Start Vertex: " + str(edge.Vertex(0).Index))
            print("Edge Start Coords: " + str(edge.Vertex(0).SurfacePoint))
            print("Edge End Vertex: " + str(edge.Vertex(1).Index))

        self.assertTrue(len(subd.VertexIterator) == 201)
        self.assertTrue(subd.VertexIterator.VertexCount == len(subd.VertexIterator))

        #for vertex in subd.VertexIterator:
            # print("Vertex Index: " + str(vertex.Index))
            # #print(vertex.Next().Index) #segfault
            # #print(vertex.Previous().Index) #segfault
            # print("Control Net Pt: " + str(vertex.ControlNetPoint))
            # print("Surface Pt: " + str(vertex.SurfacePoint))
            # print("Sharpness: " + str(vertex.VertexSharpness))
            # print("IsSharp(T): " + str(vertex.IsSharp(True)))
            # print("IsSharp(F): " + str(vertex.IsSharp(True)))
            # print("IsSmooth: " + str(vertex.IsSmooth))
            # print("IsCrease: " + str(vertex.IsCrease))
            # print("IsDart: " + str(vertex.IsDart))
            # print("IsCorner: " + str(vertex.IsCorner))

            # for i in range(vertex.EdgeCount):
            #     print("Edge " + str(i) + " index: " + str(vertex.Edge(i).Index))
            # print('-------')

        

        

        




if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")