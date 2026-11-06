import rhino3dm
import unittest
import os
import sys
import helpers
import uuid

# objective: cover Material texture-type behavior.
# Regression guard for the SetTextureHelper bug where the Texture-object setters
# (SetBumpTexture/SetEnvironmentTexture/SetTransparencyTexture) forced every
# texture to the Bitmap type instead of the channel they belong to, plus coverage
# for the generic SetTexture, which must honor the texture's own TextureType.


class TestMaterial(unittest.TestCase):

    def test_setBumpTexture_keepsBumpType(self):
        # The Texture-object overload of SetBumpTexture used to store the texture as
        # Bitmap. It must round-trip as a Bump texture, and must NOT leak in as Bitmap.
        mat = rhino3dm.Material()
        tex = rhino3dm.Texture()
        tex.FileName = 'bump.png'
        mat.SetBumpTexture(tex)

        bump = mat.GetBumpTexture()
        self.assertIsNotNone(bump)
        self.assertEqual(bump.TextureType, rhino3dm.TextureType.Bump)
        self.assertIsNone(mat.GetBitmapTexture())

    def test_setTexture_honorsTextureType(self):
        # The generic SetTexture assigns using the texture's own TextureType, so any
        # channel can be set - here a PBR roughness map.
        mat = rhino3dm.Material()
        tex = rhino3dm.Texture()
        tex.FileName = 'roughness.png'
        tex.TextureType = rhino3dm.TextureType.PBR_Roughness
        mat.SetTexture(tex, tex.TextureType)     # RhinoCommon names the channel explicitly

        roughness = mat.GetTexture(rhino3dm.TextureType.PBR_Roughness)
        self.assertIsNotNone(roughness)
        self.assertEqual(roughness.TextureType, rhino3dm.TextureType.PBR_Roughness)

    def test_materialPhysicallyBasedIsNoneUntilConverted(self):
        """Material.PhysicallyBased is None until ToPhysicallyBased(), then a view onto the material."""
        m = rhino3dm.Material()
        self.assertIsNone(m.PhysicallyBased)
        m.ToPhysicallyBased()
        pbr = m.PhysicallyBased
        pbr.Metallic = 0.25
        self.assertEqual(m.PhysicallyBased.Metallic, 0.25)

    def test_materialMaxShineIs255(self):
        """Material.MaxShine is 255."""
        self.assertEqual(rhino3dm.Material.MaxShine, 255.0)

    def test_materialBitmapTextureIsACopyOnWriteView(self):
        """GetBitmapTexture returns the material's texture; changing it does not change the material until SetBitmapTexture."""
        m = rhino3dm.Material()
        self.assertIsNone(m.GetBitmapTexture())
        t = rhino3dm.Texture()
        t.FileName = 'a.png'
        self.assertTrue(m.SetBitmapTexture(t))
        v = m.GetBitmapTexture()
        self.assertEqual(v.FileName, 'a.png')
        v.FileName = 'c.png'
        self.assertEqual(v.FileName, 'c.png')
        self.assertEqual(m.GetBitmapTexture().FileName, 'a.png')
        # to change the material's texture, set it back, as in RhinoCommon
        self.assertTrue(m.SetBitmapTexture(v))
        self.assertEqual(m.GetBitmapTexture().FileName, 'c.png')

    def test_materialPhysicallyBasedBaseColorIsAColor4f(self):
        """A physically based material's BaseColor reads back the Color4f it was set to."""
        c = rhino3dm.Color4f(0.25, 0.5, 0.75, 1.0)
        m = rhino3dm.Material()
        m.ToPhysicallyBased()
        pbr = m.PhysicallyBased
        pbr.BaseColor = c
        self.assertEqual(pbr.BaseColor, c)

    def test_materialToPhysicallyBasedEmission(self):
        """ToPhysicallyBased makes a material physically based, and its Emission color can be set."""
        m = rhino3dm.Material()
        m.ToPhysicallyBased()
        self.assertIs(m.IsPhysicallyBased, True)
        m.PhysicallyBased.Emission = rhino3dm.Color4f(1.0, 0.5, 0.25, 1.0)
        e = m.PhysicallyBased.Emission
        self.assertEqual((round(e.R, 6), round(e.G, 6), round(e.B, 6)), (1.0, 0.5, 0.25))

    def test_materialSetTextureByType(self):
        """SetTexture(texture, type) sets the texture, and GetTexture(type) reads it back."""
        m, tex = rhino3dm.Material(), rhino3dm.Texture()
        tex.FileName = '/tmp/bump.png'
        tex.TextureType = rhino3dm.TextureType.Bump
        self.assertIs(m.SetTexture(tex, tex.TextureType), True)
        self.assertEqual(m.GetTexture(rhino3dm.TextureType.Bump).FileName, '/tmp/bump.png')

    def test_materialRenderMaterialInstanceId(self):
        """RenderMaterialInstanceId starts nil and can be set; setting it makes the universal render engine the plug-in."""
        m = rhino3dm.Material()
        self.assertEqual(m.RenderMaterialInstanceId, uuid.UUID(int=0))
        uid = uuid.uuid4()
        m.RenderMaterialInstanceId = uid
        self.assertEqual(m.RenderMaterialInstanceId, uid)
        self.assertEqual(str(m.RenderPlugInId), '99999999-9999-9999-9999-999999999999')

    def test_materialCompareAppearance(self):
        """CompareAppearance ignores names, and orders materials with a different diffuse color antisymmetrically."""
        a, b = rhino3dm.Material(), rhino3dm.Material()
        a.Name, b.Name = 'one', 'two'
        self.assertEqual(rhino3dm.Material.CompareAppearance(a, b), 0)
        b.DiffuseColor = (255, 0, 0, 255)
        c = rhino3dm.Material.CompareAppearance(a, b)
        self.assertNotEqual(c, 0)
        self.assertEqual(rhino3dm.Material.CompareAppearance(b, a), -c)
        self.assertGreater(c, 0)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
