import unittest
import rhino3dm

class TestMesh(unittest.TestCase):

    file3dm = rhino3dm.File3dm.Read('../models/mesh.3dm')
    mesh = list(file3dm.Objects)[0].Geometry
    attr = list(file3dm.Objects)[0].Attributes

    def test_meshIsManifold(self):

        isManifold = self.mesh.IsManifold(True)

        self.assertTrue(type(isManifold) == tuple)
        self.assertTrue(len(isManifold) == 3)
        self.assertTrue(type(isManifold[0]) == bool)
        self.assertTrue(type(isManifold[1]) == bool)
        self.assertTrue(type(isManifold[2]) == bool)

        self.assertTrue(isManifold[0] == True) #result
        self.assertTrue(isManifold[1] == True) #oriented
        self.assertTrue(isManifold[2] == False) #has boundary

    def test_meshGetFace(self):

        # RhinoCommon's face list yields MeshFace (A, B, C, D); 8.x gave a 4-tuple
        face = self.mesh.Faces[0]

        self.assertTrue(type(face) == rhino3dm.MeshFace)
        self.assertTrue(all(type(i) == int for i in (face.A, face.B, face.C, face.D)))

    def test_meshGetFaceVertices(self):

        faceVertices = self.mesh.Faces.GetFaceVertices(0)

        self.assertTrue(type(faceVertices) == tuple)
        self.assertTrue(len(faceVertices) == 5)
        self.assertTrue(type(faceVertices[0]) == bool)
        self.assertTrue(type(faceVertices[1]) == rhino3dm.Point3f)
        self.assertTrue(type(faceVertices[2]) == rhino3dm.Point3f)
        self.assertTrue(type(faceVertices[3]) == rhino3dm.Point3f)
        self.assertTrue(type(faceVertices[4]) == rhino3dm.Point3f)

    @unittest.skip("Not implemented")
    def test_meshCachedTextureCoordinates_TryGetAt(self):

        ctc = self.mesh.GetCachedTextureCoordinates(0)

        print(ctc)

    

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
