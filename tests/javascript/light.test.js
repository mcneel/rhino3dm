const rhino3dm = require('rhino3dm')

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