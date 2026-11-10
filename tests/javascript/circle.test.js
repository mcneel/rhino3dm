const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('circleClosestParameter', async () => {

    const circle = rhino.Circle.create(5)
    const result = rhino.Circle.closestParameter(circle, { X: 0, Y: 0, Z: 0 })
    expect(result[0]).toBe(true)

})