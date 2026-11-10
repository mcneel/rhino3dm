const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('getSpotlightRadii', async () => {

  const light = new rhino.Light()
  light.lightStyle = rhino.LightStyle.CameraSpot
  light.spotAngleRadians = 0.5
  slr = light.getSpotLightRadii()

  expect(slr.length === 3).toBe(true)
  expect(Array.isArray(slr)).toBe(true)
  expect(typeof slr[0] === 'boolean').toBe(true)
  expect(typeof slr[1] === 'number').toBe(true)
  expect(typeof slr[2] === 'number').toBe(true)

  expect(slr[0]).toBe(true)
  expect(slr[1]).toBeCloseTo(light.spotAngleRadians, 1)
  expect(slr[2]).toBeCloseTo(light.spotAngleRadians, 1)

})

test('lightStyleDecidesPointSpotAndLinear', () => {
  // isPointLight, isSpotLight and isLinearLight follow lightStyle
  const light = new rhino.Light()
  light.lightStyle = rhino.LightStyle.WorldPoint
  expect(light.isPointLight).toBe(true)
  expect(light.isSpotLight).toBe(false)
  expect(light.isLinearLight).toBe(false)
  light.lightStyle = rhino.LightStyle.WorldSpot
  expect(light.isSpotLight).toBe(true)
  expect(light.isPointLight).toBe(false)
})
