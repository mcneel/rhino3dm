const rhino3dm = require('rhino3dm')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

test('append', async () => {

    const sphere = rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5)

    const result = rhino.Sphere.closestParameter(sphere, { X: 0, Y: 0, Z: 5 })

    expect(Array.isArray(result)).toBe(true)
    expect(result.length === 3).toBe(true)
    expect(typeof result[0] === 'boolean').toBe(true)
    expect(typeof result[1] === 'number').toBe(true)
    expect(typeof result[2] === 'number').toBe(true)

    expect(result[0]).toBe(true)

})
