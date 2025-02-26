const rhino3dm = require('rhino3dm')

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

    expect(screenPort[0] === 0).toBe(true)
    expect(screenPort[1] === 0).toBe(true)
    expect(screenPort[2] === 0).toBe(true)
    expect(screenPort[3] === 0).toBe(true)

    viewport.screenPort = [0, 0, 100, 100]

    screenPort = viewport.screenPort

    expect(screenPort[0] === 0)
    expect(screenPort[1] === 0)
    expect(screenPort[2] === 100)
    expect(screenPort[3] === 100)

})
