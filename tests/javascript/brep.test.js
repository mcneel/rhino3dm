const rhino3dm = require('rhino3dm')

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