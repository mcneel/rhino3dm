import rhino3dm, os, os.path

def writeFile():
    file3dm = rhino3dm.File3dm()
    points = [rhino3dm.Point3d(0,0,0), rhino3dm.Point3d(1,1,0), rhino3dm.Point3d(2,0,0), rhino3dm.Point3d(3,-1,0), rhino3dm.Point3d(4,0,0) ]

    for i in range(5):
          spline = rhino3dm.Curve.CreateControlPointCurve(points, i)
          file3dm.Objects.AddCurve(spline, None)

    file3dm.Write("spline.3dm", 8)
    return os.path.exists("spline.3dm")