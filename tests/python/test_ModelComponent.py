import os
import sys
import unittest
import rhino3dm
import helpers


class TestModelComponent(unittest.TestCase):
    def test_modelComponentIsValidComponentNameChecksTheName(self):
        """IsValidComponentName accepts an ordinary name and rejects an empty one."""
        self.assertIs(rhino3dm.ModelComponent.IsValidComponentName("Layer 01"), True)
        self.assertIs(rhino3dm.ModelComponent.IsValidComponentName(""), False)

    def test_modelComponentNamePathSeparator(self):
        """NamePathSeparator is '::', on ModelComponent and on a derived class such as Layer."""
        self.assertEqual(rhino3dm.ModelComponent.NamePathSeparator, '::')
        self.assertEqual(rhino3dm.Layer.NamePathSeparator, '::')


if __name__ == '__main__':
    unittest.main()
