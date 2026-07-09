import os
import unittest
import rhino3dm


# RH3DM-170: headless (ONX_Model based) cached texture coordinates.
#   Mesh.SetCachedTextureCoordinatesFromMaterial(file, objectId, material)
#   Mesh.GetCachedTextureCoordinatesFromTexture(file, objectId, texture)
# The RhinoCommon overloads take a RhinoObject/RhinoDoc (RHINO_SDK only); the
# rhino3dm variants resolve the object's texture mappings from the File3dm.


def _make_mesh():
    mesh = rhino3dm.Mesh()
    mesh.Vertices.Add(0.0, 0.0, 0.0)
    mesh.Vertices.Add(10.0, 0.0, 0.0)
    mesh.Vertices.Add(10.0, 10.0, 0.0)
    mesh.Vertices.Add(0.0, 10.0, 0.0)
    mesh.Faces.AddFace(0, 1, 2, 3)
    return mesh


def _make_textured_material():
    material = rhino3dm.Material()
    material.Name = "Textured"
    texture = rhino3dm.Texture()
    texture.FileName = "fake_texture.png"
    material.SetBitmapTexture(texture)
    return material


def _fixture_path():
    here = os.path.dirname(__file__)
    for rel in ("../models/meshWithTexture.3dm", "models/meshWithTexture.3dm"):
        candidate = os.path.normpath(os.path.join(here, rel))
        if os.path.exists(candidate):
            return candidate
    return None


class TestMeshCachedTextureCoordinates(unittest.TestCase):

    def test_methods_are_callable_and_null_safe(self):
        # Exercises the full marshalling path (File3dm + objectId + material/texture)
        # and the null-safe return when nothing is cached.
        file3dm = rhino3dm.File3dm()
        mesh = _make_mesh()
        material = _make_textured_material()
        file3dm.Materials.Add(material)

        attributes = rhino3dm.ObjectAttributes()
        attributes.MaterialSource = rhino3dm.ObjectMaterialSource.MaterialFromObject
        object_id = file3dm.Objects.AddMesh(mesh, attributes)

        # Should run without throwing regardless of whether coordinates get cached.
        result = mesh.SetCachedTextureCoordinatesFromMaterial(file3dm, object_id, material)
        self.assertIn(result, (True, False))

        texture = material.GetBitmapTexture()
        self.assertIsNotNone(texture)

        tc = mesh.GetCachedTextureCoordinatesFromTexture(file3dm, object_id, texture)
        # Without object-level texture mappings there is nothing to cache -> None.
        # If something was cached it must be a well-formed set over all vertices.
        if tc is not None:
            self.assertEqual(len(tc), len(mesh.Vertices))
            self.assertIn(tc.Dimension, (2, 3))

    def test_get_with_unrelated_texture_returns_none(self):
        file3dm = rhino3dm.File3dm()
        mesh = _make_mesh()
        object_id = file3dm.Objects.AddMesh(mesh, None)

        unrelated = rhino3dm.Texture()
        unrelated.FileName = "unrelated.png"
        tc = mesh.GetCachedTextureCoordinatesFromTexture(file3dm, object_id, unrelated)
        self.assertIsNone(tc)

    @unittest.skipIf(_fixture_path() is None, "meshWithTexture.3dm fixture not present")
    def test_positive_path_from_fixture(self):
        # A Rhino-authored file with a mesh object that has a textured material and a
        # texture mapping. Verifies coordinates are actually produced and read back.
        file3dm = rhino3dm.File3dm.Read(_fixture_path())

        found = False
        for obj in file3dm.Objects:
            geometry = obj.Geometry
            if not isinstance(geometry, rhino3dm.Mesh):
                continue
            material = file3dm.Materials.FindIndex(obj.Attributes.MaterialIndex)
            if material is None:
                continue
            texture = material.GetBitmapTexture()
            if texture is None:
                continue

            geometry.SetCachedTextureCoordinatesFromMaterial(file3dm, obj.Attributes.Id, material)
            tc = geometry.GetCachedTextureCoordinatesFromTexture(file3dm, obj.Attributes.Id, texture)
            if tc is not None:
                found = True
                self.assertEqual(len(tc), len(geometry.Vertices))
                self.assertIn(tc.Dimension, (2, 3))
                success, u, v, w = tc.TryGetAt(0)
                self.assertTrue(success)
                self.assertTrue(all(isinstance(c, float) for c in (u, v, w)))

        self.assertTrue(found, "fixture present but no cached texture coordinates were produced")


if __name__ == "__main__":
    unittest.main()
