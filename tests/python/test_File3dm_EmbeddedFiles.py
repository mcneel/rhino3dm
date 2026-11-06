import base64
import os
import sys
import unittest
import rhino3dm
import helpers


def _embedded_base_name(s):
    """a path's file name, whichever separator it was saved with"""
    return s.replace('\\', '/').split('/')[-1]


def _embedded_note(d):
    """a small text file in directory d to embed"""
    path = os.path.join(d, 'note.txt')
    with open(path, 'w') as fp:
        fp.write('hello world')
    return path


class TestFile3dmEmbeddedFiles(unittest.TestCase):
    def test_embeddedFilesAddAndIterate(self):
        """EmbeddedFiles.Add embeds a file that iteration yields, and it survives a write and read."""
        with helpers.TempDir() as d:
            data = os.path.join(d, 'note.txt')
            with open(data, 'w') as fh:
                fh.write('hello')
            f = rhino3dm.File3dm()
            self.assertIs(f.EmbeddedFiles.Add(data), True)
            files = list(f.EmbeddedFiles)
            self.assertEqual(len(files), 1)
            self.assertIsInstance(files[0], rhino3dm.File3dmEmbeddedFile)
            self.assertTrue(files[0].Filename.endswith('note.txt'))
            self.assertEqual(files[0].ComponentType, rhino3dm.ModelComponentType.EmbeddedFile)
            path = os.path.join(d, 'embedded.3dm')
            self.assertIs(f.Write(path, 7), True)
            g = rhino3dm.File3dm.Read(path)
        self.assertEqual([e.Filename for e in g.EmbeddedFiles], [files[0].Filename])

    def test_embeddedFilesListTheModelsFiles(self):
        """EmbeddedFiles lists a model's embedded files in order, their paths in this platform's form. (D69)"""
        expected = {'file3dm_stuff.3dm': ['hops.png'], 'sphereDecals.3dm': ['E6AD.jpg', 'E6EC.jpg'],
                    'subd.3dm': ['bump_texture.png']}
        for name, files in expected.items():
            f = helpers.read_model(name)
            self.assertEqual(files, [_embedded_base_name(e.Filename) for e in f.EmbeddedFiles], name)

    def test_embeddedFilesAddCountLength(self):
        """EmbeddedFiles.Add embeds a file: Count goes up and the embedded file's Length is the file's size."""
        with helpers.TempDir() as d:
            data = _embedded_note(d)
            f = rhino3dm.File3dm()
            self.assertEqual(f.EmbeddedFiles.Count, 0)
            self.assertIs(f.EmbeddedFiles.Add(data), True)
            self.assertEqual(f.EmbeddedFiles.Count, 1)
            self.assertEqual(list(f.EmbeddedFiles)[0].Length, len('hello world'))

    def test_embeddedFilesFindIndex(self):
        """EmbeddedFiles.FindIndex returns the embedded file at an index, None past the end."""
        with helpers.TempDir() as d:
            f = rhino3dm.File3dm()
            f.EmbeddedFiles.Add(_embedded_note(d))
            ef = list(f.EmbeddedFiles)[0]
            found = f.EmbeddedFiles.FindIndex(0)
            self.assertIsInstance(found, rhino3dm.File3dmEmbeddedFile)
            self.assertEqual(found.Filename, ef.Filename)
            self.assertIsNone(f.EmbeddedFiles.FindIndex(5))

    def test_embeddedFileClear(self):
        """File3dmEmbeddedFile.Clear empties the embedded file's contents."""
        with helpers.TempDir() as d:
            f = rhino3dm.File3dm()
            f.EmbeddedFiles.Add(_embedded_note(d))
            ef = list(f.EmbeddedFiles)[0]
            self.assertIs(ef.Clear(), True)
            self.assertEqual(ef.Length, 0)

    def test_embeddedFilesSurviveAWrite(self):
        """An embedded file is written with the model and read back with its contents."""
        with helpers.TempDir() as d:
            g = rhino3dm.File3dm()
            g.EmbeddedFiles.Add(_embedded_note(d))
            path = os.path.join(d, 'embedded.3dm')
            self.assertTrue(g.Write(path, 7))
            h = rhino3dm.File3dm.Read(path)
            self.assertEqual(h.EmbeddedFiles.Count, 1)
            self.assertEqual(list(h.EmbeddedFiles)[0].Length, len('hello world'))

    def test_embeddedFileReadReadsAFileFromDisk(self):
        """File3dmEmbeddedFile.Read reads a file from disk, or returns None when it does not exist."""
        with helpers.TempDir() as d:
            note = os.path.join(d, 'note.txt')
            with open(note, 'w') as fh:
                fh.write('hello')
            ef = rhino3dm.File3dmEmbeddedFile.Read(note)
            self.assertIsInstance(ef, rhino3dm.File3dmEmbeddedFile)
            self.assertEqual(ef.Length, 5)
            self.assertTrue(ef.Filename.endswith('note.txt'))
            self.assertIsNone(rhino3dm.File3dmEmbeddedFile.Read(os.path.join(d, 'missing.txt')))

    def test_file3dmGetEmbeddedFileAsBase64(self):
        """GetEmbeddedFileAsBase64 finds a file Rhino embedded by its name, in any case; '' when absent or not the full path in strict mode."""
        f = helpers.read_model('sphereDecals.3dm')
        files = list(f.EmbeddedFiles)
        found = [f.GetEmbeddedFileAsBase64(_embedded_base_name(e.Filename).lower()) for e in files]
        for e, b in zip(files, found):
            data = base64.b64decode(b)
            self.assertEqual(len(data), e.Length)                       # each name finds its own file
            self.assertTrue(data.startswith(b'\xff\xd8'))              # a JPEG
            self.assertEqual(f.GetEmbeddedFileAsBase64(e.Filename), b)
            self.assertEqual(f.GetEmbeddedFileAsBase64(_embedded_base_name(e.Filename), True), '')
        self.assertNotEqual(found[0], found[1])
        self.assertEqual(f.GetEmbeddedFileAsBase64('nothing.png'), '')

    @unittest.skipIf(sys.platform == 'win32', 'Filename reads a path saved on macOS with backslashes on Windows (D69)')
    def test_file3dmGetEmbeddedFileAsBase64StrictTakesTheFullPath(self):
        """GetEmbeddedFileAsBase64(path, True) finds a file by the full path it was embedded with."""
        f = helpers.read_model('file3dm_stuff.3dm')                       # embedded on macOS: Filename is the stored path
        p = list(f.EmbeddedFiles)[0].Filename
        b = f.GetEmbeddedFileAsBase64(p, True)
        self.assertTrue(base64.b64decode(b).startswith(b'\x89PNG'))
        self.assertEqual(b, f.GetEmbeddedFileAsBase64(p))

    def test_file3dmGetEmbeddedFileAsBase64ReadsOnlyWhatRhinoEmbedded(self):
        """GetEmbeddedFileAsBase64 reads the files Rhino's renderer embedded, not one added with EmbeddedFiles.Add."""
        with helpers.TempDir() as d:
            f = rhino3dm.File3dm()
            f.EmbeddedFiles.Add(_embedded_note(d))
            self.assertEqual(f.GetEmbeddedFileAsBase64(list(f.EmbeddedFiles)[0].Filename), '')


if __name__ == '__main__':
    unittest.main()
