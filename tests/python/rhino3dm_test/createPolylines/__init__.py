import rhino3dm

def createPolylines():
    file3dm = rhino3dm.File3dm()
    points = [rhino3dm.Point3d(0,0,0), rhino3dm.Point3d(1,1,0), rhino3dm.Point3d(2,0,0), rhino3dm.Point3d(3,-1,0), rhino3dm.Point3d(4,0,0) ]
    pointList = rhino3dm.Point3dList(5)
    for i in range(5):
        pointList.Add(points[i].X, points[i].Y, points[i].Z)
    file3dm.Objects.AddPolyline(points, None)
    file3dm.Objects.AddPolyline(pointList, None)

    polylineArrayConstructor = rhino3dm.Polyline(points)
    polylineStatic = rhino3dm.Polyline.CreateFromPoints(points)
    polylineStaticList = rhino3dm.Polyline.CreateFromPoints(pointList)

    file3dm.Objects.Add(polylineArrayConstructor.ToPolylineCurve(), None)
    file3dm.Objects.Add(polylineStatic.ToPolylineCurve(), None)
    file3dm.Objects.Add(polylineStaticList.ToPolylineCurve(), None)

    objqty = len(file3dm.Objects)
    isCurve1 = file3dm.Objects[0].Geometry.ObjectType == rhino3dm.ObjectType.Curve
    isCurve2 = file3dm.Objects[1].Geometry.ObjectType == rhino3dm.ObjectType.Curve
    isCurve3 = file3dm.Objects[2].Geometry.ObjectType == rhino3dm.ObjectType.Curve
    isCurve4 = file3dm.Objects[3].Geometry.ObjectType == rhino3dm.ObjectType.Curve
    isCurve5 = file3dm.Objects[4].Geometry.ObjectType == rhino3dm.ObjectType.Curve
    len1 = file3dm.Objects[0].Geometry.PointCount
    len2 = file3dm.Objects[1].Geometry.PointCount
    len3 = file3dm.Objects[2].Geometry.PointCount
    len4 = file3dm.Objects[3].Geometry.PointCount
    len5 = file3dm.Objects[4].Geometry.PointCount

    return objqty == 5 and isCurve1 and isCurve2 and isCurve3 and isCurve4 and isCurve5 and len1 == 5 and len2 == 5 and len3 == 5 and len4 == 5 and len2 == 5