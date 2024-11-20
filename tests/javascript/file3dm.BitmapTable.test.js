const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })
//TODO
// Skipping for now.
test.skip('DeleteBitmap', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-bitmapTable-deleteBitmap'
  file3dm.applicationUrl = 'https://rhino3d.com'

  const bm1 = new rhino.Bitmap()
  const bm2 = new rhino.Bitmap()

  // .bitmaps().add() is void
  model.bitmaps().add(bm1)
  model.bitmaps().add(bm2)

  const qtyBitmaps1 = model.bitmaps().count

  const id1 = model.bitmaps().get(0).id

  model.bitmaps().delete(id1)

  const qtyBitmaps2 = model.bitmaps().count

  expect(qtyDims1 === 2 && qtyDims2 === 1).toBe(true)

})