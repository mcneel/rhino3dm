const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('renderSettingsSettersCommitIntoTheFile', () => {
  // setting a property on a file's render settings writes it into the file
  const f = new rhino.File3dm()
  const rs = f.settings.renderSettings
  const before = rs.scaleBackgroundToFit
  rs.scaleBackgroundToFit = !before
  expect(rs.scaleBackgroundToFit).toBe(!before)
  expect(f.settings.renderSettings.scaleBackgroundToFit).toBe(!before)
  rs.shadowmapLevel = 3
  expect(f.settings.renderSettings.shadowmapLevel).toBe(3)
})

test('renderSettingsWrittenAndReadBack', () => {
  // render settings set on a file are written out and read back
  const nw = new rhino.File3dm()
  nw.settings.renderSettings.shadowmapLevel = 2
  nw.settings.renderSettings.transparentBackground = true
  const back = rhino.File3dm.fromByteArray(nw.toByteArray()).settings.renderSettings
  expect(back.shadowmapLevel).toBe(2)
  expect(back.transparentBackground).toBe(true)
})

test('renderSettingsViewKeepsTheFileAlive', () => {
  // a file's render settings stay usable after the file is deleted
  const f = new rhino.File3dm()
  const rs = f.settings.renderSettings
  f.delete()
  rs.shadowmapLevel = 1
  expect(rs.shadowmapLevel).toBe(1)
})

test('renderSettingsGroundPlaneWritesThroughIntoTheFile', () => {
  // the ground plane of a file's render settings writes into the file
  const f = new rhino.File3dm()
  const gp = f.settings.renderSettings.groundPlane
  gp.enabled = true
  gp.altitude = 2.5
  gp.textureOffset = new rhino.Vector2d(0.25, 0.75)
  const again = f.settings.renderSettings.groundPlane
  expect(again.enabled).toBe(true)
  expect(again.altitude).toBe(2.5)
  expect([again.textureOffset.X, again.textureOffset.Y]).toEqual([0.25, 0.75])
})

test('renderSettingsGroundPlaneIsAViewIntoConstructedSettings', () => {
  // the ground plane of a constructed RenderSettings is a view into it, not a copy
  const rs = new rhino.RenderSettings()
  const inner = rs.groundPlane
  inner.altitude = 3.0
  expect(rs.groundPlane.altitude).toBe(3.0)
})

test('renderSettingsSunAndSafeFrameWriteThroughIntoTheFile', () => {
  // the sun and safe frame of a file's render settings write into the file
  const f = new rhino.File3dm()
  const rs = f.settings.renderSettings
  rs.sun.enabled = true
  rs.safeFrame.enabled = true
  expect(f.settings.renderSettings.sun.enabled).toBe(true)
  expect(f.settings.renderSettings.safeFrame.enabled).toBe(true)
})

test('renderSettingsRenderChannelsDefaultToAutomatic', () => {
  // RenderSettings.renderChannels and a new RenderChannels are in Automatic mode, with UUIDs in customList
  const rc = new rhino.RenderSettings().renderChannels
  expect(rc.mode).toBe(rhino.RenderChannelsModes.Automatic)
  for (const i of Array.from(rc.customList)) expect(i).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
  expect(new rhino.RenderChannels().mode).toBe(rhino.RenderChannelsModes.Automatic)
})

test('linearWorkflowGammaRoundTrips', () => {
  // LinearWorkflow preProcessGamma and postProcessGamma read back what was set
  const lw = new rhino.File3dm().settings.renderSettings.linearWorkflow
  lw.preProcessGamma = 2.5
  expect(lw.preProcessGamma).toBeCloseTo(2.5, 6)
  lw.postProcessGamma = 1.75
  expect(lw.postProcessGamma).toBeCloseTo(1.75, 6)
})

test('ditheringMethodUsesRhinoCommonsEnum', () => {
  // Dithering.method takes and returns RhinoCommon's Dithering.Methods (FloydSteinberg = 1, SimpleNoise = 2) (D36)
  const d = new rhino.File3dm().settings.renderSettings.dithering
  const M = rhino.DitheringMethods
  expect(M.SimpleNoise.value).toBe(2)
  expect(M.FloydSteinberg.value).toBe(1)
  d.method = M.SimpleNoise
  expect(d.method).toBe(M.SimpleNoise)
  d.method = M.FloydSteinberg
  expect(d.method).toBe(M.FloydSteinberg)
})

test('renderSettingsUserStrings', () => {
  // A file's RenderSettings keep user strings: set, get, count and list them (D62)
  const o = new rhino.File3dm().settings.renderSettings
  expect(o.userStringCount).toBe(0)
  expect(o.setUserString('k', 'v')).toBe(true)
  expect(o.getUserString('k')).toBe('v')
  expect(Array.from(o.getUserStrings(), p => Array.from(p))).toEqual([['k', 'v']])
})

test('renderSettingsEnvironmentIdsAndOverrides', () => {
  // renderEnvironmentId and renderEnvironmentOverride read per usage, and their setters survive a file round trip
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const U = rhino.RenderSettingsEnvironmentUsage, Pp = rhino.RenderSettingsEnvironmentPurpose
  const rs = f.settings.renderSettings
  for (const u of [U.Background, U.Reflection, U.Skylighting])
    expect(rs.renderEnvironmentId(u, Pp.Standard)).toBe('16421078-5d0e-6851-2d34-062d266c3949')
  for (const u of [U.Reflection, U.Skylighting])
    expect(rs.renderEnvironmentOverride(u)).toBe(true)
  const eid = require('crypto').randomUUID()
  rs.setRenderEnvironmentId(U.Skylighting, eid)
  rs.setRenderEnvironmentOverride(U.Skylighting, true)
  const brs = rhino.File3dm.fromByteArray(f.toByteArray()).settings.renderSettings
  expect(brs.renderEnvironmentId(U.Skylighting, Pp.Standard)).toBe(eid)
  expect(brs.renderEnvironmentOverride(U.Skylighting)).toBe(true)
})

test('renderSettingsLinearWorkflowPreProcessGammaOn', () => {
  // linearWorkflow.preProcessGammaOn can be toggled
  const lw = new rhino.File3dm().settings.renderSettings.linearWorkflow
  const on = lw.preProcessGammaOn
  lw.preProcessGammaOn = !on
  expect(lw.preProcessGammaOn).toBe(!on)
})

test('renderSettingsCopyConstructorsCopy', () => {
  // Dithering, GroundPlane, LinearWorkflow, SafeFrame and Skylight copy constructors copy their settings
  for (const [C, k] of [[rhino.Dithering, 'enabled'], [rhino.GroundPlane, 'enabled'], [rhino.LinearWorkflow, 'preProcessColors'],
                        [rhino.SafeFrame, 'enabled'], [rhino.Skylight, 'enabled']]) {
    const a = new C()
    a[k] = !a[k]
    expect(new C(a)[k]).toBe(a[k])
  }
})
