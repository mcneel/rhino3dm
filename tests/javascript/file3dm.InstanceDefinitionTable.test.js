const rhino3dm = require('rhino3dm')
const fs = require('fs')

let rhino
let exportMeshJson, badMeshes
beforeAll(async () => {
  rhino = await rhino3dm()
  exportMeshJson = fs.readFileSync('../models/badMesh.json')
  badMeshes = JSON.parse(exportMeshJson.toString())
})

test('AddMesh1', async () => {

  const file3dm = new rhino.File3dm()
  const mesh1 = exportMeshToRhinoMesh(badMeshes[0])
  const blockId = file3dm.instanceDefinitions().add(`block`, '', '', '', [0, 0, 0], [mesh1], [new rhino.ObjectAttributes()])

})

test('AddMesh2', async () => {

  const file3dm = new rhino.File3dm()
  const mesh2 = exportMeshToRhinoMesh(badMeshes[1])
  const blockId = file3dm.instanceDefinitions().add(`block`, '', '', '', [0, 0, 0], [mesh2], [new rhino.ObjectAttributes()])

})

test('AddMesh3', async () => {

  const file3dm = new rhino.File3dm()
  const mesh3 = exportMeshToRhinoMesh(badMeshes[1])
  const blockId = file3dm.instanceDefinitions().add(`block`, '', '', '', [0, 0, 0], [mesh3], [new rhino.ObjectAttributes()])

})

test('AddMeshes1', async () => {

  const file3dm = new rhino.File3dm()
  let cnt = 0
  const meshes = []
  badMeshes.forEach((m) => {
    console.log(cnt)
    cnt++
    const mesh = exportMeshToRhinoMesh(m)
    const blockId = file3dm.instanceDefinitions().add(`block ${cnt}`, '', '', '', [0, 0, 0], [mesh], [new rhino.ObjectAttributes()])
  })

})

test('AddMeshes2', async () => {

  const file3dm = new rhino.File3dm()
  const meshes = badMeshes.map((m) => exportMeshToRhinoMesh(m))
  const blockId = file3dm.instanceDefinitions().add(`block`, '', '', '', [0, 0, 0], meshes, meshes.map((_) => new rhino.ObjectAttributes()))

})

test('AddMeshes3', async () => {

  const file3dm = new rhino.File3dm()
  const meshes = badMeshes.map((m) => exportMeshToRhinoMesh(m))
  const blockId1 = file3dm.instanceDefinitions().add(`block`, '', '', '', [0, 0, 0], [meshes[0]], [new rhino.ObjectAttributes()])
  const blockId2 = file3dm.instanceDefinitions().add(`block`, '', '', '', [0, 0, 0], [meshes[1]], [new rhino.ObjectAttributes()])
  const blockId3 = file3dm.instanceDefinitions().add(`block`, '', '', '', [0, 0, 0], [meshes[2]], [new rhino.ObjectAttributes()])

})

test('AddMeshes4', async () => {

  const file3dm = new rhino.File3dm()

  const oa1 = new rhino.ObjectAttributes()
  const oa2 = new rhino.ObjectAttributes()
  const oa3 = new rhino.ObjectAttributes()

  file3dm.objects().addPoint([0, 0, 0], oa1)
  file3dm.objects().addPoint([0, 0, 0], oa2)
  file3dm.objects().addPoint([0, 0, 0], oa3)



})

const exportMeshToRhinoMesh = (mesh) => {
  const rhinoMesh = new rhino.Mesh()
  mesh.vertices.forEach((vertex) => {
    rhinoMesh.vertices().add(vertex.x, vertex.y, vertex.z)
  })
  for (let i = 0; i < mesh.faces.length; i++) {
    rhinoMesh.faces().addTriFace(mesh.faces[i][0], mesh.faces[i][1], mesh.faces[i][2])
  }
  return rhinoMesh
}