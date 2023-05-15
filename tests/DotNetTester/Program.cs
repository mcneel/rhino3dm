using Rhino.Geometry;

namespace DotNetTester
{
  internal class Program
  {
    static void Main(string[] args)
    {
      string pathThis = typeof(Program).Assembly.Location;
      int rhino3dmIndex = pathThis.IndexOf("rhino3dm");
      string pathLibRhino3dm = pathThis.Substring(0, rhino3dmIndex);
      pathLibRhino3dm += "rhino3dm\\src\\build\\librhino3dm_native_64\\Debug\\librhino3dm_native.dll";

      // Force load librhino3dm
      nint handleLibRhino3dm = System.Runtime.InteropServices.NativeLibrary.Load(pathLibRhino3dm);

      var points = new Point3d[] { new Point3d(0, 0, 0), new Point3d(5, 0, 0), new Point3d(5, 5, 0) };
      Rhino.Geometry.Curve curve = Rhino.Geometry.Curve.CreateControlPointCurve(points);
      bool isLinear = curve.IsLinear();
      Console.WriteLine($"curve.IsLinear = {isLinear}");
    }
  }
}