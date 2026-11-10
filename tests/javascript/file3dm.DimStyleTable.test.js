const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })
//TODO
// Skipping for now.
test('DeleteDimStyle', async () => {

  const file3dm = new rhino.File3dm()
  file3dm.applicationName = 'rhino3dm.js'
  file3dm.applicationDetails = 'rhino3dm-tests-dimStyleTable-deleteDimStyle'
  file3dm.applicationUrl = 'https://rhino3d.com'

  const ds1 = new rhino.DimensionStyle()
  const ds2 = new rhino.DimensionStyle()

  // .dimStyles.add() is void
  file3dm.dimStyles.add(ds1)
  file3dm.dimStyles.add(ds2)

  const qtyDimStyles1 = file3dm.dimStyles.count

  const item1 = file3dm.dimStyles.get(0)

  // D14: delete(item), the id form is gone
  file3dm.dimStyles.delete(item1)

  const qtyDimStyles2 = file3dm.dimStyles.count

  expect(qtyDimStyles1 === 2 && qtyDimStyles2 === 1).toBe(true)

})