const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

// rhino3dm 8.x's three.js JSON results for the meshes and clouds below
const threejsJSONOracle = () => JSON.parse(require('fs').readFileSync(helpers.model('old_oracle/threejs_json.json'), 'utf8'))

// the quad + triangle mesh over 5 vertices; colored: vertex colors (255, 50 * i, 0)
function threejsJSONMesh(rhino, colored = true) {
  const m = new rhino.Mesh()
  for (const [x, y] of [[0, 0], [1, 0], [1, 1], [0, 1], [2, 0]]) m.vertices.add(x, y, 0)
  m.faces.addFace(0, 1, 2, 3)
  if (colored) {
    m.faces.addFace(1, 4, 2)
    for (let i = 0; i < 5; i++) m.vertexColors.add(255, i * 50, 0)
  }
  return m
}

// two points with normals and colors (red, blue)
function threejsJSONPointCloud(rhino) {
  const pc = new rhino.PointCloud()
  pc.add(new rhino.Point3d(1, 2, 3), new rhino.Vector3d(0, 0, 1), { r: 255, g: 0, b: 0, a: 255 })
  pc.add(new rhino.Point3d(4, 5, 6), new rhino.Vector3d(0, 1, 0), { r: 0, g: 0, b: 255, a: 255 })
  return pc
}

// the same three.js JSON object as 8.x's: keys, index, attribute item sizes, types and lengths, numbers to float precision
function expectThreejsJSONLike(a, b) {
  expect(Object.keys(a.data).sort()).toEqual(Object.keys(b.data).sort())
  if (b.data.index) expect(a.data.index).toEqual(b.data.index)
  expect(Object.keys(a.data.attributes).sort()).toEqual(Object.keys(b.data.attributes).sort())
  for (const k of Object.keys(b.data.attributes)) {
    const x = a.data.attributes[k], y = b.data.attributes[k]
    expect([x.itemSize, x.type, x.array.length]).toEqual([y.itemSize, y.type, y.array.length])
    x.array.forEach((v, i) => helpers.approx(v, y.array[i], 1e-6))
  }
}

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

// (first in the file: each member warns only the first time it is called in the module)
test('threejsJSONMembersWarnOnceThatTheyAreDeprecated', () => {
  // each three.js JSON member warns once, naming its Buffers replacement
  const warned = []
  const warn = console.warn
  console.warn = (...a) => warned.push(a.join(' '))
  try {
    const m = threejsJSONMesh(rhino)
    const pc = threejsJSONPointCloud(rhino)
    for (let k = 0; k < 2; k++) {
      const j = m.toThreejsJSON()
      m.toThreejsJSONRotate(true)
      rhino.Mesh.toThreejsJSONMerged([threejsJSONMesh(rhino, false), m], true)
      rhino.Mesh.createFromThreejsJSON(j)
      rhino.PointCloud.createFromThreejsJSON(pc.toThreejsJSON())
    }
  } finally {
    console.warn = warn
  }
  const mine = warned.filter(w => /^rhino3dm: .* is deprecated/.test(w))
  for (const name of ['Mesh.toThreejsJSON ', 'Mesh.toThreejsJSONRotate', 'Mesh.toThreejsJSONMerged', 'Mesh.createFromThreejsJSON',
    'PointCloud.toThreejsJSON', 'PointCloud.createFromThreejsJSON'])
    expect([name, mine.filter(w => w.includes(name)).length]).toEqual([name, 1])
  expect(mine.every(w => /use .*Buffers/.test(w))).toBe(true)
})

test('meshToThreejsJSONGivesTheSameObjectAs8x', () => {
  // Mesh.toThreejsJSON and toThreejsJSONRotate(true) give 8.x's objects; the normals are computed on a copy
  const oracle = threejsJSONOracle()
  const m = threejsJSONMesh(rhino)
  const j = m.toThreejsJSON()
  expectThreejsJSONLike(j, oracle.meshJSON)
  expect(j.data.index).toEqual({ type: 'Uint16Array', array: [0, 1, 2, 2, 3, 0, 1, 4, 2] })
  expect(m.normals.count).toBe(0)                           // the caller's mesh is unchanged
  expectThreejsJSONLike(m.toThreejsJSONRotate(true), oracle.meshJSONRotate)
})

test('meshToThreejsJSONMergedGivesTheSameObjectAs8x', () => {
  // Mesh.toThreejsJSONMerged(meshes, true) gives 8.x's merged object
  const oracle = threejsJSONOracle()
  const tri = new rhino.Mesh()
  for (const [x, y] of [[5, 0], [6, 0], [5, 1]]) tri.vertices.add(x, y, 0)
  tri.faces.addFace(0, 1, 2)
  // (8.x's merge mixes a colored mesh with an uncolored one into no colors: both uncolored here)
  expectThreejsJSONLike(rhino.Mesh.toThreejsJSONMerged([threejsJSONMesh(rhino, false), tri], true), oracle.mergedRotate)
})

test('meshCreateFromThreejsJSONReadsItBackAs8x', () => {
  // Mesh.createFromThreejsJSON reads a toThreejsJSON object back as 8.x did (turned from Y-up), colors included
  const oracle = threejsJSONOracle()
  const m = threejsJSONMesh(rhino)
  const back = rhino.Mesh.createFromThreejsJSON(m.toThreejsJSON())
  expect([back.vertices.count, back.faces.count]).toEqual([5, 3])
  expect(Array.from(back.toThreejsBuffers(false).position)).toEqual(oracle.createFromNewJSONPosition)
  expect(Array.from(back.vertexColors.toARGBArray())).toEqual(Array.from(m.vertexColors.toARGBArray()))
})

test('meshCreateFromThreejsJSONRefusesBadObjects', () => {
  // an object without data gives null; an index past the vertices throws
  expect(rhino.Mesh.createFromThreejsJSON({})).toBeNull()
  helpers.raises(rhino, () => rhino.Mesh.createFromThreejsJSON({ data: { index: { array: [0, 1, 7] },
    attributes: { position: { itemSize: 3, array: [0, 0, 0, 1, 0, 0, 0, 1, 0] } } } }), /outside the vertices/)
})

test('pointCloudThreejsJSONGivesTheSameObjectAs8x', () => {
  // PointCloud.toThreejsJSON gives 8.x's object, and createFromThreejsJSON reads its points back
  const oracle = threejsJSONOracle()
  const pc = threejsJSONPointCloud(rhino)
  expectThreejsJSONLike(pc.toThreejsJSON(), oracle.pcJSON)
  expect(Array.from(rhino.PointCloud.createFromThreejsJSON(pc.toThreejsJSON()).getPoints()).map(q => [q.X, q.Y, q.Z]))
    .toEqual([[1, 2, 3], [4, 5, 6]])
})

test('pointCloudCreateFromThreejsJSONReadsRGBAColorsAs8x', () => {
  // an RGBA color's 4th channel is opacity, as in 8.x
  const oracle = threejsJSONOracle()
  const rgba = { data: { attributes: { position: { itemSize: 3, array: [1, 2, 3] }, color: { itemSize: 4, array: [1, 0, 0, 0.5] } } } }
  const cols = c => Array.from(c.getColors()).map(k => [k.r, k.g, k.b, k.a])
  expect(cols(rhino.PointCloud.createFromThreejsJSON(rgba))).toEqual(oracle.pcFromRGBAColors)
})
