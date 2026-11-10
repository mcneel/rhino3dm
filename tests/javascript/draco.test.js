const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs');

let rhino

beforeAll(async () => {

  rhino = await rhino3dm()

});
beforeEach(async () => {

})

test('dracoDecompress', async () => {

  // read model 
  const model = '../models/mesh.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  // D17: no index on the object table; iterate
  const mesh = [...doc.objects][0].geometry

  const face_count = mesh.faces.count
  const vertex_count = mesh.vertices.count

  const options = new rhino.DracoCompressionOptions()
  options.includeVertexColors = true

  // compressOptions -> compress(mesh, options)
  const draco = rhino.DracoCompression.compress(mesh, options)

  const b64 = draco.toBase64String()

  const decompressed_b64_mesh = rhino.DracoCompression.decompressBase64String(b64)

  expect(face_count === decompressed_b64_mesh.faces.count).toBe(true)
  expect(vertex_count === decompressed_b64_mesh.vertices.count).toBe(true)

  const b64_encoded = new Buffer.from(b64, "base64")
  const ba = Uint8Array.from(b64_encoded)

  const decompressed_ba_mesh = rhino.DracoCompression.decompressByteArray(ba)

  expect(face_count === decompressed_ba_mesh.faces.count).toBe(true)
  expect(vertex_count === decompressed_ba_mesh.vertices.count).toBe(true)

})

test('dracoCompressMeshRoundTrip', () => {
  // a mesh compressed with DracoCompression decompresses from its bytes and from its base64 string
  const D = rhino.DracoCompression
  const d = D.compress(helpers.quadMesh(rhino))
  const b = d.toByteArray()
  expect(b).toBeInstanceOf(Uint8Array)
  expect(b.length).toBeGreaterThan(0)
  const g = D.decompressByteArray(b)
  expect(g).toBeInstanceOf(rhino.Mesh)
  expect(g.vertices.count).toBe(4)
  expect(D.decompressBase64String(d.toBase64String()).vertices.count).toBe(4)
})

test('dracoCompressionOptions', () => {
  // DracoCompressionOptions has RhinoCommon's defaults, and leaving out normals gives no normal attribute
  const o = new rhino.DracoCompressionOptions()
  expect([o.compressionLevel, o.positionQuantizationBits, o.includeNormals]).toEqual([7, 14, true])
  o.includeNormals = false
  o.vertexColorFormat = rhino.DracoColorFormat.RGBA
  expect(rhino.DracoCompression.compress(helpers.quadMesh(rhino), o).normalAttributePosition).toBe(-1)
})

test('dracoCompressPointCloud', () => {
  // a point cloud compressed with DracoCompression decompresses to a point cloud of the same count
  const D = rhino.DracoCompression
  const pc = new rhino.PointCloud()
  for (let i = 0; i < 5; i++) pc.add({ X: i, Y: 2 * i, Z: 0 })
  const g = D.decompressByteArray(D.compress(pc).toByteArray())
  expect(g).toBeInstanceOf(rhino.PointCloud)
  expect(g.count).toBe(5)
})

test('dracoWriteAndDecompressFile', () => {
  // write saves the compressed data, decompressFile reads it back, and a missing file throws (the module's in-memory file system)
  const D = rhino.DracoCompression
  const d = D.compress(helpers.quadMesh(rhino))
  expect(d.write('/tmp/rh3dm_quad.drc')).toBe(true)
  expect(D.decompressFile('/tmp/rh3dm_quad.drc').vertices.count).toBe(4)
  helpers.raises(rhino, () => D.decompressFile('/tmp/rh3dm_missing.drc'), /FileNotFound|File/)
})

test('dracoCompressRejectsANonGeometry', () => {
  // compress with an argument that is not a mesh or a point cloud throws
  helpers.raises(rhino, () => rhino.DracoCompression.compress({}), /no overload matches/)
})

test('dracoCompressTakesOptions', () => {
  // DracoCompression.compress(mesh, options) compresses a mesh that decompresses back to the same vertices
  const m = helpers.quadMesh(rhino)
  const d = rhino.DracoCompression.compress(m, new rhino.DracoCompressionOptions())
  expect(d).toBeTruthy()
  const back = rhino.DracoCompression.decompressByteArray(d.toByteArray())
  expect(back.vertices.count).toBe(4)
  expect(back.faces.count).toBe(2)                    // draco stores triangles: the quad comes back as two
})
