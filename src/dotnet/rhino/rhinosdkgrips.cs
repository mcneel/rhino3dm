#pragma warning disable 1591
using System;
using System.Collections.Generic;
//public struct GripDirections { }

#if RHINO_SDK
namespace Rhino.DocObjects.Custom
{
  public class GripStatus
  {
    readonly GripsDrawEventArgs m_parent;
    internal readonly int m_index;
    internal GripStatus(GripsDrawEventArgs parent, int index)
    {
      m_parent = parent;
      m_index = index;
    }

    const int idxVisible = 0;
    const int idxHighlighted = 1;
    const int idxLocked = 2;
    const int idxMoved = 3;
    const int idxCulled = 4;
    const int idxIsVisible = 5;
    bool GetBool(int which)
    {
      IntPtr pParent = m_parent.m_pGripsDrawSettings;
      return UnsafeNativeMethods.CRhinoGripStatus_GetBool(pParent, m_index, which);
    }
    void SetBool(int which, bool val)
    {
      IntPtr pParent = m_parent.m_pGripsDrawSettings;
      UnsafeNativeMethods.CRhinoGripStatus_SetBool(pParent, m_index, which, val);
    }

    /*
  bool m_bVisible;
  bool m_bHighlighted;
  bool m_bLocked;
  bool m_bMoved;
     */

    public bool Culled
    {
      get{ return GetBool(idxCulled); }
      set{ SetBool(idxCulled, value); }
    }

    public bool Visible
    {
      get { return GetBool(idxIsVisible); }
    }
    /*
  CRhinoGripDirections m_directions;
     */
  }

  public class GripsDrawEventArgs : Rhino.Display.DrawEventArgs
  {
    internal IntPtr m_pGripsDrawSettings;

    internal GripsDrawEventArgs(IntPtr pGripsDrawSettings)
    {
      m_pGripsDrawSettings = pGripsDrawSettings;
      m_ptr_display_pipeline = UnsafeNativeMethods.CRhinoDrawGripsSettings_DisplayPipelinePtr(pGripsDrawSettings);
    }

