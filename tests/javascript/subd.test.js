const rhino3dm = require('rhino3dm')
const fs = require('fs')
const path = require('path')

// Read-only SubD access. The JS binding currently exposes SubD counts and the
// scalar data of SubDFace/Edge/Vertex; component traversal via the templated
// iterators is Python-first for now (exposing them through embind is a follow-up).

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

test('empty subd counts', async () => {
    const subd = new rhino.SubD()
    expect(subd.vertexCount).toBe(0)
    expect(subd.edgeCount).toBe(0)
    expect(subd.faceCount).toBe(0)
})

test('read counts from fixture', async () => {
    const fixture = fixturePath()
    if (fixture === null) {
        console.warn('subdBox.3dm fixture not present - skipping fixture read')
        return
    }

    const buffer = fs.readFileSync(fixture)
    const file3dm = rhino.File3dm.fromByteArray(new Uint8Array(buffer))
    const subd = firstSubD(file3dm)
    expect(subd).not.toBeNull()

    expect(subd.vertexCount).toBeGreaterThan(0)
    expect(subd.edgeCount).toBeGreaterThan(0)
    expect(subd.faceCount).toBeGreaterThan(0)
})
