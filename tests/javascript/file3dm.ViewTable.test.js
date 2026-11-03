const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
beforeEach(async () => {
  rhino = await rhino3dm()
})
//TODO
test('CreateFileWithView', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests'
  file3dm.applicationUrl = 'https://rhino3d.com'

  // make some geometry
  for (let i = 0; i < 100; i++) {

    const x = Math.random() * 100
    const y = Math.random() * 100
    const z = Math.random() * 100
    const sphere = rhino.Sphere.create({ X: x, Y: y, Z: z }, 10)

    file3dm.objects.addSphere(sphere, null)

  }

  const vi = new rhino.ViewInfo()
  vi.name = 'main_js'

  // D11: points are {X, Y, Z}
  const loc = { X: 50, Y: 50, Z: 100 }

  // getViewport() -> viewport
  vi.viewport.setCameraLocation({ X: 50, Y: 50, Z: 100 })
  const loc2 = vi.viewport.cameraLocation 

  file3dm.views.add(vi)
  loc3 = vi.viewport.cameraLocation

  const bufferWrite = file3dm.toByteArray()
  fs.writeFileSync('test_createFileWithView.3dm', bufferWrite)

  const bufferRead = fs.readFileSync('test_createFileWithView.3dm')
  const arr = new Uint8Array(bufferRead)
  const file3dmRead = rhino.File3dm.fromByteArray(arr)

  const vi_read = file3dmRead.views.get(0)
  const vp_read = vi_read.viewport
  const loc_read = vp_read.cameraLocation

  expect(JSON.stringify(loc) === JSON.stringify(loc_read) && JSON.stringify(loc2) === JSON.stringify(loc_read) && JSON.stringify(loc3) === JSON.stringify(loc_read)).toBe(true)

})