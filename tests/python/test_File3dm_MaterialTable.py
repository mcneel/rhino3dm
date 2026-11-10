import rhino3dm
import unittest
import os
import sys
import helpers

#objective: to test creating file with bitmaps


class TestFile3dmMaterialTable(unittest.TestCase):

    def test_deleteMaterial(self):
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-deleteMaterial'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create materials
        mat1 = rhino3dm.Material()
        mat2 = rhino3dm.Material()

        # RhinoCommon's table Add returns nothing; Delete takes the item or its index (D14)
        file3dm.Materials.Add(mat1)
        file3dm.Materials.Add(mat2)

        qtyMaterials1 = len(file3dm.Materials)

        file3dm.Materials.Delete(file3dm.Materials[0])

        qtyMaterials2 = len(file3dm.Materials)

        self.assertTrue(qtyMaterials1 == 2 and qtyMaterials2 == 1)

    def test_materialTableFindIdSkipsDeletedMaterials(self):
        """Materials.FindId finds a material by id, and returns None once it is deleted."""
        f = rhino3dm.File3dm()
        f.Materials.Add(rhino3dm.Material())
        mi = f.Materials.Count - 1
        mid = f.Materials.FindIndex(mi).Id
        self.assertIsNotNone(f.Materials.FindId(mid))
        f.Materials.Delete(mi)
        self.assertIsNone(f.Materials.FindId(mid))

    def test_materialTableFindFromAttributes(self):
        """FindFromAttributes resolves an object's material: the default material by default, a model material through the object."""
        f = rhino3dm.File3dm()
        a = rhino3dm.ObjectAttributes()
        m = f.AllMaterials.FindFromAttributes(a)
        self.assertIsInstance(m, rhino3dm.Material)
        self.assertEqual(m.Index, -1)
        self.assertEqual(str(m.Id).lower(), 'cf2be1de-b81c-4d69-9418-dde91d266086')      # ON_Material::Default
        mat = rhino3dm.Material()
        mat.Name = 'red'
        mi = f.AllMaterials.AddMaterial(mat)
        a.MaterialSource = rhino3dm.ObjectMaterialSource.MaterialFromObject
        a.MaterialIndex = mi
        self.assertEqual(f.AllMaterials.FindFromAttributes(a).Name, 'red')

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
