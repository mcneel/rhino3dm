#pragma warning disable 1591
using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Drawing;

#if RHINO_SDK
namespace Rhino.Display
{
  /// <summary>
  /// Represents display pipeline settings, such as "show transparency" and "show grips".
  /// </summary>
  [Serializable]
  public class DisplayPipelineAttributes : IDisposable, ISerializable
  {
    #region pointer tracking
    private object m_parent;
    internal IntPtr m_ptr_attributes = IntPtr.Zero;

    internal DisplayPipelineAttributes(IntPtr pAttrs)
    {
      m_ptr_attributes = pAttrs;
    }

    private bool m_dontdelete;
    /// <summary>
    /// Use this constructor if attributes shouldn't be fully disposed (i.e.
    /// not owned by this instance)
    /// </summary>
    internal DisplayPipelineAttributes(IntPtr pAttrs, bool dontdelete) : this(pAttrs)
    {
      m_dontdelete = dontdelete;
    }

    internal DisplayPipelineAttributes(DisplayModeDescription parent)
    {
      m_parent = parent;
    }

    internal DisplayPipelineAttributes(DisplayPipeline parent)
    {
      m_parent = parent;
    }

    internal IntPtr ConstPointer()
    {
      if (m_ptr_attributes != IntPtr.Zero)
        return m_ptr_attributes;

      // Check pipeline_parent first since this is typically time critical
      // code when this is used.
      DisplayPipeline pipeline_parent = m_parent as DisplayPipeline;
      if (pipeline_parent != null)
        return pipeline_parent.DisplayAttributeConstPointer();

      DisplayModeDescription parent = m_parent as DisplayModeDescription;
      if (parent != null)
        return parent.DisplayAttributeConstPointer();
      return IntPtr.Zero;
    }

    internal IntPtr NonConstPointer()
    {
      if (m_ptr_attributes != IntPtr.Zero)
        return m_ptr_attributes;

      // Check pipeline_parent first since this is typically time critical
      // code when this is used.
      DisplayPipeline pipeline_parent = m_parent as DisplayPipeline;
      if (pipeline_parent != null)
      {
        // We can cheat and use a const pointer for the non-const version. This is
        // because when a pipeline is associated with attributes, it typically
        // happens when conduits are involved. When conduits are being called, the
        // attributes are exposed as a non const pointer.
        return pipeline_parent.DisplayAttributeConstPointer();
      }

      DisplayModeDescription parent = m_parent as DisplayModeDescription;
      if (parent != null)
        return parent.DisplayAttributeNonConstPointer();
      return IntPtr.Zero;
    }

    internal void CopyContents(DisplayPipelineAttributes other)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_other = other.ConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_CopyContents(ptr_this, const_ptr_other);
    }
    #endregion
 
    protected DisplayPipelineAttributes(SerializationInfo info, StreamingContext context)
    {
      m_parent = null;
      m_ptr_attributes = UnsafeNativeMethods.CDisplayPipelineAttributes_New();
      IntPtr ptr_profile_context = Runtime.HostUtils.ReadIntoProfileContext(info, "DisplayPipelineAttributes");
      UnsafeNativeMethods.CDisplayPipelineAttributes_LoadProfile(m_ptr_attributes, ptr_profile_context, "DisplayPipelineAttributes");
      UnsafeNativeMethods.CRhinoProfileContext_Delete(ptr_profile_context);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_profile_context = UnsafeNativeMethods.CRhCmnProfileContext_New();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SaveProfile(const_ptr_this, ptr_profile_context, "DisplayPipelineAttributes");
      Runtime.HostUtils.WriteIntoSerializationInfo(ptr_profile_context, info, "DisplayPipelineAttributes");
      UnsafeNativeMethods.CRhinoProfileContext_Delete(ptr_profile_context);
    }

