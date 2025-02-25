const rhino3dm = require('rhino3dm')
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

  const mesh = doc.objects().get(0).geometry()

  const face_count = mesh.faces().count
  const vertex_count = mesh.vertices().count

  const options = new rhino.DracoCompressionOptions()
  options.includeVertexColors = true

  const draco = rhino.DracoCompression.compressOptions(mesh, options)

  const b64 = draco.toBase64String()

  const decompressed_b64_mesh = rhino.DracoCompression.decompressBase64String(b64)

  expect(face_count === decompressed_b64_mesh.faces().count).toBe(true)
  expect(vertex_count === decompressed_b64_mesh.vertices().count).toBe(true)

  const b64_encoded = new Buffer.from(b64, "base64")
  const ba = Uint8Array.from(b64_encoded)

  const decompressed_ba_mesh = rhino.DracoCompression.decompressByteArray(ba)

  expect(face_count === decompressed_ba_mesh.faces().count).toBe(true)
  expect(vertex_count === decompressed_ba_mesh.vertices().count).toBe(true)

})

