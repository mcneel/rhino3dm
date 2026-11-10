const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
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

    // D17: no index on the object table; iterate
    const obj = [...file3dm.objects][0]
    m = obj.geometry
    a = obj.attributes


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
    expect(mesh.vertices.count === geometry.attributes.position.count).toBe(true)
    expect(mesh.normals.count === geometry.attributes.normal.count).toBe(true)
    expect(mesh.vertexColors.count === geometry.attributes.color.count).toBe(true)

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
    expect(mesh.vertices.count === geometry.attributes.position.count).toBe(true)
    expect(mesh.normals.count === geometry.attributes.normal.count).toBe(true)
    expect(mesh.vertexColors.count === geometry.attributes.color.count).toBe(true)

    const r = geometry.attributes.color.array[0]
    const g = geometry.attributes.color.array[1]
    const b = geometry.attributes.color.array[2]
    const a = geometry.attributes.color.array[3]

    const rn = r * 255
    const gn = g * 255
    const bn = b * 255
    const an = a * 255

    expect(mesh.vertexColors.get(0).r === ~~rn).toBe(true)
    expect(mesh.vertexColors.get(0).g === ~~gn).toBe(true)
    expect(mesh.vertexColors.get(0).b === ~~bn).toBe(true)
    expect(mesh.vertexColors.get(0).a === ~~an).toBe(true)

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

    const face = m.faces.get(0)

    // D11: a MeshFace is a plain object {A, B, C, D}
    expect(Object.keys(face)).toEqual(['A', 'B', 'C', 'D'])
    expect(Number.isInteger(face.A)).toBe(true)
    expect(Number.isInteger(face.B)).toBe(true)
    expect(Number.isInteger(face.C)).toBe(true)
    expect(Number.isInteger(face.D)).toBe(true)

})

test('getFaceVertices', async () => {

    const faceVertices = m.faces.getFaceVertices(0)

    expect(Array.isArray(faceVertices)).toBe(true)
    expect(faceVertices.length === 5).toBe(true)
    expect(typeof faceVertices[0] === 'boolean').toBe(true)
    // D11: points are {X, Y, Z} objects
    for (let i = 1; i <= 4; i++) expect(Object.keys(faceVertices[i])).toEqual(['X', 'Y', 'Z'])

})

function meshFolded(rhino) {
  const m = new rhino.Mesh()
  for (const [x, y, z] of [[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0], [2, 0, 1], [2, 1, 1]]) m.vertices.add(x, y, z)
  m.faces.addFace(0, 1, 2, 3)
  m.faces.addFace(1, 4, 5, 2)
  return m
}

const meshNormalValues = l => Array.from({ length: l.count }, (_, i) => { const v = l.get(i); return [v.X, v.Y, v.Z] })

// the vertex normals computeNormals gives meshFolded
const meshA = 0.3826834261417389, meshB = 0.9238795638084412, meshC = 0.7071067690849304
const meshFoldedNormals = [[0, 0, 1], [-meshA, 0, meshB], [-meshA, 0, meshB], [0, 0, 1], [-meshC, 0, meshC], [-meshC, 0, meshC]]

// the first SubD in a model in tests/models
const meshFirstSubd = name => [...helpers.readModel(rhino, name).objects].map(o => o.geometry).find(g => g instanceof rhino.SubD)

// a 3 x 3 vertex grid (vertex j*3 + i at (i, j)) with four quads; vertex 4 is the only interior one
function meshGrid(rhino) {
  const m = new rhino.Mesh()
  for (let j = 0; j < 3; j++) for (let i = 0; i < 3; i++) m.vertices.add(i, j, 0)
  for (let j = 0; j < 2; j++) for (let i = 0; i < 2; i++) { const a = j * 3 + i; m.faces.addFace(a, a + 1, a + 4, a + 3) }
  return m
}

test('meshVertexColorsToARGBArrayIsEmptyWithoutColors', () => {
  // vertexColors.toARGBArray returns an empty array for a mesh without vertex colors
  expect(Array.from(helpers.quadMesh(rhino).vertexColors.toARGBArray())).toEqual([])
})

