const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function boxBrep(rhino) {
    const sphere = rhino.Sphere.create({ X: 0, Y: 0, Z: 0 }, 5)
    const bbox = rhino.Sphere.toBrep(sphere).getBoundingBox(false)
    return rhino.Brep.createFromBox(bbox)
}

// RH3DM #713 follow-up: Brep loop/trim topology traversal (Py + JS parity).
test('faceLoopsAndTrims', async () => {
    const brep = boxBrep(rhino)
    const face = brep.faces.get(0)

    const loops = face.loops
    expect(loops.count).toBe(1)

    const loop = loops.get(0)
    expect(loop.loopType).toBe(rhino.BrepLoopType.Outer)
    expect(loop.trims.count).toBe(4)

    const trims = loop.trims
    expect(trims.count).toBe(4)

    const trim = trims.get(0)
    expect(typeof trim.edge.edgeIndex === 'number').toBe(true)
    expect(trim.edge.edgeIndex).toBeGreaterThanOrEqual(0)
    expect(typeof trim.isReversed() === 'boolean').toBe(true)
    expect(typeof trim.startVertex.vertexIndex === 'number').toBe(true)
    expect(typeof trim.endVertex.vertexIndex === 'number').toBe(true)
})

test('outerLoop', async () => {
    const brep = boxBrep(rhino)
    const face = brep.faces.get(0)

    const outer = face.outerLoop
    expect(outer).not.toBeNull()
    expect(outer.loopType).toBe(rhino.BrepLoopType.Outer)
    expect(outer.trims.count).toBe(4)
})

test('loopTypeEnum', async () => {
    // enum exposed and matches RhinoCommon BrepLoopType ordinals
    expect(rhino.BrepLoopType.Unknown.value).toBe(0)
    expect(rhino.BrepLoopType.Outer.value).toBe(1)
    expect(rhino.BrepLoopType.Inner.value).toBe(2)
    expect(rhino.BrepLoopType.Slit.value).toBe(3)
    expect(rhino.BrepLoopType.CurveOnSurface.value).toBe(4)
    expect(rhino.BrepLoopType.PointOnSurface.value).toBe(5)
})

// the items of a count/get(i) list (BrepFaceList and the other this[int] lists)
function brepTopologyItems(coll) {
  const out = []
  for (let i = 0; i < coll.count; i++) out.push(coll.get(i))
  return out
}

test('brepFaceListIsASequenceOfFaceComponents', () => {
  // brep.faces lists the BrepFaces by index, each a SurfaceProxy; past the end throws
  const b = helpers.boxBrep(rhino)
  const faces = b.faces
  expect(faces.count).toBe(6)
  expect(brepTopologyItems(faces).map(f => f.faceIndex)).toEqual([0, 1, 2, 3, 4, 5])
  const f = faces.get(2)
  expect(f instanceof rhino.SurfaceProxy).toBe(true)
  expect(f instanceof rhino.Surface).toBe(true)
  expect(f.isSurface).toBe(true)              // BrepFace's own member
  expect(f.isPlanar(1e-9)).toBe(true)         // inherited from Surface
  helpers.raises(rhino, () => faces.get(6), /out_of_range/)
})

test('brepFaceWritesLandInTheBrep', () => {
  // setting a property on a BrepFace changes the brep's face
  const b = helpers.boxBrep(rhino)
  const f = b.faces.get(2)
  const was = f.orientationIsReversed
  f.orientationIsReversed = !was
  expect(b.faces.get(2).orientationIsReversed).toBe(!was)
})

test('brepFaceKeepsItsBrepAlive', async () => {
  // a BrepFace stays usable after the last reference to its brep is dropped
  const f = (() => helpers.boxBrep(rhino).faces.get(0))()
  await helpers.gcSettle()
  expect(f.faceIndex).toBe(0)
  expect(f.isSurface).toBe(true)
})

