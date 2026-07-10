const rhino3dm = require('rhino3dm')

// RH3DM-195 / RH-87934: page absolute/relative tolerance setters must be independent.
// The bug was .NET-only; this is a parity guard for the shared setter surface.

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

test('setting pageAbsoluteTolerance sticks and leaves relative', async () => {
    const file3dm = new rhino.File3dm()
    const relBefore = file3dm.settings().pageRelativeTolerance

    const settings = file3dm.settings()
    settings.pageAbsoluteTolerance = 0.01

    expect(file3dm.settings().pageAbsoluteTolerance).toBe(0.01)
    expect(file3dm.settings().pageRelativeTolerance).toBe(relBefore)
})

test('setting pageRelativeTolerance sticks and leaves absolute', async () => {
    const file3dm = new rhino.File3dm()
    const absBefore = file3dm.settings().pageAbsoluteTolerance

    const settings = file3dm.settings()
    settings.pageRelativeTolerance = 0.5

    expect(file3dm.settings().pageRelativeTolerance).toBe(0.5)
    expect(file3dm.settings().pageAbsoluteTolerance).toBe(absBefore)
})

test('page tolerances round-trip through byte array', async () => {
    const file3dm = new rhino.File3dm()
    const settings = file3dm.settings()
    settings.pageAbsoluteTolerance = 0.01
    settings.pageRelativeTolerance = 0.25

    const bytes = file3dm.toByteArray()
    const read = rhino.File3dm.fromByteArray(bytes)

    expect(read.settings().pageAbsoluteTolerance).toBe(0.01)
    expect(read.settings().pageRelativeTolerance).toBe(0.25)
})