test('meshTopologyEdgesGetConnectedFaces', () => {
  // topologyEdges.getConnectedFaces lists the faces on an edge
  expect(Array.from(helpers.quadMesh(rhino).topologyEdges.getConnectedFaces(0))).toEqual([0])
})

test('meshVertexListHideAllKeepsTheVerticesAndClearRemovesThem', () => {
  // vertices.hideAll leaves the vertex count alone; vertices.clear empties the list
  const m = helpers.quadMesh(rhino)
  m.vertices.hideAll()
  expect(m.vertices.count).toBe(4)
  m.vertices.clear()
  expect(m.vertices.count).toBe(0)
})

test('meshVertexListHideAndShow', () => {
  // vertices.hide hides one vertex (twice is harmless) and vertices.show shows it again
  const m = helpers.quadMesh(rhino)
  m.vertices.hide(1)
  expect(m.vertices.isHidden(1)).toBe(true)
  expect(m.vertices.isHidden(0)).toBe(false)
  m.vertices.hide(1)
  m.vertices.show(1)
  expect(m.vertices.isHidden(1)).toBe(false)
})

test('meshFaceListConvertNonPlanarQuadsToTrianglesLeavesAPlanarQuad', () => {
  // faces.convertNonPlanarQuadsToTriangles converts no faces of a planar quad and returns 0
  expect(helpers.quadMesh(rhino).faces.convertNonPlanarQuadsToTriangles(0.01, 0.1, 0)).toBe(0)
})

test('meshVertexListToPoint3dArray', () => {
  // vertices.toPoint3dArray returns every vertex as a point, and an empty array for an empty mesh
  const pts = helpers.quadMesh(rhino).vertices.toPoint3dArray()
  expect(pts.map(p => [p.X, p.Y, p.Z])).toEqual([[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0]])
  expect(new rhino.Mesh().vertices.toPoint3dArray()).toEqual([])
})

test('meshVertexListToPoint3fArray', () => {
  // vertices.toPoint3fArray returns every vertex as a Point3f ({X, Y, Z} numbers)
  const pf = helpers.quadMesh(rhino).vertices.toPoint3fArray()
  expect(pf.every(p => ['X', 'Y', 'Z'].every(k => typeof p[k] === 'number'))).toBe(true)
  expect(pf.map(p => [p.X, p.Y])).toEqual([[0, 0], [1, 0], [1, 1], [0, 1]])
})

test('meshAppendAddsTheOtherMeshAndSkipsNullAndItself', () => {
  // append copies another mesh's vertices and faces; null and the mesh itself are ignored
  const m = new rhino.Mesh()
  expect(m.vertices.count).toBe(0)
  m.append(null)
  m.append(m)
  expect(m.vertices.count).toBe(0)
  const q = helpers.quadMesh(rhino)
  m.append(q)
  expect(m.vertices.count).toBe(4)
  expect(m.faces.count).toBe(1)
  m.append(q)
  expect(m.vertices.count).toBe(8)
})

test('meshDuplicateMeshIsAMesh', () => {
  // duplicateMesh returns a Mesh
  expect(helpers.quadMesh(rhino).duplicateMesh()).toBeInstanceOf(rhino.Mesh)
})

test('meshVertexColorsAddReturnsTheIndex', () => {
  // vertexColors.add(color) appends the color and returns its index
  const m = helpers.quadMesh(rhino)
  expect(m.vertexColors.add({ r: 255, g: 0, b: 0, a: 255 })).toBe(0)
  expect(m.vertexColors.count).toBe(1)
})

test('meshVertexListAddTakesPointsAndCoordinates', () => {
  // vertices.add takes a Point3d, three coordinates or a Point3f; toFloatArray has three floats per vertex
  const m = new rhino.Mesh()
  const v = m.vertices
  v.add(new rhino.Point3d(0, 0, 0))
  v.add(1.0, 0.0, 0.0)
  v.add(new rhino.Point3f(0, 1, 0))
  expect(v.count).toBe(3)
  expect(v.toFloatArray().length).toBe(9)
})

