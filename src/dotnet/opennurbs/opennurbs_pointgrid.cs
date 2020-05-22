using System;
using System.Runtime.Serialization;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a rectangular grid of 3D points.
  /// </summary>
  [Serializable]
  public class Point3dGrid : GeometryBase
  {
    /// <summary>
    /// Initializes a rectangular grid of points, with no points in it.
    /// </summary>
    /// <since>5.0</since>
    public Point3dGrid()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PointGrid_New(0,0);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Initializes a rectangular grid of points with a given number of columns and rows.
    /// </summary>
    /// <param name="rows">An amount of rows.</param>
    /// <param name="columns">An amount of columns.</param>
    /// <since>5.0</since>
    public Point3dGrid(int rows, int columns)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PointGrid_New(rows, columns);
      ConstructNonConstObject(ptr);
    }


    internal Point3dGrid(IntPtr ptr, object parent) 
      : base(ptr, parent, -1)
    { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Point3dGrid(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

  }
}
