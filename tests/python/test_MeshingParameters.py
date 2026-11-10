import os
import sys
import unittest
import rhino3dm
import helpers

_MESHING_PARAMETERS_8X_JSON = {
    'TextureRange': 2, 'JaggedSeams': False, 'RefineGrid': True, 'SimplePlanes': False, 'ComputeCurvature': False,
    'ClosedObjectPostProcess': False, 'GridMinCount': 0, 'GridMaxCount': 0, 'GridAngle': 0.3490658503988659,
    'GridAspectRatio': 6.0, 'GridAmplification': 1.0, 'Tolerance': 0.0, 'MinimumTolerance': 0.0,
    'RelativeTolerance': 0.0, 'MinimumEdgeLength': 0.0001, 'MaximumEdgeLength': 0.0, 'RefineAngle': 0.3490658503988659}


class TestMeshingParameters(unittest.TestCase):
    def test_meshingParametersPresetsAreMeshingParameters(self):
        """The static presets (FastRenderMesh, QualityRenderMesh, DefaultAnalysisMesh) are MeshingParameters."""
        self.assertIsInstance(rhino3dm.MeshingParameters.FastRenderMesh, rhino3dm.MeshingParameters)
        self.assertIsInstance(rhino3dm.MeshingParameters.QualityRenderMesh, rhino3dm.MeshingParameters)
        self.assertIsInstance(rhino3dm.MeshingParameters.DefaultAnalysisMesh.RelativeTolerance, float)

    def test_meshingParametersPresetsHaveRhinoCommonsValues(self):
        """MeshingParameters constructs, and the Default, Minimal and Smooth presets carry RhinoCommon's settings."""
        MP = rhino3dm.MeshingParameters
        self.assertIsInstance(MP(), MP)
        self.assertIsInstance(MP.Default, MP)
        minimal = MP.Minimal
        self.assertEqual(minimal.GridMinCount, 16)
        self.assertIs(minimal.JaggedSeams, True)
        self.assertIs(minimal.SimplePlanes, False)
        self.assertIs(MP.Smooth.SimplePlanes, True)

    def test_meshingParametersCopyFrom(self):
        """CopyFrom copies another MeshingParameters' settings."""
        mp = rhino3dm.MeshingParameters()
        mp.CopyFrom(rhino3dm.MeshingParameters.QualityRenderMesh)
        self.assertEqual(mp.GridAngle, rhino3dm.MeshingParameters.QualityRenderMesh.GridAngle)

    def test_meshingParametersEncodeIsThe8xJsonForm(self):
        """MeshingParameters().Encode() is the 8.x JSON dict, key for key."""
        self.assertEqual(rhino3dm.MeshingParameters().Encode(), _MESHING_PARAMETERS_8X_JSON)

    def test_meshingParametersEncodeDecodeRoundTrip(self):
        """Encode writes the edited settings, and Decode reads every key back."""
        mp = rhino3dm.MeshingParameters()
        mp.JaggedSeams, mp.RefineGrid, mp.GridMinCount, mp.GridMaxCount = True, False, 17, 300
        mp.Tolerance, mp.MinimumEdgeLength, mp.MaximumEdgeLength, mp.RefineAngle = 0.25, 0.01, 12.5, 0.3
        d = mp.Encode()
        self.assertEqual((d['JaggedSeams'], d['RefineGrid'], d['GridMinCount'], d['GridMaxCount']), (True, False, 17, 300))
        self.assertEqual((d['Tolerance'], d['MinimumEdgeLength'], d['MaximumEdgeLength'], d['RefineAngle']), (0.25, 0.01, 12.5, 0.3))
        self.assertEqual(set(d), set(_MESHING_PARAMETERS_8X_JSON))
        back = rhino3dm.MeshingParameters.Decode(d)
        self.assertIsInstance(back, rhino3dm.MeshingParameters)
        self.assertEqual(back.Encode(), d)       # GridMinCount stays an int

    def test_meshingParametersConstructFromDensity(self):
        """MeshingParameters(density[, minimumEdgeLength]) sets the minimum edge length when given."""
        self.assertEqual(rhino3dm.MeshingParameters(0.5).MinimumEdgeLength,
                         rhino3dm.MeshingParameters(0.5, -1.23432101234321e+308).MinimumEdgeLength)
        self.assertEqual(rhino3dm.MeshingParameters(0.5, 0.25).MinimumEdgeLength, 0.25)

    def test_meshingParametersCopyConstructorCopies(self):
        """MeshingParameters(other) is an independent copy."""
        mp2 = rhino3dm.MeshingParameters(0.5, 0.25)
        mp3 = rhino3dm.MeshingParameters(mp2)
        mp2.MinimumEdgeLength = 1.0
        self.assertEqual(mp3.MinimumEdgeLength, 0.25)


if __name__ == '__main__':
    unittest.main()
