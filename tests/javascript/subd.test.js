const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs')
const path = require('path')

// Read-only SubD access, mirroring tests/python/test_SubD.py: SubD/component
// scalar data, the SubDVertexTag/SubDEdgeTag enums, the component lists
// (SubD.faces/edges/vertices and the per-component neighbours), and component
// identity. D26: subd.faces/edges/vertices are RhinoCommon's lists (count,
// for...of, find(id)); a component's neighbours are arrays (face.edges, ...)
// or edgeAt(i) / vertexAt(i) / faceAt(i); the first()/next()/last()/current()/
// currentIndex cursor is gone (retired-members.md: proposed drop).

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function fixturePath() {
    const candidate = path.join(__dirname, '..', 'models', 'subdBox.3dm')
    return fs.existsSync(candidate) ? candidate : null
}

function firstSubD(file3dm) {
    // D17: iterate the object table
    for (const obj of file3dm.objects) {
        const g = obj.geometry
        if (g instanceof rhino.SubD) return g
    }
    return null
}

function loadSubD() {
    const fixture = fixturePath()
    if (fixture === null) return null
    const buffer = fs.readFileSync(fixture)
    const file3dm = rhino.File3dm.fromByteArray(new Uint8Array(buffer))
    return firstSubD(file3dm)
}

// Walk a SubD list or a neighbour array (D26: for...of replaces the cursor).
function collect(iter) {
    return [...iter]
}

test('empty subd counts', async () => {
    const subd = new rhino.SubD()
    // vertexCount / edgeCount / faceCount -> vertices.count / edges.count / faces.count
    expect(subd.vertices.count).toBe(0)
    expect(subd.edges.count).toBe(0)
    expect(subd.faces.count).toBe(0)
})

test('tag enums', async () => {
    expect(rhino.SubDVertexTag.Crease).not.toBe(rhino.SubDVertexTag.Smooth)
    expect(rhino.SubDEdgeTag.Crease).not.toBe(rhino.SubDEdgeTag.Smooth)
})

test('counts and iterator counts agree', async () => {
    const subd = loadSubD()
    if (!subd) { console.warn('subdBox.3dm fixture not present - skipping'); return }
    // faceCount / edgeCount / vertexCount -> the lists' count
    expect(subd.faces.count).toBeGreaterThan(0)
    expect(subd.edges.count).toBeGreaterThan(0)
    expect(subd.vertices.count).toBeGreaterThan(0)
    expect(collect(subd.faces).length).toBe(subd.faces.count)
    expect(collect(subd.edges).length).toBe(subd.edges.count)
    expect(collect(subd.vertices).length).toBe(subd.vertices.count)
})

test('iteration yields every component', async () => {
    const subd = loadSubD()
    if (!subd) return
    const cases = [[subd.faces, subd.faces.count], [subd.edges, subd.edges.count], [subd.vertices, subd.vertices.count]]
    for (const [iter, n] of cases) {
        const items = collect(iter)
        expect(items.length).toBe(n)
        const ids = items.map(c => c.id)
        ids.forEach(id => expect(id).toBeGreaterThan(0)) // no null wrapper
        expect(new Set(ids).size).toBe(n)                // distinct
    }
})

test('find by id round trips', async () => {
    const subd = loadSubD()
    if (!subd) return
    for (const iter of [subd.faces, subd.edges, subd.vertices]) {
        const id = collect(iter)[0].id
        expect(iter.find(id).id).toBe(id) // D26: get(id) -> find(id)
    }
})

test('face sub-iterator counts', async () => {
    const subd = loadSubD()
    if (!subd) return
    const face = collect(subd.faces)[0]
    expect(face.edges.length).toBe(face.edgeCount)
    expect(face.vertices.length).toBe(face.vertexCount)
    expect(collect2(face.edges).length).toBe(face.edgeCount)
    expect(collect2(face.vertices).length).toBe(face.vertexCount)
})

test('vertex and edge sub-iterator counts', async () => {
    const subd = loadSubD()
    if (!subd) return
    const v = subd.vertices.first
    expect(v.faces.length).toBe(v.faceCount)
    expect(v.edges.length).toBe(v.edgeCount)
    const e = collect(subd.edges)[0]
    expect(e.faces.length).toBe(e.faceCount)
    expect(e.vertices.length).toBe(2) // edge.vertexCount: always 2 (vertexFrom / vertexTo)
})