    ~DisplayPipelineAttributes()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_ptr_attributes!=IntPtr.Zero && !m_dontdelete)
      {
        UnsafeNativeMethods.CDisplayPipelineAttributes_Delete(m_ptr_attributes);
      }
      m_ptr_attributes = IntPtr.Zero;
    }


    #region General display overrides...
    public bool XrayAllObjects
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.XrayAllOjbjects); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.XrayAllOjbjects, value); }
    }

    public bool IgnoreHighlights
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IgnoreHighlights); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IgnoreHighlights, value); }
    }

    public bool DisableConduits
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableConduits); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableConduits, value); }
    }

    public bool DisableTransparency
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableTransparency); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableTransparency, value); }
    }
    #endregion

    #region General dynamic/runtime object drawing attributes
    public Color ObjectColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.ObjectColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.ObjectColor, value); }
    }

    public bool ShowGrips
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGrips); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGrips, value); }
    }

    public enum FrameBufferFillMode
    {
      DefaultColor = UnsafeNativeMethods.FrameBufferFillMode.DEFAULT_COLOR,
      SolidColor = UnsafeNativeMethods.FrameBufferFillMode.SOLID_COLOR,
      Gradient2Color = UnsafeNativeMethods.FrameBufferFillMode.GRADIENT_2_COLOR,
      Gradient4Color = UnsafeNativeMethods.FrameBufferFillMode.GRADIENT_4_COLOR,
      Bitmap = UnsafeNativeMethods.FrameBufferFillMode.BITMAP,
      Renderer = UnsafeNativeMethods.FrameBufferFillMode.RENDERER,
      Transparent = UnsafeNativeMethods.FrameBufferFillMode.TRANSPARENT
    }
    FrameBufferFillMode GetFillMode()
    {
      IntPtr const_ptr_this = ConstPointer();
      return (FrameBufferFillMode)UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetFillMode(const_ptr_this, false, UnsafeNativeMethods.FrameBufferFillMode.DEFAULT_COLOR);
    }

    void SetFillMode(UnsafeNativeMethods.FrameBufferFillMode mode)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetFillMode(ptr_this, true, mode);
    }
    /// <summary>
    /// Get or set the frame buffer fill mode.
    /// </summary>
    public FrameBufferFillMode FillMode
    {
      get { return GetFillMode(); }
      set { SetFillMode((UnsafeNativeMethods.FrameBufferFillMode)value); }
    }

    public enum BoundingBoxDisplayMode : int
    {
      None = UnsafeNativeMethods.DisplayPipelineAttributesBBox.BBoxOff,
      OnDuringDynamicDisplay = UnsafeNativeMethods.DisplayPipelineAttributesBBox.BBoxOnDynamicDisplay,
      OnAlways = UnsafeNativeMethods.DisplayPipelineAttributesBBox.BBoxOnAlways
    }

    public BoundingBoxDisplayMode BoundingBoxMode
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        return (BoundingBoxDisplayMode)UnsafeNativeMethods.CDisplayPipelineAttributes_GetBBoxMode(constPtrThis);
      }
      set
      {
        IntPtr ptrThis = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_SetBBoxMode(ptrThis, (UnsafeNativeMethods.DisplayPipelineAttributesBBox)value);
      }
    }
    /// <summary>Show clipping planes.</summary>
    public bool ShowClippingPlanes
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClippingPlanes); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClippingPlanes, value); }
    }
    #endregion

    #region View specific attributes...
    public sealed class ViewDisplayAttributes
    {
      readonly DisplayPipelineAttributes m_parent;
      internal ViewDisplayAttributes(DisplayPipelineAttributes parent)
      {
        m_parent = parent;
      }

      //bool m_bUseDefaultGrid; <-skipped. Not used in Rhino

      public bool UseDocumentGrid
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseDocumentGrid); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseDocumentGrid, value); }
      }

      public bool DrawGrid
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawGrid); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawGrid, value); }
      }

      public bool DrawGridAxes
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawAxes); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawAxes, value); }
      }

      public bool DrawZAxis
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawZAxis); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawZAxis, value); }
      }

      public bool DrawWorldAxes
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawWorldAxes); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawWorldAxes, value); }
      }

      public bool ShowGridOnTop
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGridOnTop); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGridOnTop, value); }
      }
      //bool m_bShowTransGrid;

      public bool BlendGrid
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.BlendGrid); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.BlendGrid, value); }
      }

      //int m_nGridTrans;
      public bool DrawTransparentGridPlane
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawTransGridPlane); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawTransGridPlane, value); }
      }

      //int                   m_nGridPlaneTrans;
      //int                   m_nAxesPercentage;
      //bool                  m_bPlaneUsesGridColor;
      //COLORREF              m_GridPlaneColor;
      //int                   m_nPlaneVisibility;
      //int                   m_nWorldAxesColor;
      public Color WorldAxisColorX
      {
        get { return m_parent.GetColor(UnsafeNativeMethods.DisplayAttrsColor.WxColor); }
        set { m_parent.SetColor(UnsafeNativeMethods.DisplayAttrsColor.WxColor, value); }
      }
      public Color WorldAxisColorY
      {
        get { return m_parent.GetColor(UnsafeNativeMethods.DisplayAttrsColor.WyColor); }
        set { m_parent.SetColor(UnsafeNativeMethods.DisplayAttrsColor.WyColor, value); }
      }
      public Color WorldAxisColorZ
      {
        get { return m_parent.GetColor(UnsafeNativeMethods.DisplayAttrsColor.WzColor); }
        set { m_parent.SetColor(UnsafeNativeMethods.DisplayAttrsColor.WzColor, value); }
      }

      //bool                  m_bUseDefaultBg;
      //EFrameBufferFillMode  m_eFillMode;
      //COLORREF              m_SolidColor;
      //ON_wString            m_sBgBitmap;
      //const CRhinoDib*    m_pBgBitmap;
      //COLORREF              m_GradTopLeft;
      //COLORREF              m_GradBotLeft;
      //COLORREF              m_GradTopRight;
      //COLORREF              m_GradBotRight;
      //int                   m_nStereoModeEnabled;
      //float                 m_fStereoSeparation;
      //float                 m_fStereoParallax;
      //int                   m_nAGColorMode;
      //int                   m_nAGViewingMode;


      //bool                  m_bUseDefaultScale; <-skipped. Not used in Rhino

      public double HorizontalViewportScale
      {
        get { return m_parent.GetDouble(idx_dHorzScale); }
        set { m_parent.SetDouble(idx_dHorzScale, value); }
      }
      public double VerticalViewportScale
      {
        get { return m_parent.GetDouble(idx_dVertScale); }
        set { m_parent.SetDouble(idx_dVertScale, value); }
      }

      //bool                  m_bUseLineSmoothing;
      //bool                  LoadBackgroundBitmap(const ON_wString&);
      //int                   m_eAAMode;
      //bool                  m_bFlipGlasses;
    }

    ViewDisplayAttributes m_view_specific_attributes;
    public ViewDisplayAttributes ViewSpecificAttributes
    {
      get { return m_view_specific_attributes ?? (m_view_specific_attributes = new ViewDisplayAttributes(this)); }
    }
    #endregion

    #region bool
    bool GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetBool(const_ptr_this, which, false, false);
    }
    void SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool which, bool b)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetBool(ptr_this, which, true, b);
    }

    /// <summary>
    /// Gets whether objects ought to be drawn using their assigned rendering material.
    /// </summary>
    public bool UseAssignedObjectMaterial
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseObjectMaterial); }
    }
    /// <summary>
    /// Gets whether objects ought to be drawn using a custom color.
    /// </summary>
    public bool UseCustomObjectColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.OverrideObjectColor); }
    }
    /// <summary>
    /// Gets whether objects ought to be drawn using a custom material.
    /// </summary>
    public bool UseCustomObjectMaterial
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomFrontMaterial); }
    }
    #endregion

    #region color
    //int                 m_IsHighlighted;
    //int                 m_nLineThickness;
    //UINT                m_nLinePattern;

    Color GetColor(UnsafeNativeMethods.DisplayAttrsColor which)
    {
      IntPtr pConstThis = ConstPointer();
      int argb = UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetColor(pConstThis, which, false, 0);
      return System.Drawing.Color.FromArgb(argb);
    }
    void SetColor(UnsafeNativeMethods.DisplayAttrsColor which, Color c)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetColor(pThis, which, true, c.ToArgb());
    }
    #endregion

    #region double
    const int idx_dHorzScale = 0;
    const int idx_dVertScale = 1;

    double GetDouble(int which)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetDouble(pConstThis, which, false, 0);
    }
    void SetDouble(int which, double d)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetDouble(pThis, which, true, d);
    }
    #endregion

    #region int
    int GetInt(UnsafeNativeMethods.DisplayAttributesInt which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetInt(const_ptr_this, which, false, 0);
    }
    void SetInt(UnsafeNativeMethods.DisplayAttributesInt which, int i)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetInt(ptr_this, which, true, i);
    }
    #endregion
    
    #region Curves specific attributes...
    /// <summary>Draw curves</summary>
    public bool ShowCurves
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowCurves); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowCurves, value); }
    }

    /// <summary>
    /// Use a single color for drawing curves
    /// </summary>
    public bool UseSingleCurveColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleCurveColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleCurveColor, value); }
    }

    //bool m_bUseDefaultCurve; -- doesn't appear to be used in display pipelane
    /// <summary>Pixel thickness for curves</summary>
    public int CurveThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.CurveThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.CurveThickness, (int)value); }
    }

    //UINT m_nCurvePattern;
    //bool m_bCurveKappaHair;
    //bool m_bSingleCurveColor;
    /// <summary>Color used for drawing curves</summary>
    public System.Drawing.Color CurveColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.CurveColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.CurveColor, value); }
    }
    //ELineEndCapStyle m_eLineEndCapStyle;
    //ELineJoinStyle m_eLineJoinStyle;
    #endregion

    #region Both surface and mesh specific attributes...
    //bool m_bUseDefaultShading;

    /// <summary>Draw shaded meshes and surfaces. Set to false to use Flat Shading.</summary>
    public bool ShadingEnabled
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShadeSurface); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShadeSurface, value); }
    }

    /// <summary>Shade using vertex colors.</summary>
    public bool ShadeVertexColors
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShadeVertexColors); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShadeVertexColors, value); }
    }

    //bool m_bUseObjectMaterial;
    //bool m_bSingleWireColor;
    //COLORREF m_WireColor;
    //COLORREF m_ShadedColor;
    //bool m_bUseDefaultBackface;
    //bool m_bUseObjectBFMaterial;
    //bool m_bCullBackfaces;
    #endregion

    #region Surfaces specific attributes...
    //bool m_bUseDefaultSurface;
    //bool m_bSurfaceKappaHair;
    //bool m_bHighlightSurfaces;
    // iso's...
    //bool m_bUseDefaultIso;
    //bool m_bShowIsocurves;
    /// <summary>Draw surface ISO curves.</summary>
    public bool ShowIsoCurves
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowIsoCurves); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowIsoCurves, value); }
    }
    //bool m_bIsoThicknessUsed;
    //int m_nIsocurveThickness;
    //int m_nIsoUThickness;
    //int m_nIsoVThickness;
    //int m_nIsoWThickness;
    //bool m_bSingleIsoColor;
    //COLORREF m_IsoColor;
    //bool m_bIsoColorsUsed;
    //COLORREF m_IsoUColor;
    //COLORREF m_IsoVColor;
    //COLORREF m_IsoWColor;
    //bool m_bIsoPatternUsed;
    //UINT m_nIsocurvePattern;
    //UINT m_nIsoUPattern;
    //UINT m_nIsoVPattern;
    //UINT m_nIsoWPattern;
    // edges....
    //bool m_bUseDefaultEdges;
    /// <summary>Show surface edges.</summary>
    public bool ShowSurfaceEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSurfaceEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSurfaceEdges, value); }
    }
    /// <summary>Show tangent edges.</summary>
    public bool ShowTangentEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentEdges, value); }
    }
    /// <summary>Show tangent seams.</summary>
    public bool ShowTangentSeams
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentSeams); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentSeams, value); }
    }
    //bool m_bShowNakedEdges;
    //bool m_bShowEdgeEndpoints;

    /// <summary> Thickness for surface edges </summary>
    public int SurfaceEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeThickness, value); }
    }
    //int m_nEdgeThickness;
    //int m_nEdgeColorUsage;
    //int m_nNakedEdgeOverride;
    //int m_nNakedEdgeColorUsage;
    //int m_nNakedEdgeThickness;
    //int m_nEdgeColorReduction;
    //int m_nNakedEdgeColorReduction;
    //COLORREF m_EdgeColor;
    //COLORREF m_NakedEdgeColor;
    //UINT m_nEdgePattern;
    //UINT m_nNakedEdgePattern;
    //UINT m_nNonmanifoldEdgePattern;
    #endregion

    #region Object locking attributes...
    //bool m_bUseDefaultObjectLocking;
    //int m_nLockedUsage;
    //bool m_bGhostLockedObjects;
    //int m_nLockedTrans;
    //COLORREF m_LockedColor;
    /*
    /// <summary>
    /// If Color.Unset, then a specific lock color is NOT used
    /// </summary>
    public System.Drawing.Color UseSpecificLockColor
    {
      get
      {
      }
      set
      {
      }
    }
    */

    /// <summary>Locked object are drawn behind other objects</summary>
    public bool LockedObjectsDrawBehindOthers
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.LockedObjectsBehind); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.LockedObjectsBehind, value); }
    }
    #endregion

    #region Meshes specific attributes...
    public sealed class MeshDisplayAttributes
    {
      readonly DisplayPipelineAttributes m_parent;
      internal MeshDisplayAttributes(DisplayPipelineAttributes parent)
      {
        m_parent = parent;
      }

      //bool m_bUseDefaultMesh; <-skipped. Not used in Rhino
      public bool HighlightMeshes
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.HighlightMeshes); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.HighlightMeshes, value); }
      }

      /// <summary>
      /// Color.Empty means that we are NOT using a single color for all mesh wires.
      /// </summary>
      public Color AllMeshWiresColor
      {
        // This is a combination of
        //bool m_bSingleMeshWireColor;
        //COLORREF m_MeshWireColor;
        get
        {
          if (m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleMeshWireColor))
            return m_parent.GetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshWireColor);
          return Color.Empty;
        }
        set
        {
          bool single_color = (value!=Color.Empty);
          m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleMeshWireColor, single_color);
          if( single_color )
            m_parent.SetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshWireColor, value);
        }
      }

      public int MeshWireThickness
      {
        get { return m_parent.GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshWireThickness); }
        set { m_parent.SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshWireThickness, value); }
      }

      //UINT m_nMeshWirePattern;

      public bool ShowMeshWires
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshWires); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshWires, value); }
      }
      public bool ShowMeshVertices
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshVertices); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshVertices, value); }
      }

      //int m_nVertexCountTolerance;
      //ERhinoPointStyle m_eMeshVertexStyle;

      //int m_nMeshVertexSize;

      //bool m_bUseDefaultMeshEdges; // obsolete - not used in display code
      //bool m_bShowMeshEdges;            // here "Edge" means "break" or "crease" edges
      //bool m_bShowMeshNakedEdges;       // "Naked" means boundary edges
      //bool m_bShowMeshNonmanifoldEdges; // "Nonmanifold means 3 or more faces meet at the edge
      //int m_nMeshEdgeThickness;        // here "Edge" means "break" or "crease" edges
      //bool m_bMeshNakedEdgeOverride; // obsolete - not used in display code
      //int m_nMeshNakedEdgeThickness;
      //int m_nMeshEdgeColorReduction;   // here "Edge" means "break" or "crease" edges
      //int m_nMeshNakedEdgeColorReduction;
      //COLORREF m_MeshEdgeColor;             // here "Edge" means "break" or "crease" edges
      //COLORREF m_MeshNakedEdgeColor;
      //int m_nMeshNonmanifoldEdgeThickness;
      //int m_nMeshNonmanifoldEdgeColorReduction;
      //COLORREF m_MeshNonmanifoldEdgeColor;
    }

    MeshDisplayAttributes m_MeshSpecificAttributes;
    public MeshDisplayAttributes MeshSpecificAttributes
    {
      get { return m_MeshSpecificAttributes ?? (m_MeshSpecificAttributes = new MeshDisplayAttributes(this)); }
    }

    #endregion

    #region Dimensions & Text specific attributes...
    //bool m_bUseDefaultText;
    /// <summary>Show text.</summary>
    public bool ShowText
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowText); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowText, value); }
    }
    /// <summary>Show annotations.</summary>
    public bool ShowAnnotations
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowAnnotations); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowAnnotations, value); }
    }
    //COLORREF m_DotTextColor;
    //COLORREF m_DotBorderColor;
    #endregion

    #region Lights & Lighting specific attributes...
    //bool m_bUseDefaultLights;
    /// <summary>Show light widgets.</summary>
    public bool ShowLights
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowLights); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowLights, value); }
    }
    //bool m_bUseHiddenLights;
    //bool m_bUseLightColor;
    //bool m_bUseDefaultLightingScheme;
    //ELightingScheme m_eLightingScheme;
    //bool m_bUseDefaultEnvironment;
    //int m_nLuminosity;
    public Color AmbientLightingColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.AmbientColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.AmbientColor, value); }
    }
    //ON_ObjectArray<ON_Light> m_Lights;
    //int m_eShadowMapType;
    //int m_nShadowMapSize;
    //int m_nNumSamples;
    public Color ShadowColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.ShadowColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.ShadowColor, value); }
    }
    //ON_3dVector m_ShadowBias;
    //double m_fShadowBlur;
    /// <summary>Cast shadows.</summary>
    public bool CastShadows
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.Shadows); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.Shadows, value); }
    }
    //BYTE m_nShadowBitDepth;
    //BYTE m_nTransparencyTolerance;
    //bool m_bPerPixelLighting;
    #endregion

    #region Points specific attributes...
    /// <summary>Show points.</summary>
    public bool ShowPoints
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPoints); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPoints, value); }
    }

    public PointStyle PointStyle
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        PointStyle rc = PointStyle.Simple;
        UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetPointStyle(const_ptr_this, true, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_GetSetPointStyle(ptr_this, false, ref value);
      }
    }

    public float PointRadius
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.PointSize); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.PointSize, (int)value); }
    }
    #endregion

    #region Control Polygon specific attributes...
    //bool m_bUseDefaultControlPolygon;
    //bool m_bCPSolidLines;
    //bool m_bCPSingleColor;
    //bool m_bCPHidePoints;
    //bool m_bCPHideSurface;
    //bool m_bCPHighlight;
    //bool m_bCPHidden;
    //int m_nCPWireThickness;
    //int m_nCVSize;
    //ERhinoPointStyle m_eCVStyle;
    //ON_Color m_CPColor;
    #endregion

    #region PointClouds specific attributes...
    //bool m_bUseDefaultPointCloud;
    //int m_nPCSize;
    //ERhinoPointStyle m_ePCStyle;
    //int m_nPCGripSize;
    //ERhinoPointStyle m_ePCGripStyle;
    /// <summary>Show point clouds.</summary>
    public bool ShowPointClouds
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPointClouds); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPointClouds, value); }
    }
    #endregion

