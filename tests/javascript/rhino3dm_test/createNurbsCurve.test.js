const createNurbsCurve = require('./createNurbsCurve')

test('createNurbsCurve', async () => {
    const result = await createNurbsCurve()
    expect(result).toBe(true)
    }
)