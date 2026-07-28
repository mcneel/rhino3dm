const rhino3dm = require('rhino3dm')
const fs = require('fs')
const path = require('path')

// Read-only SubD access, mirroring tests/python/test_SubD.py: SubD/component
// scalar data, the SubDVertexTag/SubDEdgeTag enums, the component iterators
// (SubD.faces()/edges()/vertices() and the per-component sub-traversals), and
// component equality. Iterators expose count + get() (index for a component-
// rooted iterator, Id for a SubD-rooted one) plus a first()/next()/last()/
// current()/currentIndex cursor; embind cannot install a Symbol.iterator, so JS
// walks by index or with the cursor (currentIndex < count).

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function fixturePath() {
    const candidate = path.join(__dirname, '..', 'models', 'subdBox.3dm')
    return fs.existsSync(candidate) ? candidate : null
}

function firstSubD(file3dm) {
    const objects = file3dm.objects()
    for (let i = 0; i < objects.count; i++) {
        const g = objects.get(i).geometry()
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

// Cursor walk of a SubD-rooted iterator (get() there is by Id, not position).
function collect(iter) {
    const out = []
    for (let c = iter.first(); iter.currentIndex < iter.count; c = iter.next()) out.push(c)
    return out
}

test('empty subd counts', async () => {
    const subd = new rhino.SubD()
    expect(subd.vertexCount).toBe(0)
    expect(subd.edgeCount).toBe(0)
    expect(subd.faceCount).toBe(0)
})

test('tag enums', async () => {
    expect(rhino.SubDVertexTag.Crease).not.toBe(rhino.SubDVertexTag.Smooth)
    expect(rhino.SubDEdgeTag.Crease).not.toBe(rhino.SubDEdgeTag.Smooth)
})

test('counts and iterator counts agree', async () => {
    const subd = loadSubD()
    if (!subd) { console.warn('subdBox.3dm fixture not present - skipping'); return }
    expect(subd.faceCount).toBeGreaterThan(0)
    expect(subd.edgeCount).toBeGreaterThan(0)
    expect(subd.vertexCount).toBeGreaterThan(0)
    expect(subd.faces().count).toBe(subd.faceCount)
    expect(subd.edges().count).toBe(subd.edgeCount)
    expect(subd.vertices().count).toBe(subd.vertexCount)
})

test('iteration yields every component', async () => {
    const subd = loadSubD()
    if (!subd) return
    const cases = [[subd.faces(), subd.faceCount], [subd.edges(), subd.edgeCount], [subd.vertices(), subd.vertexCount]]
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
    for (const iter of [subd.faces(), subd.edges(), subd.vertices()]) {
        const id = iter.first().id
        expect(iter.get(id).id).toBe(id) // SubD-rooted get() is index-by-Id
    }
})

test('face sub-iterator counts', async () => {
    const subd = loadSubD()
    if (!subd) return
    const face = subd.faces().first()
    expect(face.edges().count).toBe(face.edgeCount)
    expect(face.vertices().count).toBe(face.vertexCount)
    expect(collect2(face.edges()).length).toBe(face.edgeCount)
    expect(collect2(face.vertices()).length).toBe(face.vertexCount)
})

test('vertex and edge sub-iterator counts', async () => {
    const subd = loadSubD()
    if (!subd) return
    const v = subd.vertices().first()
    expect(v.faces().count).toBe(v.faceCount)
    expect(v.edges().count).toBe(v.edgeCount)
    const e = subd.edges().first()
    expect(e.faces().count).toBe(e.faceCount)
    expect(e.vertices().count).toBe(e.vertexCount)
})

// Component-rooted iterators support get(i) by position too.
function collect2(iter) {
    const out = []
    for (let i = 0; i < iter.count; i++) out.push(iter.get(i))
    return out
}

test('face properties', async () => {
    const subd = loadSubD()
    if (!subd) return
    const face = subd.faces().first()
    expect(face.edgeCount).toBeGreaterThanOrEqual(3)
    expect(typeof face.materialChannelIndex).toBe('number')
    expect(typeof face.isConvex).toBe('boolean')
    expect(typeof face.isNotConvex).toBe('boolean')
    expect(typeof face.isPlanar(0.001)).toBe('boolean')
    expect(typeof face.isNotPlanar(0.001)).toBe('boolean')
    expect(face.hasEdges).toBe(true)
    expect(typeof face.sharpEdgeCount).toBe('number')
    expect(typeof face.texturePointsCapacity).toBe('number')
    expect(typeof face.texturePointsAreSet).toBe('boolean')
    // ON_3dPoint/ON_3dVector are value_arrays [x, y, z] in JS
    for (const p of [face.controlNetCenterPoint, face.controlNetCenterNormal, face.controlNetPoint(0), face.subdivisionPoint]) {
        expect(p.length).toBe(3)
        expect(typeof p[0]).toBe('number')
    }
    expect(face.perFaceColor).toBeDefined()
    // sharp-edge and texture-point accessors
    expect(typeof face.hasSharpEdges).toBe('boolean')
    expect(typeof face.maximumEdgeSharpness).toBe('number')
    expect(face.textureCenterPoint.length).toBe(3)
    expect(face.texturePoint(0).length).toBe(3) // safe even when texture points are unset
    // per-corner accessors line up with the face's own sub-iterators
    expect(face.vertex(0).id).toBe(face.vertices().first().id)
    expect(face.edge(0).id).toBe(face.edges().first().id)
})

test('edge properties', async () => {
    const subd = loadSubD()
    if (!subd) return
    const edge = subd.edges().first()
    expect(edge.vertexCount).toBe(2)
    const t = edge.tag
    const known = (t === rhino.SubDEdgeTag.Unset) || (t === rhino.SubDEdgeTag.Smooth) ||
                  (t === rhino.SubDEdgeTag.Crease) || (t === rhino.SubDEdgeTag.SmoothX)
    expect(known).toBe(true)
    expect(edge.isCrease).toBe(edge.tag === rhino.SubDEdgeTag.Crease)
    expect(edge.vertexId(0)).toBe(edge.vertex(0).id)
    expect(edge.vertexId(1)).toBe(edge.vertex(1).id)
    for (const b of [edge.isSmooth, edge.isSharp, edge.isCrease, edge.isHardCrease, edge.isDartCrease]) {
        expect(typeof b).toBe('boolean')
    }
    expect(typeof edge.dartCount).toBe('number')
    expect(typeof edge.endSharpness(0)).toBe('number')
    for (const p of [edge.controlNetPoint(0), edge.controlNetDirection, edge.subdivisionPoint, edge.controlNetCenterPoint]) {
        expect(p.length).toBe(3)
    }
    // per-face center normal (indexed by edge-face); the first edge has a face
    if (edge.faceCount > 0) expect(edge.controlNetCenterNormal(0).length).toBe(3)
})

test('vertex properties', async () => {
    const subd = loadSubD()
    if (!subd) return
    const v = subd.vertices().first()
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
    for (const p of [v.controlNetPoint, v.surfacePoint]) {
        expect(p.length).toBe(3)
    }
    expect(v.edgeCount).toBe(v.edges().count)
    expect(v.edge(0).id).toBe(v.edges().first().id)
    // next()/previous() walk the SubD vertex list; the head's next has the head
    // as its previous (avoids dereferencing the null end of the list).
    if (subd.vertexCount >= 2) {
        const head = subd.vertices().first()
        expect(head.next().previous().equals(head)).toBe(true)
    }
})

// ---- full begin-to-end traversal of every iterator type ----
//
// Nine BND_SubDComponentIterator<To, From> instantiations exist: three rooted on
// the whole SubD (faces/edges/vertices) and six rooted on a single component (a
// face's edges/vertices, an edge's faces/vertices, a vertex's faces/edges). Each
// is driven from its first component to its last through the first()/next() cursor
// (currentIndex < count), matching tests/python/test_SubD.py. embind has no
// Symbol.iterator, so there is no native-iteration leg here.

// First component of iter satisfying predicate, or null.
function firstWith(iter, predicate) {
    for (let c = iter.first(); iter.currentIndex < iter.count; c = iter.next()) if (predicate(c)) return c
    return null
}

function assertFullTraversal(makeIter, label, byId) {
    let it = makeIter()
    const n = it.count
    expect(n).toBeGreaterThan(0)          // label: empty iterator, nothing to traverse
    // Cursor walk covers the whole range: n components, all valid (id > 0 on a null
    // wrapper is false), all distinct (no skip, dupe, or overrun past the end).
    const walked = collect(it)
    const ids = walked.map(c => c.id)
    expect(walked.length).toBe(n)
    ids.forEach(id => expect(id).toBeGreaterThan(0))
    expect(new Set(ids).size).toBe(n)

    // Cursor endpoints: first() is the head (index 0) and equals current(); last()
    // is the tail.
    it = makeIter()
    const first = it.first()
    expect(it.currentIndex).toBe(0)
    expect(it.current().id).toBe(first.id)
    expect(first.id).toBe(ids[0])
    expect(it.last().id).toBe(ids[n - 1])

    // Indexing spans the whole range too: SubD-rooted get() is by Id, component-
    // rooted by position. Either way it reproduces the walk.
    it = makeIter()
    if (byId) ids.forEach(id => expect(it.get(id).id).toBe(id))
    else ids.forEach((id, i) => expect(it.get(i).id).toBe(id))
}

test('subd-rooted iterators traverse fully', async () => {
    const subd = loadSubD()
    if (!subd) return
    assertFullTraversal(() => subd.faces(),    'SubD.faces',    true)
    assertFullTraversal(() => subd.edges(),    'SubD.edges',    true)
    assertFullTraversal(() => subd.vertices(), 'SubD.vertices', true)
})

test('component-rooted iterators traverse fully', async () => {
    const subd = loadSubD()
    if (!subd) return
    // From a face: its edges and vertices (every face has at least three of each).
    const face = subd.faces().first()
    assertFullTraversal(() => face.edges(),    'Face.edges',    false)
    assertFullTraversal(() => face.vertices(), 'Face.vertices', false)
    // From an edge: its faces and vertices (pick an edge that borders a face).
    const edge = firstWith(subd.edges(), e => e.faceCount > 0)
    expect(edge).not.toBeNull()
    assertFullTraversal(() => edge.faces(),    'Edge.faces',    false)
    assertFullTraversal(() => edge.vertices(), 'Edge.vertices', false)
    // From a vertex: its faces and edges (pick a vertex that has both).
    const vert = firstWith(subd.vertices(), v => v.faceCount > 0 && v.edgeCount > 0)
    expect(vert).not.toBeNull()
    assertFullTraversal(() => vert.faces(), 'Vertex.faces', false)
    assertFullTraversal(() => vert.edges(), 'Vertex.edges', false)
})

// ---- ON_SubDComponentPtr direction accessors ----
//
// Each component wrapper stores an ON_SubDComponentPtr (pointer + direction bit).
// componentDirection surfaces the bit; face/vertex -> edge traversal is wired
// through EdgePtr so a shared edge remembers its orientation. Mirrors the direction
// tests in tests/python/test_SubD.py.

test('component direction defaults to zero', async () => {
    const subd = loadSubD()
    if (!subd) return
    expect(subd.faces().first().componentDirection).toBe(0)
    expect(subd.edges().first().componentDirection).toBe(0)
    expect(subd.vertices().first().componentDirection).toBe(0)
    expect(subd.faces().first().vertex(0).componentDirection).toBe(0) // vertices never carry a direction
})

test('face edge direction is wired in', async () => {
    const subd = loadSubD()
    if (!subd) return
    const seen = new Set()
    for (const face of collect(subd.faces())) {
        for (let i = 0; i < face.edgeCount; i++) {
            const edge = face.edge(i)
            const matches = face.edgeDirectionMatchesFaceOrientation(i)
            expect(edge.componentDirection).toBe(matches ? 0 : 1)   // RhinoCommon parity
            const natural = subd.edges().get(edge.id)               // SubD-rooted get() is by Id
            expect(natural.componentDirection).toBe(0)
            expect(edge.equals(natural)).toBe(true)                 // identity ignores direction
            seen.add(edge.componentDirection)
        }
    }
    // Shared edges are traversed both ways by their two faces: both must appear.
    expect([...seen].sort()).toEqual([0, 1])
})

test('vertex edge direction is wired in', async () => {
    const subd = loadSubD()
    if (!subd) return
    const seen = new Set()
    for (const v of collect(subd.vertices())) {
        for (let i = 0; i < v.edgeCount; i++) {
            const edge = v.edge(i)
            expect([0, 1]).toContain(edge.componentDirection)
            expect(edge.equals(subd.edges().get(edge.id))).toBe(true) // identity ignores direction
            seen.add(edge.componentDirection)
        }
    }
    expect([...seen].sort()).toEqual([0, 1])
})

test('component equality', async () => {
    const subd = loadSubD()
    if (!subd) return
    // Same component reached two ways is equal; two different ones are not.
    expect(subd.faces().first().equals(subd.faces().first())).toBe(true)
    const faces = collect(subd.faces())
    if (faces.length >= 2) expect(faces[0].equals(faces[1])).toBe(false)
    // Identity across traversal: a face's first edge equals the same edge reached
    // through SubD.edges() by Id.
    const e = subd.faces().first().edges().first()
    expect(e.equals(subd.edges().get(e.id))).toBe(true)
    // NOTE: JS Set de-dup (Python test) relies on __hash__/__eq__, which embind
    // does not map; use equals() instead. Cross-type equality is likewise omitted
    // because equals() is typed per component in embind.
})
