const rhino3dm = require('rhino3dm')
const THREE = require('three')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
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

    geometry.setAttribute( 'color', new THREE.Float32BufferAttribute( colors3, 3 ) )

    let mesh = rhino.Mesh.createFromThreejsJSON( { data: geometry })

    expect(mesh instanceof rhino.Mesh).toBe(true)
    expect(mesh.vertices().count        === geometry.attributes.position.count).toBe(true)
    expect(mesh.normals().count         === geometry.attributes.normal.count).toBe(true)
    expect(mesh.vertexColors().count    === geometry.attributes.color.count).toBe(true)
    console.log(mesh.vertexColors().get(0))

    //assign random color to verts
    for (let i = 0; i < count; i++) {

        const r = geometry.attributes.color.getX(i)
        const g = geometry.attributes.color.getY(i)
        const b = geometry.attributes.color.getZ(i)
        const a = Math.random()

        colors4.push(r, g, b, a)

    }

    geometry.setAttribute( 'color', new THREE.Float32BufferAttribute( colors4, 4 ) )

    mesh = rhino.Mesh.createFromThreejsJSON( { data: geometry })

    expect(mesh instanceof rhino.Mesh).toBe(true)
    expect(mesh.vertices().count        === geometry.attributes.position.count).toBe(true)
    expect(mesh.normals().count         === geometry.attributes.normal.count).toBe(true)
    expect(mesh.vertexColors().count    === geometry.attributes.color.count).toBe(true)
    console.log(mesh.vertexColors().get(0))

    const r = geometry.attributes.color.array[0]
    const g = geometry.attributes.color.array[1]
    const b = geometry.attributes.color.array[2]
    const a = geometry.attributes.color.array[3]

    const rn = r*255
    const gn = g*255
    const bn = b*255
    const an = a*255

    expect(mesh.vertexColors().get(0).r === ~~rn).toBe(true)
    expect(mesh.vertexColors().get(0).g === ~~gn).toBe(true)
    expect(mesh.vertexColors().get(0).b === ~~bn).toBe(true)
    expect(mesh.vertexColors().get(0).a === ~~an).toBe(true)

})