// Neighbour arrays index by position.
function collect2(iter) {
    const out = []
    for (let i = 0; i < iter.length; i++) out.push(iter[i])
    return out
}

test('face properties', async () => {
    const subd = loadSubD()
    if (!subd) return
    const face = collect(subd.faces)[0]
    expect(face.edgeCount).toBeGreaterThanOrEqual(3)
    expect(typeof face.materialChannelIndex).toBe('number')
    // D72: isNotConvex / isNotPlanar removed (use !isConvex / !isPlanar)
    expect(typeof face.isConvex).toBe('boolean')
    expect(typeof face.isPlanar(0.001)).toBe('boolean')
    expect(face.hasEdges).toBe(true)
    expect(typeof face.sharpEdgeCount).toBe('number')
    expect(typeof face.texturePointsCapacity).toBe('number')
    expect(typeof face.texturePointsAreSet).toBe('boolean')
    // D11: points/vectors are {X, Y, Z}; controlNetPoint(i) -> vertexAt(i).controlNetPoint
    for (const p of [face.controlNetCenterPoint, face.controlNetCenterNormal, face.vertexAt(0).controlNetPoint, face.subdivisionPoint]) {
        expect(Object.keys(p)).toEqual(['X', 'Y', 'Z'])
        expect(typeof p.X).toBe('number')
    }
    expect(face.perFaceColor).toBeDefined()
    // sharp-edge and texture-point accessors
    expect(typeof face.hasSharpEdges).toBe('boolean')
    expect(typeof face.maximumEdgeSharpness).toBe('number')
    expect(Object.keys(face.textureCenterPoint)).toEqual(['X', 'Y', 'Z'])
    expect(Object.keys(face.texturePoint(0))).toEqual(['X', 'Y', 'Z']) // safe even when texture points are unset
    // per-corner accessors (vertex(i) / edge(i) -> vertexAt(i) / edgeAt(i)) line up with the neighbour arrays
    expect(face.vertexAt(0).id).toBe(face.vertices[0].id)
    expect(face.edgeAt(0).id).toBe(face.edges[0].id)
})

test('edge properties', async () => {
    const subd = loadSubD()
    if (!subd) return
    const edge = collect(subd.edges)[0]
    expect(edge.vertices.length).toBe(2) // vertexCount: always 2
    const t = edge.tag
    const known = (t === rhino.SubDEdgeTag.Unset) || (t === rhino.SubDEdgeTag.Smooth) ||
                  (t === rhino.SubDEdgeTag.Crease) || (t === rhino.SubDEdgeTag.SmoothX)
    expect(known).toBe(true)
    expect(edge.isCrease).toBe(edge.tag === rhino.SubDEdgeTag.Crease)
    // vertexId(i) / vertex(i) -> vertexFrom / vertexTo
    expect(edge.vertexFrom.id).toBe(edge.vertices[0].id)
    expect(edge.vertexTo.id).toBe(edge.vertices[1].id)
    for (const b of [edge.isSmooth, edge.isSharp, edge.isCrease, edge.isHardCrease, edge.isDartCrease]) {
        expect(typeof b).toBe('boolean')
    }
    expect(typeof edge.dartCount).toBe('number')
    expect(typeof edge.endSharpness(0, false)).toBe('number')   // 8.x endSharpness(i): useCreaseSharpness false
    // controlNetPoint(0) -> vertexFrom.controlNetPoint; controlNetDirection -> controlNetLine's direction
    for (const p of [edge.vertexFrom.controlNetPoint, rhino.Line.direction(edge.controlNetLine), edge.subdivisionPoint, edge.controlNetCenterPoint]) {
        expect(Object.keys(p)).toEqual(['X', 'Y', 'Z'])
    }
    // per-face center normal (indexed by edge-face); the first edge has a face
    if (edge.faceCount > 0) expect(Object.keys(edge.controlNetCenterNormal(0))).toEqual(['X', 'Y', 'Z'])
})

