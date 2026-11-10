const rhino3dm = require('rhino3dm')
const helpers = require('./helpers')

let rhino

beforeAll(async () => {
    rhino = await rhino3dm()
})

test('GetScreenPort', async () => {

    const viewport = new rhino.ViewportInfo()
    let screenPort = viewport.screenPort

    expect(Array.isArray(screenPort)).toBe(true)
    expect(screenPort.length == 4).toBe(true)
    expect(Number.isInteger(screenPort[0])).toBe(true)
    expect(Number.isInteger(screenPort[1])).toBe(true)
    expect(Number.isInteger(screenPort[2])).toBe(true)
    expect(Number.isInteger(screenPort[3])).toBe(true)

    // D58: RhinoCommon reads a new viewport's default port (8.x read [0, 0, 0, 0])
    expect(screenPort[0] === 0).toBe(true)
    expect(screenPort[1] === 0).toBe(true)
    expect(screenPort[2] === 1000).toBe(true)
    expect(screenPort[3] === 1000).toBe(true)

    viewport.screenPort = [0, 0, 100, 100]

    screenPort = viewport.screenPort

    expect(screenPort[0] === 0)
    expect(screenPort[1] === 0)
    expect(screenPort[2] === 100)
    expect(screenPort[3] === 100)

})

test('viewportInfoSetCameraLocationMovesTheCamera', () => {
  // setCameraLocation returns true and cameraLocation reads the new location back
  const v = new rhino.ViewportInfo()
  expect(v.setCameraLocation(new rhino.Point3d(7, 8, 9))).toBe(true)
  expect(v.cameraLocation).toEqual({ X: 7, Y: 8, Z: 9 })
})

test('viewportGetScreenPortReturnsXYWidthHeight', () => {
  // getScreenPort returns the screen port set by setScreenPort as [x, y, width, height]
  const vp = new rhino.ViewportInfo()
  vp.setScreenPort(0, 640, 480, 0, 0, 0)
  expect(Array.from(vp.getScreenPort()).slice(0, 4)).toEqual([0, 0, 640, 480])
})

test('viewportInfoScreenPortGetAndSet', () => {
  // screenPort is an [x, y, width, height] rectangle; getScreenPortLocation gives [left, top, right, bottom]
  const v = new rhino.ViewportInfo()
  v.screenPort = [10, 20, 640, 480]
  expect(v.screenPort).toEqual([10, 20, 640, 480])
  expect(v.getScreenPort()).toEqual([10, 20, 640, 480])
  expect(v.getScreenPortLocation()).toEqual([10, 20, 650, 500])
})

test('viewportInfoSetScreenPort', () => {
  // setScreenPort takes a rectangle and a depth range, or left, right, bottom, top, near, far; top == bottom is refused
  const v = new rhino.ViewportInfo()
  expect(v.setScreenPort([0, 0, 100, 50], 1, 2)).toBe(true)
  expect(v.screenPort).toEqual([0, 0, 100, 50])
  expect(v.setScreenPort(0, 640, 0, 480, 0, 0)).toBe(true)     // y-up: left, right, bottom, top
  expect(v.screenPort).toEqual([0, 0, 640, 480])
  expect(v.setScreenPort(0, 640, 100, 100, 0, 0)).toBe(false)
  expect(v.screenPort).toEqual([0, 0, 640, 480])                // the port is kept
})

test('viewportInfoScreenPortRejectsAShortTuple', () => {
  // setting screenPort to an array that is not four numbers throws
  const v = new rhino.ViewportInfo()
  helpers.raises(rhino, () => { v.screenPort = [1, 2, 3] }, /rectangle/)
})

test('viewportInfoUserStrings', () => {
  // A ViewportInfo keeps user strings: set, get, count and list them (D62)
  const o = new rhino.ViewportInfo()
  expect(o.userStringCount).toBe(0)
  expect(o.setUserString('k', 'v')).toBe(true)
  expect(o.getUserString('k')).toBe('v')
  expect(Array.from(o.getUserStrings(), p => Array.from(p))).toEqual([['k', 'v']])
})

test('viewportInfoPerspectiveProjectionSetter', () => {
  // Setting isPerspectiveProjection to false makes the viewport a parallel projection
  const v = new rhino.ViewportInfo()
  v.isPerspectiveProjection = false
  expect(v.isParallelProjection).toBe(true)
})

test('viewportInfoIsValidCameraFrame', () => {
  // A new ViewportInfo has a valid camera frame
  expect(new rhino.ViewportInfo().isValidCameraFrame).toBe(true)
})

test('viewportInfoDefaultTopAndPerspective', () => {
  // defaultTop and defaultPerspective give opennurbs' default camera directions
  const p3 = p => [p.X, p.Y, p.Z]
  expect(p3(rhino.ViewportInfo.defaultTop.cameraDirection)).toEqual([-0, -0, -1])
  expect(p3(rhino.ViewportInfo.defaultPerspective.cameraDirection)).toEqual([-0.4330127018922193, 0.75, -0.5])
})
