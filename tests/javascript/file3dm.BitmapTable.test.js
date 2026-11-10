const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

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

  expect(qtyBitmaps1 === 2 && qtyBitmaps2 === 1).toBe(true)

})

const bitmapTablePaths = ['/tmp/rh3dm_a.png', '/tmp/rh3dm_b.png', '/tmp/rh3dm_c.png']

// a File3dm with three bitmaps: [file, ids]
function bitmapTableFile(rhino) {
  const f = new rhino.File3dm()
  const ids = bitmapTablePaths.map(p => {
    const b = new rhino.Bitmap()
    b.fileFullPath = p
    return f.bitmaps.add(b).id
  })
  return [f, ids]
}

test('bitmapTableAddReturnsTheFilesCopy', () => {
  // bitmaps.add adds a Bitmap and returns the file's copy, with its path and a new id
  const f = new rhino.File3dm()
  const ids = []
  for (const p of bitmapTablePaths) {
    const b = new rhino.Bitmap()
    b.fileFullPath = p
    const added = f.bitmaps.add(b)
    expect(added == null).toBe(false)
    expect(added.fileFullPath).toBe(p)
    ids.push(added.id)
  }
  expect(f.bitmaps.count).toBe(3)
  expect(new Set(ids.map(String)).size).toBe(3)
})

test('bitmapTableFindIdFindIndexAndIndexer', () => {
  // findIndex and get are positional, findId finds by id; all three return null when absent
  const [f, ids] = bitmapTableFile(rhino)
  expect([0, 1, 2].map(i => f.bitmaps.findIndex(i).fileFullPath)).toEqual(bitmapTablePaths)
  expect(f.bitmaps.get(1).fileFullPath).toBe(bitmapTablePaths[1])
  expect(f.bitmaps.findId(ids[1]).fileFullPath).toBe(bitmapTablePaths[1])
  expect(f.bitmaps.findId(require('crypto').randomUUID())).toBeNull()
  expect(f.bitmaps.findIndex(999)).toBeNull()
  // get(i) is RhinoCommon's indexer (FindIndex): null past the end (Python's [i] raises IndexError)
  expect(f.bitmaps.get(999)).toBeNull()
})

test('bitmapTableImagePathCannotChange', () => {
  // setting fileFullPath on a bitmap in a file raises: the file keys images by path
  const [f, ids] = bitmapTableFile(rhino)
  helpers.raises(rhino, () => { f.bitmaps.findId(ids[0]).fileFullPath = '/tmp/other.png' },
    /runtime_error,Bitmap: the path of an image in a File3dm cannot be changed/)
})

test('bitmapTableDeleteThenIterate', () => {
  // delete removes a bitmap, and iterating afterwards yields the remaining ones
  const [f, ids] = bitmapTableFile(rhino)
  expect(f.bitmaps.delete(ids[0])).toBe(true)
  expect(f.bitmaps.count).toBe(2)
  expect([...f.bitmaps].map(b => b.fileFullPath)).toEqual(bitmapTablePaths.slice(1))
})

test('bitmapTableRoundTripsThroughAFile', async () => {
  // bitmaps are written and read back, and a bitmap read from a file keeps the file alive
  const [f, ids] = bitmapTableFile(rhino)
  f.bitmaps.delete(ids[0])
  const out = '/tmp/rh3dm_bitmaps_' + process.pid + '.3dm'   // the module's in-memory file system
  expect(f.write(out, null)).toBe(true)
  const bm = (() => {
    const g = rhino.File3dm.read(out)
    expect([...g.bitmaps].map(b => b.fileFullPath).sort()).toEqual(bitmapTablePaths.slice(1).sort())
    return [...g.bitmaps][0]
  })()
  await helpers.gcSettle()
  expect(bitmapTablePaths).toContain(bm.fileFullPath)
})