/* 
public:
  ON_MeshParameters*  m_pMeshSettings;
  CDisplayPipelineMaterial*  m_pMaterial;
  // experimental
  bool                m_bDegradeIsoDensity;
  bool                m_bLayersFollowLockUsage;
*/

    public Guid Id
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_GetId(pConstThis);
      }
    }

    public string EnglishName
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        using (var sw = new Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptrName = sw.NonConstPointer;
          UnsafeNativeMethods.CDisplayPipelineAttributes_GetName(constPtrThis, true, ptrName);
          return sw.ToString();
        }
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_SetEnglishName(pThis, value);
      }
    }

    public string LocalName
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        using (var sw = new Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptrName = sw.NonConstPointer;
          UnsafeNativeMethods.CDisplayPipelineAttributes_GetName(constPtrThis, false, ptrName);
          return sw.ToString();
        }
      }
    }

    public enum ContextsForDraw : int
    {
      Unset = 0,          //CRhinoDisplayPipeline::DrawToDC will ASSERT if called with this value.  Proceed your call with one of the values below.
      FilePreview = 1,    //called from GetPreviewBitmap
      ViewCapture = 2,    //ViewCaptureToFile or ViewCaptureToClipboard
      Printing = 3,       //Printing
      UIPreview = 4,      //Used by various dialogs to draw the document into a bitmap.
      Mask = 5,
      RenderOverlays = 6,
    }

    public ContextsForDraw ContextForDraw
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        return (ContextsForDraw)UnsafeNativeMethods.CDisplayPipelineAttributes_GetContextDrawToDC(constPtrThis);
      }
    }

    #region realtime display integration
    /// <summary>
    /// Get the ID of the realtime display engine attached to the view. This will be
    /// Guid.Empty if no realtime display engine is in use. This can be the case for instance
    /// when starting a _Render session for a realtime viewport integration. That still would
    /// cause this ID to be Guid.Empty.
    /// </summary>
    public Guid RealtimeDisplayId
    {
      // added for https://mcneel.myjetbrains.com/youtrack/issue/RH-39449
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_GetRealtimeDisplayId(pConstThis);
      }
    }

    /// <summary>
    /// Get or set the realtime passes amount
    /// </summary>
    public int RealtimeRenderPasses
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.RealtimeRenderPasses); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.RealtimeRenderPasses, value); }
    }

    /// <summary>
    /// Get or set whether the display is used for preview rendering or not.
    /// </summary>
    public bool ShowRealtimeRenderProgressBar
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowRealtimeRenderProgressBar); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowRealtimeRenderProgressBar, value); }
    }
#endregion

    public enum StereoRenderContextEnum : int
    {
      NotApplicable = 0,
      RenderingLeftEye = 1,
      RenderingRightEye = 2,
      RenderingBothEyes = 3 /*RenderingLeftEye | RenderingRightEye*/
    }

    /// <summary>
    /// Get or set the stereo render context.
    /// </summary>
    public StereoRenderContextEnum StereoRenderContext
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return (StereoRenderContextEnum)UnsafeNativeMethods.CDisplayPipelineAttributes_GetStereoRenderContext(ptr);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_SetStereoRenderContext(ptr, (UnsafeNativeMethods.StereoRenderContext)value);
      }
    }
  }
}
#endif