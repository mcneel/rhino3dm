#if RHINO_SDK

using Rhino.Display;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;
using System;

namespace Rhino.DocObjects
{
  /// <summary>
  /// Rhino Object that represents text geometry and attributes
  /// </summary>
  public class TextObject : AnnotationObjectBase
  {
    internal TextObject(uint serialNumber) : base(serialNumber)
    { }

    /// <summary> Get the text geometry for this object. </summary>
    /// <since>5.0</since>
    public Geometry.TextEntity TextGeometry => Geometry as Geometry.TextEntity;

    /// <summary>
    /// Gets the world 3D corner points of the whole text object.
    /// </summary>
    /// <param name="viewport">The viewport in which to make the calculation.</param>
    /// <returns>The four corner points if successful, an empty array on failure.</returns>
    /// <since>7.21</since>
    /// <exception cref="ArgumentNullException"></exception>
    public Point3d[] GetTextCorners(RhinoViewport viewport)
    {
      if (null == viewport)
        throw new ArgumentNullException(nameof(viewport));

      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_const_viewport = viewport.ConstPointer();

      using (SimpleArrayPoint3d corners = new SimpleArrayPoint3d())
      {
        IntPtr ptr_corners = corners.NonConstPointer();
        bool rc = UnsafeNativeMethods.RHC_GetCornerPointsFromText(ptr_const_this, ptr_const_viewport, ptr_corners);
        if (rc && corners.Count > 0)
          return corners.ToArray();
      }
      return new Point3d[0];
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoRichTextObject_InternalCommitChanges;
    }
  }
}

#endif