test('vertex properties', async () => {
    const subd = loadSubD()
    if (!subd) return
    const v = subd.vertices.first
    const t = v.tag
    const known = (t === rhino.SubDVertexTag.Unset) || (t === rhino.SubDVertexTag.Smooth) ||
                  (t === rhino.SubDVertexTag.Crease) || (t === rhino.SubDVertexTag.Corner) ||
                  (t === rhino.SubDVertexTag.Dart)
    expect(known).toBe(true)
    expect(v.isSmooth).toBe(v.tag === rhino.SubDVertexTag.Smooth)
    expect(v.isCrease).toBe(v.tag === rhino.SubDVertexTag.Crease)
    expect(v.isCorner).toBe(v.tag === rhino.SubDVertexTag.Corner)
    expect(v.isDart).toBe(v.tag === rhino.SubDVertexTag.Dart)
    expect(typeof v.isSharp(true)).toBe('boolean')
    expect(typeof v.vertexSharpness).toBe('number')
    // surfacePoint is a method in RhinoCommon
    for (const p of [v.controlNetPoint, v.surfacePoint()]) {
        expect(Object.keys(p)).toEqual(['X', 'Y', 'Z'])
    }
    expect(v.edgeCount).toBe(v.edges.length)
    expect(v.edgeAt(0).id).toBe(v.edges[0].id)
    // next/previous (properties) walk the SubD vertex list; the head's next has the head
    // as its previous (avoids dereferencing the null end of the list).
    // D25: no equals() on components yet; compare by id
    if (subd.vertices.count >= 2) {
        const head = subd.vertices.first
        expect(head.next.previous.id).toBe(head.id)
    }
})

// ---- full begin-to-end traversal of every list type ----
//
// 8.x had nine BND_SubDComponentIterator<To, From> instantiations: three rooted on
// the whole SubD (faces/edges/vertices) and six rooted on a single component (a
// face's edges/vertices, an edge's faces/vertices, a vertex's faces/edges). Now the
// first three are RhinoCommon's SubD lists and the six are arrays (D26); each is
// walked end to end with for...of, matching tests/python/test_SubD.py.

// First component of iter satisfying predicate, or null.
function firstWith(iter, predicate) {
    for (const c of iter) if (predicate(c)) return c
    return null
}

function assertFullTraversal(makeIter, label, byId) {
    let it = makeIter()
    const n = byId ? it.count : it.length
    expect(n).toBeGreaterThan(0)          // label: empty iterator, nothing to traverse
    // The walk covers the whole range: n components, all valid (id > 0 on a null
    // wrapper is false), all distinct (no skip, dupe, or overrun past the end).
    const walked = collect(it)
    const ids = walked.map(c => c.id)
    expect(walked.length).toBe(n)
    ids.forEach(id => expect(id).toBeGreaterThan(0))
    expect(new Set(ids).size).toBe(n)

    // The first()/current()/last() cursor is gone (D26); a fresh walk yields the
    // same begin->end sequence instead.
    expect(collect(makeIter()).map(c => c.id)).toEqual(ids)

    // Lookup spans the whole range too: a SubD list finds by Id (get(id) -> find(id)),
    // a neighbour array indexes by position. Either way it reproduces the walk.
    it = makeIter()
    if (byId) ids.forEach(id => expect(it.find(id).id).toBe(id))
    else ids.forEach((id, i) => expect(it[i].id).toBe(id))
}

test('subd-rooted iterators traverse fully', async () => {
    const subd = loadSubD()
    if (!subd) return
    assertFullTraversal(() => subd.faces,    'SubD.faces',    true)
    assertFullTraversal(() => subd.edges,    'SubD.edges',    true)
    assertFullTraversal(() => subd.vertices, 'SubD.vertices', true)
})

test('component-rooted iterators traverse fully', async () => {
    const subd = loadSubD()
    if (!subd) return
    // From a face: its edges and vertices (every face has at least three of each).
    const face = collect(subd.faces)[0]
    assertFullTraversal(() => face.edges,    'Face.edges',    false)
    assertFullTraversal(() => face.vertices, 'Face.vertices', false)
    // From an edge: its faces and vertices (pick an edge that borders a face).
    const edge = firstWith(subd.edges, e => e.faceCount > 0)
    expect(edge).not.toBeNull()
    assertFullTraversal(() => edge.faces,    'Edge.faces',    false)
    assertFullTraversal(() => edge.vertices, 'Edge.vertices', false)
    // From a vertex: its faces and edges (pick a vertex that has both).
    const vert = firstWith(subd.vertices, v => v.faceCount > 0 && v.edgeCount > 0)
    expect(vert).not.toBeNull()
    assertFullTraversal(() => vert.faces, 'Vertex.faces', false)
    assertFullTraversal(() => vert.edges, 'Vertex.edges', false)
})

