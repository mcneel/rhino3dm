const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('brepVertexGetEdgeIndices', async () => {

   const sphere = rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5)
   const bbox = rhino.Sphere.toBrep(sphere).getBoundingBox(false)
   const brep = rhino.Brep.createFromBox(bbox)
   const vertex = brep.vertices.get(0)
   const edgeIndices = vertex.edgeIndices()

   expect(Array.isArray(edgeIndices)).toBe(true)
   expect(edgeIndices.length).toBe(3)
   expect(typeof edgeIndices[0] === "number").toBe(true)

})

test('brepTranslateByComponents', () => {
  // translate(x, y, z) moves the brep and returns true
  const b = helpers.boxBrep(rhino)
  expect(b.translate(1.0, 2.0, 3.0)).toBe(true)
  expect(b.getBoundingBox(true).Min).toEqual(new rhino.Point3d(1, 2, 3))
})

test('brepDuplicateIsAnIndependentBrep', () => {
  // duplicate of a Brep is a new Brep with the same faces
  const b = helpers.boxBrep(rhino)
  const d = b.duplicate()
  expect(d).toBeInstanceOf(rhino.Brep)
  expect(d.isAliasOf(b)).toBe(false)
  expect(d.faces.count).toBe(b.faces.count)
})

test('brepDefaultConstructorIsEmpty', () => {
  // a new Brep has no faces
  expect(new rhino.Brep().faces.count).toBe(0)
})

test('brepDuplicateBrepIsABrep', () => {
  // duplicateBrep returns a Brep with the same faces
  const d = helpers.boxBrep(rhino).duplicateBrep()
  expect(d).toBeInstanceOf(rhino.Brep)
  expect(d.faces.count).toBe(6)
})

test('brepTryConvertBrepOfAFileBrepIsANewBrep', () => {
  // tryConvertBrep of a brep read from a file returns a new Brep; editing it leaves the argument alone (D42)
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const g = helpers.firstGeometry(f, rhino.Brep)
  const b = rhino.Brep.tryConvertBrep(g)
  expect(b).toBeInstanceOf(rhino.Brep)
  expect(b.isAliasOf(g)).toBe(false)
  expect(b.faces.count).toBe(g.faces.count)
  const identity = rhino.Transform.identity
  const x0 = g.getBoundingBox(identity).Min.X
  expect(b.transform(rhino.Transform.translation(new rhino.Vector3d(100, 0, 0)))).toBe(true)
  helpers.approx(b.getBoundingBox(identity).Min.X - x0, 100, 1e-6)
  expect(g.getBoundingBox(identity).Min.X).toBe(x0)
  g.delete(); f.delete()
  expect(b.faces.count).toBeGreaterThan(0)          // the result keeps the file alive
})

test('brepTryConvertBrepOfABrepIsACopy', () => {
  // tryConvertBrep of a Brep made in memory returns another Brep with the same faces (D42)
  const own = helpers.boxBrep(rhino)
  const c = rhino.Brep.tryConvertBrep(own)
  expect(c.isAliasOf(own)).toBe(false)
  expect(c.faces.count).toBe(own.faces.count)
})

test('brepCreateFromBoxBoundingBoxAndBox', () => {
  // Brep.createFromBox takes a BoundingBox or a Box and makes a six-face brep
  const bb = new rhino.BoundingBox({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 })
  expect(rhino.Brep.createFromBox(bb).faces.count).toBe(6)
  expect(rhino.Brep.createFromBox(new rhino.Box(bb)).faces.count).toBe(6)
})

test('brepCreateTrimmedPlaneFromACurveOrAList', () => {
  // Brep.createTrimmedPlane takes one boundary curve or a list of them
  const circle = new rhino.ArcCurve(new rhino.Circle(2.0))
  const one = rhino.Brep.createTrimmedPlane(rhino.Plane.worldXY, circle)
  const many = rhino.Brep.createTrimmedPlane(rhino.Plane.worldXY, [circle])
  expect(one.faces.count).toBe(1)
  expect(many.faces.count).toBe(1)
  expect(one.edges.count).toBe(1)
})

test('brepSurfaceListIndexAndIterate', () => {
  // Brep.surfaces has a count, an indexer that throws past the end, and iterates
  const srfs = helpers.boxBrep(rhino).surfaces
  expect(srfs.count).toBe(6)
  const items = [...srfs]
  expect(items.length).toBe(6)
  expect(items.every(s => s instanceof rhino.Surface && s.isPlanar())).toBe(true)
  helpers.raises(rhino, () => srfs.get(6), /out_of_range/)
})

test('brepSurfaceKeepsItsBrepAlive', async () => {
  // a surface taken from Brep.surfaces stays usable after the brep and the list are dropped
  const s0 = (() => helpers.boxBrep(rhino).surfaces.get(0))()
  await helpers.gcSettle()
  expect(s0.isPlanar()).toBe(true)
})

test('brepGetBoundingBoxOfABox', () => {
  // getBoundingBox(false) of a box brep is the box
  const b = helpers.boxBrep(rhino).getBoundingBox(false)
  expect([b.Min.X, b.Max.Y, b.Max.Z]).toEqual([0, 2, 3])
})
