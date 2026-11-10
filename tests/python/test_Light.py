import unittest
import rhino3dm
import os
import sys
import helpers

#objective


class TestLight(unittest.TestCase):

    def test_lightGetSpotlightRadii(self):
        light = rhino3dm.Light()
        light.LightStyle = rhino3dm.LightStyle.CameraSpot
        light.SpotAngleRadians = 0.5
        slr = light.GetSpotLightRadii()

        self.assertTrue( len(slr) == 3 )
        self.assertTrue( type(slr) == tuple )
        self.assertTrue( type(slr[0]) == bool )
        self.assertTrue( type(slr[1]) == float )
        self.assertTrue( type(slr[2]) == float )

        self.assertTrue(slr[0])
        self.assertAlmostEqual(slr[1], light.SpotAngleRadians, 1)
        self.assertAlmostEqual(slr[2], light.SpotAngleRadians, 1)

    def test_lightStyleDecidesPointSpotAndLinear(self):
        """IsPointLight, IsSpotLight and IsLinearLight follow LightStyle."""
        light = rhino3dm.Light()
        light.LightStyle = rhino3dm.LightStyle.WorldPoint
        self.assertIs(light.IsPointLight, True)
        self.assertIs(light.IsSpotLight, False)
        self.assertIs(light.IsLinearLight, False)
        light.LightStyle = rhino3dm.LightStyle.WorldSpot
        self.assertIs(light.IsSpotLight, True)
        self.assertIs(light.IsPointLight, False)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