test('component equality', async () => {
    const subd = loadSubD()
    if (!subd) return
    // D25: a new object per access and no equals() yet (SubDComponent's operators
    // are not generated); RhinoCommon compares SubD components by id, so compare ids.
    // Same component reached two ways is equal; two different ones are not.
    expect(collect(subd.faces)[0].id).toBe(collect(subd.faces)[0].id)
    const faces = collect(subd.faces)
    if (faces.length >= 2) expect(faces[0].id).not.toBe(faces[1].id)
    // Identity across traversal: a face's first edge equals the same edge reached
    // through SubD.edges by Id.
    const e = collect(subd.faces)[0].edges[0]
    expect(e.id).toBe(subd.edges.find(e.id).id)
    // NOTE: JS Set de-dup (Python test) relies on __hash__/__eq__, which embind
    // does not map. Cross-type equality is likewise omitted.
})

// subdBox.3dm and the SubD in it
function subdBox(rhino) {
  const f = helpers.readModel(rhino, 'subdBox.3dm')
  const s = helpers.firstGeometry(f, rhino.SubD)
  if (s === null) throw new Error('no SubD in subdBox.3dm')
  return [f, s]
}

const subdOf = (rhino, name) => [...helpers.readModel(rhino, name).objects].map(o => o.geometry).filter(g => g instanceof rhino.SubD)

// the sharp edges of each SubD in the fixture files, as 8.x counted them
const subdSharpEdges = [['subd.3dm', [90]], ['subdBox.3dm', [0]], ['subd_creases.3dm', [3]]]

// the first SubD in a model in tests/models
const subdFirst = name => [...helpers.readModel(rhino, name).objects].map(o => o.geometry).find(g => g instanceof rhino.SubD)

// a point as an array, 'nan' for an unset coordinate (as the stored answers write it)
const subdP3 = p => [p.X, p.Y, p.Z].map(c => (Number.isNaN(c) ? 'nan' : c))

// stored answers in tests/models/old_oracle
const subdOracle = name => JSON.parse(fs.readFileSync(helpers.model('old_oracle/' + name), 'utf8'))

const subdBoxGeometry = rhino => helpers.firstGeometry(helpers.readModel(rhino, 'subdBox.3dm'), rhino.SubD)
const subdCylinder = (rhino, cap, loc) => {
  const cyl = new rhino.Cylinder(new rhino.Circle(rhino.Plane.worldXY, 2.0), 3.0)
  return rhino.SubD.createFromCylinder(cyl, 8, 2, rhino.SubDEndCapStyle[cap], rhino.SubDEdgeTag.Crease,
    loc === undefined ? rhino.SubDComponentLocation.ControlNet : loc)
}

test('subdComponentEquality', () => {
  // SubD components are equal when they have the same type, id and owning SubD; JS has no ==, so equals(other) (D25)
  const [f, s] = subdBox(rhino)
  const faces = [...s.faces]
  const face = faces[0]
  expect(face.equals(s.faces.find(face.id))).toBe(true)
  expect(face.equals(faces[1])).toBe(false)
  const edge = face.edges[0]
  expect(edge.equals(s.edges.find(edge.id))).toBe(true)
  expect(edge.getHashCode()).toBe(s.edges.find(edge.id).getHashCode())
  expect(face.vertices[0].equals(s.vertices.find(face.vertices[0].id))).toBe(true)
  expect(face.equals(s.edges.find(face.id))).toBe(false)     // same id, another type
  const dup = s.duplicate()
  expect(face.equals(dup.faces.find(face.id))).toBe(false)   // another SubD
  expect(face.equals(null)).toBe(false)
  dup.delete(); s.delete(); f.delete()
})

test('subdListsIterateLikeForeach', () => {
  // subd.faces, edges and vertices iterate every component; the count agrees (D26)
  const [, s] = subdBox(rhino)
  for (const lst of [s.faces, s.edges, s.vertices]) {
    expect(lst.count).toBeGreaterThan(0)
    expect([...lst].length).toBe(lst.count)
  }
})

test('subdVertexNextWalksTheVertexList', () => {
  // following SubDVertex.next from the first vertex visits every vertex once
  const [, s] = subdBox(rhino)
  let n = 0
  for (let cur = [...s.vertices][0]; cur !== null && cur !== undefined; cur = cur.next) n++
  expect(n).toBe(s.vertices.count)
})

