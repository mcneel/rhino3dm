const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs')

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})

test('createAnnotation', async () => { 

    // read model 
    const model = '../models/textEntities_r8.3dm'

    const buffer = fs.readFileSync(model)
    const arr = new Uint8Array(buffer)
    const doc = rhino.File3dm.fromByteArray(arr)

    expect(doc !== null).toBe(true)

    const testArray = ["Hello World!", "Hello Cruel World!", "Hi there!", "WTF"]

    // D17: iterate the object table
    for ( const obj of doc.objects ) {

        const geometry = obj.geometry

        switch(geometry.objectType){
            case rhino.ObjectType.Annotation:
                expect(testArray.includes( geometry.plainText )).toBe(true)
                break
            case rhino.ObjectType.TextDot:
                expect(testArray.includes( geometry.text )).toBe(true)
                break
        }


    }



})

test('annotationEffectiveStyle', async () => {

    const model = '../models/textEntities_r8.3dm'

    const buffer = fs.readFileSync(model)
    const arr = new Uint8Array(buffer)
    const doc = rhino.File3dm.fromByteArray(arr)

    expect(doc !== null).toBe(true)

    const dimStyles = doc.dimStyles

    let annotationCount = 0

    // D17: iterate the object table
    for ( const obj of doc.objects ) {

        const geometry = obj.geometry

        if ( geometry.objectType !== rhino.ObjectType.Annotation )
            continue

        annotationCount ++

        // The parent dimension style comes from the model's dimstyle table.
        let parent = dimStyles.findId( geometry.dimensionStyleId )
        if ( parent === null )
            parent = new rhino.DimensionStyle()

        // Effective dimension style (parent + per-object overrides)
        const effective = geometry.getDimensionStyle( parent )
        expect( effective !== null ).toBe( true )

        // Per-object effective values. D67: set the annotation's own style as
        // parentDimensionStyle, then read the property
        geometry.parentDimensionStyle = parent
        expect( geometry.textHeight ).toBeGreaterThan( 0 )
        expect( geometry.dimensionScale ).toBeGreaterThan( 0 )

        // hasPropertyOverrides should be a boolean
        expect( typeof geometry.hasPropertyOverrides ).toBe( 'boolean' )

        // The bounding box is measured with parentDimensionStyle (D67), but
        // WASM has no font engine, so the text is unmeasured and the box is
        // empty (D90; 8.x returned a degenerate zero-width box)
        const bbox = geometry.getBoundingBox( true )
        expect( rhino.BoundingBox.isValid( bbox ) ).toBe( false )

        effective.delete()
        parent.delete()
    }

    expect( annotationCount ).toBeGreaterThan( 0 )

})

test('dimensionStyleModelSpaceScale', async () => {

    const ds = new rhino.DimensionStyle()
    ds.dimensionScale = 2.5
    expect( ds.dimensionScale ).toBeCloseTo( 2.5 )
    ds.delete()

})

test('plainTextToRtf', async () => {

    const rtf = rhino.AnnotationBase.plainTextToRtf( 'Hello' )
    expect( rtf.includes( 'Hello' ) ).toBe( true )
    expect( rtf.startsWith( '{\\rtf' ) ).toBe( true )

})

// the first annotation in textEntities_r8.3dm, with its file
function annotationFromTextFile(rhino) {
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  const ann = [...f.objects].map(o => o.geometry).find(g => g instanceof rhino.AnnotationBase)
  return [f, ann]
}

const annotationTexts = {
  'ec2da4f6-3e95-4a77-a327-2f4b08d457bb': 'Hello World!',
  'ac85dee1-f3b5-4582-ab90-59d0523d95fd': 'Hello Cruel World!',
}

function annotationDimensions(rhino) {
  const S = new rhino.DimensionStyle()
  const P = (x, y) => ({ X: x, Y: y, Z: 0 })
  const od = rhino.OrdinateDimension.create(S, rhino.Plane.worldXY, rhino.OrdinateDimensionMeasuredDirection.Xaxis, P(0, 0), P(5, 3), P(5, 8), 1.0, 1.0)
  const ld = rhino.LinearDimension.create(rhino.AnnotationType.Aligned, S, rhino.Plane.worldXY, { X: 1, Y: 0, Z: 0 }, P(0, 0), P(4, 1), P(2, 3), 0.0)
  return [S, od, ld]
}

// the file textEntities_r8.3dm and its first annotation, with its own style as the parent style
function annotationFromTextEntities() {
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  const ann = [...f.objects].map(o => o.geometry).find(g => g instanceof rhino.AnnotationBase)
  ann.parentDimensionStyle = [...f.allDimStyles].find(s => s.id === ann.dimensionStyleId)
  return [f, ann]
}

