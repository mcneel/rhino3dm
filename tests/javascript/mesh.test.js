const rhino3dm = require('rhino3dm')
const fs = require('fs')
const THREE = require('three')

let rhino
let m, a
beforeAll(async () => {
    rhino = await rhino3dm()

    // read model 
    const model = '../models/mesh.3dm'

    const buffer = fs.readFileSync(model)
    const arr = new Uint8Array(buffer)
    const file3dm = rhino.File3dm.fromByteArray(arr)

    expect(file3dm !== null).toBe(true)

    m = file3dm.objects().get(0).geometry()
    a = file3dm.objects().get(0).attributes()


})

test('createFromThreejsJSON', async () => {

    let geometry = new THREE.IcosahedronGeometry(10, 2)
    const colors3 = []
    const colors4 = []
    const count = geometry.attributes.position.count

    //assign random color to verts
    for (let i = 0; i < count; i++) {

        const r = Math.random()
        const g = Math.random()
        const b = Math.random()

        colors3.push(r, g, b)

    }

    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors3, 3))

    let mesh = rhino.Mesh.createFromThreejsJSON({ data: geometry })

    expect(mesh instanceof rhino.Mesh).toBe(true)
    expect(mesh.vertices().count === geometry.attributes.position.count).toBe(true)
    expect(mesh.normals().count === geometry.attributes.normal.count).toBe(true)
    expect(mesh.vertexColors().count === geometry.attributes.color.count).toBe(true)

    //assign random color to verts
    for (let i = 0; i < count; i++) {

        const r = geometry.attributes.color.getX(i)
        const g = geometry.attributes.color.getY(i)
        const b = geometry.attributes.color.getZ(i)
        const a = Math.random()

        colors4.push(r, g, b, a)

    }

    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors4, 4))

    mesh = rhino.Mesh.createFromThreejsJSON({ data: geometry })

    expect(mesh instanceof rhino.Mesh).toBe(true)
    expect(mesh.vertices().count === geometry.attributes.position.count).toBe(true)
    expect(mesh.normals().count === geometry.attributes.normal.count).toBe(true)
    expect(mesh.vertexColors().count === geometry.attributes.color.count).toBe(true)

    const r = geometry.attributes.color.array[0]
    const g = geometry.attributes.color.array[1]
    const b = geometry.attributes.color.array[2]
    const a = geometry.attributes.color.array[3]

    const rn = r * 255
    const gn = g * 255
    const bn = b * 255
    const an = a * 255

    expect(mesh.vertexColors().get(0).r === ~~rn).toBe(true)
    expect(mesh.vertexColors().get(0).g === ~~gn).toBe(true)
    expect(mesh.vertexColors().get(0).b === ~~bn).toBe(true)
    expect(mesh.vertexColors().get(0).a === ~~an).toBe(true)

})

test('isManifold', async () => {

    const isManifold = m.isManifold(true)

    expect(Array.isArray(isManifold)).toBe(true)
    expect(isManifold.length === 3).toBe(true)
    expect(typeof isManifold[0] === 'boolean').toBe(true)
    expect(typeof isManifold[0] === 'boolean').toBe(true)
    expect(typeof isManifold[0] === 'boolean').toBe(true)

    expect(isManifold[0]).toBe(true) //result
    expect(isManifold[1]).toBe(true) //oriented
    expect(isManifold[2]).toBe(false) //has boundary

})

test('getFace', async () => {

    const face = m.faces().get(0)

    expect(Array.isArray(face)).toBe(true)
    expect(face.length === 4).toBe(true)
    expect(Number.isInteger(face[0])).toBe(true)
    expect(Number.isInteger(face[1])).toBe(true)
    expect(Number.isInteger(face[2])).toBe(true)
    expect(Number.isInteger(face[3])).toBe(true)

})

test('getFaceVertices', async () => {

    const faceVertices = m.faces().getFaceVertices(0)

    expect(Array.isArray(faceVertices)).toBe(true)
    expect(faceVertices.length === 5).toBe(true)
    expect(typeof faceVertices[0] === 'boolean').toBe(true)
    expect(Array.isArray(faceVertices[1])).toBe(true)
    expect(Array.isArray(faceVertices[2])).toBe(true)
    expect(Array.isArray(faceVertices[3])).toBe(true)
    expect(Array.isArray(faceVertices[4])).toBe(true)

})