test('brepEdgesAndVerticesAreComponentsOfTheBrep', () => {
  // brep.edges and brep.vertices list the brep's edges (CurveProxy) and vertices by index
  const b = helpers.boxBrep(rhino)
  expect(b.edges.count).toBe(12)
  expect(b.vertices.count).toBe(8)
  const e = b.edges.get(3)
  expect(e.edgeIndex).toBe(3)
  expect(e instanceof rhino.CurveProxy).toBe(true)
  expect(e instanceof rhino.Curve).toBe(true)
  expect(b.vertices.get(7).vertexIndex).toBe(7)
})

test('brepLoopsAndTrimsAreComponentsOfTheBrep', () => {
  // brep.loops and brep.trims list the brep's loops and trims; a loop reaches its face
  const b = helpers.boxBrep(rhino)
  const loops = b.loops, trims = b.trims
  expect(loops.count).toBe(6)                 // a box: one loop per face ...
  expect(trims.count).toBe(24)                // ... and 4 trims per loop
  expect(brepTopologyItems(loops).map(l => l.loopIndex)).toEqual([0, 1, 2, 3, 4, 5])
  for (const t of brepTopologyItems(trims)) {
    expect(t instanceof rhino.BrepTrim).toBe(true)
    expect(t instanceof rhino.Curve).toBe(true)
  }
  expect(trims.get(5).trimIndex).toBe(5)
  expect(loops.get(0).face.faceIndex).toBe(0)
  helpers.raises(rhino, () => loops.get(6), /out_of_range/)
})

test('brepTrimSetTolerancesAcceptsUAndV', () => {
  // BrepTrim.setTolerances takes the u and v tolerances
  const t = helpers.boxBrep(rhino).trims.get(0)
  t.setTolerances(0.25, 0.5)
})

test('brepFaceUnderlyingSurfaceCopiesOnWrite', () => {
  // changing the surface from BrepFace.underlyingSurface leaves the face's surface unchanged
  const face = helpers.boxBrep(rhino).faces.get(0)
  const srf = face.underlyingSurface()
  const before = face.underlyingSurface().pointAt(0.0, 0.0)
  srf.translate(new rhino.Vector3d(0, 0, 5))
  const moved = srf.pointAt(0.0, 0.0)
  const after = face.underlyingSurface().pointAt(0.0, 0.0)
  expect(moved.Z).toBe(before.Z + 5)
  expect([after.X, after.Y, after.Z]).toEqual([before.X, before.Y, before.Z])
})

test('brepFaceGetMeshIsNoneOnANewBox', () => {
  // BrepFace.getMesh returns null when the face has no mesh of that type
  expect(helpers.boxBrep(rhino).faces.get(0).getMesh(rhino.MeshType.Render)).toBeNull()
})

test('brepFaceSetMeshCopiesTheMeshIntoTheFace', () => {
  // BrepFace.setMesh gives the face the mesh; later changes to the Mesh object do not reach the face
  const face = helpers.boxBrep(rhino).faces.get(0)
  const m = helpers.quadMesh(rhino)
  expect(face.setMesh(rhino.MeshType.Render, m)).toBe(true)
  const got = face.getMesh(rhino.MeshType.Render)
  expect(got).not.toBeNull()
  expect(got.vertices.count).toBe(4)
  expect(m.vertices.count).toBe(4)
  m.vertices.clear()
  expect(m.vertices.count).toBe(0)
  expect(face.getMesh(rhino.MeshType.Render).vertices.count).toBe(4)
})

test('brepFaceComponentIndexIsBrepFaceAndItsIndex', () => {
  // BrepFace.componentIndex is a BrepFace component index with the face's index
  const face = helpers.boxBrep(rhino).faces.get(1)
  const CI = rhino.ComponentIndex
  expect(CI.equals(face.componentIndex(), new CI(rhino.ComponentIndexType.BrepFace, 1))).toBe(true)
})

test('brepEdgeValenceOfABoxIsInterior', () => {
  // every edge of a closed box has Interior valence
  const b = helpers.boxBrep(rhino)
  for (let i = 0; i < b.edges.count; i++) expect(b.edges.get(i).valence).toBe(rhino.EdgeAdjacency.Interior)
})