// the geometry of a file with a linear, an angular and a radial dimension and a leader, after a round trip
function annotationDimensionsFile() {
  const P = (X, Y, Z) => new rhino.Point3d(X, Y, Z)
  const f = new rhino.File3dm()
  f.allDimStyles.add(new rhino.DimensionStyle())
  const style = [...f.allDimStyles][0]
  const W = rhino.Plane.worldXY
  f.objects.add(rhino.LinearDimension.create(rhino.AnnotationType.Aligned, style, W, P(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0), null)
  f.objects.add(rhino.AngularDimension.create(style.id, W, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0)), null)
  f.objects.addRadialDimension(rhino.RadialDimension.create(style, rhino.AnnotationType.Radius, W, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0)))
  f.objects.addLeader('note', W, [{ X: 0, Y: 0 }, { X: 2, Y: 1 }, { X: 4, Y: 1 }], null)
  return [...rhino.File3dm.fromByteArray(f.toByteArray()).objects].map(o => o.geometry)
}

const annotationXyz = q => [q.X, q.Y, q.Z].map(v => Math.round(v * 1e9) / 1e9)

test('annotationBaseTextPropertiesAndMethods', () => {
  // AnnotationBase has the text, mask and dimension-length properties (the text ones writable) and runReplace / setRichText
  // embind gives every read-only property a setter that throws "read-only": a real setter is any other
  const proto = rhino.AnnotationBase.prototype
  const desc = name => Object.getOwnPropertyDescriptor(proto, name)
  const writable = d => d && typeof d.set === 'function' && !/read-only/.test(String(d.set))
  for (const name of ['textHeight', 'dimensionScale', 'maskEnabled', 'dimensionStyleId', 'richText']) {
    expect(typeof desc(name).get).toBe('function')
    expect(writable(desc(name))).toBe(true)
  }
  for (const name of ['maskFrame', 'maskColorSource', 'dimensionLengthDisplay', 'alternateDimensionLengthDisplay']) {
    expect(typeof desc(name).get).toBe('function')
  }
  expect(typeof proto.runReplace).toBe('function')
  expect(typeof proto.setRichText).toBe('function')
})

test('annotationBasePlainTextToRtfEscapesBracesAndNewlines', () => {
  // plainTextToRtf wraps text in RTF, turns newlines into paragraphs and escapes braces and backslashes
  expect(rhino.AnnotationBase.plainTextToRtf('a\nb')).toBe('{\\rtf1{\\ltrch a}\\par {b}}')
  expect(rhino.AnnotationBase.plainTextToRtf('x{y}\\')).toBe('{\\rtf1{\\ltrch x\\{y\\}\\\\}}')
})

test('annotationParentDimensionStyleIsStoredAndSetsTheStyleId', () => {
  // setting parentDimensionStyle stores the style, sets dimensionStyleId and drives the style-dependent members; null clears both
  const [f, ann] = annotationFromTextFile(rhino)
  expect(ann.parentDimensionStyle).toBeNull()
  const ds = [...f.allDimStyles][0]
  ann.parentDimensionStyle = ds
  const p = ann.parentDimensionStyle
  expect(p).not.toBeNull()
  expect(p.id).toBe(ds.id)
  expect(ann.dimensionStyleId).toBe(ds.id)
  // the stored object every time: embind hands out a new handle per read, of the same C++ object
  expect(ann.parentDimensionStyle.isAliasOf(p)).toBe(true)
  if (!ann.hasPropertyOverrides) expect(ann.textHeight).toBeCloseTo(ds.textHeight, 12)   // the parent's value
  expect(ann.setBold(true)).toBe(true)
  expect(ann.isAllBold()).toBe(true)
  ann.parentDimensionStyle = null
  expect(ann.parentDimensionStyle).toBeNull()
  expect(ann.dimensionStyleId).toBe('00000000-0000-0000-0000-000000000000')
  expect(p.id).toBe(ds.id)                                 // a style handed out stays valid
})

test('annotationGetBoundingBoxOverridesGeometryBase', () => {
  // getBoundingBox(xform) on an annotation is the annotation's own, also when called through GeometryBase (D90)
  const [f, ann] = annotationFromTextFile(rhino)
  ann.parentDimensionStyle = [...f.allDimStyles][0]
  const identity = rhino.Transform.identity
  const bb = ann.getBoundingBox(identity)
  expect(rhino.GeometryBase.prototype.getBoundingBox.call(ann, identity)).toEqual(bb)
  // WASM has no font engine: a text's bounding box is empty, so the measured part cannot run
  if (!rhino.BoundingBox.isValid(bb)) return
  const moved = ann.getBoundingBox(rhino.Transform.translation(new rhino.Vector3d(10, 0, 0)))
  helpers.approx(moved.Min.X - bb.Min.X, 10, 1e-9)
  helpers.approx(moved.Max.X - bb.Max.X, 10, 1e-9)
})

