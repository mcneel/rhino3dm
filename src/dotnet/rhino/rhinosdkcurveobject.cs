#pragma warning disable 1591
using System;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.DocObjects
{
  /// <summary>
  /// A Rhino Object that represents curve geometry and attributes
  /// </summary>
  public class CurveObject : RhinoObject
  {
    internal CurveObject(uint serialNumber)
      : base(serialNumber)
    { }

    internal CurveObject() { }

    /// <summary>
    /// Returns the underlying curve geometry.
    /// </summary>
    /// <since>5.0</since>
    public Curve CurveGeometry
    {
      get
      {
        Curve rc = Geometry as Curve;
        return rc;
      }
    }

    /// <summary>
    /// Returns a copy of the underlying curve geometry.
    /// </summary>
    /// <since>5.0</since>
    public Curve DuplicateCurveGeometry()
    {
      Curve rc = DuplicateGeometry() as Curve;
      return rc;
    }

    /// <summary>
    /// Converts the linetype pattern of the curve into curve segments and points
    /// based on the active Rhino viewport.
    /// </summary>
    /// <returns>An array of curve and point objects if successful.</returns>
    /// <since>8.4</since>
    public GeometryBase[] GetLinetypeSegments()
    {
      return GetLinetypeSegments(null);
    }

    /// <summary>
    /// Converts the linetype pattern of the curve into curve segments and points
    /// based on the specified Rhino viewport.
    /// </summary>
    /// <param name="viewport">The Rhino viewport used to generate the curve segments and points.</param>
    /// <returns>An array of curve and point objects if successful.</returns>
    /// <since>8.4</since>
    public GeometryBase[] GetLinetypeSegments(RhinoViewport viewport)
    {
      using (SimpleArrayGeometryPointer geometry_array = new SimpleArrayGeometryPointer())
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_viewport = (null != viewport) ? viewport.ConstPointer() : IntPtr.Zero;
        IntPtr ptr_geometry_array = geometry_array.NonConstPointer();
        int rc = UnsafeNativeMethods.CRhinoCurveObject_GetLinetypeSegments(ptr_const_this, ptr_viewport, ptr_geometry_array);
        if (rc > 0)
          return geometry_array.ToNonConstArray();
        return new GeometryBase[0];
      }
    }

    internal override RhinoObject.CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoCurveObject_InternalCommitChanges;
    }
  }
}

namespace Rhino.DocObjects.Custom
{
  public abstract class CustomCurveObject : CurveObject, IDisposable
  {
    protected CustomCurveObject() : base()
    {
      Guid type_id = GetType().GUID;
      if (SubclassCreateNativePointer)
        m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomCurveObject_New(type_id);
    }
    protected CustomCurveObject(Curve curve) : base()
    {
      Guid type_id = GetType().GUID;
      IntPtr pConstCurve = curve.ConstPointer();
      m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomObject_New2(type_id, pConstCurve);
    }

    ~CustomCurveObject() { Dispose(false); }
    /// <since>5.0</since>
    public new void Dispose()
    {
      base.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
      if ( IntPtr.Zero != m_pRhinoObject )
      {
        // This delete is safe in that it makes sure the object is NOT
        // under control of the Rhino Document
        UnsafeNativeMethods.CRhinoObject_Delete(m_pRhinoObject);
      }
      m_pRhinoObject = IntPtr.Zero;
    }

    /// <summary>
    /// Only for developers who are defining custom subclasses of CurveObject.
    /// Directly sets the internal curve geometry for this object.  Note that
    /// this function does not work with Rhino's "undo".
    /// </summary>
    /// <param name="curve"></param>
    /// <returns>
    /// The old curve geometry that was set for this object
    /// </returns>
    /// <remarks>
    /// Note that this function does not work with Rhino's "undo".  The typical
    /// approach for adjusting the curve geometry is to modify the object that you
    /// get when you call the CurveGeometry property and then call CommitChanges.
    /// </remarks>
    protected Curve SetCurve(Curve curve)
    {
      var parent = curve.ParentRhinoObject();
      if (parent != null && parent.RuntimeSerialNumber == this.RuntimeSerialNumber)
        return curve;

      IntPtr pThis = this.NonConstPointer_I_KnowWhatImDoing();

      IntPtr pCurve = curve.NonConstPointer();
      IntPtr pOldCurve = UnsafeNativeMethods.CRhinoCurveObject_SetCurve(pThis, pCurve);
      curve.ChangeToConstObject(this);
      if (pOldCurve != pCurve && pOldCurve != IntPtr.Zero)
        return new Curve(pOldCurve, null);
      return curve;
    }
  }
}
#endif
