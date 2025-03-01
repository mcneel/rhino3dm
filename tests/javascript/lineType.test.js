const rhino3dm = require('rhino3dm')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('getSegments', async () => {

  const lt = rhino.Linetype.dashed()

  const segment = lt.getSegment(0)

  expect(Array.isArray(segment)).toBe(true)
  expect(segment.length === 2)
  expect(typeof segment[0] === 'number')
  expect(typeof segment[1] === 'boolean')

})