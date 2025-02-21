const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('brepVertexGetEdgeIndices', async () => {

   const sphere = new rhino.Sphere([0,0,0], 5)
   const bbox = sphere.toBrep().getBoundingBox()
   const brep = rhino.Brep.createFromBoundingBox(bbox)
   const vertex = brep.vertices().get(0)
   const edgeIndices = vertex.edgeIndices

   expect(Array.isArray(edgeIndices)).toBe(true)
   expect(edgeIndices.length).toBe(3)
   expect(Number.isFinite(edgeIndices[0])).toBe(true)

})