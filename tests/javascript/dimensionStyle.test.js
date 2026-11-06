const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('dimensionStyleArrowLengthSetterWritesThrough', () => {
  // setting arrowLength changes the style's arrow length
  const ds = new rhino.DimensionStyle()
  ds.arrowLength = 0.75
  expect(ds.arrowLength).toBe(0.75)
})

test('dimensionStyleArrowTypeIsNestedInDimensionStyle', () => {
  // the nested enum DimensionStyle.ArrowType is rhino.DimensionStyleArrowType in JS (D18)
  const E = rhino.DimensionStyleArrowType
  expect(E.Dot).toBeDefined()
  const ds = new rhino.DimensionStyle()
  ds.arrowType1 = E.Dot
  expect(ds.arrowType1).toBe(E.Dot)
})

test('dimensionStyleFontOfFileStyles', () => {
  // each dimension style of a file has its font
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  const faces = { '3eb2bbaf-5018-485f-be99-568fd0918a84': 'Regular', '3ca66641-8cfc-447e-9ae9-ca6a21cfb828': 'Regular',
    '6d104644-18b4-464c-a49c-dd4892fcbe39': 'Regular', '7f6f1c3a-f6a7-4c30-ae0e-44b5631d9c65': 'Regular' }
  for (const ds of f.allDimStyles) expect(ds.font.faceName).toBe(faces[String(ds.id)])
})

test('dimensionStyleFontOutlivesTheStyle', () => {
  // the font of a dimension style, its default or one it was given, stays usable after the style is deleted
  const s0 = new rhino.DimensionStyle()
  const viaStyle = s0.font
  s0.delete()
  expect(viaStyle.familyName).toBeTruthy()
  const s = new rhino.DimensionStyle()
  s.font = new rhino.Font('Courier New')
  const got = s.font
  s.delete()
  expect(got.familyName).toBeTruthy()
})

test('dimensionStyleFitTextAndFitArrowSetters', () => {
  // fitText and fitArrow accept the values they read
  const s = new rhino.DimensionStyle()
  const ft = s.fitText, fa = s.fitArrow
  s.fitText = ft
  s.fitArrow = fa
  expect([s.fitText, s.fitArrow]).toEqual([ft, fa])
})

test('dimensionStyleFieldHasRhinoCommonsValues', () => {
  // DimensionStyleField has all of RhinoCommon's DimensionStyle.Field values (D18)
  const F = rhino.DimensionStyleField
  expect(Object.keys(F.values).length).toBe(119)
  expect([F.Arrowsize.value, F.ZeroSuppress.value, F.ClippingArrowSize.value, F.Count.value]).toEqual([5, 76, 119, 122])
})

test('dimensionStyleSetFieldOverride', () => {
  // setFieldOverride marks only that field as overridden
  const F = rhino.DimensionStyleField
  const ds = new rhino.DimensionStyle()
  ds.setFieldOverride(F.TextHeight)
  expect(ds.isFieldOverriden(F.TextHeight)).toBe(true)
  expect(ds.isFieldOverriden(F.Arrowsize)).toBe(false)
})