test('annotationPlainTextOfTextEntities', () => {
  // an annotation's text is its plainText, the text entity's own text line for line
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  let seen = 0
  for (const o of f.objects) {
    const g = o.geometry
    if (!(g instanceof rhino.AnnotationBase)) continue
    expect(g.text).toBe(g.plainText)
    expect(typeof g.textHasRtfFormatting).toBe('boolean')
    const oid = String(o.attributes.objectId)
    if (oid in annotationTexts) {
      expect(g.plainText.replace(/\r\n/g, '\n')).toBe(annotationTexts[oid])
      seen++
    }
  }
  expect(seen).toBeGreaterThan(0)
})

test('annotationPlainTextSetterAndRtfFormatting', () => {
  // setting plainText or text replaces the text; two lines read back joined by \r\n and count as RTF formatting
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  const ann = [...f.objects].find(o => String(o.attributes.objectId) in annotationTexts).geometry
  ann.plainText = 'one\ntwo'
  expect(ann.plainText).toBe('one\r\ntwo')
  expect(ann.textHasRtfFormatting).toBe(true)
  ann.text = 'single'
  expect(ann.plainText).toBe('single')
  expect(ann.textHasRtfFormatting).toBe(false)
})

test('textEntityRichTextIsRtf', () => {
  // richText of a text entity read from a file is RTF
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  const texts = [...f.objects].map(o => o.geometry).filter(g => g instanceof rhino.TextEntity)
  expect(texts.length).toBeGreaterThan(0)
  for (const g of texts) expect(g.richText.startsWith('{\\rtf')).toBe(true)
})

test('annotationDimensionScaleFromParentStyle', () => {
  // dimensionScale is the scale of the annotation's parentDimensionStyle (D39)
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  const ds = [...f.allDimStyles][0]
  let n = 0
  for (const o of f.objects) {
    const g = o.geometry
    if (g instanceof rhino.AnnotationBase && String(o.attributes.objectId) in annotationTexts) {
      g.parentDimensionStyle = ds
      expect(g.dimensionScale).toBe(10.0)
      n++
    }
  }
  expect(n).toBeGreaterThan(0)
})

test('dimensionGetDisplayLines', () => {
  // getDisplayLines(style, scale) returns [rc, lines], the lines in the dimension's plane (D90)
  const [S, od, ld] = annotationDimensions(rhino)
  for (const d of [od, ld]) d.updateDimensionText(S, rhino.UnitSystem.Millimeters)   // the text rectangle needs formatted text
  const [orc] = od.getDisplayLines(S, 1.0)
  expect(typeof orc).toBe('boolean')
  const [trc] = ld.getTextRectangle()
  // WASM has no font engine: no text rectangle, so no display lines (as for text bounding boxes)
  if (!trc) return
  const [rc, lines] = ld.getDisplayLines(S, 1.0)
  expect(rc).toBe(true)
  expect(lines.length).toBeGreaterThan(0)
})

test('dimensionsReadBackAsTheirOwnClass', () => {
  // ordinate and linear dimensions added to a file read back as OrdinateDimension and LinearDimension
  const [, od, ld] = annotationDimensions(rhino)
  const f = new rhino.File3dm()
  const ids = [f.objects.addOrdinateDimension(od), f.objects.addLinearDimension(ld)]
  const back = rhino.File3dm.fromByteArray(f.toByteArray(null))
  const geom = {}
  for (const o of back.objects) geom[String(o.attributes.objectId)] = o.geometry
  expect(geom[String(ids[0])]).toBeInstanceOf(rhino.OrdinateDimension)
  expect(geom[String(ids[1])]).toBeInstanceOf(rhino.LinearDimension)
})

test('linearDimensionCreateKeepsItsStyleAlive', async () => {
  // a dimension keeps the style create or the parentDimensionStyle setter gave it, even a temporary one (D39)
  const P = (x, y) => ({ X: x, Y: y, Z: 0 })
  const ld = rhino.LinearDimension.create(rhino.AnnotationType.Aligned, new rhino.DimensionStyle(), rhino.Plane.worldXY, { X: 1, Y: 0, Z: 0 }, P(0, 0), P(4, 1), P(2, 3), 0.0)
  await helpers.gcSettle()
  expect(ld.parentDimensionStyle.textHeight).toBeGreaterThan(0)
  ld.parentDimensionStyle = new rhino.DimensionStyle()
  await helpers.gcSettle()
  expect(ld.parentDimensionStyle.textHeight).toBeGreaterThan(0)
})