test('brepVertexEdgeIndicesOfABoxCorner', () => {
  // BrepVertex.edgeIndices lists the three edge indices at a box corner
  const edges = helpers.boxBrep(rhino).vertices.get(0).edgeIndices()
  expect(edges.length).toBe(3)
  expect(edges.every(i => Number.isInteger(i))).toBe(true)
})

test('brepEdgeAdjacentFacesAndTrimIndices', () => {
  // a box edge has two adjacent faces and two trims
  const e = helpers.boxBrep(rhino).edges.get(0)
  expect(e.adjacentFaces().length).toBe(2)
  expect(e.trimIndices().length).toBe(2)
})

test('brepFaceAdjacentEdgesAndFaces', () => {
  // a box face has four adjacent edges and four adjacent faces
  const f = helpers.boxBrep(rhino).faces.get(0)
  expect(f.adjacentEdges().length).toBe(4)
  expect(f.adjacentFaces().length).toBe(4)
})

test('brepFaceLoopsAndLoopTrimsAreScopedLists', () => {
  // BrepFace.loops lists only the face's loops and BrepLoop.trims only the loop's trims; the brep's own lists hold all
  const b = helpers.boxBrep(rhino)
  expect(b.loops.count).toBe(6)
  for (let fi = 0; fi < b.faces.count; fi++) {
    const f = b.faces.get(fi)
    const loops = f.loops
    expect(loops).toBeInstanceOf(rhino.BrepLoopList)
    expect(loops.count).toBe(1)
    const outer = f.outerLoop
    expect(outer.loopIndex).toBe(loops.get(0).loopIndex)
    const trims = outer.trims
    expect(trims).toBeInstanceOf(rhino.BrepTrimList)
    expect(trims.count).toBe(4)
    const ts = [...trims]
    expect(ts.every(t => t.loop.loopIndex === outer.loopIndex)).toBe(true)
    expect(ts.every(t => t.face.faceIndex === fi)).toBe(true)
    expect(ts.every(t => t.edge instanceof rhino.BrepEdge)).toBe(true)
  }
  expect(b.trims.count).toBe(24)
  expect([...b.loops].map(l => l.loopIndex)).toEqual([0, 1, 2, 3, 4, 5])
  helpers.raises(rhino, () => b.faces.get(0).loops.get(1), /out_of_range/)
})

test('brepTrimEdgeAndVertexIndicesOfABox', () => {
  // each trim of a box brep reaches its edge and its start and end vertices
  const b = rhino.Brep.createFromBox(new rhino.BoundingBox({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 }))
  const key = a => a.join(',')
  const expected = [[0, 0, 1], [0, 1, 0], [1, 1, 2], [1, 2, 1], [2, 2, 3], [2, 3, 2], [3, 0, 3], [3, 3, 0], [4, 4, 5],
    [4, 5, 4], [5, 5, 6], [5, 6, 5], [6, 6, 7], [6, 7, 6], [7, 4, 7], [7, 7, 4], [8, 0, 4], [8, 4, 0],
    [9, 1, 5], [9, 5, 1], [10, 2, 6], [10, 6, 2], [11, 3, 7], [11, 7, 3]]
  const trims = []
  for (let i = 0; i < b.trims.count; i++) {
    const t = b.trims.get(i)
    trims.push([t.edge ? t.edge.edgeIndex : -1, t.startVertex.vertexIndex, t.endVertex.vertexIndex])
  }
  expect(trims.map(key).sort()).toEqual(expected.map(key).sort())
})

test('brepVertexEdgeIndicesOfABox', () => {
  // every vertex of a box brep has three edges
  const b = rhino.Brep.createFromBox(new rhino.BoundingBox({ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 2, Z: 3 }))
  const counts = []
  for (let i = 0; i < b.vertices.count; i++) counts.push(b.vertices.get(i).edgeIndices().length)
  expect(counts).toEqual([3, 3, 3, 3, 3, 3, 3, 3])
})

test('brepFaceOuterLoopTrimCount', () => {
  // A box face's outer loop has four trims
  expect(helpers.boxBrep(rhino).faces.get(0).outerLoop.trims.count).toBe(4)
})
