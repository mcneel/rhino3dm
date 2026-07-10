const rhino3dm = require('rhino3dm')
const fs = require('fs')
const path = require('path')

// RH3DM-178/177/176/175/169: read-only SubD component access.

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

test('empty subd component lists', async () => {
    const subd = new rhino.SubD()
    expect(subd.vertexCount).toBe(0)
    expect(subd.edgeCount).toBe(0)
    expect(subd.faceCount).toBe(0)
    expect(subd.vertices().count).toBe(0)
    expect(subd.vertices().get(0)).toBeNull()   // out of range -> null, not a crash
})

test('tag enums', async () => {
    expect(rhino.SubDVertexTag.Crease).not.toBe(rhino.SubDVertexTag.Smooth)
    expect(rhino.SubDEdgeTag.Crease).not.toBe(rhino.SubDEdgeTag.Smooth)
})

test('read components from fixture', async () => {
    const fixture = fixturePath()
    if (fixture === null) {
        console.warn('subdBox.3dm fixture not present - skipping component read')
        return
    }

    const buffer = fs.readFileSync(fixture)
    const file3dm = rhino.File3dm.fromByteArray(new Uint8Array(buffer))
    const subd = firstSubD(file3dm)
    expect(subd).not.toBeNull()

    expect(subd.vertexCount).toBeGreaterThan(0)
    expect(subd.edgeCount).toBeGreaterThan(0)
    expect(subd.faceCount).toBeGreaterThan(0)
    expect(subd.vertices().count).toBe(subd.vertexCount)
    expect(subd.edges().count).toBe(subd.edgeCount)
    expect(subd.faces().count).toBe(subd.faceCount)

    const v0 = subd.vertices().get(0)
    expect(v0).not.toBeNull()
    // ON_3dPoint is a value_array in JS -> [x, y, z]
    expect(v0.controlNetPoint.length).toBe(3)
    expect(typeof v0.controlNetPoint[0]).toBe('number')

    const f0 = subd.faces().get(0)
    expect(f0.vertexCount).toBeGreaterThanOrEqual(3)
    expect(f0.vertexAt(0)).not.toBeNull()

    const e0 = subd.edges().get(0)
    expect(e0.vertexFrom()).not.toBeNull()
    expect(e0.vertexTo()).not.toBeNull()
    expect(e0.isCrease).toBe(e0.tag === rhino.SubDEdgeTag.Crease)

    const found = subd.vertices().find(v0.id)
    expect(found).not.toBeNull()
    expect(found.id).toBe(v0.id)
})
