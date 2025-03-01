const rhino3dm = require('rhino3dm')

let rhino
let surface

beforeAll(async () => {
    rhino = await rhino3dm()
    surface = new rhino.Sphere([0, 0, 0], 5).toBrep().surfaces().get(0)
})

test('toNurbsSurface', async () => {

    const result = surface.toNurbsSurfaceTolerance(0.0)
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBe(2)
    expect(result[0]).toBeInstanceOf(rhino.NurbsSurface)
    expect(Number.isInteger(result[1])).toBe(true)
    expect(result[0].isValid).toBe(true)

})

test('frameAt', async () => {

    const result = surface.frameAt(0.1, 0.1)
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBe(2)
    expect(typeof result[0] === 'boolean').toBe(true)
    expect(typeof result[1] === 'object').toBe(true)
    expect(result[0]).toBe(true)
    expect(result[1].origin).toBeDefined()
    expect(result[1].xAxis).toBeDefined()
    expect(result[1].yAxis).toBeDefined()
    expect(result[1].zAxis).toBeDefined()

})

test('getSurfaceParameterFromNurbsFormParameter', async () => {

    const result = surface.getSurfaceParameterFromNurbsFormParameter(0.1, 0.1)
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBe(3)
    expect(typeof result[0] === 'boolean').toBe(true)
    expect(typeof result[1] === 'number').toBe(true)
    expect(typeof result[2] === 'number').toBe(true)
    expect(result[0]).toBe(true)
})

test('getNurbsFormParameterFromSurfaceParameter', async () => {

    const result = surface.getNurbsFormParameterFromSurfaceParameter(0.1, 0.1)
    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBe(3)
    expect(typeof result[0] === 'boolean').toBe(true)
    expect(typeof result[1] === 'number').toBe(true)
    expect(typeof result[2] === 'number').toBe(true)
    expect(result[0]).toBe(true)
})

test('getSpanVector', async () => {

    const result = surface.getSpanVector(0)
    expect(Array.isArray(result)).toBe(true)
    expect(result.length > 0).toBe(true)
    expect(typeof result[0] === 'number').toBe(true)

})
