import rhino3dm
import unittest

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
        tex.TextureType = int(rhino3dm.TextureType.PBR_Roughness)
        mat.SetTexture(tex)

        roughness = mat.GetTexture(rhino3dm.TextureType.PBR_Roughness)
        self.assertIsNotNone(roughness)
        self.assertEqual(roughness.TextureType, rhino3dm.TextureType.PBR_Roughness)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
