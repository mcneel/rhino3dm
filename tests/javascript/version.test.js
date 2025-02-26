const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})

test('versionInfo', async () => { 

    console.log(rhino.Version)
    console.log(typeof rhino.Version)

})