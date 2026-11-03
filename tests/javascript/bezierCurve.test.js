const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('bezierSplit', async () => {

    const pointArray = [{ X: 0, Y: 0, Z: 0 }, { X: 1, Y: 1, Z: 0 }, { X: 2, Y: 0, Z: 0 }, { X: 3, Y: -1, Z: 0 }, { X: 4, Y: 0, Z: 0 }]

    const ncurve = rhino.NurbsCurve.create(true, 3, pointArray)

    const bezier = ncurve.convertSpanToBezier(0)
    const result = bezier.split(0.5)
    
    expect(result[0]).toBe(true)
    expect(result.length).toBe(3)
    expect(result[1].constructor.name).toBe('BezierCurve')
    expect(result[2].constructor.name).toBe('BezierCurve')

})