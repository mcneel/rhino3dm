import os
import sys
import unittest
import rhino3dm
import helpers
import xml.etree.ElementTree as ET


def _rendercontent_all(f):
    return list(f.RenderMaterials) + list(f.RenderEnvironments) + list(f.RenderTextures)


def _rendercontent_with_children(f):
    return next(c for c in _rendercontent_all(f) if list(c.Children))


def _render_content_xml_tree(x):
    """a parsed XML tree to compare: opennurbs may write the attributes back in another order"""
    def walk(e):
        return (e.tag, sorted(e.attrib.items()), (e.text or '').strip(), [walk(c) for c in e])
    return walk(ET.fromstring(x))


class TestRenderContent(unittest.TestCase):
    def test_renderContentChildSlotOnAndAmount(self):
        """ChildSlotOn / SetChildSlotOn and ChildSlotAmount / SetChildSlotAmount take the child slot name first (D64)."""
        f = helpers.read_model('file3dm_stuff.3dm')
        top = _rendercontent_with_children(f)
        slot = list(top.Children)[0].ChildSlotName
        self.assertEqual((str(top.Id), slot), ('660433b2-4c4f-459e-962c-8f70942c5ccd', 'bitmap-texture'))
        self.assertIs(top.ChildSlotOn(slot), False)
        self.assertTrue(top.SetChildSlotOn(slot, True))
        self.assertIs(top.ChildSlotOn(slot), True)
        self.assertTrue(top.SetChildSlotAmount(slot, 42.0))
        self.assertEqual(top.ChildSlotAmount(slot), 42.0)

    def test_renderContentChildSlotAmountOfAnUnknownSlotIsZero(self):
        """ChildSlotAmount of a slot that has no amount is 0.0, as in RhinoCommon (D64)."""
        top = _rendercontent_with_children(helpers.read_model('file3dm_stuff.3dm'))
        self.assertEqual(top.ChildSlotAmount('no-such-slot'), 0.0)

    def test_renderContentSetChildAddsAChild(self):
        """SetChild(content, slot) adds a child that FindChild finds by its slot name."""
        f = helpers.read_model('file3dm_stuff.3dm')
        top = _rendercontent_with_children(f)
        n = len(list(top.Children))
        other = next(c for c in _rendercontent_all(f) if c.Id != top.Id)
        self.assertTrue(top.SetChild(other, 'new-slot'))
        self.assertEqual(len(list(top.Children)), n + 1)
        self.assertIsNotNone(top.FindChild('new-slot'))

    def test_renderContentChildSlotsSurviveAFileRoundTrip(self):
        """A child slot amount and a child set with SetChild read back from the file's bytes."""
        f = helpers.read_model('file3dm_stuff.3dm')
        top = _rendercontent_with_children(f)
        slot = list(top.Children)[0].ChildSlotName
        self.assertTrue(top.SetChildSlotAmount(slot, 42.0))
        other = next(c for c in _rendercontent_all(f) if c.Id != top.Id)
        self.assertTrue(top.SetChild(other, 'new-slot'))
        back = rhino3dm.File3dm.FromByteArray(f.ToByteArray(None))
        btop = next(c for c in _rendercontent_all(back) if c.Id == top.Id)
        self.assertEqual(btop.ChildSlotAmount(slot), 42.0)
        self.assertIsNotNone(btop.FindChild('new-slot'))

    def test_renderContentSetChildGivesTheCopyItsOwnId(self):
        """SetChild adds a copy with an id of its own, a child in the given slot."""
        f = helpers.read_model('file3dm_stuff.3dm')
        top = _rendercontent_with_children(f)
        other = next(c for c in _rendercontent_all(f) if c.Id != top.Id)
        self.assertTrue(top.SetChild(other, 'new-slot'))
        kid = top.FindChild('new-slot')
        self.assertNotEqual(kid.Id, other.Id)
        self.assertTrue(kid.IsChild)
        self.assertEqual(kid.ChildSlotName, 'new-slot')

    def test_renderContentSetXmlRoundTrips(self):
        """SetXML takes a render content's own XML back and refuses text that is not XML."""
        f = helpers.read_model('file3dm_stuff.3dm')
        mats = list(f.RenderMaterials)
        self.assertTrue(mats)
        a = mats[0]
        xml = a.XML(False)
        self.assertIs(a.SetXML(xml), True)
        self.assertEqual(_render_content_xml_tree(a.XML(False)), _render_content_xml_tree(xml))
        self.assertIs(a.SetXML('<not xml'), False)


if __name__ == '__main__':
    unittest.main()
