import os
import sys
import unittest
import rhino3dm
import helpers
import json


def _archivable_dictionary_geometry():
    return next(iter(helpers.read_model('file3dm_stuff.3dm').Objects)).Geometry


class TestArchivableDictionary(unittest.TestCase):
    def test_archivableDictionaryRoundTripsValuesAndGeometry(self):
        """EncodeDict / DecodeDict round-trip bools, ints, floats, strings, points, vectors and geometry."""
        r = rhino3dm
        g = _archivable_dictionary_geometry()
        d = {'b': True, 'i': 7, 'f': 2.5, 's': 'héllo', 'p': r.Point3d(1, 2, 3), 'v': r.Vector3d(0, 0, 1),
             'g': g, 'gl': [g, g]}
        enc = r.ArchivableDictionary.EncodeDict(d)
        self.assertEqual(set(enc), {'version', 'archive3dm', 'opennurbs', 'data'})
        back = r.ArchivableDictionary.DecodeDict(enc)
        self.assertEqual({k: back[k] for k in 'bifs'}, {'b': True, 'i': 7, 'f': 2.5, 's': 'héllo'})
        self.assertIsInstance(back['p'], r.Point3d)
        self.assertEqual((back['p'].X, back['p'].Y, back['p'].Z), (1, 2, 3))
        self.assertIsInstance(back['v'], r.Vector3d)
        self.assertEqual(back['v'].Z, 1)
        self.assertEqual(type(back['g']).__name__, type(g).__name__)
        self.assertEqual(back['g'].Encode(), g.Encode())
        self.assertEqual([type(x).__name__ for x in back['gl']], [type(g).__name__] * 2)

    def test_archivableDictionaryDecodesAn8xPayload(self):
        """DecodeDict reads what 8.x's EncodeDict({'i': 3, 'p': Point3d(4, 5, 6), 'gl': [g]}) wrote."""
        g = _archivable_dictionary_geometry()
        with open(helpers.model('old_oracle/json_forms.json')) as fh:
            payload = json.load(fh)['archivable_dictionary_encode']
        back = rhino3dm.ArchivableDictionary.DecodeDict(payload)
        self.assertEqual(back['i'], 3)
        self.assertEqual((back['p'].X, back['p'].Y, back['p'].Z), (4, 5, 6))
        self.assertEqual(len(back['gl']), 1)
        self.assertEqual(type(back['gl'][0]).__name__, type(g).__name__)

    def test_archivableDictionaryEncodeRaisesOnAnUnsupportedValue(self):
        """EncodeDict raises RuntimeError naming a value it cannot serialize."""
        with self.assertRaisesRegex(RuntimeError, "Unable to serialize 'x'"):
            rhino3dm.ArchivableDictionary.EncodeDict({'x': {}})

    def test_archivableDictionaryDecodeRaisesOnAnObjectPayload(self):
        """DecodeDict raises RuntimeError on an encoded object that is not a dictionary."""
        g = _archivable_dictionary_geometry()
        with self.assertRaises(RuntimeError):
            rhino3dm.ArchivableDictionary.DecodeDict(rhino3dm.CommonObject.Decode(g.Encode()).Encode())


if __name__ == '__main__':
    unittest.main()
