"""8.x members renamed in 9.2: the replacements listed in docs/migrating-to-9/python-members.md work."""
import os
import unittest
import uuid

import rhino3dm
import helpers


class TestRenamedMembers(unittest.TestCase):
    def test_layerFlags(self):
        """Layer.Visible / Locked / Expanded are IsVisible / IsLocked / IsExpanded."""
        layer = rhino3dm.Layer()
        layer.IsVisible = False
        layer.IsLocked = True
        layer.IsExpanded = True
        self.assertEqual((layer.IsVisible, layer.IsLocked, layer.IsExpanded), (False, True, True))

    def test_renderSettings(self):
        """RenderSettings, Sun, SafeFrame, LinearWorkflow and RenderChannels members with RhinoCommon's names."""
        rs = rhino3dm.File3dm().Settings.RenderSettings
        rs.RenderBackfaces = False
        rs.RenderIsoparams = True
        rs.ShadowmapLevel = 2
        self.assertEqual((rs.RenderBackfaces, rs.RenderIsoparams, rs.ShadowmapLevel), (False, True, 2))
        self.assertIsInstance(rs.Snapshot, str)
        rs.Sun.Enabled = True
        self.assertTrue(rs.Sun.Enabled)
        rs.SafeFrame.FieldsOn = True
        self.assertTrue(rs.SafeFrame.FieldsOn)
        # opennurbs keeps one pre-process gamma switch: colors and textures read and write it both
        rs.LinearWorkflow.PreProcessColors = False
        self.assertEqual((rs.LinearWorkflow.PreProcessColors, rs.LinearWorkflow.PreProcessTextures), (False, False))
        rs.LinearWorkflow.PreProcessTextures = True
        self.assertTrue(rs.LinearWorkflow.PreProcessColors)
        self.assertTrue(all(isinstance(i, uuid.UUID) for i in rs.RenderChannels.CustomList))

    def test_objectAttributes(self):
        """ObjectAttributes.ActiveSpace / Id / GetGroupList2 are Space / ObjectId / GetGroupList."""
        a = rhino3dm.ObjectAttributes()
        self.assertEqual(a.Space, rhino3dm.ActiveSpace.ModelSpace)
        a.ObjectId = uuid.UUID(int=7)
        self.assertEqual(a.ObjectId, uuid.UUID(int=7))
        a.AddToGroup(3)
        self.assertEqual(list(a.GetGroupList()), [3])

    def test_fileGroupsAndFont(self):
        """File3dm.Groups is AllGroups; Font.StrikeOut is Strikeout."""
        f = helpers.read_model('groups.3dm')
        self.assertGreater(f.AllGroups.Count, 0)
        self.assertIsInstance(f.AllGroups[0], rhino3dm.Group)
        self.assertFalse(rhino3dm.Font('Arial').Strikeout)

    def test_dimensionStyle(self):
        """DimensionStyle.FixedExtensionLengthOn / IsFieldOverridden / TextHEight with RhinoCommon's names."""
        ds = rhino3dm.DimensionStyle()
        ds.FixedExtensionOn = True
        ds.TextHeight = 2.5
        self.assertEqual((ds.FixedExtensionOn, ds.TextHeight), (True, 2.5))
        self.assertIsInstance(ds.IsFieldOverriden(rhino3dm.DimensionStyle.Field.TextHeight), bool)

    def test_pointCloudItem(self):
        """PointCloudItem.Value is PointValue."""
        pc = rhino3dm.PointCloud()
        pc.Add(rhino3dm.Point3d(1, 2, 3), 7.5)
        self.assertEqual(pc[0].PointValue, 7.5)

    def test_nurbsSurfacePoints(self):
        """NurbsSurfacePointList: len and [u, v] are CountU / CountV and Get/SetControlPoint;
        MakeRational is on the surface."""
        srf = rhino3dm.NurbsSurface.Create(3, False, 2, 2, 3, 3)
        pts = srf.Points
        self.assertEqual((pts.CountU, pts.CountV), (3, 3))
        pts.SetControlPoint(1, 1, rhino3dm.ControlPoint(1, 2, 3))
        cp = pts.GetControlPoint(1, 1)
        self.assertEqual((cp.X, cp.Y, cp.Z), (1, 2, 3))
        self.assertTrue(srf.MakeRational())
        self.assertTrue(srf.IsRational)

    def test_embeddedFileAndInstanceDefinition(self):
        """EmbeddedFile.Write is File3dmEmbeddedFile.SaveToFile; InstanceDefinition.GetObjectIds2 is GetObjectIds."""
        ef = rhino3dm.File3dmEmbeddedFile.Read(helpers.model('mesh.3dm'))
        with helpers.TempDir() as d:
            out = os.path.join(d, 'copy.3dm')
            self.assertTrue(ef.SaveToFile(out))
            self.assertEqual(os.path.getsize(out), os.path.getsize(helpers.model('mesh.3dm')))
        idef = helpers.read_model('blocks.3dm').AllInstanceDefinitions[0]
        self.assertTrue(all(isinstance(i, uuid.UUID) for i in idef.GetObjectIds()))

    def test_environmentBackgroundImage(self):
        """Environment.BackgroundImage reads, and (rhino3dm only) sets a copy of a texture."""
        env = rhino3dm.Environment()
        self.assertIsInstance(env.BackgroundImage, rhino3dm.Texture)
        tex = rhino3dm.Texture()
        tex.FileName = 'background.png'
        env.BackgroundImage = tex
        self.assertEqual(env.BackgroundImage.FileName, 'background.png')
        tex.FileName = 'other.png'
        self.assertEqual(env.BackgroundImage.FileName, 'background.png')

    def test_renderContentAndDisplacement(self):
        """RenderTexture.FileName / RenderContent XML / Displacement.Texture are Filename / XML / TextureId."""
        f = helpers.read_model('sphereDecals.3dm')
        names = [t.Filename.replace('\\', '/').split('/')[-1] for t in f.RenderTextures]
        self.assertEqual(sorted(names), ['E6AD.jpg', 'E6EC.jpg'])
        xml = list(helpers.read_model('meshWithTexture.3dm').RenderMaterials)[0].XML(False)
        self.assertTrue(xml.lstrip().startswith('<'))
        disp = [o.Attributes.File3dmMeshModifiers.Displacement for o in helpers.read_model('subd_creases.3dm').Objects
                if o.Attributes.File3dmMeshModifiers and o.Attributes.File3dmMeshModifiers.Displacement]
        self.assertTrue(disp)
        self.assertIsInstance(disp[0].TextureId, uuid.UUID)


if __name__ == '__main__':
    unittest.main()
