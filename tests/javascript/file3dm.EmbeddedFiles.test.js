const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('embeddedFilesAddAndIterate', () => {
  // embeddedFiles.add embeds a file that iteration yields, and it survives a write and read
  // WASM has no host file system: a file in the module's own file system to embed (any bytes do)
  const data = '/tmp/rh3dm_js_embedded_note.txt'
  expect(new rhino.File3dm().write(data, 7)).toBe(true)
  const f = new rhino.File3dm()
  expect(f.embeddedFiles.add(data)).toBe(true)
  const files = [...f.embeddedFiles]
  expect(files.length).toBe(1)
  expect(files[0]).toBeInstanceOf(rhino.File3dmEmbeddedFile)
  expect(files[0].filename.endsWith('note.txt')).toBe(true)
  expect(files[0].componentType).toBe(rhino.ModelComponentType.EmbeddedFile)
  const path = '/tmp/rh3dm_js_embedded.3dm'
  expect(f.write(path, 7)).toBe(true)
  const g = rhino.File3dm.read(path)
  expect([...g.embeddedFiles].map(e => e.filename)).toEqual([files[0].filename])
})

test('embeddedFilesFindIndex', () => {
  // embeddedFiles.findIndex returns the embedded file at an index, null past the end
  const f = new rhino.File3dm()
  // WASM has no host file system: a file written to the module's own file system to embed (any bytes do)
  const path = '/tmp/embedded_files_find_index.3dm'
  expect(new rhino.File3dm().write(path, 7)).toBe(true)
  expect(f.embeddedFiles.add(path)).toBe(true)
  expect(f.embeddedFiles.count).toBe(1)
  const ef = f.embeddedFiles.findIndex(0)
  expect(ef).toBeInstanceOf(rhino.File3dmEmbeddedFile)
  expect(ef.filename).toBe([...f.embeddedFiles][0].filename)
  expect(ef.length).toBeGreaterThan(0)
  expect(f.embeddedFiles.findIndex(5) == null).toBe(true)
})

test('embeddedFileReadReadsAFileFromDisk', () => {
  // File3dmEmbeddedFile.read reads a file, or returns null when it does not exist
  // (WASM has no host file system: the file is written to the module's own)
  expect(new rhino.File3dm().write('/tmp/embedded_file_read.3dm', 7)).toBe(true)
  const ef = rhino.File3dmEmbeddedFile.read('/tmp/embedded_file_read.3dm')
  expect(ef).toBeInstanceOf(rhino.File3dmEmbeddedFile)
  expect(Number(ef.length)).toBeGreaterThan(0)
  expect(rhino.File3dmEmbeddedFile.read('/tmp/missing_embedded_file.txt')).toBeNull()
})

test('embeddedFilesAddAnEmbeddedFileFromBytes', () => {
  // fromByteArray(bytes, filename) names the file; embeddedFiles.add(file) adds a copy, which survives a write
  const bytes = new Uint8Array(Array.from({ length: 300 }, (_, i) => i % 251))
  const ef = rhino.File3dmEmbeddedFile.fromByteArray(bytes, 'texture.bin')
  expect(ef.filename).toBe('texture.bin')
  const f = new rhino.File3dm()
  expect(f.embeddedFiles.add(ef)).toBe(true)
  expect(f.embeddedFiles.count).toBe(1)
  expect(f.embeddedFiles.findIndex(0).filename).toBe('texture.bin')
  expect(f.embeddedFiles.findIndex(0).length).toBe(BigInt(300))
  const g = rhino.File3dm.fromByteArray(f.toByteArray())
  expect([...g.embeddedFiles].map(e => e.filename)).toEqual(['texture.bin'])
  expect(f.embeddedFiles.add(new rhino.File3dmEmbeddedFile())).toBe(false)
})

test('embeddedFileToByteArray', () => {
  // toByteArray returns an embedded file's bytes, also after a write and read; empty without data
  const bytes = new Uint8Array(Array.from({ length: 768 }, (_, i) => i % 256))
  const f = new rhino.File3dm()
  expect(f.embeddedFiles.add(rhino.File3dmEmbeddedFile.fromByteArray(bytes, 'data.bin'))).toBe(true)
  const out = f.embeddedFiles.findIndex(0).toByteArray()
  expect(out).toBeInstanceOf(Uint8Array)
  expect([...out]).toEqual([...bytes])
  const g = rhino.File3dm.fromByteArray(f.toByteArray())
  expect([...g.embeddedFiles.findIndex(0).toByteArray()]).toEqual([...bytes])
  expect(new rhino.File3dmEmbeddedFile().toByteArray().length).toBe(0)
})

test('embeddedFileFromByteArray', () => {
  // File3dmEmbeddedFile.fromByteArray makes an embedded file of those bytes
  const bytes = new Uint8Array(Array.from({ length: 300 }, (_, i) => i % 251))
  const ef = rhino.File3dmEmbeddedFile.fromByteArray(bytes)
  expect(ef).toBeInstanceOf(rhino.File3dmEmbeddedFile)
  expect(ef.length).toBe(BigInt(300))
})

test('file3dmGetEmbeddedFileAsBase64', () => {
  // getEmbeddedFileAsBase64 finds a file Rhino embedded by its name, in any case; '' when absent or not the full path in strict mode
  const f = helpers.readModel(rhino, 'sphereDecals.3dm')
  const baseName = s => s.replace(/\\/g, '/').split('/').pop()
  const files = [...f.embeddedFiles]
  const found = files.map(e => f.getEmbeddedFileAsBase64(baseName(e.filename).toLowerCase()))
  files.forEach((e, i) => {
    const data = Buffer.from(found[i], 'base64')
    expect(data.length).toBe(Number(e.length))                          // each name finds its own file
    expect([data[0], data[1]]).toEqual([0xff, 0xd8])                    // a JPEG
    expect(f.getEmbeddedFileAsBase64(e.filename)).toBe(found[i])
    expect(f.getEmbeddedFileAsBase64Strict(baseName(e.filename), true)).toBe('')
  })
  expect(found[0]).not.toBe(found[1])
  expect(f.getEmbeddedFileAsBase64('nothing.png')).toBe('')
})

test('file3dmGetEmbeddedFileAsBase64StrictTakesTheFullPath', () => {
  // getEmbeddedFileAsBase64Strict(path, true) finds a file by the full path it was embedded with
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')             // embedded on macOS: filename is the stored path
  const p = [...f.embeddedFiles][0].filename
  const b = f.getEmbeddedFileAsBase64Strict(p, true)
  expect(Buffer.from(b, 'base64').subarray(1, 4).toString()).toBe('PNG')
  expect(b).toBe(f.getEmbeddedFileAsBase64(p))
})

test('file3dmGetEmbeddedFileAsBase64ReadsOnlyWhatRhinoEmbedded', () => {
  // getEmbeddedFileAsBase64 reads the files Rhino's renderer embedded, not one added with embeddedFiles.add
  // (WASM has no host file system: the file to embed is a model written to the module's own)
  const path = '/tmp/embedded_base64.3dm'
  expect(new rhino.File3dm().write(path, 7)).toBe(true)
  const f = new rhino.File3dm()
  expect(f.embeddedFiles.add(path)).toBe(true)
  expect(f.getEmbeddedFileAsBase64([...f.embeddedFiles][0].filename)).toBe('')
})
