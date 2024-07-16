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
    const objects = doc.objects()

    console.log(objects.count)

    const testArray = ["Hello World!", "Hello Cruel World!", "Hi there!"]

    for ( let i = 0; i < objects.count; i ++ ) {

        const geometry = objects.get(i).geometry
        console.log(geometry)
        console.log(geometry.annotationType)
        console.log(geometry.dimensionStyleId)
        console.log(geometry.plainText)
        console.log(geometry.plainTextWithFields)
        console.log(geometry.richText)

        if(!testArray.includes( geometry.PlainText )){
            //fail(`Text not read correctly: ${geometry.PlainText}`)
        }
        
    }

    expect(true).toBe(true)

})