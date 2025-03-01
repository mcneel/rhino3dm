const rhino3dm = require('rhino3dm')

let rhino
beforeEach( async() => {
    rhino = await rhino3dm()
  })

test('bezierSplit', async () => {

    const pointArray = [ [ 0, 0, 0 ], [ 1, 1, 0 ], [ 2, 0, 0 ], [ 3, -1, 0 ], [ 4, 0, 0 ] ]

    const ncurve = rhino.NurbsCurve.create(true, 3, pointArray)

    const bezier = ncurve.convertSpanToBezier(0)
    const result = bezier.split(0.5)
    
    expect(result[0]).toBe(true)
    expect(result.length).toBe(3)
    expect(result[1].constructor.name).toBe('BezierCurve')
    expect(result[2].constructor.name).toBe('BezierCurve')

})