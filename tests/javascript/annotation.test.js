const rhino3dm = require('rhino3dm')
const fs = require('fs')
const { fail } = require('assert')

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

    const testArray = ["Hello World!", "Hello Cruel World!"]

    for ( let i = 0; i < objects.count; i ++ ) {

        const geometry = objects.get(i).geometry
        if(!testArray.includes( geometry.PlainText )){
            fail(`Text not read correctly: ${geometry.PlainText}`)
        }
        
    }

    //assert.pass()

})