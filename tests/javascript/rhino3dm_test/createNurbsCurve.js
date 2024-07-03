const rhino3dm = require('rhino3dm')

async function createNurbsCurve() {

    const rhino = await rhino3dm()

    const pointArray = [ [ 0, 0, 0 ], [ 1, 1, 0 ], [ 2, 0, 0 ], [ 3, -1, 0 ], [ 4, 0, 0 ] ]

    const pointList = new rhino.Point3dList(5)
    pointList.add(0, 0, 0)
    pointList.add(1, 1, 0)
    pointList.add(2, 0, 0)
    pointList.add(3, -1, 0)
    pointList.add(4, 0, 0)

    const crvFromList = rhino.NurbsCurve.create(false, 3, pointList)
    const crvFromArray = rhino.NurbsCurve.create(false, 3, pointArray)
    
    return crvFromArray.points().count === crvFromList.points().count

}

module.exports = createNurbsCurve