test('subdComponentsReachTheirNeighbours', () => {
  // a SubD vertex has edges and faces; an edge has its two vertices and at least one face
  const [, s] = subdBox(rhino)
  const v = [...s.vertices][0]
  expect(v.edgeCount).toBeGreaterThan(0)
  expect(v.faceCount).toBeGreaterThan(0)
  const e = [...s.edges][0]
  expect(e.vertexFrom).not.toBeNull()
  expect(e.vertexTo).not.toBeNull()
  expect(e.faceCount).toBeGreaterThanOrEqual(1)
})

test('subdVertexControlNetPointWritesLandInTheSubD', () => {
  // setting a vertex's controlNetPoint changes the SubD's vertex
  const [, s] = subdBox(rhino)
  const v = [...s.vertices][0]
  v.controlNetPoint = new rhino.Point3d(10, 20, 30)
  const p = [...s.vertices][0].controlNetPoint
  expect([p.X, p.Y, p.Z]).toEqual([10, 20, 30])
})

test('subdNeighboursAreCountAndAt', () => {
  // a component's neighbours are xCount + xAt(i); xAt throws outside 0..count-1 (D26)
  const [, s] = subdBox(rhino)
  const face = [...s.faces][0]
  const edges = [], verts = []
  for (let i = 0; i < face.edgeCount; i++) edges.push(face.edgeAt(i))
  for (let i = 0; i < face.vertexCount; i++) verts.push(face.vertexAt(i))
  expect(face.edgeCount).toBeGreaterThanOrEqual(3)
  expect(edges.length).toBe(face.edgeCount)
  expect(verts.length).toBe(face.edgeCount)
  const v = verts[0]
  for (let i = 0; i < v.edgeCount; i++) expect(v.edgeAt(i)).toBeTruthy()
  for (let i = 0; i < v.faceCount; i++) expect(v.faceAt(i)).toBeTruthy()
  const e = edges[0]
  for (let i = 0; i < e.faceCount; i++) expect(e.faceAt(i)).toBeTruthy()
  helpers.raises(rhino, () => v.edgeAt(-1), /out_of_range,index cannot be negative/)
  helpers.raises(rhino, () => v.edgeAt(v.edgeCount), /out_of_range,index is greater than or equal to EdgeCount/)
})

test('subdVerticesFindById', () => {
  // subd.vertices.find(id) returns the vertex with that id, or null; ids start at 1 (D26)
  const [, s] = subdBox(rhino)
  expect(s.vertices.find(0)).toBeNull()
  expect(s.vertices.find(1)).not.toBeNull()
})

test('subdComponentsHaveIdsAndStatus', () => {
  // SubD components are SubDComponents with unique ids from 1 and a selected/deleted status
  const [, s] = subdBox(rhino)
  const faces = [...s.faces]
  const ids = faces.map(x => x.id)
  expect(new Set(ids).size).toBe(ids.length)
  expect(Math.min(...ids)).toBeGreaterThanOrEqual(1)
  expect(faces[0] instanceof rhino.SubDComponent).toBe(true)
  expect(s.faces.find(ids[0]).id).toBe(ids[0])
  const v = [...s.vertices][0]
  expect(typeof v.isSelected).toBe('boolean')
  expect(v.isDeleted).toBe(false)
})

test('subdEdgeEndSharpnessGuardsTheEndIndex', () => {
  // SubDEdge.endSharpness takes an end index of 0 or 1 and throws otherwise (D9)
  const [, s] = subdBox(rhino)
  const e = [...s.edges][0]
  expect(typeof e.endSharpness(0, false)).toBe('number')
  helpers.raises(rhino, () => e.endSharpness(2, false), /out_of_range,endIndex must be 0 or 1/)
})

test('subdFacePerFaceColorIsEmptyWhenUnset', () => {
  // SubDFace.perFaceColor is the empty color (0, 0, 0, 0) when no face color is set
  const s = helpers.firstGeometry(helpers.readModel(rhino, 'subdBox.3dm'), rhino.SubD)
  const pfc = [...s.faces][0].perFaceColor
  expect([pfc.r, pfc.g, pfc.b, pfc.a]).toEqual([0, 0, 0, 0])
})

