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