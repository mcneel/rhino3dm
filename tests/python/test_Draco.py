#test for ensuring continuity of functionality when changing to Nanobind

import rhino3dm
import unittest
import base64
import os
import sys
import helpers


class TestDraco(unittest.TestCase):

    def test_DracoDecompress(self):

        file3dm = rhino3dm.File3dm.Read('../models/mesh.3dm')

        mesh = list(file3dm.Objects)[0].Geometry

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
        file.Write('decompressed.3dm', 0)

    def test_dracoCompressMeshRoundTrip(self):
        """A mesh compressed with DracoCompression decompresses from its bytes and from its base64 string."""
        m = helpers.quad_mesh()
        d = rhino3dm.DracoCompression.Compress(m)
        b = d.ToByteArray()
        self.assertIsInstance(b, bytes)
        self.assertGreater(len(b), 0)
        g = rhino3dm.DracoCompression.DecompressByteArray(b)
        self.assertIsInstance(g, rhino3dm.Mesh)
        self.assertEqual(g.Vertices.Count, 4)
        self.assertGreaterEqual(g.Faces.Count, 1)
        g64 = rhino3dm.DracoCompression.DecompressBase64String(d.ToBase64String())
        self.assertIsInstance(g64, rhino3dm.Mesh)
        self.assertEqual(g64.Vertices.Count, 4)
        self.assertGreaterEqual(d.VertexAttributePosition, 0)

    def test_dracoCompressionOptions(self):
        """DracoCompressionOptions has RhinoCommon's defaults, and leaving out normals gives no normal attribute."""
        o = rhino3dm.DracoCompressionOptions()
        self.assertEqual((o.CompressionLevel, o.PositionQuantizationBits, o.IncludeNormals), (7, 14, True))
        o.IncludeNormals = False
        o.VertexColorFormat = rhino3dm.DracoColorFormat.RGBA
        o.PositionQuantizationBits = 11
        d = rhino3dm.DracoCompression.Compress(helpers.quad_mesh(), o)
        self.assertEqual(d.NormalAttributePosition, -1)

    def test_dracoCompressPointCloud(self):
        """A point cloud compressed with DracoCompression decompresses to a point cloud of the same count."""
        pc = rhino3dm.PointCloud()
        for i in range(5):
            pc.Add(rhino3dm.Point3d(i, 2 * i, 0))
        d = rhino3dm.DracoCompression.Compress(pc)
        g = rhino3dm.DracoCompression.DecompressByteArray(d.ToByteArray())
        self.assertIsInstance(g, rhino3dm.PointCloud)
        self.assertEqual(g.Count, 5)

    def test_dracoWriteAndDecompressFile(self):
        """Write saves the compressed data, DecompressFile reads it back, and a missing file raises FileNotFoundError."""
        d = rhino3dm.DracoCompression.Compress(helpers.quad_mesh())
        with helpers.TempDir() as tmp:
            path = os.path.join(tmp, 'quad.drc')
            self.assertIs(d.Write(path), True)
            self.assertEqual(rhino3dm.DracoCompression.DecompressFile(path).Vertices.Count, 4)
            with self.assertRaises(FileNotFoundError):
                rhino3dm.DracoCompression.DecompressFile(os.path.join(tmp, 'missing.drc'))


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
