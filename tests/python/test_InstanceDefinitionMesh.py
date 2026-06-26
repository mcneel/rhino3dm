import unittest
import rhino3dm

# RH-86691: adding instance definitions whose objects carry meshes used to corrupt memory
# (a by-value copy of ObjectAttributes freed the shared ON_3dmObjectAttributes). The crash
# was JS/WASM-only (pybind returns a reference, embind materializes a by-value copy), so this
# is a parity/regression guard rather than a crash repro. Exercises the deep-copy copy ctor
# of BND_3dmObjectAttributes via File3dmInstanceDefinitionTable.Add.
class TestInstanceDefinitionMesh(unittest.TestCase):

    @staticmethod
    def _make_mesh(n):
        mesh = rhino3dm.Mesh()
        for i in range(n):
            mesh.Vertices.Add((i % 100) * 0.1, (i // 100) * 0.1, (i % 5) * 0.05)
        i = 0
        while i + 3 < n:
            mesh.Faces.AddFace(i, i + 1, i + 2, i + 3)
            i += 4
        return mesh

    def test_addMultipleInstanceDefinitionsWithMeshes(self):
        file3dm = rhino3dm.File3dm()
        for k in range(3):
            mesh = self._make_mesh(20000)
            attr = rhino3dm.ObjectAttributes()
            idx = file3dm.InstanceDefinitions.Add(
                "idef" + str(k), "", "", "",
                rhino3dm.Point3d(0, 0, 0), (mesh,), (attr,))
            self.assertGreaterEqual(idx, 0)
        self.assertEqual(len(file3dm.InstanceDefinitions), 3)


if __name__ == '__main__':
    unittest.main()
