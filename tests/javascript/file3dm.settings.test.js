const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

// RH3DM-195 / RH-87934: page absolute/relative tolerance setters must be independent.
// The bug was .NET-only; this is a parity guard for the shared setter surface.

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

test('setting pageAbsoluteTolerance sticks and leaves relative', async () => {
    const file3dm = new rhino.File3dm()
    const relBefore = file3dm.settings.pageRelativeTolerance

    const settings = file3dm.settings
    settings.pageAbsoluteTolerance = 0.01

    expect(file3dm.settings.pageAbsoluteTolerance).toBe(0.01)
    expect(file3dm.settings.pageRelativeTolerance).toBe(relBefore)
})

test('setting pageRelativeTolerance sticks and leaves absolute', async () => {
    const file3dm = new rhino.File3dm()
    const absBefore = file3dm.settings.pageAbsoluteTolerance

    const settings = file3dm.settings
    settings.pageRelativeTolerance = 0.5

    expect(file3dm.settings.pageRelativeTolerance).toBe(0.5)
    expect(file3dm.settings.pageAbsoluteTolerance).toBe(absBefore)
})

test('page tolerances round-trip through byte array', async () => {
    const file3dm = new rhino.File3dm()
    const settings = file3dm.settings
    settings.pageAbsoluteTolerance = 0.01
    settings.pageRelativeTolerance = 0.25

    const bytes = file3dm.toByteArray()
    const read = rhino.File3dm.fromByteArray(bytes)

    expect(read.settings.pageAbsoluteTolerance).toBe(0.01)
    expect(read.settings.pageRelativeTolerance).toBe(0.25)
})

test('settingsModelAbsoluteToleranceWritesThrough', () => {
  // setting file.settings.modelAbsoluteTolerance changes the file's settings
  const f = new rhino.File3dm()
  f.settings.modelAbsoluteTolerance = 0.25
  expect(f.settings.modelAbsoluteTolerance).toBe(0.25)
})

test('settingsModelAngleToleranceDegreesIsTheRadiansInDegrees', () => {
  // setting modelAngleToleranceDegrees sets modelAngleToleranceRadians to the same angle
  const f = new rhino.File3dm()
  f.settings.modelAngleToleranceDegrees = 3.0
  expect(f.settings.modelAngleToleranceRadians).toBeCloseTo(3.0 * Math.PI / 180, 12)
  expect(f.settings.modelAngleToleranceDegrees).toBeCloseTo(3.0, 12)
})

test('settingsGridDefaultsRoundTrip', () => {
  // gridDefaults returns the grid settings (70 lines by default) and setting it back stores changes
  const f = new rhino.File3dm()
  const gd = f.settings.gridDefaults
  expect(gd).not.toBeNull()
  expect(gd.gridLineCount).toBe(70)
  gd.gridLineCount = 12
  f.settings.gridDefaults = gd
  expect(f.settings.gridDefaults.gridLineCount).toBe(12)
})

test('settingsModelBasepoint', () => {
  // settings.modelBasepoint reads the file's base point
  const b = helpers.readModel(rhino, 'file3dm_stuff.3dm').settings.modelBasepoint
  expect([b.X, b.Y, b.Z]).toEqual([0, 0, 0])
})

test('settingsCurrentLinetypeSource', () => {
  // currentLinetypeSource defaults to the layer and can be set
  const S = rhino.ObjectLinetypeSource
  const f = new rhino.File3dm()
  expect(f.settings.currentLinetypeSource).toBe(S.LinetypeFromLayer)
  f.settings.currentLinetypeSource = S.LinetypeFromObject
  expect(f.settings.currentLinetypeSource).toBe(S.LinetypeFromObject)
})
