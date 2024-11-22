#pragma warning disable 1591
using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.DocObjects
{
  public class PointObject : RhinoObject
  {
    internal PointObject(uint serialNumber)
      : base(serialNumber)
    { }

    internal PointObject(bool custom) { }

    /// <since>5.0</since>
    public Point PointGeometry
    {
      get
      {
        return Geometry as Point;
      }
    }

    /// <since>5.0</since>
    public Point DuplicatePointGeometry()
    {
      return DuplicateGeometry() as Point;
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoPointObject_InternalCommitChanges;
    }
  }

  public class PointCloudObject : RhinoObject
  {
    internal PointCloudObject(uint serialNumber)
      : base(serialNumber)
    { }

    /// <since>5.0</since>
    public PointCloud PointCloudGeometry
    {
      get
      {
        return Geometry as PointCloud;
      }
    }

    /// <since>5.0</since>
    public PointCloud DuplicatePointCloudGeometry()
    {
      return DuplicateGeometry() as PointCloud;
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoPointCloudObject_InternalCommitChanges;
    }
  }


  // 20 Jan 2010 - S. Baer
  // I think CRhinoGripObjectEx can probably be merged with GripObject
  public class GripObject : RhinoObject
  {
    internal GripObject() { }

    internal GripObject(uint serialNumber)
      : base(serialNumber)
    {
    }


    /// <since>5.0</since>
    public Point3d CurrentLocation
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var rc = new Point3d();
        UnsafeNativeMethods.CRhinoGripObject_GripLocation(const_ptr_this, ref rc, true);
        return rc;
      }
      set
      {
        Move(value);
      }
    }

    /// <since>5.0</since>
    public Point3d OriginalLocation
    {
      get
      {
        IntPtr ptr_this = ConstPointer();
        var rc = new Point3d();
        UnsafeNativeMethods.CRhinoGripObject_GripLocation(ptr_this, ref rc, false);
        return rc;
      }
    }

    /// <summary>
    /// true if the grip has moved from OriginalLocation.
    /// </summary>
    /// <since>5.0</since>
    public bool Moved
    {
      get 
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoGripObject_Moved(ptr);
      }
    }

    /// <summary>
    /// Moves the grip to a new location.
    /// </summary>
    /// <param name="xform">
    /// Transformation applied to the OriginalLocation point.
    /// </param>
    /// <since>5.0</since>
    public void Move(Transform xform)
    {
      IntPtr ptr = NonConstPointer_I_KnowWhatImDoing();
      UnsafeNativeMethods.CRhinoGripObject_MoveGrip1(ptr, ref xform);
    }
    /// <summary>
    /// Moves the grip to a new location.
    /// </summary>
    /// <param name="delta">
    /// Translation applied to the OriginalLocation point.
    /// </param>
    /// <since>5.0</since>
    public void Move(Vector3d delta)
    {
      IntPtr ptr = NonConstPointer_I_KnowWhatImDoing();
      var point = new Point3d(delta);
      UnsafeNativeMethods.CRhinoGripObject_MoveGrip2(ptr, point, true);
    }
    /// <summary>
    /// Moves the grip to a new location.
    /// </summary>
    /// <param name="newLocation">
    /// New location for grip.
    /// </param>
    /// <since>5.0</since>
    public void Move(Point3d newLocation)
    {
      IntPtr ptr = NonConstPointer_I_KnowWhatImDoing();
      UnsafeNativeMethods.CRhinoGripObject_MoveGrip2(ptr, newLocation, false);
    }

    /// <summary>
    /// Undoes any grip moves made by calling Move.
    /// </summary>
    /// <since>5.0</since>
    public void UndoMove()
    {
      IntPtr ptr = NonConstPointer_I_KnowWhatImDoing();
      UnsafeNativeMethods.CRhinoGripObject_UndoMode(ptr);      
    }

    /// <summary>
    /// The weight of a NURBS control point grip or RhinoMath.UnsetValue
    /// if the grip is not a NURBS control point grip.
    /// </summary>
    /// <since>5.0</since>
    public virtual double Weight
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoGripObject_GetSetWeight(ptr, false, 0);
      }
      set 
      {
        IntPtr ptr = NonConstPointer_I_KnowWhatImDoing();
        UnsafeNativeMethods.CRhinoGripObject_GetSetWeight(ptr, true, value);
      }
    }

    /// <since>5.0</since>
    public Guid OwnerId
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoGripObject_GetOwnerId(ptr);
      }
    }

    /// <summary>
    /// Used to get a grip's logical neighbors, like NURBS curve, surface,
    /// and cage control point grips.
    /// </summary>
    /// <param name="directionR">
    /// -1 to go back one grip, +1 to move forward one grip.  For curves, surfaces
    /// and cages, this is the first parameter direction.
    /// </param>
    /// <param name="directionS">
    /// -1 to go back one grip, +1 to move forward one grip.  For surfaces and
    /// cages this is the second parameter direction.
    /// </param>
    /// <param name="directionT">
    /// For cages this is the third parameter direction
    /// </param>
    /// <param name="wrap"></param>
    /// <returns>logical neighbor or null if the is no logical neighbor</returns>
    /// <since>5.0</since>
    public GripObject NeighborGrip(int directionR, int directionS, int directionT, bool wrap)
    {
      IntPtr const_ptr_this = ConstPointer();
      uint sn = UnsafeNativeMethods.CRhinoGripObject_NeighborGrip(const_ptr_this, directionR, directionS, directionT, wrap);
      if (sn != 0)
        return new GripObject(sn);
      return null;
    }

    /// <summary>
    /// Sometimes grips have directions.  These directions
    /// can have any length and do not have to be orthogonal.
    /// </summary>
    /// <param name="u"> u direction</param>
    /// <param name="v"> v direction</param>
    /// <param name="normal"> normal direction</param>
    /// <returns>True if the grip has directions.</returns>
    /// <since>6.0</since>
    public bool GetGripDirections(out Vector3d u, out Vector3d v, out Vector3d normal)
    {
      u = v = normal = Vector3d.Unset;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoGripObject_GetGripDirections(const_ptr_this, ref u, ref v, ref normal);
    }

    /// <summary>
    /// Retrieves the NURBS surface 2d parameter space values of this GripObject from the surface it's associated with.
    /// </summary>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <returns>True on success. Output is unreliable if return is false.</returns>
    /// <since>6.0</since>
    public bool GetSurfaceParameters(out double u, out double v)
    {
      u = v = Double.NaN;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoGripObject_GetSurfaceParameters(const_ptr_this, ref u, ref v);
    }

    /// <summary>
    /// Retrieves the NURBS curve control point indices of this GripObject from the curve it is associated with.
    /// </summary>
    /// <param name="cvIndices">The NURBS curve control point indices.</param>
    /// <returns>
    /// The number of NURBS curve control points managed by this grip.
    /// If the grip is not a curve control point, zero is returned.
    /// </returns>
    /// <since>8.10</since>
    public int GetCurveCVIndices(out int[] cvIndices)
    {
      cvIndices = Array.Empty<int>();
      using (var index_array = new SimpleArrayInt())
      {
        IntPtr ptr_const_this = ConstPointer();
        var ptr_index_array = index_array.NonConstPointer();
        var count = UnsafeNativeMethods.CRhinoGripObject_GetCurveCVIndices(ptr_const_this, ptr_index_array);
        if (count > 0)
          cvIndices = index_array.ToArray();
        return cvIndices.Length;
      }
    }

    /// <summary>
    /// Retrieves the NURBS surface control point indices of this GripObject from the surface it is associated with.
    /// </summary>
    /// <param name="cvIndices">The NURBS surface control point indices as tuples.</param>
    /// <returns>
    /// The number of NURBS surface control points managed by this grip.
    /// If the grip is not a surface control point, zero is returned.
    /// </returns>
    /// <since>8.10</since>
    public int GetSurfaceCVIndices(out Tuple<int, int>[] cvIndices)
    {
      cvIndices = Array.Empty<Tuple<int, int>>();
      using (var dex_array = new SimpleArray2dex())
      {
        IntPtr ptr_const_this = ConstPointer();
        var ptr_dex = dex_array.NonConstPointer();
        var count = UnsafeNativeMethods.CRhinoGripObject_GetSurfaceCVIndices(ptr_const_this, ptr_dex);
        if (count > 0)
        {
          var results = new List<Tuple<int, int>>();
          foreach (var dex in dex_array.ToArray())
          {
            var uv = new Tuple<int, int>(dex.I, dex.J);
            results.Add(uv);
          }
          cvIndices = results.ToArray();
        }
        return cvIndices.Length;
      }
    }

    /// <summary>
    /// Retrieves the 2d parameter space values of this GripObject from the cage it's associated with.
    /// </summary>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <param name="w"></param>
    /// <returns>True on success. Output is unreliable if return is false.</returns>
    /// <since>6.0</since>
    public bool GetCageParameters(out double u, out double v, out double w)
    {
      u = v = w = Double.NaN;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoGripObject_GetCageParameters(const_ptr_this, ref u, ref v, ref w);
    }

    /// <summary>
    /// Retrieves the 2d parameter space values of this GripObject from the curve it's associated with.
    /// </summary>
    /// <param name="t"></param>
    /// <returns>True on success. Output is unreliable if return is false.</returns>
    /// <since>6.0</since>
    public bool GetCurveParameters(out double t)
    {
      t = Double.NaN;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoGripObject_GetCurveParameters(const_ptr_this, ref t);
    }

    /// <since>5.0</since>
    public override int Index
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoGripObject_Index(ptr);
      }
      set
      {
        throw new NotSupportedException("Cannot set Grip index.");
      }
    }
  }


  public class NamedViewWidgetObject : RhinoObject
  {
    internal NamedViewWidgetObject(uint serialNumber)
      : base(serialNumber)
    { }

    /// <since>7.5</since>
    public string AssociatedNamedView
    {
      get
      {
        using (var holder = new StringWrapper())
        {
          bool rc = UnsafeNativeMethods.CRhinoNamedViewWidgetObject_AssociatedNamedView(ConstPointer(), holder.NonConstPointer);

          return rc ? holder.ToString() : null;
        }
      }
    }
  }

}

