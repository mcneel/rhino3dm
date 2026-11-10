const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
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

test('viewTableDelete', () => {
  // views.delete(index) removes a view, and throws for an index past the end
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const n = f.views.count
  expect(n).toBeGreaterThan(0)
  expect(f.views.delete(0)).toBe(true)
  expect(f.views.count).toBe(n - 1)
  helpers.raises(rhino, () => f.views.delete(99), /out_of_range/)
})

test('viewTableItemsAreLiveViews', () => {
  // views iterates over ViewInfo items, and views.get(i) is the file's view: writes to it reach the file
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const views = [...f.views]
  expect(views.length).toBe(f.views.count)
  expect(views.length).toBeGreaterThan(0)
  expect(views.every(x => x instanceof rhino.ViewInfo)).toBe(true)
  const v = f.views.get(0)
  v.name = 'live'
  expect(f.views.get(0).name).toBe('live')
})

test('viewTableViewKeepsTheFileAlive', async () => {
  // a view taken from views stays usable after the file is dropped
  const v = (() => {
    const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
    const v0 = f.views.get(0)
    v0.name = 'live'
    return v0
  })()
  await helpers.gcSettle()
  expect(v.name).toBe('live')
})

test('namedViewsSharingAViewportIdStayDistinct', () => {
  // named views saved from one viewport share its id; namedViews.get(i) still reads and writes view i
  const f = new rhino.File3dm()
  const shared = '2b0f9a4e-6c1d-4f3a-9e57-3f8d1c2a7b64'
  for (const name of ['One', 'Two', 'Three']) {
    const vi = new rhino.ViewInfo()
    vi.name = name
    vi.viewport.id = shared
    f.namedViews.add(vi)
  }
  expect([...f.namedViews].map(v => v.name)).toEqual(['One', 'Two', 'Three'])
  f.namedViews.get(2).name = 'Third'
  expect([0, 1, 2].map(i => f.namedViews.get(i).name)).toEqual(['One', 'Two', 'Third'])
})

test('viewTableIndexOutOfRangeRaises', () => {
  // views.get(i) past the end throws
  helpers.raises(rhino, () => helpers.readModel(rhino, 'file3dm_stuff.3dm').views.get(99), /out_of_range/)
})
