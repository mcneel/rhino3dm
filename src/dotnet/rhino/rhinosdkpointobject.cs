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

    /// <summary>
    /// The object these grips belong to.
    /// </summary>
    /// <remarks>
    /// Reaches the owner without the document lookup <see cref="OwnerId"/> would need, so
    /// <see cref="RhinoObject.EnabledGripsId"/> and <see cref="RhinoObject.EditPointGripsOn"/>
    /// are one hop from a grip.
    /// </remarks>
    public RhinoObject Owner
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return RhinoObject.CreateRhinoObjectHelper(
          UnsafeNativeMethods.CRhinoGripObject_Owner(const_ptr_this));
      }
    }

    /// <summary>
    /// Id of the SubD component this grip is on, or 0 when the grip names no SubD component.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Index"/>, which is a position in the owner's grip list, this still
    /// names the same component after the SubD has been edited. Recording it is what lets a
    /// selection of SubD grips be restored against a changed object.
    /// </remarks>
    [CLSCompliant(false)]
    public uint SubDComponentId
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoGripObject_SubDComponentId(const_ptr_this);
      }
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
    // Keyed, not scanned. Every callback below looks a grip up by serial number, and a linear
    // scan here is a scan of every grip the session has ever created.
    static readonly System.Collections.Generic.Dictionary<uint, CustomGripObject> g_all_custom_grips =
      new System.Collections.Generic.Dictionary<uint, CustomGripObject>();

    static CustomGripObject GetCustomObject(uint serialNumber)
    {
      CustomGripObject grip;
      return g_all_custom_grips.TryGetValue(serialNumber, out grip) ? grip : null;
    }
    #endregion

    /// <since>5.0</since>
    public CustomGripObject()
    {
      m_pRhinoObject = UnsafeNativeMethods.CRhCmnGripObject_New();
      m_rhinoobject_serial_number = UnsafeNativeMethods.CRhinoObject_RuntimeSN(m_pRhinoObject);
      g_all_custom_grips[m_rhinoobject_serial_number] = this;

      // The native side holds these as statics, so once is enough. A surface puts one grip on
      // every control point, so setting them per grip is thousands of redundant stores.
      if (!g_callbacks_set)
      {
        g_callbacks_set = true;
        UnsafeNativeMethods.CRhCmnGripObject_SetCallbacks(g_destructor, g_get_weight, g_set_weight);
        UnsafeNativeMethods.CRhCmnGripObject_SetCallbacks2(g_grip_directions, g_curve_param, g_surface_param,
          g_curve_cv_indices, g_surface_cv_indices, g_undo_move);
      }
    }

    static bool g_callbacks_set;

    ~CustomGripObject(){ Dispose(false); }
    /// <since>5.0</since>
    public new void Dispose()
    {
      base.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
      g_all_custom_grips.Remove(m_rhinoobject_serial_number);
      if ( IntPtr.Zero != m_pRhinoObject )
      {
        // This delete is safe in that it makes sure the object is NOT
        // under control of the Rhino Document
        UnsafeNativeMethods.CRhinoObject_Delete(m_pRhinoObject);
      }
      m_pRhinoObject = IntPtr.Zero;
    }

    /// <summary>
    /// The grip's index. CustomObjectGrips.AddGrip already assigns this the grip's position in
    /// the grips list, and Rhino passes it back as the gripIndex argument of NeighborGrip and
    /// as the key of the grip direction cache. Setting it to anything else makes those
    /// arguments disagree with CustomObjectGrips.Grip(int), which is indexed by list position.
    /// </summary>
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

    /// <summary>
    /// Evaluates this grip's directions. For a surface control point these are the two surface
    /// tangents and the normal; for a curve control point, the tangent and a frame around it.
    /// Rhino caches the result per grip and does not ask again, so the directions must not
    /// change once reported.
    /// </summary>
    /// <param name="x">First direction.</param>
    /// <param name="y">Second direction.</param>
    /// <param name="z">Third direction.</param>
    /// <returns>true if the directions were set. The default implementation returns false.</returns>
    protected virtual bool EvaluateGripDirections(out Vector3d x, out Vector3d y, out Vector3d z)
    {
      x = y = z = Vector3d.Zero;
      return false;
    }

    /// <summary>
    /// The curve parameter of the control point this grip drives.
    /// </summary>
    /// <param name="t">The curve parameter.</param>
    /// <returns>true on success. The default implementation returns false.</returns>
    protected virtual bool EvaluateCurveParameter(out double t)
    {
      t = RhinoMath.UnsetValue;
      return false;
    }

    /// <summary>
    /// The surface parameters of the control point this grip drives.
    /// </summary>
    /// <param name="u">The first surface parameter.</param>
    /// <param name="v">The second surface parameter.</param>
    /// <returns>true on success. The default implementation returns false.</returns>
    protected virtual bool EvaluateSurfaceParameters(out double u, out double v)
    {
      u = v = RhinoMath.UnsetValue;
      return false;
    }

    /// <summary>
    /// The indices of the NURBS curve control points this grip drives. A grip may drive more
    /// than one, as at a closed or periodic seam.
    /// </summary>
    /// <returns>The control point indices, or null if this grip drives none.</returns>
    protected virtual int[] EvaluateCurveCVIndices()
    {
      return null;
    }

    /// <summary>
    /// The indices of the NURBS surface control points this grip drives. A grip may drive more
    /// than one, as at a closed or periodic seam or a singular edge.
    /// Every index must lie inside the surface returned by the owning CustomObjectGrips'
    /// NurbsSurface(). Rhino writes through these indices without checking them, so a grip
    /// reporting one outside the control point grid is refused whole.
    /// </summary>
    /// <returns>The control point indices, or null if this grip drives none.</returns>
    /// <remarks>
    /// IndexPair rather than the Tuple used by GripObject.GetSurfaceCVIndices: this is called
    /// once per grip while Rhino maps a whole surface, and IndexPair is a struct.
    /// </remarks>
    protected virtual IndexPair[] EvaluateSurfaceCVIndices()
    {
      return null;
    }

    /// <summary>
    /// Set this when the grip changes something other than its location, such as a weight, so
    /// that Rhino counts the grip as moved. Rhino already counts a grip whose location moved,
    /// so this only adds to that. Cleared when Rhino undoes the grip move.
    /// </summary>
    /// <remarks>
    /// A stored flag rather than something Rhino asks for: it is read once per grip per draw,
    /// so answering it across the managed boundary would cost a transition per grip per frame.
    /// Real time work driven by grip movement belongs in CustomObjectGrips.OnDraw, which runs
    /// on the display thread, together with the NewLocation latch.
    /// </remarks>
    /// <since>8.36</since>
    public bool MovedOtherThanLocation
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var rc = UnsafeNativeMethods.CRhCmnGripObject_GetMovedOtherThanLocation(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhCmnGripObject_SetMovedOtherThanLocation(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Called when Rhino undoes a grip move. Restore anything the grip changed alongside its
    /// location, such as a weight. The location itself is restored by Rhino.
    /// </summary>
    protected virtual void OnUndoMove()
    {
    }


    internal delegate void CRhinoObjectDestructorCallback(uint serialNumber);
    internal delegate double CRhinoGripObjectWeightCallback(uint serialNumber);
    internal delegate void CRhinoGripObjectSetWeightCallback(uint serialNumber, double weight);
    internal delegate int CRhinoGripObjectGripDirectionsCallback(uint serialNumber, ref Vector3d x, ref Vector3d y, ref Vector3d z);
    internal delegate int CRhinoGripObjectCurveParamCallback(uint serialNumber, ref double t);
    internal delegate int CRhinoGripObjectSurfaceParamCallback(uint serialNumber, ref double u, ref double v);
    internal delegate int CRhinoGripObjectCurveCVIndicesCallback(uint serialNumber, IntPtr pIndices);
    internal delegate int CRhinoGripObjectSurfaceCVIndicesCallback(uint serialNumber, IntPtr pIndices);
    internal delegate void CRhinoGripObjectUndoMoveCallback(uint serialNumber);

    private static readonly CRhinoObjectDestructorCallback g_destructor = CRhinoObject_Destructor;
    private static readonly CRhinoGripObjectWeightCallback g_get_weight = CRhinoGripObject_GetWeight;
    private static readonly CRhinoGripObjectSetWeightCallback g_set_weight = CRhinoGripObject_SetWeight;
    private static readonly CRhinoGripObjectGripDirectionsCallback g_grip_directions = CRhinoGripObject_GripDirections;
    private static readonly CRhinoGripObjectCurveParamCallback g_curve_param = CRhinoGripObject_CurveParameter;
    private static readonly CRhinoGripObjectSurfaceParamCallback g_surface_param = CRhinoGripObject_SurfaceParameters;
    private static readonly CRhinoGripObjectCurveCVIndicesCallback g_curve_cv_indices = CRhinoGripObject_CurveCVIndices;
    private static readonly CRhinoGripObjectSurfaceCVIndicesCallback g_surface_cv_indices = CRhinoGripObject_SurfaceCVIndices;
    private static readonly CRhinoGripObjectUndoMoveCallback g_undo_move = CRhinoGripObject_UndoMove;

    private static void CRhinoObject_Destructor(uint serialNumber)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null)
      {
        grip.m_pRhinoObject = IntPtr.Zero;
        g_all_custom_grips.Remove(serialNumber);
        GC.SuppressFinalize(grip);
      }
    }

    private static double CRhinoGripObject_GetWeight(uint serialNumber)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null)
      {
        return grip.Weight;
      }
      return RhinoMath.UnsetValue;
    }
    private static void CRhinoGripObject_SetWeight(uint serialNumber, double weight)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null)
        grip.Weight = weight;
    }

    // GripObject's public getters - Moved, GetGripDirections, GetSurfaceParameters and the rest -
    // call the very native virtuals these callbacks implement. An override that reads its own
    // getter would recurse until the stack is gone, so each (grip, callback) pair is entered once.
    [ThreadStatic] private static System.Collections.Generic.HashSet<long> g_in_callback;

    private const int idxGripDirections = 0;
    private const int idxCurveParameter = 1;
    private const int idxSurfaceParameters = 2;
    private const int idxCurveCVIndices = 3;
    private const int idxSurfaceCVIndices = 4;
    private const int idxUndoMove = 6;

    private static bool EnterCallback(uint serialNumber, int which)
    {
      var set = g_in_callback;
      if (set == null)
        set = g_in_callback = new System.Collections.Generic.HashSet<long>();
      return set.Add(((long)serialNumber << 3) | (uint)which);
    }

    private static void ExitCallback(uint serialNumber, int which)
    {
      var set = g_in_callback;
      if (set != null)
        set.Remove(((long)serialNumber << 3) | (uint)which);
    }

    private static int CRhinoGripObject_GripDirections(uint serialNumber, ref Vector3d x, ref Vector3d y, ref Vector3d z)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null && EnterCallback(serialNumber, idxGripDirections))
      {
        try
        {
          Vector3d vx, vy, vz;
          if (grip.EvaluateGripDirections(out vx, out vy, out vz))
          {
            x = vx;
            y = vy;
            z = vz;
            return 1;
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
        finally
        {
          ExitCallback(serialNumber, idxGripDirections);
        }
      }
      return 0;
    }

    private static int CRhinoGripObject_CurveParameter(uint serialNumber, ref double t)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null && EnterCallback(serialNumber, idxCurveParameter))
      {
        try
        {
          double s;
          if (grip.EvaluateCurveParameter(out s))
          {
            t = s;
            return 1;
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
        finally
        {
          ExitCallback(serialNumber, idxCurveParameter);
        }
      }
      return 0;
    }

    private static int CRhinoGripObject_SurfaceParameters(uint serialNumber, ref double u, ref double v)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null && EnterCallback(serialNumber, idxSurfaceParameters))
      {
        try
        {
          double s, t;
          if (grip.EvaluateSurfaceParameters(out s, out t))
          {
            u = s;
            v = t;
            return 1;
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
        finally
        {
          ExitCallback(serialNumber, idxSurfaceParameters);
        }
      }
      return 0;
    }

    private static int CRhinoGripObject_CurveCVIndices(uint serialNumber, IntPtr pIndices)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null && IntPtr.Zero != pIndices && EnterCallback(serialNumber, idxCurveCVIndices))
      {
        try
        {
          int[] indices = grip.EvaluateCurveCVIndices();
          if (indices != null)
          {
            foreach (int i in indices)
              UnsafeNativeMethods.ON_IntArray_Append(pIndices, i);
            return indices.Length;
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
        finally
        {
          ExitCallback(serialNumber, idxCurveCVIndices);
        }
      }
      return 0;
    }

    private static int CRhinoGripObject_SurfaceCVIndices(uint serialNumber, IntPtr pIndices)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null && IntPtr.Zero != pIndices && EnterCallback(serialNumber, idxSurfaceCVIndices))
      {
        try
        {
          IndexPair[] indices = grip.EvaluateSurfaceCVIndices();
          if (indices != null)
          {
            foreach (IndexPair ij in indices)
              UnsafeNativeMethods.ON_2dexArray_Append(pIndices, ij.I, ij.J);
            return indices.Length;
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
        finally
        {
          ExitCallback(serialNumber, idxSurfaceCVIndices);
        }
      }
      return 0;
    }

    private static void CRhinoGripObject_UndoMove(uint serialNumber)
    {
      var grip = GetCustomObject(serialNumber);
      if (grip != null && EnterCallback(serialNumber, idxUndoMove))
      {
        try
        {
          grip.OnUndoMove();
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
        finally
        {
          ExitCallback(serialNumber, idxUndoMove);
        }
      }
    }
  }
}

#endif
