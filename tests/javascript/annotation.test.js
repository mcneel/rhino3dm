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

    const objects = doc.objects()

    const testArray = ["Hello World!", "Hello Cruel World!", "Hi there!", "WTF"]

    for ( let i = 0; i < objects.count; i ++ ) {

        const geometry = objects.get(i).geometry()

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

    const objects = doc.objects()
    const dimStyles = doc.dimstyles()

    let annotationCount = 0

    for ( let i = 0; i < objects.count; i ++ ) {

        const geometry = objects.get(i).geometry()

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

        // Per-object effective values that previously were not exposed
        expect( geometry.getTextHeight( parent ) ).toBeGreaterThan( 0 )
        expect( geometry.getDimensionScale( parent ) ).toBeGreaterThan( 0 )

        // hasPropertyOverrides should be a boolean
        expect( typeof geometry.hasPropertyOverrides ).toBe( 'boolean' )

        // A valid bounding box now requires the parent dimension style
        const bbox = geometry.getBoundingBox( parent )
        expect( bbox.isValid ).toBe( true )

        effective.delete()
        bbox.delete()
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