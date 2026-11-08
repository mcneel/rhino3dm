const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

// 8.x members renamed in 9.2: the replacements listed in docs/migrating-to-9/javascript-members.md work

test('layerFlags', () => {
  // layer.visible / locked / expanded are isVisible / isLocked / isExpanded
  const layer = new rhino.Layer()
  layer.isVisible = false
  layer.isLocked = true
  layer.isExpanded = true
  expect([layer.isVisible, layer.isLocked, layer.isExpanded]).toEqual([false, true, true])
})

test('renderSettings', () => {
  // RenderSettings, Sun, SafeFrame, LinearWorkflow and RenderChannels members with RhinoCommon's names
  const rs = new rhino.File3dm().settings.renderSettings
  rs.renderBackfaces = false
  rs.renderIsoparams = true
  rs.shadowmapLevel = 2
  expect([rs.renderBackfaces, rs.renderIsoparams, rs.shadowmapLevel]).toEqual([false, true, 2])
  expect(typeof rs.snapshot).toBe('string')
  rs.sun.enabled = true
  expect(rs.sun.enabled).toBe(true)
  expect(rhino.Sun.minYear).toBeLessThan(rhino.Sun.maxYear)
  rs.safeFrame.fieldsOn = true
  expect(rs.safeFrame.fieldsOn).toBe(true)
  // opennurbs keeps one pre-process gamma switch: colors and textures read and write it both
  rs.linearWorkflow.preProcessColors = false
  expect([rs.linearWorkflow.preProcessColors, rs.linearWorkflow.preProcessTextures]).toEqual([false, false])
  expect(rs.renderChannels.customList.every(id => typeof id === 'string')).toBe(true)
})

test('objectAttributesFontAndGroups', () => {
  // activeSpace / id are space / objectId; file3dm.groups is allGroups; font.strikeOut is strikeout
  const a = new rhino.ObjectAttributes()
  expect(a.space).toBe(rhino.ActiveSpace.ModelSpace)
  a.objectId = '00000000-0000-0000-0000-000000000007'
  expect(a.objectId).toBe('00000000-0000-0000-0000-000000000007')
  const f = helpers.readModel(rhino, 'groups.3dm')
  expect(f.allGroups.count).toBeGreaterThan(0)
  expect(new rhino.Font('Arial').strikeout).toBe(false)
  expect(f.dimStyles.count).toBeGreaterThan(0)
})

test('dimensionStyle', () => {
  // fixedExtensionLengthOn / isFieldOverridden are fixedExtensionOn / isFieldOverriden
  const ds = new rhino.DimensionStyle()
  ds.fixedExtensionOn = true
  expect(ds.fixedExtensionOn).toBe(true)
  expect(typeof ds.isFieldOverriden(rhino.DimensionStyleField.TextHeight)).toBe('boolean')
})

test('pointCloudItemAndNurbsSurface', () => {
  // pointCloudItem.value is pointValue; points.makeRational is on the surface
  const pc = new rhino.PointCloud()
  pc.addPointValue({ X: 1, Y: 2, Z: 3 }, 7.5)
  expect(pc.get(0).pointValue).toBe(7.5)
  const srf = rhino.NurbsSurface.create(3, false, 2, 2, 3, 3)
  expect(srf.makeRational()).toBe(true)
  expect(srf.isRational).toBe(true)
})

test('valueStaticsAndJsSpellings', () => {
  // TextureMapping.CreateBoxMapping, PolylineCurve.ToPolyline and Linetype.dashdot in RhinoCommon's spelling
  const I = (a, b) => new rhino.Interval(a, b)
  const tm = rhino.TextureMapping.createBoxMapping(rhino.Plane.worldXY, I(0, 1), I(0, 1), I(0, 1), true)
  expect(tm.mappingType).toBe(rhino.TextureMappingType.BoxMapping)
  const plc = new rhino.PolylineCurve([{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }])
  expect(plc.toPolyline().count).toBe(3)
  expect(rhino.Linetype.dashDot).toBeInstanceOf(rhino.Linetype)
})

test('stringTableAndViewInfo', () => {
  // file3dm.strings.getvalue is getValue; viewInfo.getViewport() is viewInfo.viewport
  const f = new rhino.File3dm()
  f.strings.setString('k', 'v')
  expect(f.strings.getValue('k')).toBe('v')
  expect(new rhino.ViewInfo().viewport).toBeInstanceOf(rhino.ViewportInfo)
})
