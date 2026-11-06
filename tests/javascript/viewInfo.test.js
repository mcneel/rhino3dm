const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('viewInfoPageSettingsRoundTrip', () => {
  // setPageSettings stores the page settings that getPageSettings returns
  const v = new rhino.ViewInfo()
  const ps = new rhino.PageSettings()
  ps.pageNumber = 3
  ps.widthMillimeters = 210.0
  ps.printerName = 'laser'
  v.setPageSettings(ps)
  const back = v.getPageSettings()
  expect([back.pageNumber, back.widthMillimeters, back.printerName]).toEqual([3, 210.0, 'laser'])
})

test('viewInfoDefaultConstructor', () => {
  // new ViewInfo() constructs a ViewInfo
  expect(new rhino.ViewInfo()).toBeInstanceOf(rhino.ViewInfo)
})

test('viewInfoViewportIsHeldAndKeepsEdits', async () => {
  // viewport returns the same object every time; an edit through it stays with the view, which it keeps alive
  let v = helpers.readModel(rhino, 'file3dm_stuff.3dm').views.get(0)
  const vp = v.viewport
  expect(vp.isAliasOf(v.viewport)).toBe(true)
  // the camera stored in file3dm_stuff.3dm
  const cl = vp.cameraLocation
  expect([cl.X, cl.Y, cl.Z]).toEqual([-1039.436783184503, -313.98699366950444, 743.7751118904666])
  expect(vp.setCameraLocation(new rhino.Point3d(1, 2, 3))).toBe(true)
  const again = v.viewport.cameraLocation
  expect([again.X, again.Y, again.Z]).toEqual([1, 2, 3])
  v = null
  await helpers.gcSettle()
  expect(vp.cameraLocation.Z).toBe(3)
})

test('viewInfoWallpaperFilenameIsEmptyWithoutWallpaper', () => {
  // wallpaperFilename of a view with no wallpaper is the empty string
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  expect(f.views.get(0).wallpaperFilename).toBe('')
})

test('viewInfoNewHasAnEmptyName', () => {
  // a new ViewInfo has an empty name
  expect(new rhino.ViewInfo().name).toBe('')
})

test('viewInfoViewportEditsLandInTheView', () => {
  // an edit of ViewInfo.viewport changes the view, so it survives views.add and a write and read
  const loc = { X: 50, Y: 50, Z: 100 }
  const v = new rhino.ViewInfo()
  v.viewport.setCameraLocation(loc)
  expect(v.viewport.cameraLocation).toEqual(loc)
  const f = new rhino.File3dm()
  f.views.add(v)
  expect(f.views.get(0).viewport.cameraLocation).toEqual(loc)
  const g = rhino.File3dm.fromByteArray(f.toByteArray())
  expect(g.views.get(0).viewport.cameraLocation).toEqual(loc)
})
