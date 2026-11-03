const rhino3dm = require('rhino3dm')
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
    expect(typeof edge.endSharpness(0)).toBe('number')
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