test('subdVertexEdgesListsEachEdge', () => {
  // SubDVertex.edges lists the vertex's edges in edgeAt order, and each edge keeps its SubD alive
  const f = helpers.readModel(rhino, 'subdBox.3dm')
  const s = helpers.firstGeometry(f, rhino.SubD)
  const v = [...s.vertices][0]
  const edges = v.edges
  expect(Array.isArray(edges)).toBe(true)
  expect(v.edgeCount).toBeGreaterThan(0)
  expect(edges.length).toBe(v.edgeCount)
  const byIndex = []
  for (let i = 0; i < v.edgeCount; i++) byIndex.push(v.edgeAt(i).id)
  expect(edges.map(e => e.id)).toEqual(byIndex)
  v.delete(); s.delete(); f.delete()
  expect(edges.every(e => e.id > 0)).toBe(true)
})

test('subdFaceControlNetCenterPointNormalAndFrame', () => {
  // a SubD face's controlNetCenterPoint is the mean of its control points, with a unit normal and a frame there
  const s = helpers.firstGeometry(helpers.readModel(rhino, 'subdBox.3dm'), rhino.SubD)
  expect(s).not.toBeNull()
  const faces = [...s.faces]
  expect(faces.length).toBeGreaterThan(0)
  for (const face of faces) {
    const n = face.vertexCount
    const pts = Array.from({ length: n }, (_, i) => face.vertexAt(i).controlNetPoint)
    const c = face.controlNetCenterPoint
    for (const k of ['X', 'Y', 'Z']) helpers.approx(c[k], pts.reduce((a, p) => a + p[k], 0) / n, 1e-12)
    const v = face.controlNetCenterNormal
    helpers.approx(Math.hypot(v.X, v.Y, v.Z), 1.0, 1e-12)
    const fr = face.controlNetCenterFrame
    expect([fr.Origin.X, fr.Origin.Y, fr.Origin.Z]).toEqual([c.X, c.Y, c.Z])
    for (const k of ['X', 'Y', 'Z']) helpers.approx(fr.ZAxis[k], v[k], 1e-12)
  }
})

test('subdSharpEdgeCount', () => {
  // sharpEdgeCount() is a method counting the edges whose isSharp is true, as 8.x counted them (D60)
  for (const [name, expected] of subdSharpEdges) {
    const subds = subdOf(rhino, name)
    expect(subds.length).toBe(expected.length)
    subds.forEach((s, k) => {
      const n = s.sharpEdgeCount()
      const edges = [...s.edges]
      expect(n).toBe(edges.filter(e => e.isSharp).length)
      expect(n).toBe(expected[k])
      for (const e of edges) expect(Object.keys(e.sharpness).sort()).toEqual(['sharpness0', 'sharpness1'])
    })
  }
})

test('subdClearEdgeSharpness', () => {
  // clearEdgeSharpness returns the number of edges it changed, leaves no sharp edge, and adds no SubD error
  for (const [name] of subdSharpEdges) {
    for (const s of subdOf(rhino, name)) {
      const n = s.sharpEdgeCount()
      const errors = rhino.SubD.errorCount
      expect(s.clearEdgeSharpness()).toBe(n)
      expect(s.sharpEdgeCount()).toBe(0)
      expect(rhino.SubD.errorCount).toBe(errors)          // a process-wide counter
    }
  }
})

test('subdEdgeSharpnessValueType', () => {
  // SubDEdgeSharpness is a plain object {sharpness0, sharpness1} with RhinoCommon's queries as statics (D61)
  const S = rhino.SubDEdgeSharpness
  const s = S.create(1, 2)
  expect(s).toEqual({ sharpness0: 1, sharpness1: 2 })
  expect(S.endSharpness(s, 1)).toBe(2)
  expect(S.isIncreasing(s)).toBe(true)
  expect(S.reversed(s)).toEqual({ sharpness0: 2, sharpness1: 1 })
  expect(S.isCrease(S.crease)).toBe(true)
  expect(S.create(5, 5)).toEqual(S.crease)
  // a crease at one end alone is not a valid sharpness
  expect(S.isNotValid(S.create(5, 0))).toBe(true)
  expect(S.reversed({ sharpness0: 0.5, sharpness1: 1.5 })).toEqual({ sharpness0: 1.5, sharpness1: 0.5 })
})

