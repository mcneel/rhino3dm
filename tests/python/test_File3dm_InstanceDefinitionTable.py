import rhino3dm
import json
import unittest

#objective: to test creating file with bitmaps
class TestFile3dmInstanceDefinitionTable(unittest.TestCase):

    def test_addInstanceDefinition(self):
        json_file = '../models/badMesh.json'
        with open(json_file) as json_data:
            data = json.load(json_data)

        #print(data[0]['vertices'])

        meshes = []
        file = rhino3dm.File3dm()

        for mesh in data:
            m = rhino3dm.Mesh()
            #print(mesh['vertices'])
            for vertex in mesh['vertices']:
                #print(vertex)
                m.Vertices.Add(vertex['x'], vertex['y'], vertex['z'])
            for face in mesh['faces']:
                m.Faces.AddFace(face[0], face[1], face[2])
            meshes.append(m)
            mm = (m,)
            oa = (rhino3dm.ObjectAttributes(),)

            # const blockId = model.instanceDefinitions().add(`block ${cnt}`, '', '', '', [0, 0, 0], [m], [new rhino.ObjectAttributes()])
            file.InstanceDefinitions.Add("block", "", "", "", rhino3dm.Point3d(0,0,0), mm, oa)

        file.Write('meshesFromJson.3dm')
        
        

                

        
if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")