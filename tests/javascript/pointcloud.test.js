const rhino3dm = require('rhino3dm')
const THREE = require('three')

let rhino
beforeEach(async () => {
    rhino = await rhino3dm()
})

test('ctor', async () => {

    const pointArray = []
    const pointList = new rhino.Point3dList(15)

    for (let i = 0; i < 15; i++) {
        pointArray.push([i, i, 0])
        pointList.add(i, i, 0)
    }

    const pcFromArray = new rhino.PointCloud(pointArray)
    const pcFromList = new rhino.PointCloud(pointList)

    const r1 = pcFromArray.closestPoint([0, 0, 0])
    const r2 = pcFromList.closestPoint([0, 0, 0])

    const result = r1[0] === r2[0] && r1[1] === r2[1] && r1[2] === r2[2]

    expect(result).toBe(true)

})

test('createFromThreejsJSON', async () => {

    const geometry = new THREE.BufferGeometry();

    const num = 100;

    const verts = []
    const colors3 = []
    const colors4 = []

    for (let i = 0; i < num; i++) {

        const x = Math.random()
        const y = Math.random()
        const z = Math.random()

        verts.push(x, y, z)

        colors3.push(x, y, z)

    }

    geometry.setAttribute('position', new THREE.Float32BufferAttribute(verts, 3))
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors3, 3))

    let pc = rhino.PointCloud.createFromThreejsJSON({ data: geometry })

    expect(pc instanceof rhino.PointCloud).toBe(true)
    expect(pc.containsColors).toBe(true)
    expect(pc.getPoints().length === geometry.attributes.position.count).toBe(true)
    expect(pc.getColors().length === geometry.attributes.color.count).toBe(true)

    //assign random color to verts
    for (let i = 0; i < num; i++) {

        const r = geometry.attributes.color.getX(i)
        const g = geometry.attributes.color.getY(i)
        const b = geometry.attributes.color.getZ(i)
        const a = Math.random()

        colors4.push(r, g, b, a)

    }

    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors4, 4))

    pc = rhino.PointCloud.createFromThreejsJSON({ data: geometry })

    expect(pc instanceof rhino.PointCloud).toBe(true)
    expect(pc.containsColors).toBe(true)
    expect(pc.getPoints().length === geometry.attributes.position.count).toBe(true)
    expect(pc.getColors().length === geometry.attributes.color.count).toBe(true)

    const r = geometry.attributes.color.array[0]
    const g = geometry.attributes.color.array[1]
    const b = geometry.attributes.color.array[2]
    const a = geometry.attributes.color.array[3]

    const rn = r * 255
    const gn = g * 255
    const bn = b * 255
    const an = a * 255

    expect(pc.getColors()[0].r === ~~rn).toBe(true)
    expect(pc.getColors()[0].g === ~~gn).toBe(true)
    expect(pc.getColors()[0].b === ~~bn).toBe(true)
    expect(pc.getColors()[0].a === ~~an).toBe(true)

})

test('members', async () => {

    const pc = new rhino.PointCloud()

    pc.addPointNormalColorValue([0, 0, 0], [0, 1, 1], { 'r': 255, 'g': 0, 'b': 0, 'a': 0 }, 1.234)
    pc.addPointNormalColorValue([0, 0, 0], [0, 1, 2], { 'r': 10, 'g': 0, 'b': 27, 'a': 0 }, 1.999)
    pc.addPointNormalColorValue([0, 0, 0], [0, 13, 1], { 'r': 255, 'g': 90, 'b': 0, 'a': 0 }, 1.333)
    pc.addPointNormalColorValue([0, 0, 0], [0, 1, 4], { 'r': 44, 'g': 0, 'b': 0, 'a': 0 }, 1.111)

    const pts = pc.getPoints()
    expect(pts.length > 0).toBe(true)
    expect(Array.isArray(pts)).toBe(true)
    expect(Array.isArray(pts[0])).toBe(true)

    const nrmls = pc.getNormals()
    expect(nrmls.length > 0).toBe(true)
    expect(Array.isArray(nrmls)).toBe(true)
    expect(Array.isArray(nrmls[0])).toBe(true)

    const cols = pc.getColors()
    expect(cols.length > 0).toBe(true)
    expect(Array.isArray(cols)).toBe(true)
    expect(typeof cols[0] === 'object').toBe(true)
    expect(cols[0].r).toBeDefined()
    expect(cols[0].g).toBeDefined()
    expect(cols[0].b).toBeDefined()
    expect(cols[0].a).toBeDefined()
    expect(Number.isInteger(cols[0].r)).toBe(true)
    expect(Number.isInteger(cols[0].g)).toBe(true)
    expect(Number.isInteger(cols[0].b)).toBe(true)
    expect(Number.isInteger(cols[0].a)).toBe(true)

    const vals = pc.getValues()
    expect(vals.length > 0).toBe(true)
    expect(Array.isArray(vals)).toBe(true)
    expect(typeof vals[0] === 'number').toBe(true)

})