test('subdComponentCountsAndIdsNumberFromOne', () => {
  // edges, faces and vertices count the components, and their ids number from 1
  const n = subdFirst('subdBox.3dm')
  const upTo = k => Array.from({ length: k }, (_, i) => i + 1)
  expect([n.edges.count, n.faces.count, n.vertices.count]).toEqual([48, 24, 26])
  expect([...n.faces].map(c => c.id).sort((a, b) => a - b)).toEqual(upTo(24))
  expect([...n.edges].map(c => c.id).sort((a, b) => a - b)).toEqual(upTo(48))
  expect([...n.vertices].map(c => c.id).sort((a, b) => a - b)).toEqual(upTo(26))
})

test('subdEdgeEndSharpness', () => {
  // SubDEdge.endSharpness(i, false) reads the crease sharpness at each end of each edge
  const s = subdFirst('subd_creases.3dm')
  const edges = [...s.edges].sort((a, b) => a.id - b.id)
  expect(edges.map(e => [e.endSharpness(0, false), e.endSharpness(1, false)])).toEqual(
    [[0, 0], [0, 0], [0, 0], [0, 0], [1, 1], [0, 0], [3, 3], [2.5, 0.5], [0, 0], [0, 0], [0, 0], [0, 0]])
})

test('subdComponentQueriesGiveRhino3dm8Answers', () => {
  // The SubD face, vertex and edge queries give rhino3dm 8.x's answers on every SubD model, component by component (by id)
  const fixtures = subdOracle('subd_queries.json').fixtures
  let checked = 0
  for (const [name, olds] of Object.entries(fixtures)) {
    const ns = [...helpers.readModel(rhino, name).objects].map(o => o.geometry).filter(g => g instanceof rhino.SubD)
    expect(ns.length).toBeGreaterThan(0)
    expect(ns.length).toBe(olds.length)
    ns.forEach((s, k) => {
      const old = olds[k]
      const byId = a => a.sort((x, y) => x.id - y.id)
      // faces q: hasEdges, hasSharpEdges, sharpEdgeCount, maximumEdgeSharpness, isConvex, isPlanar(1e-8),
      // isPlanar(1.0), materialChannelIndex, texturePointsAreSet, texturePointsCapacity
      const of = byId(old.faces.slice()), nf = byId([...s.faces])
      expect(nf.map(n => n.id)).toEqual(of.map(o => o.id))
      of.forEach((o, i) => {
        const n = nf[i], q = o.q
        expect([n.hasEdges, n.hasSharpEdges, n.sharpEdgeCount, n.maximumEdgeSharpness, n.isConvex, n.isPlanar(1e-8),
                n.materialChannelIndex, n.texturePointsAreSet, n.texturePointsCapacity, subdP3(n.subdivisionPoint)])
          .toEqual([q[0], q[1], q[2], q[3], q[4], q[5], q[7], q[8], q[9], o.sub])
      })
      // vertices q: isSmooth, isCrease, isCorner, isDart, isSharp(true), isSharp(false), vertexSharpness
      const ov = byId(old.vertices.slice()), nv = byId([...s.vertices])
      expect(nv.map(n => n.id)).toEqual(ov.map(o => o.id))
      ov.forEach((o, i) => {
        const n = nv[i], q = o.q
        expect([n.isSmooth, n.isCrease, n.isCorner, n.isDart, n.isSharp(true), n.vertexSharpness])
          .toEqual([q[0], q[1], q[2], q[3], q[4], q[6]])
      })
      // edges q: isSmooth, isCrease, isHardCrease, isDartCrease, dartCount
      const oe = byId(old.edges.slice()), ne = byId([...s.edges])
      expect(ne.map(n => n.id)).toEqual(oe.map(o => o.id))
      oe.forEach((o, i) => {
        const n = ne[i]
        expect([n.isSmooth, n.isCrease, n.isHardCrease, n.isDartCrease, n.dartCount, subdP3(n.subdivisionPoint), subdP3(n.controlNetCenterPoint)])
          .toEqual([...o.q, o.sub, o.cc])
      })
      checked += of.length + ov.length + oe.length
    })
  }
  expect(checked).toBeGreaterThan(100)
})

