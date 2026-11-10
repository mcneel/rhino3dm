import os
import sys
import unittest
import rhino3dm
import helpers


class TestFile3dmRenderChannels(unittest.TestCase):
    def test_renderChannelsModesIsANestedEnum(self):
        """RenderChannels.Modes is nested in RenderChannels with RhinoCommon's values. (D76)"""
        self.assertEqual(list(rhino3dm.RenderChannels.Modes.__members__), ['Automatic', 'Custom'])


if __name__ == '__main__':
    unittest.main()
