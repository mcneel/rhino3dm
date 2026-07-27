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
