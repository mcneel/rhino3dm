const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')
const fs = require('fs')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('read3dmTables', async () => {

  // read model 
  const model = '../models/file3dm_stuff.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  const objectTableCnt = doc.objects.count
  const layerTableCnt = doc.layers.count
  const materialsTableCnt = doc.materials.count
  const linetypeTableCnt = doc.linetypes.count
  const bitmapsTableCnt = doc.bitmaps.count
  // groups -> allGroups
  const groupTableCnt = doc.allGroups.count
  const idTableCnt = doc.instanceDefinitions.count
  const viewTableCnt = doc.views.count
  const namedViewsTableCnt = doc.namedViews.count
  const pluginDataTableCnt = doc.plugInData.count
  const stringTableCnt = doc.strings.count
  const embeddedFileTableCnt = doc.embeddedFiles.count
  // renderContent -> one collection per kind (iteration only)
  const renderContentTableCnt = [...doc.renderMaterials].length + [...doc.renderEnvironments].length + [...doc.renderTextures].length

  expect(objectTableCnt === 22).toBe(true)
  expect(layerTableCnt === 6).toBe(true)
  expect(materialsTableCnt === 2).toBe(true)
  expect(linetypeTableCnt === 6).toBe(true)
  expect(bitmapsTableCnt === 0).toBe(true)
  expect(groupTableCnt === 2).toBe(true)
  expect(idTableCnt === 1).toBe(true)
  expect(viewTableCnt === 4).toBe(true)
  expect(namedViewsTableCnt === 2).toBe(true)
  expect(pluginDataTableCnt === 1).toBe(true)
  expect(stringTableCnt === 1).toBe(true)
  expect(embeddedFileTableCnt === 1).toBe(true)
  expect(renderContentTableCnt === 3).toBe(true)

})

test('embeddedFilePaths', async () => {

  // read model 
  const model = '../models/file3dm_stuff.3dm'

  const buffer = fs.readFileSync(model)
  const arr = new Uint8Array(buffer)
  const doc = rhino.File3dm.fromByteArray(arr)

  expect(doc !== null).toBe(true)

  // D69: embeddedFilePaths -> embeddedFiles / .filename
  const ef = [...doc.embeddedFiles].map(f => f.filename)

  expect(Array.isArray(ef)).toBe(true)
  expect(typeof ef[0] === 'string').toBe(true)

})

const crypto = require('crypto')

// a unique path prefix in the module's own in-memory file system (WASM has no host file system)
const file3dmTmpPrefix = () => '/tmp/rh3dm_js_' + crypto.randomUUID().slice(0, 8)

const file3dmSha = s => crypto.createHash('sha256').update(s).digest('hex')
const FILE3DM_STUFF_RDK_SHA = '9f8be1e4d91bee9b901a1d0d0a72e454bf6045bd4c6075e295b4c95e0370e6ba'

test('file3dmWriteTakesOptionsOrNone', () => {
  // write and writeWithLog take File3dmWriteOptions or null, and the written file reads back
  const o = new rhino.File3dmWriteOptions()
  o.version = 7
  const f = new rhino.File3dm()
  const dir = file3dmTmpPrefix()
  const path = dir + '_opts.3dm'
  expect(f.write(path, o)).toBe(true)
  expect(f.write(dir + '_default.3dm', null)).toBe(true)
  const [ok, log] = f.writeWithLog(dir + '_log.3dm', null)
  expect(ok).toBe(true)
  expect(typeof log).toBe('string')
  expect(rhino.File3dm.read(path)).not.toBeNull()
})

test('file3dmCreatedAndLastEditedAreDates', () => {
  // created and lastEdited are Dates, and a written file reads back with plausible dates
  const f = new rhino.File3dm()
  expect(f.created).toBeInstanceOf(Date)
  expect(f.lastEdited).toBeInstanceOf(Date)
  const path = file3dmTmpPrefix() + '_dated.3dm'
  expect(f.write(path, 7)).toBe(true)
  const g = rhino.File3dm.read(path)
  expect(g.created >= new Date(2000, 0, 1)).toBe(true)
  expect(g.lastEdited >= g.created).toBe(true)
})

test('file3dmFromByteArrayReadsTheBytesOfAFile', () => {
  // a written .3dm reads back from the module's file system, and fromByteArray reads its bytes
  const f = new rhino.File3dm()
  f.objects.addPoint(new rhino.Point3d(1, 2, 3), null)
  const path = '/tmp/file3dm_from_bytes.3dm'      // the module's in-memory file system: WASM has no host one
  expect(f.write(path, 8)).toBe(true)
  expect(rhino.File3dm.read(path).objects.count).toBe(1)
  const data = f.toByteArray()
  const g = rhino.File3dm.fromByteArray(data)
  expect(g).toBeInstanceOf(rhino.File3dm)
  expect(g.objects.count).toBe(1)
  expect(rhino.File3dm.fromByteArray(new Uint8Array(data)).objects.count).toBe(1)
})

