const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
beforeEach(async () => {
    rhino = await rhino3dm()
})

//objective: to test that adding a user string to an object and reading it back returns the same string
test('objectUserString', async () => {

    const key = 'testKey'
    const value = 'Hello sweet world!'

    const file3dm = new rhino.File3dm()
    file3dm.applicationName = 'rhino3dm.js'
    file3dm.applicationDetails = 'rhino3dm-tests'
    file3dm.applicationUrl = 'https://rhino3d.com'

    circle = new rhino.Circle([0, 0, 0], 5)

    oa = new rhino.ObjectAttributes()
    oa.setUserString(key, value)

    file3dm.objects().addCircle(circle, oa)

    const bufferWrite = file3dm.toByteArray()
    fs.writeFileSync('test_js_objectUserString.3dm', bufferWrite)

    const bufferRead = fs.readFileSync('test_js_objectUserString.3dm')
    const arr = new Uint8Array(bufferRead)
    const file3dmRead = rhino.File3dm.fromByteArray(arr)

    const obj = file3dmRead.objects().get(0)

    valueRead = obj.attributes().getUserString(key)

    expect(valueRead === value).toBe(true)

})