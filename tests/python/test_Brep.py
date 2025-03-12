import rhino3dm
import unittest

class TestBrep(unittest.TestCase):

    #objective: 
    def test_brepBrepVertex_GetEdgeIndices(self):
        
        sphere = rhino3dm.Sphere( rhino3dm.Point3d(0,0,0), 5)
        bbox = sphere.ToBrep().GetBoundingBox()
        brep = rhino3dm.Brep.CreateFromBoundingBox(bbox)
        vertex = brep.Vertices[0]
        get_edge_indices = vertex.EdgeIndices()

        self.assertTrue(type(get_edge_indices) == list)
        self.assertEqual(len(get_edge_indices), 3)
        self.assertTrue(type(get_edge_indices[0]) == int)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")