test('meshVertexListIndexIterateAndSet', () => {
  // mesh.vertices has a count, iterates, sets by index and throws past the end
  const m = helpers.quadMesh(rhino)
  const vs = m.vertices
  expect(vs.count).toBe(4)
  expect([...vs].map(p => [p.X, p.Y])).toEqual([[0, 0], [1, 0], [1, 1], [0, 1]])
  vs.set(2, { X: 5, Y: 6, Z: 7 })
  expect([vs.get(2).X, vs.get(2).Z]).toEqual([5, 7])
  helpers.raises(rhino, () => vs.get(4), /Index was outside the bounds of the array/)
})

test('meshFaceListIterates', () => {
  // mesh.faces has a count and iterates over the faces
  const m = helpers.quadMesh(rhino)
  expect(m.faces.count).toBe(1)
  expect([...m.faces].map(f => f.D)).toEqual([3])
})

test('meshVertexColorListIndexIterateAndSet', () => {
  // mesh.vertexColors adds, gets and sets {r, g, b, a} colors by index, iterates, and throws past the end
  const vc = helpers.quadMesh(rhino).vertexColors
  for (let i = 0; i < 4; i++) vc.add({ r: 10 * i, g: 20, b: 30, a: 255 })
  expect(vc.count).toBe(4)
  expect(vc.get(2)).toEqual({ r: 20, g: 20, b: 30, a: 255 })
  vc.set(1, { r: 1, g: 2, b: 3, a: 128 })
  expect(vc.get(1)).toEqual({ r: 1, g: 2, b: 3, a: 128 })
  expect([...vc].map(c => c.r)).toEqual([0, 1, 20, 30])
  helpers.raises(rhino, () => vc.get(4), /out_of_range/)
})

test('meshVertexNormalListComputeAndFlip', () => {
  // normals.computeNormals gives each vertex its normal, and flip reverses them all
  const normals = meshFolded(rhino).normals
  expect(normals.computeNormals()).toBe(true)
  expect(normals.count).toBe(6)
  expect(meshNormalValues(normals)).toEqual(meshFoldedNormals)
  normals.flip()
  expect(meshNormalValues(normals)).toEqual(meshFoldedNormals.map(v => v.map(x => -x)))
})

test('meshVertexNormalListAddStoresTheVectorAsGiven', () => {
  // normals.add appends the vector as given, without unitizing it (D66)
  const normals = meshFolded(rhino).normals
  normals.computeNormals()
  expect(normals.add(0, 0, 2)).toBe(6)
  const n = normals.get(6)
  expect([n.X, n.Y, n.Z]).toEqual([0, 0, 2])
  // 7 normals on 6 vertices: unitizeNormals refuses and leaves the normals as they are
  expect(normals.unitizeNormals()).toBe(false)
  expect(meshNormalValues(normals).slice(0, 6)).toEqual(meshFoldedNormals)
})

test('meshVertexNormalListClearAndDestroy', () => {
  // normals.clear and normals.destroy both leave no normals
  let normals = meshFolded(rhino).normals
  normals.computeNormals()
  normals.clear()
  expect(normals.count).toBe(0)
  normals = meshFolded(rhino).normals
  normals.computeNormals()
  normals.destroy()
  expect(normals.count).toBe(0)
})

test('meshVerticesAddKeepsDoublePrecision', () => {
  // vertices.add(x, y, z) returns the new index, and the vertex keeps double precision
  const m = new rhino.Mesh()
  expect(m.vertices.add(0.1, 0.2, 0.3)).toBe(0)
  expect(m.vertices.point3dAt(0).X).toBe(0.1)
})

test('meshVertexListCountSetter', () => {
  // Setting vertices.count truncates the list; a negative count is ignored (D1)
  const m = new rhino.Mesh()
  for (let i = 0; i < 4; i++) m.vertices.add(i, 0, 0)
  m.vertices.count = 2
  expect(m.vertices.count).toBe(2)
  m.vertices.count = -1
  expect(m.vertices.count).toBe(2)
})

test('meshVertexListSetVertex', () => {
  // vertices.setVertex(i, Point3d) moves a vertex, keeping double precision (D2)
  const m = new rhino.Mesh()
  // a coordinate a float cannot hold picks add(double, ..), so the mesh keeps double precision
  // (whole numbers pick add(float, ..))
  for (let i = 0; i < 3; i++) m.vertices.add(i + 0.1, 0, 0)
  m.vertices.setVertex(1, new rhino.Point3d(0.1, 0.2, 0.3))
  const p = m.vertices.point3dAt(1)
  expect([p.X, p.Y, p.Z]).toEqual([0.1, 0.2, 0.3])
})