    /// <summary>
    /// If true, then draw stuff that moves when grips are dragged,
    /// like the curve being bent by a dragged control point.
    /// </summary>
    public bool DrawStaticStuff
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDrawGripSettings_GetBool(m_pGripsDrawSettings, true);
      }
    }

    /// <summary>
    /// If true, then draw stuff that does not move when grips are
    /// dragged, like the control polygon of the "original" curve.
    /// </summary>
    public bool DrawDynamicStuff
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDrawGripSettings_GetBool(m_pGripsDrawSettings, false);
      }
    }

    //skipping m_cp_grip_style

    const int idxCpGripLineStyle = 0;
    const int idxGripStatusCount = 1;

    /// <summary>
    /// What kind of line is used to display things like control polygons.
    /// 0 = no control polygon,  1 = solid control polygon,  2 = dotted control polygon.
    /// </summary>
    public int ControlPolygonStyle
    {
      get { return UnsafeNativeMethods.CRhinoDrawGripSettings_GetInt(m_pGripsDrawSettings, idxCpGripLineStyle); }
      set { UnsafeNativeMethods.CRhinoDrawGripSettings_SetInt(m_pGripsDrawSettings, idxCpGripLineStyle, value); }
    }

    const int idxGripColor = 0;
    const int idxLockedGripColor = 1;
    const int idxSelectedGripColor = 2;
    System.Drawing.Color GetColor(int which)
    {
      int abgr = UnsafeNativeMethods.CRhinoDrawGripSettings_GetColor(m_pGripsDrawSettings, which);
      return Rhino.Runtime.Interop.ColorFromWin32(abgr);
    }

    public System.Drawing.Color GripColor
    {
      get { return GetColor(idxGripColor); }
      set { UnsafeNativeMethods.CRhinoDrawGripSettings_SetColor(m_pGripsDrawSettings, idxGripColor, value.ToArgb()); }
    }
    public System.Drawing.Color LockedGripColor
    {
      get { return GetColor(idxLockedGripColor); }
      set { UnsafeNativeMethods.CRhinoDrawGripSettings_SetColor(m_pGripsDrawSettings, idxLockedGripColor, value.ToArgb()); }
    }
    public System.Drawing.Color SelectedGripColor
    {
      get { return GetColor(idxSelectedGripColor); }
      set { UnsafeNativeMethods.CRhinoDrawGripSettings_SetColor(m_pGripsDrawSettings, idxSelectedGripColor, value.ToArgb()); }
    }

    int m_gripstatus_count = -1;
    public int GripStatusCount
    {
      get
      {
        if( m_gripstatus_count<0 )
          m_gripstatus_count = UnsafeNativeMethods.CRhinoDrawGripSettings_GetInt(m_pGripsDrawSettings, idxGripStatusCount);
        return m_gripstatus_count;
      }
    }

    List<GripStatus> m_gs;
    public GripStatus GripStatus(int index)
    {
      if (null == m_gs)
      {
        m_gs = new List<GripStatus>(GripStatusCount);
        for (int i = 0; i < GripStatusCount; i++)
          m_gs.Add(new GripStatus(this, i));
      }
      return m_gs[index];
    }

    /// <summary>
    /// Draws the lines in a control polygons.
    /// <para>This is an helper function.</para>
    /// </summary>
    /// <param name="line">Line between two grips.</param>
    /// <param name="startStatus">Grip status at start of line.</param>
    /// <param name="endStatus">Grip status at end of line.</param>
    public void DrawControlPolygonLine( Rhino.Geometry.Line line, GripStatus startStatus, GripStatus endStatus )
    {
      DrawControlPolygonLine(line, startStatus.m_index, endStatus.m_index);
    }
    /// <summary>
    /// Draws the lines in a control polygons.
    /// <para>This is an helper function.</para>
    /// </summary>
    /// <param name="line">Line between two grips.</param>
    /// <param name="startStatus">Index of Grip status at start of line.</param>
    /// <param name="endStatus">Index if Grip status at end of line.</param>
    public void DrawControlPolygonLine(Rhino.Geometry.Line line, int startStatus, int endStatus)
    {
      DrawControlPolygonLine(line.From, line.To, startStatus, endStatus);
    }
    /// <summary>
    /// Draws the lines in a control polygons.
    /// <para>This is an helper function.</para>
    /// </summary>
    /// <param name="start">The point start.</param>
    /// <param name="end">The point end.</param>
    /// <param name="startStatus">Index of Grip status at start of line defined by start and end.</param>
    /// <param name="endStatus">Index if Grip status at end of line defined by start and end.</param>
    public void DrawControlPolygonLine(Rhino.Geometry.Point3d start, Rhino.Geometry.Point3d end, int startStatus, int endStatus)
    {
      UnsafeNativeMethods.CRhinoDrawGripsSettings_DrawControlPolygonLine(m_pGripsDrawSettings, start, end, startStatus, endStatus);
    }

    public void RestoreViewportSettings()
    {
      UnsafeNativeMethods.CRhinoDrawGripSettings_RestoreViewportSettings(m_pGripsDrawSettings);
    }
  }

