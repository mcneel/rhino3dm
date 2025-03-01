const rhino3dm = require('rhino3dm')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

test('toFloatArray', async () => {

    const xform = rhino.Transform.identity()
    const fa = xform.toFloatArray(false)

    expect(fa.length === 16).toBe(true)
    expect(Array.isArray(fa)).toBe(true)
    expect(typeof fa[0] === 'number').toBe(true)

})