test('annotationStylePropertiesReadAndWriteOnTheAnnotation', () => {
  // With parentDimensionStyle set, an annotation's style properties read and write on the annotation, keeping its overrides (D39, D67)
  const f = helpers.readModel(rhino, 'textEntities_r8.3dm')
  const ann = [...f.objects].map(o => o.geometry).find(g => g instanceof rhino.AnnotationBase)
  expect(ann).toBeTruthy()
  expect(String(ann.dimensionStyleId)).toBe('6d104644-18b4-464c-a49c-dd4892fcbe39')
  const style = [...f.allDimStyles].find(s => s.id === ann.dimensionStyleId)
  expect(style).toBeTruthy()
  ann.parentDimensionStyle = style
  expect(ann.textHeight).toBe(10.0)                      // the override survives
  const value = v => (v && typeof v === 'object' && 'value' in v ? v.value : v)   // an enum by its value
  const expected = { textHeight: 10.0, maskOffset: 0.8, maskEnabled: false, maskColorSource: 0, maskFrame: 0,
                     dimensionLengthDisplay: 3, alternateDimensionLengthDisplay: 7 }
  for (const [p, v] of Object.entries(expected)) expect([p, value(ann[p])]).toEqual([p, v])
  for (const [p, v] of [['textHeight', 7.5], ['maskOffset', 0.25], ['maskEnabled', true]]) {
    ann[p] = v
    expect([p, ann[p]]).toEqual([p, v])
  }
  ann.dimensionScale = 2.0
  expect(ann.dimensionScale).toBe(2.0)
})

test('annotationTightBoundingBoxIsItsOwnBox', () => {
  // An annotation's getBoundingBox(true) is its own box, the same as with a transform; the plane overload works too
  const [, ann] = annotationFromTextEntities()
  expect(ann.getBoundingBox(true)).toEqual(ann.getBoundingBox(rhino.Transform.identity))
  expect(ann.getBoundingBox(rhino.Plane.worldXY)).toBeTruthy()
})

test('annotationFontReadsAndSets', () => {
  // font reads the annotation's font and sets a new one
  const [, ann] = annotationFromTextEntities()
  const font = ann.font
  expect([font.familyName, font.faceName, font.bold]).toEqual(['Avenir', 'Black', true])
  expect(ann.firstCharFont.familyName).toBeTruthy()
  ann.font = new rhino.Font('Courier New')
  expect(ann.font.familyName).toBe('Courier New')
})

test('annotationFontSetToNoneRaises', () => {
  // Setting an annotation's font to null throws, as RhinoCommon throws NullReferenceException
  const [, ann] = annotationFromTextEntities()
  helpers.raises(rhino, () => { ann.font = null })
})

test('annotationRunReplace', () => {
  // runReplace replaces characters in a text run of the annotation's plain text
  const [, ann] = annotationFromTextEntities()
  expect(ann.runReplace('ZZ', 0, 0, 0, 1)).toBe(true)
  expect(ann.plainText).toBe('ZZllo World!')
})

test('leaderPoints3DAndPoints2D', () => {
  // A leader's points3D give its points, after create and after a file round trip
  const P = (X, Y, Z) => new rhino.Point3d(X, Y, Z)
  const le = rhino.Leader.create('note', rhino.Plane.worldXY, new rhino.DimensionStyle(), [P(0, 0, 0), P(2, 1, 0), P(4, 1, 0)])
  expect([...le.points3D][2].X).toBe(4)
  const ld = annotationDimensionsFile().find(g => g instanceof rhino.Leader)
  expect([...ld.points3D].map(annotationXyz)).toEqual([[0, 0, 0], [2, 1, 0], [4, 1, 0]])
})

test('dimensionGet3dPointsLeadsWithTheSuccessFlag', () => {
  // get3dPoints of a dimension returns an array led by the success flag, then the dimension's points (D68)
  const news = annotationDimensionsFile()
  const expected = [[rhino.LinearDimension, [[0, 0, 0], [4, 1, 0], [0, 3, 0], [4, 3, 0]]],
                    [rhino.AngularDimension, [[0, 4, 0], [1, 0, 0], [0, 1, 0], [0.766964989, 0.932140045, 0], [0, 0.83772234, 0],
                                              [2.445086463, 6.005380809, 0], [-0.386377405, 7.138584474, 0]]]]
  for (const [N, pts] of expected) {
    const rc = news.find(g => g instanceof N).get3dPoints()
    expect(rc[0]).toBe(true)
    pts.forEach((p, i) => expect(annotationXyz(rc[i + 1])).toEqual(p))
  }
})

test('angularDimensionNumericValueIsTheAngle', () => {
  // An angular dimension's numericValue is its angle
  const an = annotationDimensionsFile().find(g => g instanceof rhino.AngularDimension)
  expect(Math.abs(an.numericValue - 6.038206644052722)).toBeLessThan(1e-12)
})
