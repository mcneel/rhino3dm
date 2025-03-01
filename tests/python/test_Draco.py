#test for ensuring continuity of functionality when changing to Nanobind

import rhino3dm
import unittest
import base64

class TestDraco(unittest.TestCase):

    def test_DracoDecompress(self):

        file3dm = rhino3dm.File3dm.Read('../models/mesh.3dm')

        mesh = file3dm.Objects[0].Geometry

        face_count = len(mesh.Faces)
        vertex_count = len(mesh.Vertices)
        color = mesh.VertexColors[0]
        vertex = mesh.Vertices[0]

        options = rhino3dm.DracoCompressionOptions()
        options.IncludeVertexColors = True

        draco = rhino3dm.DracoCompression.Compress(mesh, options)

        b64 = draco.ToBase64String()

        decompressed_b64_mesh = rhino3dm.DracoCompression.DecompressBase64String(b64)

        self.assertTrue(face_count == len(decompressed_b64_mesh.Faces))
        self.assertTrue(vertex_count == len(decompressed_b64_mesh.Vertices))

        b64_encoded = b64.encode('ascii')
        ba = base64.b64decode(b64_encoded)
        
        decompressed_ba_mesh = rhino3dm.DracoCompression.DecompressByteArray(ba)

        self.assertTrue(face_count == len(decompressed_ba_mesh.Faces))
        self.assertTrue(vertex_count == len(decompressed_ba_mesh.Vertices))
        color2 = decompressed_ba_mesh.VertexColors[0]
        vertex2 = decompressed_ba_mesh.Vertices[0]


        #note: colors might not be the same

        
        #self.assertTrue(color == decompressed_b64_mesh.VertexColors[0])

        file = rhino3dm.File3dm()
        file.Objects.AddMesh(decompressed_b64_mesh)
        file.Write('decompressed.3dm')


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")