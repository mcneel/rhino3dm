import unittest
import rhino3dm
import helpers


# A SubD whose UVs come from a cylinder mapping on channel 1 (Rhino's projection mapping,
# not face texture points). The control net carries no texture coordinates of its own; the
# mapping is read from the file and applied to the control-net mesh.
# https://discourse.mcneel.com/t/rhino3dm-js-subds-uv-map-information/223518


def _subd_object(doc):
    for obj in doc.Objects:
        if isinstance(obj.Geometry, rhino3dm.SubD):
            return obj
    return None


class TestSubDTextureMapping(unittest.TestCase):
    def test_subdUVsComeFromTheMapping(self):
        doc = helpers.read_model('subduv.3dm')
        obj = _subd_object(doc)
        self.assertIsNotNone(obj)
        subd = obj.Geometry
        self.assertEqual(subd.TextureCoordinateType, rhino3dm.SubDTextureCoordinateType.FromMapping)

        # no face texture points: the control net alone has no UVs
        net = rhino3dm.Mesh.CreateFromSubDControlNetWithTextureCoordinates(subd)
        self.assertEqual(net.TextureCoordinates.Count, 0)

    def test_cylinderMappingAppliesToControlNetMesh(self):
        doc = helpers.read_model('subduv.3dm')
        obj = _subd_object(doc)

        self.assertIsNone(obj.GetTextureMapping(0)[0])
        mapping, xform = obj.GetTextureMapping(1)
        self.assertIsNotNone(mapping)
        self.assertEqual(mapping.MappingType, rhino3dm.TextureMappingType.CylinderMapping)

        mesh = rhino3dm.Mesh.CreateFromSubDControlNet(obj.Geometry)
        mesh.SetTextureCoordinates(mapping, xform, False)
        tc = mesh.TextureCoordinates
        # the seam splits vertices, so the count is the mesh's after the call
        self.assertGreater(tc.Count, 0)
        self.assertEqual(tc.Count, mesh.Vertices.Count)
        for i in range(tc.Count):
            p = tc[i]
            self.assertTrue(0.0 <= p.X <= 1.0)
            self.assertTrue(0.0 <= p.Y <= 1.0)


if __name__ == '__main__':
    unittest.main()