test('meshCreateFromSubDControlNetWithAndWithoutTextureCoordinates', () => {
  // createFromSubDControlNet and createFromSubDControlNetWithTextureCoordinates mesh a SubD's control net, the second with texture coordinates
  const expected = { 'subd.3dm': [[738, 279, 0], [2485, 868, 0]], 'subdBox.3dm': [[47, 24, 0], [96, 24, 96]],
                     'subd_creases.3dm': [[15, 6, 0], [24, 6, 24]] }
  for (const [name, counts] of Object.entries(expected)) {
    const s = meshFirstSubd(name)
    ;[x => rhino.Mesh.createFromSubDControlNet(x), x => rhino.Mesh.createFromSubDControlNetWithTextureCoordinates(x)].forEach((make, k) => {
      const n = make(s)
      expect([name, k, n.vertices.count, n.faces.count, n.textureCoordinates.count]).toEqual([name, k, ...counts[k]])
    })
  }
})

test('meshVertexColorsAddAndSetColorTakeRgb', () => {
  // vertexColors.add(r, g, b) returns the new index and setColor(i, r, g, b) sets an opaque color
  const m = new rhino.Mesh()
  for (const [x, y] of [[0, 0], [1, 0], [1, 1]]) m.vertices.add(x, y, 0)
  expect([m.vertexColors.add(255, 0, 0), m.vertexColors.add(0, 128, 0)]).toEqual([0, 1])
  expect(m.vertexColors.setColor(2, 0, 0, 255)).toBe(true)
  expect(Array.from(m.vertexColors.toARGBArray()).map(c => c >>> 0)).toEqual([0xFFFF0000, 0xFF008000, 0xFF0000FF])
})

test('meshVertexColorsAddRejectsAnOutOfRangeChannel', () => {
  // vertexColors.add(r, g, b) throws for a channel above 255
  const m = new rhino.Mesh()
  helpers.raises(rhino, () => m.vertexColors.add(300, 0, 0), /Value of '300' is not valid for 'red'/)
})

test('meshFaceListAddFaceAndSetFaceTakeThreeOrFourVertices', () => {
  // MeshFaceList.addFace / setFace take a triangle's three vertices or a quad's four (D20)
  const m = new rhino.Mesh()
  for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.vertices.add(x, y, 0)
  const faces = m.faces
  expect(faces.addFace(0, 1, 2)).toBe(0)
  expect(faces.addFace(0, 1, 2, 3)).toBe(1)
  expect(faces.setFace(0, 1, 4, 2)).toBe(true)
  expect(faces.setFace(1, 3, 2, 1, 0)).toBe(true)
  const abcd = f => [f.A, f.B, f.C, f.D]
  // a triangle repeats its last vertex
  expect([0, 1].map(i => abcd(faces.get(i)))).toEqual([[1, 4, 2, 2], [3, 2, 1, 0]])
})

test('meshFaceListToIntArrayListsEachFacesVertices', () => {
  // MeshFaceList.toIntArray(false) lists each quad's four vertex indices in order
  const m = meshGrid(rhino)
  expect(Array.from(m.faces.toIntArray(false)).slice(0, 8)).toEqual([0, 1, 4, 3, 1, 2, 5, 4])
})

test('meshGetNakedEdgePointStatusMarksTheBoundaryVertices', () => {
  // getNakedEdgePointStatus is true for every vertex on the boundary, false for the interior one
  const m = meshGrid(rhino)
  expect(Array.from(m.getNakedEdgePointStatus())).toEqual([true, true, true, true, false, true, true, true, true])
})

test('meshFaceListDeleteFacesCompactsTheVertices', () => {
  // deleteFaces(indices, compact) returns the number deleted and drops the vertices only those faces used
  const m = meshGrid(rhino)
  expect(m.faces.deleteFaces([0, 3], true)).toBe(2)
  expect([m.faces.count, m.vertices.count]).toEqual([2, 7])
})