test('subdControlNetGivesRhino3dm8Answers', () => {
  // Each edge's controlNetLine and vertexFrom / vertexTo give rhino3dm 8.x's control net answers on every SubD model
  for (const [name, old] of Object.entries(subdOracle('subd_controlnet.json').fixtures)) {
    const ne = [...subdFirst(name).edges].sort((x, y) => x.id - y.id)
    expect(ne.length).toBe(old.edges.length)
    old.edges.forEach((o, i) => {
      const n = ne[i], line = n.controlNetLine
      expect([subdP3(line.From), subdP3(line.To)]).toEqual(o.p)
      expect([n.vertexFrom.id, n.vertexTo.id]).toEqual(o.v)
      expect(subdP3(n.vertexTo.controlNetPoint)).toEqual(o.p[1])
    })
  }
})

test('subdNeighbourListsGive8xNeighbours', () => {
  // face.edges / .vertices, edge.faces / .vertices, vertex.faces list 8.x's neighbours, in order
  const subd = subdBoxGeometry(rhino)
  const expected = JSON.parse(fs.readFileSync(helpers.model('old_oracle/subd_neighbours.json'), 'utf8'))['subdBox.3dm']
  let checked = 0
  for (const [comps, olds, props] of [[subd.faces, expected.faces, ['edges', 'vertices']],
                                      [subd.edges, expected.edges, ['faces', 'vertices']],
                                      [subd.vertices, expected.vertices, ['faces']]]) {
    const all = [...comps]
    expect(all.length).toBe(Object.keys(olds).length)
    for (const c of all) {
      for (const prop of props) {
        expect(c[prop].map(x => x.id)).toEqual(olds[String(c.id)][prop[0].toUpperCase() + prop.slice(1)])
        checked++
      }
    }
  }
  expect(checked).toBeGreaterThan(50)
})

test('subdNeighbourListsAreLists', () => {
  // a component's neighbours are arrays, matching its edgeCount / edgeAt
  const subd = subdBoxGeometry(rhino)
  const face = [...subd.faces][0]
  const v = [...subd.vertices][0]
  for (const a of [face.edges, face.vertices, v.faces, v.edges]) expect(Array.isArray(a)).toBe(true)
  expect(face.edges.length).toBe(face.edgeCount)
  expect(face.edges.map(e => e.id)).toEqual(Array.from({ length: face.edgeCount }, (_, i) => face.edgeAt(i).id))
})

test('subdCreateFromCylinderEndCapStyles', () => {
  // createFromCylinder with 8 x 2 faces gives each end cap style's face, edge and vertex counts
  const got = {}
  for (const cap of ['None', 'Quads', 'Triangles', 'Ngon']) {
    const s = subdCylinder(rhino, cap)
    got[cap] = [s.faces.count, s.edges.count, s.vertices.count]
  }
  expect(got).toEqual({ None: [16, 40, 24], Quads: [24, 48, 26], Triangles: [32, 56, 26], Ngon: [18, 40, 24] })
})

test('subdCreateFromCylinderRadiusLocation', () => {
  // createFromCylinder puts the control net, or the limit surface, on the radius
  const net = subdCylinder(rhino, 'None').getBoundingBox(true)
  const srf = subdCylinder(rhino, 'None', rhino.SubDComponentLocation.Surface).getBoundingBox(true)
  expect([net.Min.X, net.Max.X, net.Max.Z]).toEqual([-2, 2, 3])
  helpers.approx(srf.Max.X, 2.216388375108776, 1e-12)
})

test('subdCreateFromCylinderRejectsAnIntEndCapStyle', () => {
  // createFromCylinder throws for a number where a SubDEndCapStyle goes
  const cyl = new rhino.Cylinder(new rhino.Circle(rhino.Plane.worldXY, 2.0), 3.0)
  helpers.raises(rhino, () => rhino.SubD.createFromCylinder(cyl, 8, 2, 1, rhino.SubDEdgeTag.Crease,
    rhino.SubDComponentLocation.ControlNet), /expected a SubDEndCapStyle value/)
})

test('subdTextureCoordinateTypeIsUnsetOnANewSubD', () => {
  // a new SubD's textureCoordinateType is Unset
  expect(subdCylinder(rhino, 'None').textureCoordinateType).toBe(rhino.SubDTextureCoordinateType.Unset)
})

test('subdDefaultConstructorMakesAnEmptySubD', () => {
  // new SubD() is an empty SubD that takes a SubD's members and can be added to a file
  const s = new rhino.SubD()
  expect([s.faces.count, s.edges.count, s.vertices.count]).toEqual([0, 0, 0])
  s.clearEvaluationCache()
  const f = new rhino.File3dm()
  f.objects.addSubD(s)
  expect(f.objects.count).toBe(1)
})
