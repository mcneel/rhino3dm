const rhino3dm = require('rhino3dm')

let rhino
beforeAll(async () => {
    rhino = await rhino3dm()
})

function boxBrep(rhino) {
    const sphere = new rhino.Sphere([0, 0, 0], 5)
    const bbox = sphere.toBrep().getBoundingBox()
    return rhino.Brep.createFromBoundingBox(bbox)
}

// RH3DM #713 follow-up: Brep loop/trim topology traversal (Py + JS parity).
test('faceLoopsAndTrims', async () => {
    const brep = boxBrep(rhino)
    const face = brep.faces().get(0)

    const loops = face.loops
    expect(loops.count).toBe(1)

    const loop = loops.get(0)
    expect(loop.loopType).toBe(rhino.BrepLoopType.Outer)
    expect(loop.trimCount).toBe(4)

    const trims = loop.trims
    expect(trims.count).toBe(4)

    const trim = trims.get(0)
    expect(typeof trim.edgeIndex === 'number').toBe(true)
    expect(trim.edgeIndex).toBeGreaterThanOrEqual(0)
    expect(typeof trim.isReversed === 'boolean').toBe(true)
    expect(typeof trim.startVertexIndex === 'number').toBe(true)
    expect(typeof trim.endVertexIndex === 'number').toBe(true)
})

test('outerLoop', async () => {
    const brep = boxBrep(rhino)
    const face = brep.faces().get(0)

    const outer = face.outerLoop()
    expect(outer).not.toBeNull()
    expect(outer.loopType).toBe(rhino.BrepLoopType.Outer)
    expect(outer.trimCount).toBe(4)
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
