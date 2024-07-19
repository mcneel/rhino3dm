namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class Polyline_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Polyline_AddRange()
    {

       var polyline1 = new Polyline
       {
           { 21, 21, 21 }
       };
       
       var polyline2 = new Polyline
       {
           { 21, 21, 21 }
       };

       var pointList = new List<Point3d>();
       var point3dList = new Rhino.Collections.Point3dList();

       for (var i = 0; i < 15; i++) 
       {
              pointList.Add(new Point3d(i, i, i));
              point3dList.Add(i, i, i);
       }

        polyline1.AddRange(pointList);
        polyline2.AddRange(point3dList);

        Assert.That( polyline1.Count == 16 && polyline2.Count == 16 && polyline1[10] == polyline2[10] , Is.True);
    }
}