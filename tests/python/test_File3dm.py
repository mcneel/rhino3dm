import rhino3dm
import unittest
import os
import sys
import helpers
import datetime
import uuid
import base64
import hashlib


def _file3dm_sha(s):
    return hashlib.sha256(s.encode()).hexdigest()


_FILE3DM_STUFF_RDK_SHA = '9f8be1e4d91bee9b901a1d0d0a72e454bf6045bd4c6075e295b4c95e0370e6ba'


class TestFile3dmTables(unittest.TestCase):
    #objective: to test the number of objects in the file3dm tables
    def test_read3dmTables(self):

        file3dm = rhino3dm.File3dm.Read('../models/file3dm_stuff.3dm')

        objectTableCnt = len(file3dm.Objects)
        layerTableCnt = len(file3dm.Layers)
        materialsTableCnt = len(file3dm.Materials)
        linetypeTableCnt = len(file3dm.Linetypes)
        bitmapsTableCnt = len(file3dm.Bitmaps)
        groupTableCnt = len(file3dm.AllGroups)
        idTableCnt = len(file3dm.InstanceDefinitions)
        viewTableCnt = len(file3dm.Views)
        namedViewsTableCnt = len(file3dm.NamedViews)
        pluginDataTableCnt = len(file3dm.PlugInData)
        stringTableCnt = file3dm.Strings.Count       # RhinoCommon's string table: Count, no indexer
        embeddedFileTableCnt = len(file3dm.EmbeddedFiles)
        # one collection per kind of render content
        renderContentTableCnt = sum(len(list(c)) for c in (file3dm.RenderMaterials, file3dm.RenderEnvironments, file3dm.RenderTextures))   # iteration only, as in .NET

        self.assertTrue(objectTableCnt          == 22)
        self.assertTrue(layerTableCnt           == 6)
        self.assertTrue(materialsTableCnt       == 2)
        self.assertTrue(linetypeTableCnt        == 6)
        self.assertTrue(bitmapsTableCnt         == 0)
        self.assertTrue(groupTableCnt           == 2)
        self.assertTrue(idTableCnt              == 1)
        self.assertTrue(viewTableCnt            == 4)
        self.assertTrue(namedViewsTableCnt      == 2)
        self.assertTrue(pluginDataTableCnt      == 1)
        self.assertTrue(stringTableCnt          == 1)
        self.assertTrue(embeddedFileTableCnt    == 1)
        self.assertTrue(renderContentTableCnt   == 3)

    def test_file3dmEmbeddedFilePaths(self):
        file3dm = rhino3dm.File3dm.Read('../models/file3dm_stuff.3dm')
        # D69: the old EmbeddedFilePaths2() -> RhinoCommon's EmbeddedFiles (File3dmEmbeddedFile.Filename)
        embeddedFiles = [f.Filename for f in file3dm.EmbeddedFiles]
        #print(embeddedFiles)

        self.assertTrue(type(embeddedFiles) == list)
        self.assertTrue(type(embeddedFiles[0]) == str)

    def test_file3dmReadMissingFileRaisesFileNotFound(self):
        """File3dm.Read of a path that does not exist raises FileNotFoundError, as RhinoCommon does. (D9)"""
        with self.assertRaises(FileNotFoundError):
            rhino3dm.File3dm.Read('/nonexistent/model.3dm')

    def test_file3dmWriteTakesOptionsOrNone(self):
        """Write and WriteWithLog take File3dmWriteOptions or None, and the written file reads back."""
        o = rhino3dm.File3dmWriteOptions()
        o.Version = 7
        f = rhino3dm.File3dm()
        with helpers.TempDir() as d:
            path = os.path.join(d, 'opts.3dm')
            self.assertIs(f.Write(path, o), True)
            self.assertIs(f.Write(os.path.join(d, 'default.3dm'), None), True)
            ok, log = f.WriteWithLog(os.path.join(d, 'log.3dm'), None)
            self.assertIs(ok, True)
            self.assertIsInstance(log, str)
            self.assertIsNotNone(rhino3dm.File3dm.Read(path))

    def test_file3dmCreatedAndLastEditedAreDates(self):
        """Created and LastEdited are datetimes, and a written file reads back with plausible dates."""
        f = rhino3dm.File3dm()
        self.assertIsInstance(f.Created, datetime.datetime)
        self.assertIsInstance(f.LastEdited, datetime.datetime)
        with helpers.TempDir() as d:
            path = os.path.join(d, 'dated.3dm')
            self.assertIs(f.Write(path, 7), True)
            g = rhino3dm.File3dm.Read(path)
        self.assertGreaterEqual(g.Created, datetime.datetime(2000, 1, 1))
        self.assertGreaterEqual(g.LastEdited, g.Created)

    def test_file3dmFromByteArrayReadsTheBytesOfAFile(self):
        """FromByteArray reads a written .3dm from bytes or a bytearray."""
        f = rhino3dm.File3dm()
        f.Objects.AddPoint(rhino3dm.Point3d(1, 2, 3), None)
        with helpers.TempDir() as d:
            path = os.path.join(d, 'b.3dm')
            self.assertTrue(f.Write(path, 8))
            with open(path, 'rb') as fh:
                data = fh.read()
        g = rhino3dm.File3dm.FromByteArray(data)
        self.assertIsInstance(g, rhino3dm.File3dm)
        self.assertEqual(g.Objects.Count, 1)
        self.assertEqual(rhino3dm.File3dm.FromByteArray(bytearray(data)).Objects.Count, 1)

    def test_file3dmWriteMultipleObjects(self):
        """File3dm.WriteMultipleObjects writes a file holding each geometry given."""
        circle = rhino3dm.ArcCurve(rhino3dm.Circle(2.0))
        plane = rhino3dm.Brep.CreateTrimmedPlane(rhino3dm.Plane.WorldXY, circle)
        with helpers.TempDir() as d:
            out = os.path.join(d, 'multi.3dm')
            self.assertTrue(rhino3dm.File3dm.WriteMultipleObjects(out, [circle, plane]))
            self.assertEqual(len(list(rhino3dm.File3dm.Read(out).Objects)), 2)

    def test_file3dmEarthAnchorPointIsACopy(self):
        """File3dm.EarthAnchorPoint reads a copy of the model's anchor; setting it copies one in, and it survives a file round trip."""
        f = helpers.read_model('file3dm_stuff.3dm')
        a = f.EarthAnchorPoint
        self.assertEqual((a.EarthBasepointLatitude, a.EarthBasepointLongitude, a.EarthBasepointElevation), (0.0, 0.0, 0.0))
        a.EarthBasepointLatitude = 12.5
        self.assertEqual(f.EarthAnchorPoint.EarthBasepointLatitude, 0.0)     # a copy
        f.EarthAnchorPoint = a
        self.assertEqual(f.EarthAnchorPoint.EarthBasepointLatitude, 12.5)
        back = rhino3dm.File3dm.FromByteArray(f.ToByteArray(None))
        self.assertEqual(back.EarthAnchorPoint.EarthBasepointLatitude, 12.5)

    def test_file3dmRenderContentByKind(self):
        """RenderMaterials, RenderEnvironments and RenderTextures together hold the file's render content, and each content is found in its own kind."""
        f = helpers.read_model('file3dm_stuff.3dm')
        ids = ['ab776f2b-7763-4c53-ac15-d02d3040ff26', '660433b2-4c4f-459e-962c-8f70942c5ccd',
               '16421078-5d0e-6851-2d34-062d266c3949']
        kinds = (f.RenderMaterials, f.RenderEnvironments, f.RenderTextures)
        self.assertEqual(sorted(ids), sorted(str(c.Id) for k in kinds for c in k))
        for oid in ids:
            hit = [k.Find(uuid.UUID(oid)) for k in kinds if k.Find(uuid.UUID(oid)) is not None]
            self.assertEqual(len(hit), 1)
            self.assertEqual(str(hit[0].Id), oid)

    def test_file3dmEncodeDecodeRoundTripsTheModel(self):
        """File3dm.Encode is base64 of the model's bytes; Decode reads it, and any 3dm's base64."""
        path = helpers.model('file3dm_stuff.3dm')
        f = rhino3dm.File3dm.Read(path)
        s = f.Encode()
        self.assertIsInstance(s, str)
        self.assertGreater(len(s), 1000)
        g = rhino3dm.File3dm.Decode(s)
        self.assertIsInstance(g, rhino3dm.File3dm)
        self.assertEqual(len(list(g.Objects)), len(list(f.Objects)))
        with open(path, 'rb') as fh:
            raw = base64.b64encode(fh.read()).decode()
        self.assertEqual(len(list(rhino3dm.File3dm.Decode(raw).Objects)), len(list(f.Objects)))

    def test_file3dmEncodeWithOptionsWritesThatVersion(self):
        """File3dm.Encode(options) writes the archive with those write options."""
        f = helpers.read_model('file3dm_stuff.3dm')
        opts = rhino3dm.File3dmWriteOptions()
        opts.Version = 7
        self.assertEqual(len(list(rhino3dm.File3dm.Decode(f.Encode(opts)).Objects)), len(list(f.Objects)))

    def test_file3dmRdkXmlIsTheModelsRenderXml(self):
        """File3dm.RdkXml is the model's RDK document XML, also on its plug-in data entry."""
        f = helpers.read_model('file3dm_stuff.3dm')
        self.assertEqual(_file3dm_sha(f.RdkXml()), _FILE3DM_STUFF_RDK_SHA)
        self.assertTrue(f.RdkXml().startswith('<xml>'))
        entries = [p.RdkXml() for p in f.PlugInData]
        self.assertEqual([_file3dm_sha(x) for x in entries if x], [_FILE3DM_STUFF_RDK_SHA])


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
