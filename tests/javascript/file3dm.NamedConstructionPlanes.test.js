const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('namedConstructionPlanesOfANewFileIsEmpty', () => {
  // a new file has no named construction planes
  expect(new rhino.File3dm().namedConstructionPlanes.count).toBe(0)
})

test('namedConstructionPlanesAddAndIndex', () => {
  // add stores a construction plane that get returns, and it survives a write and read
  const f = new rhino.File3dm()
  const cp = new rhino.ConstructionPlane()
  cp.name = 'top'
  cp.gridSpacing = 2.5
  f.namedConstructionPlanes.add(cp)
  expect(f.namedConstructionPlanes.count).toBe(1)
  const got = f.namedConstructionPlanes.get(0)
  expect(got.name).toBe('top')
  expect(got.gridSpacing).toBe(2.5)
  // the module's own in-memory file system (WASM has no host file system)
  const path = '/tmp/rh3dm_js_named_cplanes.3dm'
  expect(f.write(path, 7)).toBe(true)
  const g = rhino.File3dm.read(path)
  expect(g.namedConstructionPlanes.get(0).name).toBe('top')
})
