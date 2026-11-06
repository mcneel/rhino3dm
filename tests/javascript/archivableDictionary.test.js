const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

const fs = require('fs')

const archivableDictionaryGeometry = rhino => [...helpers.readModel(rhino, 'file3dm_stuff.3dm').objects][0].geometry

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('archivableDictionaryRoundTripsValuesAndGeometry', () => {
  // encodeDict / decodeDict round-trip bools, numbers, strings and geometry
  const g = archivableDictionaryGeometry(rhino)
  const back = rhino.ArchivableDictionary.decodeDict(
    rhino.ArchivableDictionary.encodeDict({ b: true, f: 2.5, s: 'héllo', g: g, gl: [g, g] }))
  expect([back.b, back.f, back.s]).toEqual([true, 2.5, 'héllo'])
  expect(back.g.constructor.name).toBe(g.constructor.name)
  expect(back.g.encode()).toEqual(g.encode())
  expect(back.gl.map(x => x.constructor.name)).toEqual([g.constructor.name, g.constructor.name])
})

test('archivableDictionaryDecodesAn8xPayload', () => {
  // decodeDict reads what 8.x's encodeDict({i: 3, p: Point3d(4, 5, 6), gl: [g]}) wrote
  const g = archivableDictionaryGeometry(rhino)
  const payload = JSON.parse(fs.readFileSync(helpers.model('old_oracle/json_forms.json'), 'utf8')).archivable_dictionary_encode
  const back = rhino.ArchivableDictionary.decodeDict(payload)
  expect([back.i, back.p.X, back.p.Y, back.p.Z, back.gl.length]).toEqual([3, 4, 5, 6, 1])
  expect(back.gl[0].constructor.name).toBe(g.constructor.name)
})

test('archivableDictionaryEncodeRaisesOnAnUnsupportedValue', () => {
  // encodeDict throws, naming a value it cannot serialize
  helpers.raises(rhino, () => rhino.ArchivableDictionary.encodeDict({ x: {} }), /Unable to serialize 'x'/)
})
