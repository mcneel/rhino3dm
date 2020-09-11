#if RHINO_SDK
using System;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Display
{
  /// <summary>
  /// Represents a base class for visual analysis modes.
  /// <para>This class is abstract.</para>
  /// </summary>
  public abstract partial class VisualAnalysisMode
  {
    /// <summary>
    /// Contains enumerated values for analysis styles, such as wireframe, texture or false colors..
    /// </summary>
    /// <since>5.0</since>
    public enum AnalysisStyle : int
    {
      /// <summary>The analysis is showing with wires.</summary>
      Wireframe = 1,

      /// <summary>The analysis is showing with textures.</summary>
      Texture = 2,

      /// <summary>The analysis is showing with false colors.</summary>
      FalseColor = 4
    }

    #region callbacks
    internal delegate void ANALYSISMODEENABLEUIPROC(Guid am_id, int enable);
    internal delegate int ANALYSISMODEOBJECTSUPPORTSPROC(Guid am_id, IntPtr pConstRhinoObject);
    internal delegate int ANALYSISMODESHOWISOCURVESPROC(Guid am_id);
    internal delegate void ANALYSISMODESETDISPLAYATTRIBUTESPROC(Guid am_id, IntPtr pConstRhinoObject, IntPtr pDisplayPipelineAttributes);
    internal delegate void ANALYSISMODEUPDATEVERTEXCOLORSPROC(Guid am_id, IntPtr pConstRhinoObject, IntPtr pSimpleArryConstMeshes, int meshCount);
    internal delegate void ANALYSISMODEDRAWRHINOOBJECTPROC(Guid am_id, IntPtr pConstRhinoObject, IntPtr pRhinoDisplayPipeline);
    internal delegate void ANALYSISMODEDRAWGEOMETRYPROC(Guid am_id, IntPtr pConstRhinoObject, IntPtr pConstGeometry, IntPtr pRhinoDisplayPipeline);

    static void OnEnableUiProc(Guid am_id, int enable)
    {
      VisualAnalysisMode mode = FindLocal(am_id);
      if (mode != null)
      {
        try
        {
          mode.EnableUserInterface(enable == 1);
        }
        catch (Exception) { }
      }
    }
    static readonly ANALYSISMODEENABLEUIPROC m_ANALYSISMODEENABLEUIPROC = OnEnableUiProc;
    
    static int OnObjectSupportsProc(Guid am_id, IntPtr pConstRhinoObject)
    {
      VisualAnalysisMode mode = FindLocal(am_id);
      if (mode != null)
      {
        var rhobj = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pConstRhinoObject);
        try
        {
          bool rc = mode.ObjectSupportsAnalysisMode(rhobj);
          return rc ? 1 : 0;
        }
        catch (Exception) { }
      }
      return 0;
    }
    static readonly ANALYSISMODEOBJECTSUPPORTSPROC m_ANALYSISMODEOBJECTSUPPORTSPROC = OnObjectSupportsProc;

    static int OnShowIsoCurvesProc(Guid am_id)
    {
      VisualAnalysisMode mode = FindLocal(am_id);
      if (mode != null)
      {
        try
        {
          return mode.ShowIsoCurves ? 1 : 0;
        }
        catch (Exception) { }
      }
      return 0;
    }
    static readonly ANALYSISMODESHOWISOCURVESPROC m_ANALYSISMODESHOWISOCURVESPROC = OnShowIsoCurvesProc;

    static void OnSetDisplayAttributesProc(Guid am_id, IntPtr pConstRhinoObject, IntPtr pDisplayPipelineAttributes)
    {
      VisualAnalysisMode mode = FindLocal(am_id);
      if (mode != null)
      {
        var rhobj = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pConstRhinoObject);
        DisplayPipelineAttributes attr = new DisplayPipelineAttributes(pDisplayPipelineAttributes);
        try
        {
          mode.SetUpDisplayAttributes(rhobj, attr);
          attr.m_ptr_attributes = IntPtr.Zero;
        }
        catch (Exception) { }
      }
    }
    static readonly ANALYSISMODESETDISPLAYATTRIBUTESPROC m_ANALYSISMODESETDISPLAYATTRIBUTESPROC = OnSetDisplayAttributesProc;

    static void OnUpdateVertexColorsProc(Guid am_id, IntPtr pConstRhinoObject, IntPtr pSimpleArrayConstMeshes, int meshCount)
    {
      VisualAnalysisMode mode = FindLocal(am_id);
      if (mode != null)
      {
        var rhobj = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pConstRhinoObject);
        Rhino.Geometry.Mesh[] meshes = new Geometry.Mesh[meshCount];
        for( int i=0; i<meshCount; i++ )
        {
          IntPtr pMesh = UnsafeNativeMethods.ON_MeshArray_Get(pSimpleArrayConstMeshes, i);
          if (IntPtr.Zero != pMesh)
          {
            meshes[i] = new Geometry.Mesh(pMesh, null);
            meshes[i].DoNotDestructOnDispose();
          }
        }
        try
        {
          mode.UpdateVertexColors(rhobj, meshes);
        }
        catch (Exception) { }
      }
    }
    static readonly ANALYSISMODEUPDATEVERTEXCOLORSPROC m_ANALYSISMODEUPDATEVERTEXCOLORSPROC = OnUpdateVertexColorsProc;

    static void OnDrawRhinoObjectProc(Guid am_id, IntPtr pConstRhinoObject, IntPtr pRhinoDisplayPipeline)
    {
      VisualAnalysisMode mode = FindLocal(am_id);
      if (mode != null)
      {
        var rhobj = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pConstRhinoObject);
        DisplayPipeline dp = new DisplayPipeline(pRhinoDisplayPipeline);
        try
        {
          Rhino.DocObjects.BrepObject brep = rhobj as Rhino.DocObjects.BrepObject;
          if (brep != null)
          {
            mode.DrawBrepObject(brep, dp);
            return;
          }
          Rhino.DocObjects.CurveObject curve = rhobj as Rhino.DocObjects.CurveObject;
          if (curve != null)
          {
            mode.DrawCurveObject(curve, dp);
            return;
          }
          Rhino.DocObjects.MeshObject mesh = rhobj as Rhino.DocObjects.MeshObject;
          if (mesh != null)
          {
            mode.DrawMeshObject(mesh, dp);
            return;
          }
          Rhino.DocObjects.PointCloudObject pointcloud = rhobj as Rhino.DocObjects.PointCloudObject;
          if (pointcloud != null)
          {
            mode.DrawPointCloudObject(pointcloud, dp);
            return;
          }
          Rhino.DocObjects.PointObject pointobj = rhobj as Rhino.DocObjects.PointObject;
          if (pointobj != null)
          {
            mode.DrawPointObject(pointobj, dp);
            return;
          }
        }
        catch (Exception) { }
      }
    }
    static readonly ANALYSISMODEDRAWRHINOOBJECTPROC m_ANALYSISMODEDRAWRHINOOBJECTPROC = OnDrawRhinoObjectProc;

    static void OnDrawGeometryProc(Guid am_id, IntPtr pConstRhinoObject, IntPtr pConstGeometry, IntPtr pRhinoDisplayPipeline)
    {
      VisualAnalysisMode mode = FindLocal(am_id);
      if (mode != null)
      {
        var rhobj = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pConstRhinoObject);
        var geom = Rhino.Geometry.GeometryBase.CreateGeometryHelper(pConstGeometry, null);
        if (geom != null)
          geom.DoNotDestructOnDispose();
        DisplayPipeline dp = new DisplayPipeline(pRhinoDisplayPipeline);
        Rhino.Geometry.Mesh mesh = geom as Rhino.Geometry.Mesh;
        try
        {
          if (mesh != null)
          {
            mode.DrawMesh(rhobj, mesh, dp);
            return;
          }
          Rhino.Geometry.NurbsCurve nurbscurve = geom as Rhino.Geometry.NurbsCurve;
          if (nurbscurve != null)
          {
            mode.DrawNurbsCurve(rhobj, nurbscurve, dp);
            return;
          }
          Rhino.Geometry.NurbsSurface nurbssurf = geom as Rhino.Geometry.NurbsSurface;
          if (nurbssurf != null)
          {
            mode.DrawNurbsSurface(rhobj, nurbssurf, dp);
            return;
          }
        }
        catch (Exception) { }
      }
    }
    static readonly ANALYSISMODEDRAWGEOMETRYPROC m_ANALYSISMODEDRAWGEOMETRYPROC = OnDrawGeometryProc;
    #endregion

    static System.Collections.Generic.List<VisualAnalysisMode> m_registered_modes;

    #region ids
    /// <summary>
    /// Id for Rhino's built-in edge analysis mode. Brep and mesh edges are
    /// shown in a selected color.
    /// </summary>
    /// <since>5.0</since>
    public static Guid RhinoEdgeAnalysisModeId
    {
      get { return new Guid("197B765D-CDA3-4411-8A0A-AD8E0891A918"); }
    }

    /// <summary>
    /// Id for Rhino's built-in curvature graphs analysis mode. Curvature hair
    /// is shown on curves and surfaces.
    /// </summary>
    /// <since>5.0</since>
    public static Guid RhinoCurvatureGraphAnalysisModeId
    {
      get { return new Guid("DF59A9CF-E517-4846-9232-D9AE56A9D13D"); }
    }

    /// <summary>
    /// Id for Rhino's built-in zebra stripe analysis mode. Zebra stripes are
    /// shown on surfaces and meshes.
    /// </summary>
    /// <since>5.0</since>
    public static Guid RhinoZebraStripeAnalysisModeId
    {
      get { return new Guid("0CCA817C-95D0-4b79-B5D7-CEB5A2975CE0"); }
    }

    /// <summary>
    /// Id for Rhino's built-in emap analysis mode.  An environment map is
    /// shown on surfaces and meshes.
    /// </summary>
    /// <since>5.0</since>
    public static Guid RhinoEmapAnalysisModeId
    {
      get { return new Guid("DAEF834E-E978-4f7b-9026-A432C678C189"); }
    }

    /// <summary>
    /// Id for Rhino's built-in curvature color analysis mode.  Surface curvature
    /// is shown using false color mapping.
    /// </summary>
    /// <since>5.0</since>
    public static Guid RhinoCurvatureColorAnalyisModeId
    {
      get { return new Guid("639E9144-1C1A-4bba-8248-D330F50D7B69"); }
    }

    /// <summary>
    /// Id for Rhino's built-in draft angle analysis mode.  Draft angle is 
    /// displayed using false colors.
    /// </summary>
    /// <since>5.0</since>
    public static Guid RhinoDraftAngleAnalysisModeId
    {
      get { return new Guid("F08463F4-22E2-4cf1-B810-F01925446D71"); }
    }

    /// <summary>
    /// Id for Rhino's built-in thickness analysis mode.
    /// </summary>
    /// <since>5.0</since>
    public static Guid RhinoThicknessAnalysisModeId
    {
      get { return new Guid("B28E5435-D299-4933-A95D-3783C496FC66"); }
    }

    //0xa5cc27f6, 0xe169, 0x443a, { 0x87, 0xed, 0xc1, 0x6, 0x57, 0xff, 0x4b, 0xc9
    /// <summary>
    /// Id for Rhino's built-in edge continuity analysis mode.
    /// </summary>
    /// <since>7.0</since>
    public static Guid RhinoEdgeContinuityAlalysisModeId
    {
      get { return new Guid("A5CC27F6-E169-443A-87ED-C10657FF4BC9"); }
    }
    #endregion

    /// <summary>
    /// Interactively adjusts surface analysis meshes of objects using a Rhino built-in analysis mode.
    /// </summary>
    /// <param name="doc">The Rhino document.</param>
    /// <param name="analysisModeId">The id of the analysis mode.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public static bool AdjustAnalysisMeshes(RhinoDoc doc, Guid analysisModeId)
    {
      if (null == doc)
        throw new ArgumentNullException(nameof(doc));
      return UnsafeNativeMethods.CRhinoVisualAnalysisMode_AnalysisAdjustMeshes(doc.RuntimeSerialNumber, analysisModeId);
    }

    /// <summary>
    /// Registers a custom visual analysis mode for use in Rhino.  It is OK to call
    /// register multiple times for a single custom analysis mode type, since subsequent
    /// register calls will notice that the type has already been registered.
    /// </summary>
    /// <param name="customAnalysisModeType">
    /// Must be a type that is a subclass of VisualAnalysisMode.
    /// </param>
    /// <returns>
    /// An instance of registered analysis mode on success.
    /// </returns>
    /// <since>5.0</since>
    public static VisualAnalysisMode Register(Type customAnalysisModeType)
    {
      if( !customAnalysisModeType.IsSubclassOf(typeof(VisualAnalysisMode)) )
        throw new ArgumentException("customAnalysisModeType must be a subclass of VisualAnalysisMode");

      // make sure this class has not already been registered
      var rc = Find(customAnalysisModeType);
      if (rc == null)
      {
        if (m_registered_modes == null)
          m_registered_modes = new System.Collections.Generic.List<VisualAnalysisMode>();
        rc = System.Activator.CreateInstance(customAnalysisModeType) as VisualAnalysisMode;
        if (rc != null)
        {
          UnsafeNativeMethods.CRhinoVisualAnalysisMode_Register(rc.Id, rc.Name, (int)rc.Style);

          UnsafeNativeMethods.CRhinoVisualAnalysisMode_SetCallbacks(m_ANALYSISMODEENABLEUIPROC,
            m_ANALYSISMODEOBJECTSUPPORTSPROC,
            m_ANALYSISMODESHOWISOCURVESPROC,
            m_ANALYSISMODESETDISPLAYATTRIBUTESPROC,
            m_ANALYSISMODEUPDATEVERTEXCOLORSPROC,
            m_ANALYSISMODEDRAWRHINOOBJECTPROC,
            m_ANALYSISMODEDRAWGEOMETRYPROC);

          m_registered_modes.Add(rc);
        }
      }
      return rc;
    }

    static VisualAnalysisMode FindLocal(Guid id)
    {
      if (m_registered_modes != null)
      {
        for (int i = 0; i < m_registered_modes.Count; i++)
        {
          if (m_registered_modes[i].GetType().GUID == id)
            return m_registered_modes[i];
        }
      }
      return null;
    }
    /// <summary>
    /// Finds a visual analysis mode by guid.
    /// </summary>
    /// <param name="id">The globally unique identifier to search for.</param>
    /// <returns>The found visual analysis mode, or null if it was not found, or on error.</returns>
    /// <since>5.0</since>
    public static VisualAnalysisMode Find(Guid id)
    {
      VisualAnalysisMode rc = FindLocal(id);
      if (rc != null)
        return rc;

      IntPtr pMode = UnsafeNativeMethods.CRhinoVisualAnalysisMode_Mode(id);
      if (pMode != IntPtr.Zero)
      {
        if (m_registered_modes == null)
          m_registered_modes = new System.Collections.Generic.List<VisualAnalysisMode>();
        var native = new NativeVisualAnalysisMode(id);
        m_registered_modes.Add(native);
        return native;
      }
      return null;
    }

    /// <summary>
    /// Finds a visual analysis mode by type.
    /// </summary>
    /// <param name="t">A visual analysis mode type.</param>
    /// <returns>A visual analysis mode on success, or null on error.</returns>
    /// <since>5.0</since>
    public static VisualAnalysisMode Find(Type t)
    {
      return Find(t.GUID);
    }

    internal IntPtr ConstPointer()
    {
      return UnsafeNativeMethods.CRhinoVisualAnalysisMode_Mode(Id);
    }


    /// <summary>
    /// Gets the name of the analysis mode. It is used by the _What command and the object
    /// properties details window to describe the object.
    /// </summary>
    /// <since>5.0</since>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the visual analysis mode style.
    /// </summary>
    /// <since>5.0</since>
    public abstract AnalysisStyle Style { get; }

    internal Guid m_id = Guid.Empty;

    /// <summary>
    /// Gets the visual analysis mode GUID.
    /// The Guid is specified with the <see cref="System.Runtime.InteropServices.GuidAttribute">GuidAttribute</see>
    /// applied to the class.
    /// </summary>
    /// <since>5.0</since>
    public Guid Id
    {
      get
      {
        if (m_id != Guid.Empty)
          return m_id;
        return GetType().GUID;
      }
    }

    /// <summary>
    /// Turns the analysis mode's user interface on and off. For Rhino's built
    /// in modes this opens or closes the modeless dialog that controls the
    /// analysis mode's display settings.
    /// </summary>
    /// <param name="on">true if the interface should be shown; false if it should be concealed.</param>
    /// <since>5.0</since>
    public virtual void EnableUserInterface(bool on) {}

    /// <summary>
    /// Gets a value indicating if this visual analysis mode can be used on a given Rhino object.
    /// </summary>
    /// <param name="obj">The object to be tested.</param>
    /// <returns>true if this mode can indeed be used on the object; otherwise false.</returns>
    /// <since>5.0</since>
    public virtual bool ObjectSupportsAnalysisMode(Rhino.DocObjects.RhinoObject obj)
    {
      IntPtr pConstPointer = ConstPointer();
      IntPtr pConstRhinoObject = obj.ConstPointer();
      return UnsafeNativeMethods.CRhinoVisualAnalysisMode_ObjectSupportsAnalysisMode(pConstPointer, pConstRhinoObject);
    }

    /// <summary>
    /// Gets true if this visual analysis mode will show isocuves on shaded surface
    /// objects.  Often a mode's user interface will provide a way to change this
    /// setting.
    /// <para>The default is false.</para>
    /// </summary>
    /// <since>5.0</since>
    public virtual bool ShowIsoCurves
    {
      get { return false; }
    }

    /// <summary>
    /// If an analysis mode needs to modify display attributes, this is the place
    /// to do it.  In particular, Style==Texture, then this function must be
    /// overridden.
    /// </summary>
    /// <remarks>
    /// Shaded analysis modes that use texture mapping, like zebra and emap,
    /// override this function set the texture, diffuse_color, and EnableLighting
    /// parameter.
    /// </remarks>
    /// <param name="obj">The object for which to set up attributes.</param>
    /// <param name="attributes">The linked attributes.</param>
    protected virtual void SetUpDisplayAttributes(Rhino.DocObjects.RhinoObject obj, DisplayPipelineAttributes attributes) { }

    /// <summary>
    /// If Style==falseColor, then this virtual function must be overridden.
    /// Rhino calls this function when it is time for to set the false colors
    /// on the analysis mesh vertices.  For breps, there is one mesh per face.
    /// For mesh objects there is a single mesh.
    /// </summary>
    /// <param name="obj">The object for which to update vertex colors.</param>
    /// <param name="meshes">An array of meshes that should be updated.</param>
    protected virtual void UpdateVertexColors(Rhino.DocObjects.RhinoObject obj, Rhino.Geometry.Mesh[] meshes) { }

    /// <summary>
    /// If Style==Wireframe, then the default decomposes the curve object into
    /// nurbs curve segments and calls the virtual DrawNurbsCurve for each segment.
    /// </summary>
    /// <param name="curve">A document curve object.</param>
    /// <param name="pipeline">The drawing pipeline.</param>
    protected virtual void DrawCurveObject(Rhino.DocObjects.CurveObject curve, DisplayPipeline pipeline )
    {
      IntPtr pConstThis = ConstPointer();
      IntPtr pConstCurve = curve.ConstPointer();
      IntPtr pPipeline = pipeline.NonConstPointer();
      UnsafeNativeMethods.CRhinoVisualAnalysisMode_DrawCurveObject(pConstThis, pConstCurve, pPipeline);
    }

    /// <summary>
    /// Draws one mesh. Override this method to add your custom behavior.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="mesh">A mesh object.</param>
    /// <param name="pipeline">The current display pipeline.</param>
    protected virtual void DrawMeshObject(Rhino.DocObjects.MeshObject mesh, DisplayPipeline pipeline )
    {
    }

    /// <summary>
    /// Draws one brep. Override this method to add your custom behavior.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="brep">A brep object.</param>
    /// <param name="pipeline">The current display pipeline.</param>
    protected virtual void DrawBrepObject(Rhino.DocObjects.BrepObject brep, DisplayPipeline pipeline )
    {
    }

    /// <summary>
    /// Draws one point. Override this method to add your custom behavior.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="point">A point object.</param>
    /// <param name="pipeline">The current display pipeline.</param>
    protected virtual void DrawPointObject(Rhino.DocObjects.PointObject point, DisplayPipeline pipeline )
    {
    }

    /// <summary>
    /// Draws one point cloud. Override this method to add your custom behavior.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="pointCloud">A point cloud object.</param>
    /// <param name="pipeline">The current display pipeline.</param>
    protected virtual void DrawPointCloudObject(Rhino.DocObjects.PointCloudObject pointCloud, DisplayPipeline pipeline )
    {
    }

    /// <summary>
    /// Draws a NURBS curve. This is a good function to override for
    /// analysis modes like curvature hair display.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="obj">A Rhino object corresponding to the curve.</param>
    /// <param name="curve">The curve geometry.</param>
    /// <param name="pipeline">The current display pipeline.</param>
    protected virtual void DrawNurbsCurve(Rhino.DocObjects.RhinoObject obj, Rhino.Geometry.NurbsCurve curve, DisplayPipeline pipeline)
    {
    }

    /// <summary>
    /// Draws a NURBS surface. This is a good function to override
    /// to display object-related meshes.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="obj">A Rhino object corresponding to the surface.</param>
    /// <param name="surface">The surface geometry.</param>
    /// <param name="pipeline">The current display pipeline.</param>
    protected virtual void DrawNurbsSurface(Rhino.DocObjects.RhinoObject obj, Rhino.Geometry.NurbsSurface surface, DisplayPipeline pipeline)
    {
    }

    /// <summary>
    /// Draws a mesh.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="obj">A Rhino object corresponding to the surface.</param>
    /// <param name="mesh">The mesh geometry.</param>
    /// <param name="pipeline">The current display pipeline.</param>
    protected virtual void DrawMesh(Rhino.DocObjects.RhinoObject obj, Rhino.Geometry.Mesh mesh, DisplayPipeline pipeline )
    {
    }
  }

  class NativeVisualAnalysisMode : VisualAnalysisMode
  {
    public NativeVisualAnalysisMode(Guid id)
    {
      m_id = id;
    }

    public override string Name
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoVisualAnalysisMode_GetAnalysisModeName(pConstThis, pString);
          return sh.ToString();
        }
      }
    }
    public override AnalysisStyle Style
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return (AnalysisStyle)UnsafeNativeMethods.CRhinoVisualAnalysisMode_Style(pConstThis);
      }
    }

    public override void EnableUserInterface(bool on)
    {
      IntPtr pConstThis = ConstPointer();
      UnsafeNativeMethods.CRhinoVisualAnalysisMode_EnableUserInterface(pConstThis, on);
    }

    public override bool ShowIsoCurves
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoVisualAnalysisMode_ShowIsoCurves(pConstThis);
      }
    }
  }
}
#endif