test('file3dmWriteMultipleObjects', () => {
  // File3dm.writeMultipleObjects writes a file holding each geometry given (the module's in-memory file system)
  const circle = new rhino.ArcCurve(new rhino.Circle(2.0))
  const plane = rhino.Brep.createTrimmedPlane(rhino.Plane.worldXY, circle)
  const out = '/tmp/rh3dm_multi_' + process.pid + '.3dm'
  expect(rhino.File3dm.writeMultipleObjects(out, [circle, plane])).toBe(true)
  expect([...rhino.File3dm.read(out).objects].length).toBe(2)
})

test('file3dmEarthAnchorPointIsACopy', () => {
  // File3dm.earthAnchorPoint reads a copy of the model's anchor; setting it copies one in, and it survives a file round trip
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const a = f.earthAnchorPoint
  expect([a.earthBasepointLatitude, a.earthBasepointLongitude]).toEqual([0, 0])
  a.earthBasepointLatitude = 12.5
  expect(f.earthAnchorPoint.earthBasepointLatitude).toBe(0)     // a copy
  f.earthAnchorPoint = a
  expect(rhino.File3dm.fromByteArray(f.toByteArray(null)).earthAnchorPoint.earthBasepointLatitude).toBe(12.5)
})

test('file3dmRenderContentByKind', () => {
  // renderMaterials, renderEnvironments and renderTextures together hold the file's render content, and each content is found in its own kind
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const ids = ['ab776f2b-7763-4c53-ac15-d02d3040ff26', '660433b2-4c4f-459e-962c-8f70942c5ccd', '16421078-5d0e-6851-2d34-062d266c3949']
  const kinds = [f.renderMaterials, f.renderEnvironments, f.renderTextures]
  expect(kinds.flatMap(k => [...k].map(c => c.id)).sort()).toEqual(ids.slice().sort())
  for (const id of ids) {
    const hit = kinds.map(k => k.find(id)).filter(x => x !== null && x !== undefined)
    expect(hit.length).toBe(1)
    expect(hit[0].id).toBe(id)
  }
})

test('file3dmToByteArrayWithWriteOptions', () => {
  // toByteArray(options) writes the file in the version the options ask for (D54)
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const options = new rhino.File3dmWriteOptions()
  options.version = 7
  const back = rhino.File3dm.fromByteArray(f.toByteArray(options))
  expect(back.archiveVersion).toBe(70)
  expect([...back.objects].length).toBe(22)
})

test('file3dmEncodeDecodeRoundTripsTheModel', () => {
  // File3dm.encode is base64 of the model's bytes; decode reads it, and any 3dm's base64
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const count = [...f.objects].length
  const s = f.encode()
  expect(typeof s).toBe('string')
  expect(s.length).toBeGreaterThan(1000)
  expect([...rhino.File3dm.decode(s).objects].length).toBe(count)
  const raw = fs.readFileSync(helpers.model('file3dm_stuff.3dm')).toString('base64')
  expect([...rhino.File3dm.decode(raw).objects].length).toBe(count)
})

test('file3dmEncodeWithOptionsWritesThatVersion', () => {
  // File3dm.encode(options) writes the archive with those write options
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  const opts = new rhino.File3dmWriteOptions()
  opts.version = 7
  const back = rhino.File3dm.decode(f.encode(opts))
  expect([back.archiveVersion, [...back.objects].length]).toEqual([70, 22])
  expect([...back.objects].length).toBe([...f.objects].length)
})

test('file3dmRdkXmlIsTheModelsRenderXml', () => {
  // File3dm.rdkXml is the model's RDK document XML, also on its plug-in data entry
  const f = helpers.readModel(rhino, 'file3dm_stuff.3dm')
  expect(file3dmSha(f.rdkXml())).toBe(FILE3DM_STUFF_RDK_SHA)
  expect(f.rdkXml().startsWith('<xml>')).toBe(true)
  expect([...f.plugInData].map(p => p.rdkXml()).filter(x => x).map(file3dmSha)).toEqual([FILE3DM_STUFF_RDK_SHA])
})

test('file3dmDestroyDeletesTheModel', () => {
  // File3dm.destroy() deletes the model; a later use throws
  const f = new rhino.File3dm()
  f.destroy()
  expect(() => f.objects).toThrow(/deleted/)
})

test('file3dmFromByteArrayTakesAnArrayBuffer', () => {
  // fromByteArray takes a Uint8Array or an ArrayBuffer (as 8.x did); anything else throws TypeError
  const bytes = new rhino.File3dm().toByteArray()
  expect(rhino.File3dm.fromByteArray(bytes)).toBeInstanceOf(rhino.File3dm)
  const ab = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength)
  expect(rhino.File3dm.fromByteArray(ab)).toBeInstanceOf(rhino.File3dm)
  expect(() => rhino.File3dm.fromByteArray('not bytes')).toThrow(/expected a Uint8Array/)
})
