import rhino3dm
import unittest
import os
import sys
import helpers
import uuid
import hashlib


def _attributes_rgb(c):
    return tuple(c)[:3]


def _attributes_draw_color_file():
    f = rhino3dm.File3dm()
    for c in ((200, 0, 0, 255), (0, 0, 200, 255)):
        m = rhino3dm.Material()
        m.DiffuseColor = c
        f.Materials.Add(m)
    layer = rhino3dm.Layer()
    layer.Name = 'L'
    layer.Color = (10, 20, 30, 255)
    layer.RenderMaterialIndex = 1
    f.Layers.Add(layer)
    pbr = rhino3dm.Material()
    pbr.DiffuseColor = (9, 9, 9, 255)
    pbr.ToPhysicallyBased()
    pbr.PhysicallyBased.BaseColor = rhino3dm.Color4f(1.0, 0.5, 0.0, 1.0)
    f.Materials.Add(pbr)
    return f


class TestAttributes(unittest.TestCase):

    def test_attributesGroupList(self):
        file3dm = rhino3dm.File3dm.Read('../models/groups.3dm')

        obj = list(file3dm.Objects)[0]

        groupList = obj.Attributes.GetGroupList()

        self.assertTrue(type(groupList) == list)
        self.assertTrue(type(groupList[0]) == int)

    def test_attributesMeshModifiersAreNoneWhenAbsent(self):
        """A new ObjectAttributes has no displacement, edge softening, thickening, curve piping or shut lining."""
        mm = rhino3dm.ObjectAttributes().File3dmMeshModifiers
        self.assertIsNone(mm.Displacement)
        self.assertIsNone(mm.EdgeSoftening)
        self.assertIsNone(mm.Thickening)
        self.assertIsNone(mm.CurvePiping)
        self.assertIsNone(mm.ShutLining)

    def test_attributesMeshModifiersOfObjectsReadFromFiles(self):
        """The mesh modifiers of objects read from files are reachable, and a present displacement reads back."""
        for name in ('subd.3dm', 'subd_creases.3dm', 'mesh.3dm'):
            f = helpers.read_model(name)
            for o in f.Objects:
                d = o.Attributes.File3dmMeshModifiers.Displacement
                if d is not None:
                    self.assertIn(d.On, (True, False))

    def test_attributesCustomMeshingParametersIsNoneUntilSet(self):
        """CustomMeshingParameters is None on new attributes and returns the parameters once set."""
        attrs = rhino3dm.ObjectAttributes()
        self.assertIsNone(attrs.CustomMeshingParameters)
        quality = rhino3dm.MeshingParameters.QualityRenderMesh
        attrs.CustomMeshingParameters = quality
        got = attrs.CustomMeshingParameters
        self.assertIsNotNone(got)
        self.assertEqual(got.RelativeTolerance, quality.RelativeTolerance)

    def test_attributesGetGroupListIsNoneWithoutGroups(self):
        """GetGroupList returns None when the object is in no group."""
        self.assertIsNone(rhino3dm.ObjectAttributes().GetGroupList())

    def test_attributesUserStringsSetAndGet(self):
        """SetUserString stores a key/value that GetUserString and UserStringCount see."""
        a = rhino3dm.ObjectAttributes()
        self.assertIs(a.SetUserString('k', 'v'), True)
        self.assertEqual(a.GetUserString('k'), 'v')
        self.assertEqual(a.UserStringCount, 1)

    def test_objectAttributesMeshModifiersCreateThickening(self):
        """File3dmMeshModifiers.Thickening is None until CreateThickening makes one."""
        mm = rhino3dm.ObjectAttributes().File3dmMeshModifiers
        self.assertIsNone(mm.Thickening)
        self.assertIsNotNone(mm.CreateThickening())
        self.assertIsNotNone(mm.Thickening)

    def test_shutLiningCurvesSurviveAWriteAndRead(self):
        """A shut lining's curves and their settings, keyed by curve id, read back from a written file."""
        cid = uuid.uuid4()
        wf = rhino3dm.File3dm()
        attrs = rhino3dm.ObjectAttributes()
        sl0 = attrs.File3dmMeshModifiers.CreateShutLining()
        self.assertEqual(sl0.AddCurve(cid), cid)
        sl0.SetCurveRadius(cid, 2.5)
        sl0.SetCurveProfile(cid, 3)
        sl0.SetCurvePull(cid, True)
        sl0.SetCurveIsBump(cid, True)
        sl0.SetCurveEnabled(cid, False)
        wf.Objects.AddPoint(rhino3dm.Point3d(0, 0, 0), attrs)
        with helpers.TempDir() as d:
            out = os.path.join(d, 'shutlining.3dm')
            self.assertTrue(wf.Write(out, 0))
            o = next(iter(rhino3dm.File3dm.Read(out).Objects))
        sl = o.Attributes.File3dmMeshModifiers.ShutLining
        self.assertIsNotNone(sl)
        self.assertEqual([str(x) for x in sl.GetCurves()], [str(cid)])
        self.assertEqual((sl.CurveRadius(cid), sl.CurveProfile(cid), sl.CurvePull(cid), sl.CurveIsBump(cid), sl.CurveEnabled(cid)),
                         (2.5, 3, True, True, False))

    def test_objectAttributesDrawColorByLayerAndObject(self):
        """DrawColor(file) is the layer's color when colored by layer and ObjectColor when colored by object."""
        f = _attributes_draw_color_file()
        a = rhino3dm.ObjectAttributes()
        a.LayerIndex = 0
        a.ColorSource = rhino3dm.ObjectColorSource.ColorFromLayer
        self.assertEqual(_attributes_rgb(a.DrawColor(f)), _attributes_rgb(f.Layers.FindIndex(0).Color))
        self.assertEqual(_attributes_rgb(a.DrawColor(f)), (10, 20, 30))
        a.ColorSource = rhino3dm.ObjectColorSource.ColorFromObject
        a.ObjectColor = (1, 2, 3, 255)
        self.assertEqual(_attributes_rgb(a.DrawColor(f)), (1, 2, 3))

    def test_objectAttributesDrawColorByMaterial(self):
        """Colored by material, DrawColor(file) is the selected material's diffuse color, or its PBR base color (D63)."""
        f = _attributes_draw_color_file()
        a = rhino3dm.ObjectAttributes()
        a.LayerIndex = 0
        a.ColorSource = rhino3dm.ObjectColorSource.ColorFromMaterial
        a.MaterialSource = rhino3dm.ObjectMaterialSource.MaterialFromObject
        a.MaterialIndex = 0
        self.assertEqual(_attributes_rgb(a.DrawColor(f)), (200, 0, 0))
        a.MaterialSource = rhino3dm.ObjectMaterialSource.MaterialFromLayer      # the layer's render material (1)
        self.assertEqual(_attributes_rgb(a.DrawColor(f)), (0, 0, 200))
        a.MaterialSource = rhino3dm.ObjectMaterialSource.MaterialFromObject
        a.MaterialIndex = 2                                                     # physically based
        self.assertIn(_attributes_rgb(a.DrawColor(f)), [(255, 127, 0), (255, 128, 0)])

    def test_objectAttributesDrawColorWithEmptyViewportId(self):
        """DrawColor(file, Guid.Empty) is DrawColor(file)."""
        f = _attributes_draw_color_file()
        a = rhino3dm.ObjectAttributes()
        a.ColorSource = rhino3dm.ObjectColorSource.ColorFromMaterial
        a.MaterialSource = rhino3dm.ObjectMaterialSource.MaterialFromObject
        a.MaterialIndex = 2
        self.assertEqual(a.DrawColor(f), a.DrawColor(f, uuid.UUID(int=0)))

    def test_objectAttributesViewportId(self):
        """ObjectAttributes.ViewportId can be set and read back."""
        at, vid = rhino3dm.ObjectAttributes(), uuid.uuid4()
        at.ViewportId = vid
        self.assertEqual(at.ViewportId, vid)

    def test_shutLiningCurveSettingsById(self):
        """A shut lining's curves are listed by id, and each curve's radius, profile, enabled, pull and bump settings read and write by id."""
        a = rhino3dm.ObjectAttributes()
        sl = a.File3dmMeshModifiers.CreateShutLining()
        sl.On = True
        cid = uuid.uuid4()
        self.assertEqual(sl.AddCurve(cid), cid)
        sl = a.File3dmMeshModifiers.ShutLining
        self.assertIs(sl.On, True)
        self.assertEqual(list(sl.GetCurves()), [cid])
        sl.SetCurveRadius(cid, 0.75); sl.SetCurveProfile(cid, 2); sl.SetCurveEnabled(cid, True)
        sl.SetCurvePull(cid, True); sl.SetCurveIsBump(cid, True)
        self.assertEqual((sl.CurveRadius(cid), sl.CurveProfile(cid), sl.CurveEnabled(cid), sl.CurvePull(cid), sl.CurveIsBump(cid)),
                         (0.75, 2, True, True, True))
        sl.SetCurveRadius(cid, 1.25); sl.SetCurveProfile(cid, 1); sl.SetCurveEnabled(cid, False)
        sl.SetCurvePull(cid, False); sl.SetCurveIsBump(cid, False)
        self.assertEqual((sl.CurveRadius(cid), sl.CurveProfile(cid), sl.CurveEnabled(cid), sl.CurvePull(cid), sl.CurveIsBump(cid)),
                         (1.25, 1, False, False, False))

    def test_meshModifiersCreateKeepsAnExistingOne(self):
        """Each File3dmMeshModifiers.Create* makes the modifier when there is none and returns the existing one otherwise."""
        mm = rhino3dm.ObjectAttributes().File3dmMeshModifiers
        self.assertIsNone(mm.ShutLining)                              # none until created
        for name in ('Displacement', 'EdgeSoftening', 'Thickening', 'CurvePiping', 'ShutLining'):
            n = getattr(mm, 'Create' + name)()
            self.assertIsNotNone(n, name)
            self.assertIsNotNone(getattr(mm, name), name)
            n.On = True
            self.assertIs(getattr(mm, 'Create' + name)().On, True, name)

    def test_shutLiningAddCurveOncePerId(self):
        """File3dmShutLining.AddCurve adds one curve per id, each with its own settings."""
        sl = rhino3dm.ObjectAttributes().File3dmMeshModifiers.CreateShutLining()
        cid, cid2 = uuid.uuid4(), uuid.uuid4()
        self.assertEqual(sl.AddCurve(cid), cid)
        self.assertEqual(sl.AddCurve(cid2), cid2)
        self.assertEqual(sl.AddCurve(cid), cid)                         # no second curve for an id
        self.assertEqual(list(sl.GetCurves()), [cid, cid2])
        sl.SetCurveRadius(cid, 2.0)
        self.assertEqual((sl.CurveRadius(cid), sl.CurveRadius(cid2)), (2.0, 1.0))

    def test_objectAttributesRdkXmlIsTheObjectsRenderXml(self):
        """ObjectAttributes.RdkXml is the object's RDK XML (here its decals)."""
        na = next(iter(helpers.read_model('sphereDecals.3dm').Objects)).Attributes
        self.assertEqual(hashlib.sha256(na.RdkXml().encode()).hexdigest(),
                         'be505faefb52c2faacf3dbf1b766542bbdb034017bd6f349edb0e8e326f200b8')

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
