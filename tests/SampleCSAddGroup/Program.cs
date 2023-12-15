using System;

var doc = new Rhino.FileIO.File3dm();
var index = doc.AllGroups.AddGroup();
Console.WriteLine("nº of groups {0}",doc.AllGroups.Count);

for( int i = 1; i < 10; i ++ ) {
   var circle = new Rhino.Geometry.Circle(i);
   var id = doc.Objects.AddCircle(circle);
   var ro = doc.Objects.FindId(id);
   ro.Attributes.AddToGroup(index);
   Console.WriteLine(ro.Attributes.GroupCount);
}

var tmpPath = System.IO.Path.GetTempPath();
tmpPath = System.IO.Path.Combine(tmpPath, "testGroup.3dm");

doc.Write(tmpPath, 8);
Console.WriteLine(tmpPath);