//public class ObjectGrips { }
//public class CopyGripsHelper{}
//public class KeepKinkySurfaces{}

  public delegate void TurnOnGripsEventHandler(Rhino.DocObjects.RhinoObject rhObj);

  public abstract class CustomObjectGrips : IDisposable
  {
    #region statics
    static int m_serial_number_counter = 1;
    static readonly List<CustomObjectGrips> m_all_custom_grips = new List<CustomObjectGrips>();
    static readonly Dictionary<Guid, TurnOnGripsEventHandler> m_registered_enablers = new Dictionary<Guid, TurnOnGripsEventHandler>();
    static CustomObjectGrips FromSerialNumber(int serial_number)
    {
      for (int i = 0; i < m_all_custom_grips.Count; i++)
      {
        CustomObjectGrips grips = m_all_custom_grips[i];
        if (grips.m_runtime_serial_number == serial_number)
          return grips;
      }
      return null;
    }

    public static void RegisterGripsEnabler(TurnOnGripsEventHandler enabler, Type customGripsType)
    {
      if (!customGripsType.IsSubclassOf(typeof(CustomObjectGrips)))
        throw new ArgumentException("customGripsType must be derived from CustomObjectGrips");
      if (enabler == null)
        throw new ArgumentNullException("enabler");

      Guid key = customGripsType.GUID;
      if (m_registered_enablers.ContainsKey(key))
        m_registered_enablers[key] = enabler;
      else
      {
        // This is a new enabler that needs to be registerer with RhinoApp()
        m_registered_enablers.Add(key, enabler);
        UnsafeNativeMethods.CRhinoApp_RegisterGripsEnabler(key, m_TurnOnGrips);
      }
    }

    #endregion

    #region pointer management

    readonly int m_runtime_serial_number;
    IntPtr m_ptr;
    bool m_bDeleteOnDispose = true;

    internal IntPtr NonConstPointer() { return m_ptr; }
    IntPtr ConstPointer() { return m_ptr; }

    internal void OnAttachedToRhinoObject(Rhino.DocObjects.RhinoObject rhObj)
    {
      m_bDeleteOnDispose = false;
      // make sure all of the callback functions are hooked up
      UnsafeNativeMethods.CRhinoObjectGrips_SetCallbacks(m_OnResetCallback,
        m_OnResetMeshesCallback, m_OnUpdateMeshCallback, m_OnNewGeometryCallback,
        m_OnDrawCallback, m_OnNeighborGripCallback, m_OnNurbsSurfaceGripCallback,
        m_NurbsSurfaceCallback);
    }
    #endregion

    protected CustomObjectGrips()
    {
      m_runtime_serial_number = m_serial_number_counter++;
      Guid id = GetType().GUID;
      m_ptr = UnsafeNativeMethods.CRhCmnObjectGrips_New(m_runtime_serial_number, id);
      m_all_custom_grips.Add(this);
    }

    readonly List<CustomGripObject> m_grip_list = new List<CustomGripObject>();
    protected void AddGrip(Rhino.DocObjects.Custom.CustomGripObject grip)
    {
      m_grip_list.Add(grip);

      IntPtr pGrip = grip.NonConstPointer();
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CRhinoObjectGrips_AddGrip(pThis, pGrip);      
    }

    public int GripCount
    {
      get { return m_grip_list.Count; }
    }

    public CustomGripObject Grip(int index)
    {
      return m_grip_list[index];
    }

    const int idxNewLocation = 0;
    const int idxGripsMoved = 1;
    const int idxDragging = 2;

    /// <summary>
    /// Determines if grips are currently being dragged.
    /// </summary>
    /// <returns>true if grips are dragged.</returns>
    public static bool Dragging()
    {
      return UnsafeNativeMethods.CRhinoObjectGrips_GetBool(IntPtr.Zero, idxDragging);
    }

    /// <summary>
    /// true if some of the grips have been moved. GripObject.NewLocation() sets
    /// NewLocation=true.  Derived classes can set NewLocation to false after 
    /// updating temporary display information.
    /// </summary>
    public bool NewLocation
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoObjectGrips_GetBool(pConstThis, idxNewLocation);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.CRhinoObjectGrips_SetBool(pThis, idxNewLocation, value);
      }
    }

    /// <summary>
    /// If GripsMoved is true if some of the grips have ever been moved
    /// GripObject.NewLocation() sets GripsMoved=true.
    /// </summary>
    public bool GripsMoved
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoObjectGrips_GetBool(pConstThis, idxGripsMoved);
      }
    }


    /// <summary>Owner of the grips.</summary>
    public Rhino.DocObjects.RhinoObject OwnerObject
    {
      get
      {
        IntPtr pThis = NonConstPointer();
        IntPtr pRhinoObject = UnsafeNativeMethods.CRhinoObjectGrips_OwnerObject(pThis);
        return Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pRhinoObject);
      }
    }



    /// <summary>
    /// Resets location of all grips to original spots and cleans up stuff that
    /// was created by dynamic dragging.  This is required when dragging is
    /// canceled or in the Copy command when grips are "copied". The override
    /// should clean up dynamic workspace stuff.
    /// </summary>
    protected virtual void OnReset()
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CRhCmnObjectGrips_ResetBase(pThis, true);
    }

    /// <summary>
    /// Just before Rhino turns off object grips, it calls this function.
    /// If grips have modified any display meshes, they must override
    /// this function and restore the meshes to their original states.
    /// </summary>
    protected virtual void OnResetMeshes()
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CRhCmnObjectGrips_ResetBase(pThis, false);
    }

    /// <summary>
    /// Just before Rhino shades an object with grips on, it calls this method
    /// to update the display meshes.  Grips that modify surface or mesh objects
    /// must override this function and modify the display meshes here.
    /// </summary>
    /// <param name="meshType">The mesh type being updated.</param>
    protected virtual void OnUpdateMesh(Rhino.Geometry.MeshType meshType)
    {
      //base class does nothing
    }

    // Use NewGeometry for now instead of NewObject(). We can always add a NewObject()
    // virtual function that just calls NewGeometry by default
    //protected virtual RhinoObject NewObject()
    //{
    //  // The default calls NewGeometry
    //  Rhino.Geometry.GeometryBase geometry = NewGeometry();
    //  if (geometry != null)
    //  {
    //    IntPtr pConstThis = ConstPointer();
    //    IntPtr pGeometry = geometry.NonConstPointer();
    //    IntPtr pNewRhinoObject = UnsafeNativeMethods.CRhCmnObjectGrips_CreateRhinoObject(pConstThis, pGeometry);
    //    return NewRhinoObject???
    //  }
    //  return null;
    //}

    /// <summary>
    /// If the grips control just one object, then override NewGeometry(). When
    /// NewGeometry() is called, return new geometry calculated from the current
    /// grip locations. This happens once at the end of a grip drag.
    /// </summary>
    /// <returns>The new geometry. The default implementation returns null.</returns>
    protected virtual Rhino.Geometry.GeometryBase NewGeometry()
    {
      return null;
    }

    /// <summary>
    /// Draws the grips. In your implementation, override this if you need to draw
    /// dynamic elements and then call this base implementation to draw the grips themselves.
    /// </summary>
    /// <param name="args">The grips draw event arguments.</param>
    protected virtual void OnDraw(GripsDrawEventArgs args)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CRhinoObjectGrips_DrawBase(pThis, args.m_pGripsDrawSettings);
    }

    /// <summary>Get neighbors.</summary>
    /// <param name="gripIndex">index of grip where the search begins.</param>
    /// <param name="dr">
    /// <para>1 = next grip in the first parameter direction.</para>
    /// <para>-1 = prev grip in the first parameter direction.</para>
    /// </param>
    /// <param name="ds">
    /// <para>1 = next grip in the second parameter direction.</para>
    /// <para>-1 = prev grip in the second parameter direction.</para>
    /// </param>
    /// <param name="dt">
    /// <para>1 = next grip in the third parameter direction.</para>
    /// <para>-1 = prev grip in the third parameter direction.</para>
    /// </param>
    /// <param name="wrap">If true and object is "closed", the search will wrap.</param>
    /// <returns>Pointer to the desired neighbor or NULL if there is no neighbor.</returns>
    protected virtual GripObject NeighborGrip(int gripIndex, int dr, int ds, int dt, bool wrap) { return null; }

    /// <summary>
    /// If the grips are control points of a NURBS surface, then this gets the
    /// index of the grip that controls the (i,j)-th cv.
    /// </summary>
    /// <param name="i">The index in the first dimension.</param>
    /// <param name="j">The index in the second dimension.</param>
    /// <returns>A grip controling a NURBS surface CV or null.</returns>
    protected virtual GripObject NurbsSurfaceGrip(int i, int j) { return null; }

    /// <summary>
    /// If the grips control a NURBS surface, this returns a pointer to that
    /// surface.  You can look at but you must NEVER change this surface.
    /// </summary>
    /// <returns>A pointer to a NURBS surface or null.</returns>
    protected virtual Rhino.Geometry.NurbsSurface NurbsSurface() { return null; }

    ~CustomObjectGrips() { Dispose(false); }
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_bDeleteOnDispose && IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.CRhCmnObjectGrips_Delete(m_ptr);
      }
      m_ptr = IntPtr.Zero;
    }

    internal delegate void CRhinoGripsEnablerCallback(IntPtr pRhinoObject, Guid enabler_id);
    private static readonly CRhinoGripsEnablerCallback m_TurnOnGrips = CRhinoGripsEnabler_TurnOnGrips;
    private static void CRhinoGripsEnabler_TurnOnGrips(IntPtr pRhinoObject, Guid enabler_id)
    {
      TurnOnGripsEventHandler handler;
      if (m_registered_enablers.TryGetValue(enabler_id, out handler))
      {
        RhinoObject rhobj = RhinoObject.CreateRhinoObjectHelper(pRhinoObject);
        if( rhobj!=null )
          handler(rhobj);
      }
    }


    internal delegate void CRhinoObjectGripsResetCallback(int serial_number);
    internal delegate void CRhinoObjectGripsUpdateMeshCallback(int serial_number, int meshType);
    internal delegate IntPtr CRhinoObjectGripsNewGeometryCallback(int serial_number);
    internal delegate void CRhinoObjectGripsDrawCallback(int serial_numer, IntPtr pDrawSettings);
    internal delegate IntPtr CRhinoObjectGripsNeighborGripCallback(int serial_number, int gripIndex, int dr, int ds, int dt, int wrap);
    internal delegate IntPtr CRhinoObjectGripsNurbsSurfaceGripCallback(int serial_number, int i, int j);
    internal delegate IntPtr CRhinoObjectGripsNurbsSurfaceCallback(int serial_number);
    
    private static readonly CRhinoObjectGripsResetCallback m_OnResetCallback = CRhinoObjectGrips_Reset;
    private static readonly CRhinoObjectGripsResetCallback m_OnResetMeshesCallback = CRhinoObjectGrips_ResetMeshes;
    private static readonly CRhinoObjectGripsUpdateMeshCallback m_OnUpdateMeshCallback = CRhinoObjectGrips_UpdateMesh;
    private static readonly CRhinoObjectGripsNewGeometryCallback m_OnNewGeometryCallback = CRhinoObjectGrips_NewGeometry;
    private static readonly CRhinoObjectGripsDrawCallback m_OnDrawCallback = CRhinoObjectGrips_Draw;
    private static readonly CRhinoObjectGripsNeighborGripCallback m_OnNeighborGripCallback = CRhinoObjectGrips_NeighborGrip;
    private static readonly CRhinoObjectGripsNurbsSurfaceGripCallback m_OnNurbsSurfaceGripCallback = CRhinoObjectGrips_NurbsSurfaceGrip;
    private static readonly CRhinoObjectGripsNurbsSurfaceCallback m_NurbsSurfaceCallback = CRhinoObjectGrips_NurbsSurface;

    private static void CRhinoObjectGrips_Reset(int serial_number)
    {
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          grips.OnReset();
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void CRhinoObjectGrips_ResetMeshes(int serial_number)
    {
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          grips.OnResetMeshes();
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void CRhinoObjectGrips_UpdateMesh(int serial_number, int meshType)
    {
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          grips.OnUpdateMesh((Geometry.MeshType)meshType);
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static IntPtr CRhinoObjectGrips_NewGeometry(int serial_number)
    {
      IntPtr rc = IntPtr.Zero;
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          Rhino.Geometry.GeometryBase geom = grips.NewGeometry();
          if (geom != null)
          {
            rc = geom.NonConstPointer();
            geom.ReleaseNonConstPointer();
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
    private static void CRhinoObjectGrips_Draw(int serial_number, IntPtr pDrawSettings)
    {
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          GripsDrawEventArgs args = new GripsDrawEventArgs(pDrawSettings);
          grips.OnDraw(args);
          args.m_pGripsDrawSettings = IntPtr.Zero; // don't let the args hold onto a pointer
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    private static IntPtr CRhinoObjectGrips_NeighborGrip(int serial_number, int gripIndex, int dr, int ds, int dt, int wrap)
    {
      IntPtr rc = IntPtr.Zero;
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          GripObject grip = grips.NeighborGrip(gripIndex, dr, ds, dt, wrap != 0);
          if (grip != null)
            rc = grip.NonConstPointer();
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
    private static IntPtr CRhinoObjectGrips_NurbsSurfaceGrip(int serial_number, int i, int j)
    {
      IntPtr rc = IntPtr.Zero;
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          GripObject grip = grips.NurbsSurfaceGrip(i,j);
          if (grip != null)
            rc = grip.NonConstPointer();
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }

    private static IntPtr CRhinoObjectGrips_NurbsSurface(int serial_number)
    {
      IntPtr rc = IntPtr.Zero;
      CustomObjectGrips grips = FromSerialNumber(serial_number);
      if (grips != null)
      {
        try
        {
          Rhino.Geometry.NurbsSurface ns = grips.NurbsSurface();
          if (ns != null)
            rc = ns.ConstPointer();
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
  }
}
#endif