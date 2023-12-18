using System;
using Rhino;
using Rhino.Geometry;

string pathThis = typeof(Program).Assembly.Location;
int rhino3dmIndex = pathThis.IndexOf("rhino3dm");
string pathLibRhino3dm = pathThis.Substring(0, rhino3dmIndex);
pathLibRhino3dm += "rhino3dm\\src\\build\\windows\\win64\\Debug\\librhino3dm_native.dll";

Console.WriteLine("Path to native lib: {0}", pathLibRhino3dm);

// Force load librhino3dm
nint handleLibRhino3dm = System.Runtime.InteropServices.NativeLibrary.Load(pathLibRhino3dm);

var doc = new Rhino.FileIO.File3dm();
var index = doc.AllGroups.AddGroup();
Console.WriteLine("nº of groups {0}",doc.AllGroups.Count);

for( int i = 1; i < 10; i ++ ) {
   var circle = new Rhino.Geometry.Circle(i);
   var id = doc.Objects.AddCircle(circle);
   var ro = doc.Objects.FindId(id);
   ro.Attributes.AddToGroup(index);
   Console.WriteLine("Group Count: {0}", ro.Attributes.GroupCount);
   Console.WriteLine("Group List: {0}",ro.Attributes.GetGroupList());
}

var tmpPath = System.IO.Path.GetTempPath();
tmpPath = System.IO.Path.Combine(tmpPath, "testGroup.3dm");

doc.Write(tmpPath, 8);
Console.WriteLine(tmpPath);

