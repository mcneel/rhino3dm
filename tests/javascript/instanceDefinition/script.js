import * as THREE from 'three'
import rhino3dm from 'rhino3dm'
const rhino = await rhino3dm()
console.log('Loaded rhino3dm.')

console.log(badMeshes)

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


// create three meshes

const g1 = new THREE.DodecahedronGeometry(10,5)
console.log(g1.getAttribute('position').count)
const g2 = new THREE.TetrahedronGeometry(10,1)
console.log(g2.getAttribute('position').count)
const g3 = new THREE.OctahedronGeometry(10,1)
console.log(g3.getAttribute('position').count)

console.log(`total verts: ${g1.getAttribute('position').count + g2.getAttribute('position').count + g3.getAttribute('position').count}`)

//create rhino meshes
const meshes = []

meshes.push(rhino.Mesh.createFromThreejsJSON( { data: g1 } ))
meshes.push(rhino.Mesh.createFromThreejsJSON( { data: g2 } ))
meshes.push(rhino.Mesh.createFromThreejsJSON( { data: g3 } ))

let vCnt = 0
let fCnt = 0
badMeshes.forEach((m) => {
    console.log(m.vertices.length)
    vCnt += m.vertices.length
    fCnt += m.faces.length
})

console.log(`total verts import: ${vCnt}, total faces import: ${fCnt}`)



// test idef add

const model = new rhino.File3dm()
model.applicationName = 'nodejs'
model.applicationDetails = 'rhino-developer-samples'
model.applicationUrl = 'https://rhino3d.com'
model.startSectionComments = 'hello'

let cnt = 0
meshes.forEach((m) => {
    console.log(cnt)
    cnt++
    //const mesh = exportMeshToRhinoMesh(m)
    const blockId = model.instanceDefinitions().add(`block ${cnt}`, '', '', '', [0, 0, 0], [m], [new rhino.ObjectAttributes()])
  })

console.log(model.instanceDefinitions().count)

