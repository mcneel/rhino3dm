const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino
beforeAll(async () => {
  rhino = await rhino3dm()
})

test('sunLightIsALight', () => {
  // Sun.light is a Light
  expect(new rhino.Sun().light).toBeInstanceOf(rhino.Light)
})

test('sunColorFromAltitude', () => {
  // Sun.colorFromAltitude gives the sun's color at an altitude, each channel rounded from 255 * c (D45)
  const expected = [[-10.0, [0, 0, 64]], [0.0, [255, 160, 24]], [5.0, [255, 197, 69]], [12.5, [255, 233, 162]],
    [30.0, [251, 252, 255]], [60.0, [251, 252, 255]], [89.0, [251, 252, 255]]]
  for (const [alt, want] of expected) {
    const c = rhino.Sun.colorFromAltitude(alt)
    const got = [c.r, c.g, c.b]
    // within 1 of the 8.x values, which truncated 255.5 * c
    want.forEach((x, i) => expect(Math.abs(x - got[i])).toBeLessThanOrEqual(1))
  }
})

test('sunSetDateTimeAndGetDateTime', () => {
  // setDateTime takes a Date as local time (the process zone) and getDateTime gives it back
  const s = new rhino.Sun()
  const K = rhino.DateTimeKind
  const when = new Date(2024, 5, 21, 14, 30, 15)
  s.setDateTime(when, K.Local)
  const got = s.getDateTime(K.Local)
  expect(got).toBeInstanceOf(Date)
  expect(got.getTime()).toBe(when.getTime())
  expect(s.getDateTime(K.Utc)).toBeInstanceOf(Date)
})

test('sunGetDateTimeUnspecifiedKindRaises', () => {
  // getDateTime with DateTimeKind.Unspecified throws, as RhinoCommon throws ArgumentException
  const s = new rhino.Sun()
  helpers.raises(rhino, () => s.getDateTime(rhino.DateTimeKind.Unspecified), /invalid_argument,DateTimeKind must be specified/)
})

test('sunSetDateTimeAwareDatetimeIsItsInstant', () => {
  // setDateTime stores the Date's instant
  const s = new rhino.Sun()
  const aware = new Date(Date.UTC(2024, 0, 2, 3, 4, 5))
  s.setDateTime(aware, rhino.DateTimeKind.Local)
  expect(s.getDateTime(rhino.DateTimeKind.Local).getTime()).toBe(aware.getTime())
})

test('sunSetPositionSetsLocationAndLocalTime', () => {
  // setPosition(when, latitude, longitude) sets the location and the local date and time
  const s = new rhino.Sun()
  const when = new Date(2024, 5, 21, 14, 30, 15)
  s.setPosition(when, 40.0, -3.5)
  expect([s.latitude, s.longitude, s.getDateTime(rhino.DateTimeKind.Local).getTime()]).toEqual([40.0, -3.5, when.getTime()])
})

test('sunSetDateTimeRejectsAString', () => {
  // setDateTime with a string instead of a Date throws
  helpers.raises(rhino, () => new rhino.Sun().setDateTime('2024-06-21', rhino.DateTimeKind.Local), /invalid_argument,a Date is required/)
})

test('sunAltitudeFromValues', () => {
  // Sun.altitudeFromValues gives an altitude between -90 and 90 degrees
  const when = new Date(2024, 5, 21, 14, 30, 15)
  const a = rhino.Sun.altitudeFromValues(40.0, -3.5, 1.0, 60, when, 14.5, false)
  expect(a).toBeGreaterThanOrEqual(-90.0)
  expect(a).toBeLessThanOrEqual(90.0)
})

test('sunDateTimeSurvivesAFileRoundTrip', () => {
  // the render settings sun's local date and time read back from the file's bytes
  const wf = new rhino.File3dm()
  wf.settings.renderSettings.sun.setDateTime(new Date(2023, 10, 5, 9, 45), rhino.DateTimeKind.Local)
  const f = rhino.File3dm.fromByteArray(wf.toByteArray())
  const d = f.settings.renderSettings.sun.getDateTime(rhino.DateTimeKind.Local)
  expect([d.getFullYear(), d.getMonth() + 1, d.getDate()]).toEqual([2023, 11, 5])
  expect(Math.abs(d.getHours() + d.getMinutes() / 60 + d.getSeconds() / 3600 - 9.75)).toBeLessThan(1 / 3600)
})

test('sunYearRangeAndAllowedFlags', () => {
  // Sun has the year range 1971-2199, a file's sun is valid, and enableAllowed can be toggled
  const sun = new rhino.File3dm().settings.renderSettings.sun
  expect([rhino.Sun.minYear, rhino.Sun.maxYear]).toEqual([1971, 2199])
  expect(sun.isValid).toBe(true)
  const ea = sun.enableAllowed
  sun.enableAllowed = !ea
  expect(sun.enableAllowed).toBe(!ea)
})
