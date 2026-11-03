const rhino3dm = require('rhino3dm')
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