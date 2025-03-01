const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('circleClosestParameter', async () => {

    const circle = new rhino.Circle(5)
    const result = circle.closestParameter([0, 0, 0])
    expect(result[0]).toBe(true)

})