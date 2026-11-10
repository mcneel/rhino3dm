import os
import unittest
import rhino3dm
import sys
import helpers
import uuid


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
        # D65: RhinoCommon's setter returns nothing
        self.assertIsNone(mesh.SetCachedTextureCoordinatesFromMaterial(file3dm, object_id, material))

        texture = material.GetBitmapTexture()
        self.assertIsNotNone(texture)

        tc = mesh.GetCachedTextureCoordinates(file3dm, object_id, texture)
        # Without object-level texture mappings there is nothing to cache -> None.
        # If something was cached it must be a well-formed set over all vertices.
        if tc is not None:
            self.assertEqual(len(tc), len(mesh.Vertices))
            self.assertIn(tc.Dim, (2, 3))

    def test_get_with_unrelated_texture_returns_none(self):
        file3dm = rhino3dm.File3dm()
        mesh = _make_mesh()
        object_id = file3dm.Objects.AddMesh(mesh, None)

        unrelated = rhino3dm.Texture()
        unrelated.FileName = "unrelated.png"
        tc = mesh.GetCachedTextureCoordinates(file3dm, object_id, unrelated)
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

            geometry.SetCachedTextureCoordinatesFromMaterial(file3dm, obj.Attributes.ObjectId, material)
            tc = geometry.GetCachedTextureCoordinates(file3dm, obj.Attributes.ObjectId, texture)
            if tc is not None:
                found = True
                self.assertEqual(len(tc), len(geometry.Vertices))
                self.assertIn(tc.Dim, (2, 3))
                success, u, v, w = tc.TryGetAt(0)
                self.assertTrue(success)
                self.assertTrue(all(isinstance(c, float) for c in (u, v, w)))

        self.assertTrue(found, "fixture present but no cached texture coordinates were produced")

    def test_cachedTextureCoordinatesAreAReadOnlyViewIntoTheMesh(self):
        """GetCachedTextureCoordinates returns None for an unknown mapping id, and a read-only view for a cached one (D27)."""
        f = helpers.read_model('meshWithTexture.3dm')
        meshes = [o.Geometry for o in f.Objects if isinstance(o.Geometry, rhino3dm.Mesh)]
        self.assertTrue(meshes, 'no mesh in meshWithTexture.3dm')
        self.assertIsNone(meshes[0].GetCachedTextureCoordinates(uuid.uuid4()))
        # a cache, when the file carries one under one of its ids
        ids = {o.Attributes.ObjectId for o in f.Objects}
        for m in meshes:
            for mid in ids:
                tc = m.GetCachedTextureCoordinates(mid)
                if tc is not None:
                    self.assertIs(tc.IsReadOnly, True)
                    self.assertGreaterEqual(tc.Count, 0)
                    self.assertIn(tc.Dim, (2, 3))
                    self.assertEqual(tc.TryGetAt(-1), (False, -1.0, -1.0, -1.0))   # RhinoCommon's -1 defaults
                    return

    def test_meshCachedTextureCoordinatesFromMaterial(self):
        """SetCachedTextureCoordinatesFromMaterial caches coordinates that GetCachedTextureCoordinates then returns."""
        f = helpers.read_model('meshWithTexture.3dm')
        meshes = [(k, o) for k, o in enumerate(f.Objects) if isinstance(o.Geometry, rhino3dm.Mesh)]
        self.assertEqual([k for k, _ in meshes], [0])
        for _, o in meshes:
            mat = f.Materials.FindIndex(o.Attributes.MaterialIndex)
            self.assertIsNotNone(mat)
            self.assertIsNotNone(mat.GetBitmapTexture())
            mesh = o.Geometry
            self.assertIsNone(mesh.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat))
            tc = mesh.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture())
            self.assertIsNotNone(tc)
            self.assertEqual(tc.Count, 92)

    def test_cachedTextureCoordinatesIndexOfAndContains(self):
        """CachedTextureCoordinates.IndexOf(point) and Contains(point) find a texture coordinate by value."""
        f = helpers.read_model('meshWithTexture.3dm')
        o = next(o for o in f.Objects if isinstance(o.Geometry, rhino3dm.Mesh))
        mat = f.Materials.FindIndex(o.Attributes.MaterialIndex)
        m = o.Geometry                       # one reference: each .Geometry is a new copy
        m.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat)
        tc = m.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture())
        q = tc[3]
        self.assertEqual(tc.IndexOf(q), 3)
        self.assertIs(tc.Contains(q), True)
        self.assertIs(tc.Contains(rhino3dm.Point3d(9e9, 0, 0)), False)

    def test_meshSetCachedTextureCoordinatesReturnsTheTransform(self):
        """SetCachedTextureCoordinates(mapping, transform) uses the caller's transform and returns it, as C#'s ref argument."""
        mesh = helpers.quad_mesh()
        I = rhino3dm.Interval
        tm = rhino3dm.TextureMapping.CreatePlaneMapping(rhino3dm.Plane.WorldXY, I(0, 1), I(0, 1), I(0, 1))
        xf = rhino3dm.Transform.Translation(rhino3dm.Vector3d(0.5, 0, 0))
        (back,) = mesh.SetCachedTextureCoordinates(tm, xf)
        self.assertTrue(rhino3dm.Transform.Equals(back, xf))
        xs = [c.X for c in mesh.GetCachedTextureCoordinates(tm.Id)]
        self.assertGreater(len(xs), 0)


if __name__ == "__main__":
    unittest.main()
