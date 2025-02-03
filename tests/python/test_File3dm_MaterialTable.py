import rhino3dm
import unittest

#objective: to test creating file with bitmaps
class TestFile3dmMaterialTable(unittest.TestCase):

    @unittest.skip("BAD CAST")
    def test_deleteMaterial(self):
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-deleteMaterial'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create materials
        mat1 = rhino3dm.Material()
        mat2 = rhino3dm.Material()

        index1 = file3dm.Materials.Add(mat1)
        index2 = file3dm.Materials.Add(mat2)

        qtyMaterials1 = len(file3dm.Materials)

        id1 = file3dm.Materials[index1].Id

        file3dm.Materials.Delete(id1)

        qtyMaterials2 = len(file3dm.Materials)

        self.assertTrue(qtyMaterials1 == 2 and qtyMaterials2 == 1)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")