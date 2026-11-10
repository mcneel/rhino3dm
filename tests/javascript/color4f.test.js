const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('color4fComponentsAndEquality', () => {
  // Color4f stores R, G, B, A and compares by value; white is (1, 1, 1, 1) (D34)
  const c = new rhino.Color4f(0.25, 0.5, 0.75, 1.0)
  expect([c.R, c.G, c.B, c.A]).toEqual([0.25, 0.5, 0.75, 1.0])
  expect(rhino.Color4f.equals(c, new rhino.Color4f(0.25, 0.5, 0.75, 1.0))).toBe(true)
  expect(rhino.Color4f.equals(rhino.Color4f.white, new rhino.Color4f(1, 1, 1, 1))).toBe(true)
})