namespace Rhino.DocObjects.Custom
{
  public abstract class CustomPointObject : PointObject, IDisposable
  {
    protected CustomPointObject()
      : base(true)
    {
      Guid type_id = GetType().GUID;
      if (SubclassCreateNativePointer)
        m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomPointObject_New(type_id);
    }
    protected CustomPointObject(Point point)
      : base(true)
    {
      Guid type_id = GetType().GUID;
      IntPtr const_ptr_this = point.ConstPointer();
      m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomObject_New2(type_id, const_ptr_this);
    }

    ~CustomPointObject() { Dispose(false); }
    /// <since>5.6</since>
    public new void Dispose()
    {
      base.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_pRhinoObject)
      {
        // This delete is safe in that it makes sure the object is NOT
        // under control of the Rhino Document
        UnsafeNativeMethods.CRhinoObject_Delete(m_pRhinoObject);
      }
      m_pRhinoObject = IntPtr.Zero;
    }
  }


  public class CustomGripObject : GripObject, IDisposable
  {
    #region statics
    // this will probably end up in RhinoObject
    static readonly System.Collections.Generic.List<CustomGripObject> g_all_custom_grips = new System.Collections.Generic.List<CustomGripObject>();
    static CustomGripObject g_prev_found;
    static RhinoObject GetCustomObject(uint serialNumber)
    {
      if (g_prev_found != null && g_prev_found.m_rhinoobject_serial_number == serialNumber)
        return g_prev_found;

      foreach (CustomGripObject grip in g_all_custom_grips)
      {
        if (grip.m_rhinoobject_serial_number == serialNumber)
        {
          g_prev_found = grip;
          return g_prev_found;
        }
      }
      return null;
    }
    #endregion

    /// <since>5.0</since>
    public CustomGripObject()
    {
      m_pRhinoObject = UnsafeNativeMethods.CRhCmnGripObject_New();
      m_rhinoobject_serial_number = UnsafeNativeMethods.CRhinoObject_RuntimeSN(m_pRhinoObject);
      g_all_custom_grips.Add(this);

      UnsafeNativeMethods.CRhCmnGripObject_SetCallbacks(g_destructor, g_get_weight, g_set_weight);
    }

    ~CustomGripObject(){ Dispose(false); }
    /// <since>5.0</since>
    public new void Dispose()
    {
      base.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
      g_all_custom_grips.Remove(this);
      if ( IntPtr.Zero != m_pRhinoObject )
      {
        // This delete is safe in that it makes sure the object is NOT
        // under control of the Rhino Document
        UnsafeNativeMethods.CRhinoObject_Delete(m_pRhinoObject);
      }
      m_pRhinoObject = IntPtr.Zero;
    }

    /// <since>5.0</since>
    public new int Index
    {
      get{ return base.Index; }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoGripObject_SetIndex(ptr_this, value);
      }
    }

    /// <since>5.0</since>
    public new Point3d OriginalLocation
    {
      get{ return base.OriginalLocation; }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoGripObject_SetGripLocation(ptr_this, value);
      }
    }

    // define a weight override so we don't end up in a circular call
    /// <since>5.0</since>
    public override double Weight
    {
      get { return RhinoMath.UnsetValue; }
      set { //do nothing
      }
    }

    /// <since>5.0</since>
    public virtual void NewLocation()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhCmnGripObject_NewLocationBase(ptr_this);
    }


    internal delegate void CRhinoObjectDestructorCallback(uint serialNumber);
    internal delegate double CRhinoGripObjectWeightCallback(uint serialNumber);
    internal delegate void CRhinoGripObjectSetWeightCallback(uint serialNumber, double weight);

    private static readonly CRhinoObjectDestructorCallback g_destructor = CRhinoObject_Destructor;
    private static readonly CRhinoGripObjectWeightCallback g_get_weight = CRhinoGripObject_GetWeight;
    private static readonly CRhinoGripObjectSetWeightCallback g_set_weight = CRhinoGripObject_SetWeight;

    private static void CRhinoObject_Destructor(uint serialNumber)
    {
      var grip = GetCustomObject(serialNumber) as CustomGripObject;
      if (grip != null)
      {
        grip.m_pRhinoObject = IntPtr.Zero;
        GC.SuppressFinalize(grip);
      }
    }

    private static double CRhinoGripObject_GetWeight(uint serialNumber)
    {
      var grip = GetCustomObject(serialNumber) as CustomGripObject;
      if (grip != null)
      {
        return grip.Weight;
      }
      return RhinoMath.UnsetValue;
    }
    private static void CRhinoGripObject_SetWeight(uint serialNumber, double weight)
    {
      var grip = GetCustomObject(serialNumber) as CustomGripObject;
      if (grip != null)
        grip.Weight = weight;
    }
  }
}

#endif
