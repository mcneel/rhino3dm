#pragma warning disable 1591
using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Drawing;
using Rhino.UI;

#if RHINO_SDK
namespace Rhino.Display
{
  /// <since>7.0</since>
  public enum StereoContext : int
  {
    None = 0,
    LeftEye = 1,
    RightEye = 2,
    BothEyes = 3 /*LeftEye | RightEye*/
  }

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

    /// <since>5.0</since>
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

    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_ptr_attributes != IntPtr.Zero && !m_dontdelete)
      {
        UnsafeNativeMethods.CDisplayPipelineAttributes_Delete(m_ptr_attributes);
      }
      m_ptr_attributes = IntPtr.Zero;
    }


    #region General display overrides...
    /// <since>5.0</since>
    public bool XrayAllObjects
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.XrayAllOjbjects); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.XrayAllOjbjects, value); }
    }

    /// <since>5.0</since>
    public bool IgnoreHighlights
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IgnoreHighlights); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IgnoreHighlights, value); }
    }

    /// <since>5.0</since>
    public bool DisableConduits
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableConduits); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableConduits, value); }
    }

    /// <since>5.0</since>
    public bool DisableTransparency
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableTransparency); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DisableTransparency, value); }
    }
    #endregion

    #region General dynamic/runtime object drawing attributes
    /// <since>5.0</since>
    public Color ObjectColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.ObjectColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.ObjectColor, value); }
    }

    /// <since>5.0</since>
    public bool ShowGrips
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGrips); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGrips, value); }
    }

    /// <since>6.0</since>
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
      return (FrameBufferFillMode)UnsafeNativeMethods.CDisplayPipelineAttributes_GetFillMode(const_ptr_this);
    }

    void SetFillMode(UnsafeNativeMethods.FrameBufferFillMode mode)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetFillMode(ptr_this, mode);
    }
    /// <summary>
    /// Get or set the frame buffer fill mode.
    /// </summary>
    /// <since>6.0</since>
    public FrameBufferFillMode FillMode
    {
      get { return GetFillMode(); }
      set { SetFillMode((UnsafeNativeMethods.FrameBufferFillMode)value); }
    }

    /// <summary>
    /// Set fill mode to solid color and set the fill color
    /// </summary>
    /// <param name="singleColor"></param>
    /// <since>6.23</since>
    public void SetFill(Color singleColor)
    {
      SetFill(singleColor, singleColor, singleColor, singleColor);
    }

    /// <summary>
    /// Set fill mode to two color and set the colors
    /// </summary>
    /// <param name="gradientTop"></param>
    /// <param name="gradientBottom"></param>
    /// <since>6.23</since>
    public void SetFill(Color gradientTop, Color gradientBottom)
    {
      SetFill(gradientTop, gradientBottom, gradientTop, gradientBottom);
    }

    /// <summary>
    /// Set the fill mode to four color gradient and set the colors
    /// </summary>
    /// <param name="gradientTopLeft"></param>
    /// <param name="gradientBottomLeft"></param>
    /// <param name="gradientTopRight"></param>
    /// <param name="gradientBottomRight"></param>
    /// <since>6.23</since>
    public void SetFill(Color gradientTopLeft, Color gradientBottomLeft, Color gradientTopRight, Color gradientBottomRight)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetFillColors(ptr_this,
        gradientTopLeft.ToArgb(), gradientBottomLeft.ToArgb(), gradientTopRight.ToArgb(), gradientBottomRight.ToArgb());
    }

    /// <summary>
    /// Get fill colors used for clearing the frame buffer
    /// </summary>
    /// <param name="topLeft"></param>
    /// <param name="bottomLeft"></param>
    /// <param name="topRight"></param>
    /// <param name="bottomRight"></param>
    /// <since>6.23</since>
    public void GetFill(out Color topLeft, out Color bottomLeft, out Color topRight, out Color bottomRight)
    {
      IntPtr const_ptr_this = ConstPointer();
      int tl = 0;
      int bl = 0;
      int tr = 0;
      int br = 0;
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetFillColors(const_ptr_this, ref tl, ref bl, ref tr, ref br);
      topLeft = Color.FromArgb(tl);
      topRight = Color.FromArgb(tr);
      bottomLeft = Color.FromArgb(bl);
      bottomRight = Color.FromArgb(br);
    }

    /// <since>6.1</since>
    public enum BoundingBoxDisplayMode : int
    {
      None = UnsafeNativeMethods.DisplayPipelineAttributesBBox.BBoxOff,
      OnDuringDynamicDisplay = UnsafeNativeMethods.DisplayPipelineAttributesBBox.BBoxOnDynamicDisplay,
      OnAlways = UnsafeNativeMethods.DisplayPipelineAttributesBBox.BBoxOnAlways
    }

    /// <since>6.1</since>
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
    /// <since>6.4</since>
    public bool ShowClippingPlanes
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClippingPlanes); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClippingPlanes, value); }
    }

    /// <summary>Show fills where clipping planes clip solid objects</summary>
    /// <since>8.0</since>
    public bool ShowClipIntersectionSurfaces
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClipIntersectionSurfaces); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClipIntersectionSurfaces, value); }
    }

    /// <summary>Show edges and hatches where clipping planes clip objects</summary>
    /// <since>8.0</since>
    public bool ShowClipIntersectionEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClipIntersectionEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowClipIntersectionEdges, value); }
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

      /// <since>5.0</since>
      public bool UseDocumentGrid
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseDocumentGrid); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseDocumentGrid, value); }
      }

      /// <since>5.0</since>
      public bool DrawGrid
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawGrid); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawGrid, value); }
      }

      /// <since>5.0</since>
      public bool DrawGridAxes
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawAxes); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawAxes, value); }
      }

      /// <since>5.0</since>
      public bool DrawZAxis
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawZAxis); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawZAxis, value); }
      }

      /// <since>5.0</since>
      public bool DrawWorldAxes
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawWorldAxes); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.DrawWorldAxes, value); }
      }

      /// <since>5.0</since>
      public bool ShowGridOnTop
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGridOnTop); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowGridOnTop, value); }
      }
      //bool m_bShowTransGrid;

      /// <since>5.0</since>
      public bool BlendGrid
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.BlendGrid); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.BlendGrid, value); }
      }

      //int m_nGridTrans;
      /// <since>5.0</since>
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
      /// <since>5.0</since>
      public Color WorldAxisColorX
      {
        get { return m_parent.GetColor(UnsafeNativeMethods.DisplayAttrsColor.WxColor); }
        set { m_parent.SetColor(UnsafeNativeMethods.DisplayAttrsColor.WxColor, value); }
      }
      /// <since>5.0</since>
      public Color WorldAxisColorY
      {
        get { return m_parent.GetColor(UnsafeNativeMethods.DisplayAttrsColor.WyColor); }
        set { m_parent.SetColor(UnsafeNativeMethods.DisplayAttrsColor.WyColor, value); }
      }
      /// <since>5.0</since>
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

      /// <since>5.0</since>
      public double HorizontalViewportScale
      {
        get { return m_parent.GetDouble(UnsafeNativeMethods.DisplayAttributesDouble.HorzScale); }
        set { m_parent.SetDouble(UnsafeNativeMethods.DisplayAttributesDouble.HorzScale, value); }
      }
      /// <since>5.0</since>
      public double VerticalViewportScale
      {
        get { return m_parent.GetDouble(UnsafeNativeMethods.DisplayAttributesDouble.VertScale); }
        set { m_parent.SetDouble(UnsafeNativeMethods.DisplayAttributesDouble.VertScale, value); }
      }

      //bool                  m_bUseLineSmoothing;
      //bool                  LoadBackgroundBitmap(const ON_wString&);
      //int                   m_eAAMode;
      //bool                  m_bFlipGlasses;
    }

    ViewDisplayAttributes m_view_specific_attributes;
    /// <since>5.0</since>
    public ViewDisplayAttributes ViewSpecificAttributes
    {
      get { return m_view_specific_attributes ?? (m_view_specific_attributes = new ViewDisplayAttributes(this)); }
    }
    #endregion

    #region bool
    bool GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetBool(const_ptr_this, which);
    }
    void SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool which, bool b)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetBool(ptr_this, which, b);
    }

    /// <summary>
    /// Gets or sets whether objects ought to be drawn using their assigned rendering material.
    /// </summary>
    /// <since>6.0</since>
    public bool UseAssignedObjectMaterial
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseObjectMaterial); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseObjectMaterial, value); }
    }
    /// <summary>
    /// Gets or sets whether objects ought to be drawn using a custom color.
    /// </summary>
    /// <since>6.0</since>
    public bool UseCustomObjectColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.OverrideObjectColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.OverrideObjectColor, value); }
    }
    /// <summary>
    /// Gets or sets whether objects ought to be drawn using a custom material.
    /// </summary>
    /// <since>6.0</since>
    public bool UseCustomObjectMaterial
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomFrontMaterial); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomFrontMaterial, value); }
    }

    /// <summary>
    /// Gets or sets whether objects ought to be drawn using a custom color for back faces.
    /// </summary>
    /// <since>7.1</since>
    public bool UseCustomObjectColorBackfaces
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.BackOverrideObjectColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.BackOverrideObjectColor, value); }
    }

    /// <summary>
    /// Gets or sets whether objects ought to be drawn using a custom material on backfaces.
    /// </summary>
    /// <since>7.1</since>
    public bool UseCustomObjectMaterialBackfaces
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseBackMaterial); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseBackMaterial, value); }
    }
    #endregion

    #region color
    //int                 m_IsHighlighted;
    //int                 m_nLineThickness;
    //UINT                m_nLinePattern;

    Color GetColor(UnsafeNativeMethods.DisplayAttrsColor which)
    {
      IntPtr pConstThis = ConstPointer();
      int argb = UnsafeNativeMethods.CDisplayPipelineAttributes_GetColor(pConstThis, which);
      return System.Drawing.Color.FromArgb(argb);
    }
    void SetColor(UnsafeNativeMethods.DisplayAttrsColor which, Color c)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetColor(pThis, which, c.ToArgb());
    }
    #endregion

    #region double
    double GetDouble(UnsafeNativeMethods.DisplayAttributesDouble which)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetDouble(pConstThis, which);
    }
    void SetDouble(UnsafeNativeMethods.DisplayAttributesDouble which, double d)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetDouble(pThis, which, d);
    }
    #endregion

    #region float
    float GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat which)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetFloat(pConstThis, which);
    }
    void SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat which, float d)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetFloat(pThis, which, d);
    }
    #endregion

    #region int
    int GetInt(UnsafeNativeMethods.DisplayAttributesInt which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetInt(const_ptr_this, which);
    }
    void SetInt(UnsafeNativeMethods.DisplayAttributesInt which, int i)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetInt(ptr_this, which, i);
    }
    #endregion

    #region byte
    int GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineAttributes_GetByte(const_ptr_this, which);
    }
    void SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte which, int i)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetByte(ptr_this, which, i);
    }
    #endregion
    #region Curves specific attributes...
    /// <summary>Draw curves</summary>
    /// <since>5.1</since>
    public bool ShowCurves
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowCurves); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowCurves, value); }
    }

    /// <summary>
    /// Use a single color for drawing curves
    /// </summary>
    /// <since>6.3</since>
    public bool UseSingleCurveColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleCurveColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleCurveColor, value); }
    }

    /// <since>8.6</since>
    public enum CurveThicknessUse : int
    {
      ObjectWidth = 0,
      Pixels = 1
    }

    /// <summary>
    /// Use a pixel thickness (CurveThickness) or a scale thickness (CurveThicknessScale)
    /// </summary>
    /// <since>8.4</since>
    public CurveThicknessUse CurveThicknessUsage
    {
      get { return (CurveThicknessUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.CurveThicknessUsage); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.CurveThicknessUsage, (int)value); }
    }

    /// <summary>
    /// Sets usage, pixel thickness (CurveThickness) or a scale thickness (CurveThicknessScale)
    /// </summary>
    /// <since>8.7</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void SetCurveThicknessUsage(CurveThicknessUse usage)
    {
      SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.CurveThicknessUsage, (int)usage);
    }

    /// <summary>
    /// Gets current usage, pixel thickness (CurveThickness) or a scale thickness (CurveThicknessScale)
    /// </summary>
    /// <since>8.7</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public CurveThicknessUse GetCurveThicknessUsage()
    {
      return (CurveThicknessUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.CurveThicknessUsage); 
    }

    //bool m_bUseDefaultCurve; -- doesn't appear to be used in display pipelane
    /// <summary>Pixel thickness for curves</summary>
    /// <since>5.1</since>
    public int CurveThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.CurveThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.CurveThickness, (int)value); }
    }

    /// <summary>
    /// Scale thickness for curves
    /// </summary>
    /// <since>8.4</since>
    public float CurveThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.CurveThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.CurveThicknessScale, value); }
    }

    //UINT m_nCurvePattern;
    //bool m_bCurveKappaHair;
    //bool m_bSingleCurveColor;
    /// <summary>Color used for drawing curves</summary>
    /// <since>5.1</since>
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

    /// <summary>Draw shaded meshes and surfaces</summary>
    /// <since>5.1</since>
    public bool ShadingEnabled
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShadeSurface); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShadeSurface, value); }
    }

    /// <summary>Shade using vertex colors.</summary>
    /// <since>6.4</since>
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
    /// <since>6.4</since>
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
    /// <since>6.4</since>
    public bool ShowSurfaceEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSurfaceEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSurfaceEdges, value); }
    }
    /// <summary>Show tangent edges.</summary>
    /// <since>6.4</since>
    public bool ShowTangentEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentEdges, value); }
    }
    /// <summary>Show tangent seams.</summary>
    /// <since>6.4</since>
    public bool ShowTangentSeams
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentSeams); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowTangentSeams, value); }
    }
    //bool m_bShowNakedEdges;
    //bool m_bShowEdgeEndpoints;

    /// <summary> Thickness for surface edges </summary>
    /// <since>6.1</since>
    public int SurfaceEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeThickness, value); }
    }

    enum SurfaceEdgeThicknessFlags : int
    {
      EdgeFixedWidth = 1,
      NakedEdgeFixedWidth = 2,
      IsoFixedWidth = 4 | 8 | 16,
      AllSurfaceFixedWidth = EdgeFixedWidth | NakedEdgeFixedWidth | IsoFixedWidth
    }

    public enum SurfaceThicknessUse : int
    {
      ObjectWidth = 0,
      Pixels = 1
    }

    /// <summary>
    /// Helper function for setting the SurfaceEdgeThicknessFlags
    /// </summary>
    /// <returns></returns>
    /// <since>8.6</since>
    public SurfaceThicknessUse GetSurfaceEdgeThicknessUsage()
    {
      SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)(GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage) & (int)SurfaceEdgeThicknessFlags.EdgeFixedWidth);
      if (currentUsage == SurfaceEdgeThicknessFlags.EdgeFixedWidth) return SurfaceThicknessUse.Pixels;
      return SurfaceThicknessUse.ObjectWidth;
    }

    /// <summary>
    /// Helper function for getting the SurfaceEdgeThicknessFlags
    /// </summary>
    /// <returns></returns>
    public void SetSurfaceEdgeThicknessUsage(SurfaceThicknessUse use)
    {
      SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage);
      if (SurfaceThicknessUse.ObjectWidth == use)
        currentUsage &= ~SurfaceEdgeThicknessFlags.EdgeFixedWidth;
      else
        currentUsage |= SurfaceEdgeThicknessFlags.EdgeFixedWidth;
      SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage, (int)currentUsage);
    }

    /// <since>8.6</since>
    public enum SurfaceNakedEdgeThicknessUse : int
    {
      UseSurfaceEdgeSettings = 0,
      ObjectWidth = 1,
      Pixels = 2
    }

    /// <summary>
    /// This is a helper function that combines setting SurfaceNakeEdgeUseNormalThickness and SurfaceNakedEdgeThicknessUsageFlags settings to correspond
    /// to the behavor of the Settings page.
    /// </summary>
    /// <since>8.6</since>
    public void SetSurfaceNakedEdgeThicknessUsage(SurfaceNakedEdgeThicknessUse use)
    {
      if (SurfaceNakedEdgeThicknessUse.UseSurfaceEdgeSettings == use) SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceNakedEdgeUseNormalEdgeThickness, true);
      else
      {
        SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage);
        SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceNakedEdgeUseNormalEdgeThickness, false);
        if (SurfaceNakedEdgeThicknessUse.ObjectWidth == use)
          currentUsage &= ~SurfaceEdgeThicknessFlags.NakedEdgeFixedWidth;
        else if (SurfaceNakedEdgeThicknessUse.Pixels == use)
          currentUsage |= SurfaceEdgeThicknessFlags.NakedEdgeFixedWidth;
        SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage, (int)currentUsage);
      }
    }

    /// <summary>
    /// This is a helper function that combines getting SurfaceNakeEdgeUseNormalThickness and SurfaceNakedEdgeThicknessUsageFlags settings to correspond
    /// to the behavor of the Settings page. 
    /// </summary>
    /// <since>8.6</since>
    public SurfaceNakedEdgeThicknessUse GetSurfaceNakedEdgeThicknessUsage()
    {
      if (GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceNakedEdgeUseNormalEdgeThickness)) 
        return SurfaceNakedEdgeThicknessUse.UseSurfaceEdgeSettings;
      SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage);
      if (0 != (SurfaceEdgeThicknessFlags.NakedEdgeFixedWidth & currentUsage))
        return SurfaceNakedEdgeThicknessUse.Pixels;
      else
        return SurfaceNakedEdgeThicknessUse.ObjectWidth;
    }

    /// <since>8.6</since>
    public enum SurfaceIsoThicknessUse : int
    {
      ObjectWidth = 0,
      SingleWidthForAllCurves = 1,
      PixelsUV = 2
    }

    /// <summary>
    /// This is a helper function that combines setting IsoThicknessUsed and SurfaceNakedEdgeThicknessUsageFlags settings to correspond
    /// to the behavor of the Settings page. 
    /// </summary>
    /// <since>8.6</since>
    public void SetSurfaceIsoThicknessUsage(SurfaceIsoThicknessUse value)
    {
      SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)(GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage) & (int)SurfaceEdgeThicknessFlags.IsoFixedWidth);

      if (SurfaceIsoThicknessUse.ObjectWidth == value)
        currentUsage &= ~SurfaceEdgeThicknessFlags.IsoFixedWidth;
      else
        currentUsage |= SurfaceEdgeThicknessFlags.IsoFixedWidth;
      SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage, (int)currentUsage);
      SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoThicknessUsed, value == SurfaceIsoThicknessUse.PixelsUV);
    }

    /// <summary>
    /// This is a helper function that combines getting IsoThicknessUsed and SurfaceNakedEdgeThicknessUsageFlags settings to correspond
    /// to the behavor of the Settings page. 
    /// </summary>
    /// <since>8.6</since>
    public SurfaceIsoThicknessUse GetSurfaceIsoThicknessUsage()
    {
      if (GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoThicknessUsed))
        return SurfaceIsoThicknessUse.PixelsUV;
      SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage);
      if (0 != (SurfaceEdgeThicknessFlags.IsoFixedWidth & currentUsage))
        return SurfaceIsoThicknessUse.SingleWidthForAllCurves;
      else
        return SurfaceIsoThicknessUse.ObjectWidth;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <param name="w"></param>
    /// <since>8.6</since>
    public void SetSurfaceIsoApplyPattern(bool u, bool v, bool w)
    {
      UnsafeNativeMethods.CDisplayPipelineAttributes_SetIsoApplyPattern(ConstPointer(), u, v, w);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="u">Gets mode in the u direction</param>
    /// <param name="v">Gets mode in the v direction</param>
    /// <param name="w">Gets mode in the w direction</param>
    /// <since>8.6</since>
    public void GetSurfaceIsoApplyPattern(out bool u, out bool v, out bool w)
    {
      u = v = w = false;
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetIsoApplyPattern(NonConstPointer(), ref u, ref v, ref w);
    }

    /// <since>8.6</since>
    public bool SurfaceIsoShowForFlatFaces
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowFlatSurfaceIsos); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowFlatSurfaceIsos, value); }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <since>8.6</since>
    public bool SurfaceIsoThicknessUsed
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoThicknessUsed); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoThicknessUsed, value); }
    }
 
    /// <summary>
    /// Turn pattern application on or off
    /// </summary>
    /// <since>8.6</since>
    public bool SurfaceEdgeApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceEdgeApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceEdgeApplyPattern, value); }
    }

    /// <summary>
    /// Turn pattern application on or off
    /// </summary>
    /// <since>8.6</since>
    public bool SurfaceNakedEdgeApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceNakedEdgeApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceNakedEdgeApplyPattern, value); }
    }

    /// <summary>
    /// Turn Surface Edge visibility on or off
    /// </summary>
    /// <since>8.6</since>
    public bool ShowSurfaceEdge
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowEdges, value); }
    }

    /// <summary>
    /// Turn Surface Naked Edge visibility on or off
    /// </summary>
    /// <since>8.6</since>
    public bool ShowSurfaceNakedEdge
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowNakedEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowNakedEdges, value); }
    }

    /// <since>8.6</since>
    public enum SurfaceEdgeColorUse : int
    {
      ObjectColor = 0,
      IsocurveColor = 1,
      SingleColorForAll = 2
    }
    /// <since>8.6</since>
    public SurfaceEdgeColorUse SurfaceEdgeColorUsage
    { 
      get { return (SurfaceEdgeColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorUsage, (int)value); }
    }

    /// <since>8.6</since>
    public enum SurfaceNakedEdgeColorUse : int
    {
      UseSurfaceEdgeSettings = 0,
      ObjectColor = 1,
      IsoCurveColor = 2,
      SingleColorForAll = 3
    }

    /// <since>8.6</since>
    public SurfaceNakedEdgeColorUse SurfaceNakedEdgeColorUsage
    {
      get { return (SurfaceNakedEdgeColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorUsage, (int)value); }
    }

    /// <since>8.6</since>
    public enum SurfaceIsoColorUse : int
    {
      ObjectColor = 0,
      SingleColorForAll = 1,
      SpecifiedUV = 2
    }

    /// <summary>
    /// Helper function for setting SurfaceIsoColorsUsed and SurfaceIsoSingleColor
    /// </summary>
    /// <param name="use"></param>
    /// <since>8.6</since>
    public void SetSurfaceIsoColorUsage(SurfaceIsoColorUse use)
    {
      SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed, SurfaceIsoColorUse.SpecifiedUV == use);
      SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor, SurfaceIsoColorUse.SingleColorForAll == use);
    }
    /// <summary>
    /// Helper function for getting SurfaceIsoColorsUsed and SurfaceSingleIsoColor
    /// </summary>
    /// <since>8.6</since>
    public SurfaceIsoColorUse GetSurfaceIsoColorUsage()
    {
      if (GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor)) return SurfaceIsoColorUse.SingleColorForAll;
      else if (GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed)) return SurfaceIsoColorUse.SpecifiedUV;
      return SurfaceIsoColorUse.ObjectColor;
    }

    /// <since>8.6</since>
    public bool SurfaceIsoSingleColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor, value); }
    }

    /// <since>8.6</since>
    public bool SurfaceIsoColorsUsed
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed, value); }
    }

    /// <since>8.6</since>
    public Color SurfaceEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.EdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.EdgeColor, value); }
    }

    /// <since>8.6</since>
    public Color SurfaceNakedEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.NakedEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.NakedEdgeColor, value); }
    }

    /// <since>8.6</since>
    public Color SurfaceIsoUVColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoColor, value); }
    }

    /// <since>8.6</since>
    public Color SurfaceIsoUColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoUColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoUColor, value); }
    }

    /// <since>8.6</since>
    public Color SurfaceIsoVColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoVColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoVColor, value); }
    }

    /// <since>8.6</since>
    public int SurfaceEdgeColorReduction
    {
      get { return SurfaceEdgeColorReductionPercent; }
      set { SurfaceEdgeColorReductionPercent = value; }
    }

    /// <since>8.6</since>
    public int SurfaceNakedAdgeColorReduction
    {
      get { return SurfaceNakedEdgeColorReductionPercent; }
      set { SurfaceNakedEdgeColorReductionPercent = value; }
    }

    /// <since>8.7</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int SurfaceEdgeColorReductionPercent
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorReduction); }
      set { if (value < 0) value = 0; if (value > 100) value = 100; SetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorReduction, value); }
    }

    /// <since>8.7</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int SurfaceNakedEdgeColorReductionPercent
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorReduction); }
      set { if (value < 0) value = 0; if (value > 100) value = 100; SetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorReduction, value); }
    }

    /// <since>8.6</since>
    public int SurfaceNakedEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeThickness, value); }
    }

    /// <since>8.6</since>
    public int SurfaceIsoThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoThickness, value); }
    }

    /// <since>8.6</since>
    public int SurfaceIsoUThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoUThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoUThickness, value); }
    }
    /// <since>8.6</since>
    public int SurfaceIsoVThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoVThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoVThickness, value); }
    }

    /// <since>8.6</since>
    public float SurfaceEdgeThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceEdgeThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceEdgeThicknessScale, value); }
    }
    /// <since>8.6</since>
    public float SurfaceNakedEdgeThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceNakedEdgeThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceNakedEdgeThicknessScale, value); }
    }
    /// <since>8.6</since>
    public float SurfaceIsoThicknessUScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoUThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoUThicknessScale, value); }
    }
    /// <since>8.6</since>
    public float SurfaceIsoThicknessVScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoVThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoVThicknessScale, value); }
   }
    /// <since>8.7</since>
    public float SurfaceIsoThicknessWScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoWThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoWThicknessScale, value); }
    }


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
    /// <since>5.1</since>
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
      /// <since>5.0</since>
      public bool HighlightMeshes
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.HighlightMeshes); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.HighlightMeshes, value); }
      }

      /// <summary>
      /// Color.Empty means that we are NOT using a single color for all mesh wires.
      /// </summary>
      /// <since>5.0</since>
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
          bool single_color = (value != Color.Empty);
          m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleMeshWireColor, single_color);
          if (single_color)
            m_parent.SetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshWireColor, value);
        }
      }

      /// <since>5.0</since>
      public int MeshWireThickness
      {
        get { return m_parent.GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshWireThickness); }
        set { m_parent.SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshWireThickness, value); }
      }

      //UINT m_nMeshWirePattern;

      /// <since>5.0</since>
      public bool ShowMeshWires
      {
        get { return m_parent.GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshWires); }
        set { m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshWires, value); }
      }
      /// <since>5.0</since>
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
    /// <since>5.0</since>
    public MeshDisplayAttributes MeshSpecificAttributes
    {
      get { return m_MeshSpecificAttributes ?? (m_MeshSpecificAttributes = new MeshDisplayAttributes(this)); }
    }

    #endregion

    #region Dimensions & Text specific attributes...
    //bool m_bUseDefaultText;
    /// <summary>Show text.</summary>
    /// <since>6.4</since>
    public bool ShowText
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowText); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowText, value); }
    }
    /// <summary>Show annotations.</summary>
    /// <since>6.4</since>
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
    /// <since>6.4</since>
    public bool ShowLights
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowLights); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowLights, value); }
    }

    /// <since>8.4</since>
    public enum LightingSchema
    {
      None = 0,
      DefaultLighting = 1,
      SceneLighting = 2,
      CustomLighting = 3,
      AmbientOcclusion = 4
    }

    //ELightingScheme m_eLightingScheme;
    /// <since>8.4</since>
    public LightingSchema LightingScheme
    {
      get
      {
        return (LightingSchema)GetInt(UnsafeNativeMethods.DisplayAttributesInt.LightingScheme);
      }
      set
      {
        SetInt(UnsafeNativeMethods.DisplayAttributesInt.LightingScheme, (int)value);
      }
    }

    //bool m_bUseHiddenLights;
    //bool m_bUseLightColor;
    //bool m_bUseDefaultLightingScheme;
    
    //bool m_bUseDefaultEnvironment;
    //int m_nLuminosity;
    /// <since>6.3</since>
    public Color AmbientLightingColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.AmbientColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.AmbientColor, value); }
    }
    //ON_ObjectArray<ON_Light> m_Lights;
    //int m_eShadowMapType;
    //int m_nShadowMapSize;
    //int m_nNumSamples;
    /// <since>6.3</since>
    public Color ShadowColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.ShadowColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.ShadowColor, value); }
    }

    /// <since>8.6</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int ColorReductionPct
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorReduction); }
      set { if (value < 0) value = 0; if (value > 100) value = 100; SetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorReduction, value); }
    }

    //ON_3dVector m_ShadowBias;
    //double m_fShadowBlur;
    /// <summary>Cast shadows.</summary>
    /// <since>6.4</since>
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
    /// <since>6.4</since>
    public bool ShowPoints
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPoints); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPoints, value); }
    }

    /// <since>6.0</since>
    public PointStyle PointStyle
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_GetPointStyle(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_SetPointStyle(ptr_this, value);
      }
    }

    /// <since>6.0</since>
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
    /// <since>6.4</since>
    public bool ShowPointClouds
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPointClouds); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowPointClouds, value); }
    }

    /// <since>8.4</since>
    public PointStyle PointCloudStyle
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_GetPointCloudStyle(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_SetPointCloudStyle(ptr_this, value);
      }
    }

    /// <since>8.4</since>
    public float PointCloudRadius
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.PointCloudSize); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.PointCloudSize, (int)value); }
    }
    #endregion


    #region Draw effect attributes

    /// <summary>
    /// Set a color fade effect to make objects fade a given amount towards a given color.
    /// </summary>
    /// <param name="fadeAmount">The color to fade towards.</param>
    /// <param name="fadeColor">The amount of fade towards the given color (0..1).</param>
    /// <since>8.0</since>
    public void SetColorFadeEffect(in Color fadeColor, in float fadeAmount)
    {
      int fade_color_argb = fadeColor.ToArgb();

      if (m_parent is DisplayPipeline pipeline_parent)
      {
        int current_fade_color_argb = 0;
        float current_fade_amount = 0.0f;
        UnsafeNativeMethods.CDisplayPipelineAttributes_GetColorFadeEffect(ConstPointer(), ref current_fade_color_argb, ref current_fade_amount);

        if (fade_color_argb != current_fade_color_argb || fadeAmount != current_fade_amount)
        {
          pipeline_parent.Flush();
        }
      }

      UnsafeNativeMethods.CDisplayPipelineAttributes_SetColorFadeEffect(ConstPointer(), fade_color_argb, fadeAmount);
    }

    /// <summary>
    /// Get the current color fade effect data.
    /// </summary>
    /// <param name="fadeAmount">The current fade color</param>
    /// <param name="fadeColor">The current fade amount</param>
    /// <since>8.0</since>
    public void GetColorFadeEffect(out Color fadeColor, out float fadeAmount)
    {
      int fade_color_argb = 0;
      fadeAmount = 0.0f;
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetColorFadeEffect(ConstPointer(), ref fade_color_argb, ref fadeAmount);
      fadeColor = Color.FromArgb(fade_color_argb);
    }

    /// <summary>
    /// Returns TRUE if there is a color fade effect enabled with a color fade effect amount
    /// larger than 0.0, FALSE otherwise.
    /// </summary>
    /// <since>8.0</since>
    public bool HasColorFadeEffect()
    {
      return UnsafeNativeMethods.CDisplayPipelineAttributes_HasColorFadeEffect(ConstPointer());
    }

    /// <summary>
    /// Set a dither transparency effect to make objects render with a given amount of 
    /// transparency using a dither effect.
    /// </summary>
    /// <param name="transparencyAmount">The amount of transparency (0..1).</param>
    /// <since>8.0</since>
    public void SetDitherTransparencyEffect(in float transparencyAmount)
    {
      if (m_parent is DisplayPipeline pipeline_parent)
      {
        float current_transparency_amount = 0.0f;
        UnsafeNativeMethods.CDisplayPipelineAttributes_GetDitherTransparencyEffect(ConstPointer(), ref current_transparency_amount);

        if (transparencyAmount != current_transparency_amount)
        {
          pipeline_parent.Flush();
        }
      }

      UnsafeNativeMethods.CDisplayPipelineAttributes_SetDitherTransparencyEffect(ConstPointer(), transparencyAmount);
    }

    /// <summary>
    /// Get the current dither transparency amount.
    /// </summary>
    /// <since>8.0</since>
    public float GetDitherTransparencyEffect()
    {
      float transparency_amount = 0.0f;
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetDitherTransparencyEffect(ConstPointer(), ref transparency_amount);
      return transparency_amount;
    }

    /// <summary>
    /// Returns TRUE if there is a dither transparency effect enabled with a transparency
    /// amount larger than 0.0, FALSE otherwise.
    /// </summary>
    /// <since>8.0</since>
    public bool HasDitherTransparencyEffect()
    {
      return UnsafeNativeMethods.CDisplayPipelineAttributes_HasDitherTransparencyEffect(ConstPointer());
    }

    /// <summary>
    /// Set a diagonal hatch effect to make objects render with diagonal hatch with
    /// a given strength and width in pixels. The effect works by brightening and 
    /// darkening pixels in a diagonal pattern.
    /// </summary>
    /// <param name="hatchStrength">The strength of the hatch effect (0..1).</param>
    /// <param name="hatchWidth">The width of the diagonal hatch in pixels (>= 0).</param>
    /// <since>8.0</since>
    public void SetDiagonalHatchEffect(in float hatchStrength, in float hatchWidth)
    {
      if (m_parent is DisplayPipeline pipeline_parent)
      {
        float current_hatch_strength = 0.0f;
        float current_hatch_width = 0.0f;
        UnsafeNativeMethods.CDisplayPipelineAttributes_GetDiagonalHatchEffect(ConstPointer(), ref current_hatch_strength, ref current_hatch_width);

        if (hatchStrength != current_hatch_strength || hatchWidth != current_hatch_width)
        {
          pipeline_parent.Flush();
        }
      }

      UnsafeNativeMethods.CDisplayPipelineAttributes_SetDiagonalHatchEffect(ConstPointer(), hatchStrength, hatchWidth);
    }

    /// <summary>
    /// Get the current diagonal hatch strength and width in pixels.
    /// </summary>
    /// <param name="hatchStrength">The strength of the hatch effect.</param>
    /// <param name="hatchWidth">The width of the diagonal hatch in pixels.</param>
    /// <since>8.0</since>
    public void GetDiagonalHatchEffect(out float hatchStrength, out float hatchWidth)
    {
      hatchStrength = 0.0f;
      hatchWidth = 0.0f;
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetDiagonalHatchEffect(ConstPointer(), ref hatchStrength, ref hatchWidth);
    }

    /// <summary>
    /// Returns TRUE if there is a diagonal hatch effect enabled with a hatch strength
    /// larger than 0.0, FALSE otherwise.
    /// </summary>
    /// <since>8.0</since>
    public bool HasDiagonalHatchEffect()
    {
      return UnsafeNativeMethods.CDisplayPipelineAttributes_HasDiagonalHatchEffect(ConstPointer());
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

    /// <since>5.0</since>
    public Guid Id
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_GetId(pConstThis);
      }
    }

    /// <since>5.0</since>
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

    /// <since>5.0</since>
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

    /// <since>6.5</since>
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

    /// <since>6.5</since>
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
    /// Get the ID of the real-time display engine attached to the view. This will be
    /// Guid.Empty if no real-time display engine is in use. This can be the case for instance
    /// when starting a _Render session for a real-time viewport integration. That still would
    /// cause this ID to be Guid.Empty.
    /// </summary>
    /// <since>6.0</since>
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
    /// Get or set the real-time passes amount
    /// </summary>
    /// <since>6.0</since>
    public int RealtimeRenderPasses
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.RealtimeRenderPasses); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.RealtimeRenderPasses, value); }
    }

    /// <summary>
    /// Get or set whether the display is used for preview rendering or not.
    /// </summary>
    /// <since>6.0</since>
    public bool ShowRealtimeRenderProgressBar
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowRealtimeRenderProgressBar); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowRealtimeRenderProgressBar, value); }
    }
#endregion

    /// <summary>
    /// Get or set the stereo render context.
    /// </summary>
    /// <since>7.0</since>
    public StereoContext StereoContext
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return (StereoContext)UnsafeNativeMethods.CDisplayPipelineAttributes_GetStereoRenderContext(ptr);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_SetStereoRenderContext(ptr, (UnsafeNativeMethods.StereoRenderContext)value);
      }
    }
    /// <summary>
    /// Get or set the front material shine (0 to Rhino.DocObjects.MaxShine). You must call DisplayModeDescription.UpdateDisplayMode() to commit this change.
    /// </summary>
    /// <since>8.4</since>
    public double FrontMaterialShine
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_GetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.FrontMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Shine);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_SetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.FrontMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Shine, value);
      }
    }
    /// <summary>
    /// Get or set the back material shine (0 to Rhino.DocObjects.MaxShine). You must call DisplayModeDescription.UpdateDisplayMode() to commit this change.
    /// </summary>
    /// <since>8.4</since>
    public double BackMaterialShine
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_GetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.BackMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Shine);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_SetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.BackMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Shine, value);
      }
    }
    /// <summary>
    /// Get or set the front material transparency (0 to 100). You must call DisplayModeDescription.UpdateDisplayMode() to commit this change.
    /// </summary>
    /// <since>8.4</since>
    public double FrontMaterialTransparency
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_GetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.FrontMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Transparency);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_SetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.FrontMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Transparency, value);
      }
    }
    /// <summary>
    /// Get or set the back material transparency (0 to 100). You must call DisplayModeDescription.UpdateDisplayMode() to commit this change.
    /// </summary>
    /// <since>8.4</since>
    public double BackMaterialTransparency
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_GetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.BackMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Transparency);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineAttributes_DisplayAttributeMaterial_SetDouble(ptr, UnsafeNativeMethods.DisplayAttributesMaterialIdx.BackMaterial, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Transparency, value);
      }
    }

    /// <since>8.8</since>
    public Color BackMaterialDiffuseColor
    {
      get 
      {
        return GetColor(UnsafeNativeMethods.DisplayAttrsColor.BackDiffuse);
      }
      set
      {
        SetColor(UnsafeNativeMethods.DisplayAttrsColor.BackDiffuse, value);
      }
    }

    /// <since>8.8</since>
    public bool CullBackfaces
    {
      get
      {
        return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CullBackfaces);
      }
      set
      {
        SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CullBackfaces, value);
      }
    }
    /// <summary>
    /// Color reduction percentage
    /// </summary>
    public int SubDSmoothInteriorColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDSmoothInteriorColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDSmoothInteriorColorReduction, value); }
    }
    /// <summary>
    /// Color reduction percentage
    /// </summary>
    public int SubDCreaseInteriorColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDCreaseInteriorColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDCreaseInteriorColorReduction, value); }
    }
    /// <summary>
    /// Color reduction percentage
    /// </summary>
    public int SubDNonManifoldColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDNonManifoldColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDNonManifoldColorReduction, value); }
    }
    /// <summary>
    /// Color reduction percentage
    /// </summary>
    public int SubDBoundaryColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDBoundaryColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDBoundaryColorReduction, value); }
    }
    /// <summary>
    /// SubD edge color use
    /// </summary>
    public enum SubDEdgeColorUse : int
    {
      ObjectColor = 0,
      SingleColorForAll = 1
    }
    /// <summary>
    /// Edge color usage
    /// </summary>
    public SubDEdgeColorUse SubDSmoothInteriorEdgeColorUsage
    {
      get { return (SubDEdgeColorUse)(UnsafeNativeMethods.DisplayAttributesInt.SubDSmoothInteriorEdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDSmoothInteriorEdgeColorUsage, (int)value); }
    }
    /// <summary>
    /// Edge color usage
    /// </summary>
    public SubDEdgeColorUse SubDCreaseInteriorEdgeColorUsage
    {
      get { return (SubDEdgeColorUse)(UnsafeNativeMethods.DisplayAttributesInt.SubDCreaseInteriorEdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDCreaseInteriorEdgeColorUsage, (int)value); }
    }
    /// <summary>
    /// Edge color usage
    /// </summary>
    public SubDEdgeColorUse SubDNonManifoldEdgeColorUsage
    {
      get { return (SubDEdgeColorUse)(UnsafeNativeMethods.DisplayAttributesInt.SubDNonManifoldEdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDNonManifoldEdgeColorUsage, (int)value); }
    }
    /// <summary>
    /// Edge color usage
    /// </summary>
    public SubDEdgeColorUse SubDBoundaryEdgeColorUsage
    {
      get { return (SubDEdgeColorUse)(UnsafeNativeMethods.DisplayAttributesInt.SubDBoundaryEdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDBoundaryEdgeColorUsage, (int)value); }
    }
    /// <summary>
    /// SubD replection plane color use
    /// </summary>
    public enum SubDReflectionPlaneColorUse : int
    {
      ObjectColor = 0,
      CustomColor = 1,
      SingleColorForAll = 2
    }
    /// <summary>
    /// SubD replection plane color use
    /// </summary>
    public SubDReflectionPlaneColorUse SubDReflectionPlaneColorUsage
    {
      get { return (SubDReflectionPlaneColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDReflectionPlaneColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDReflectionPlaneColorUsage, (int)value); }
    }
    /// <summary>
    /// SubD replection plane color reduction percentage
    /// </summary>
    public int SubDReflectionPlaneColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDReflectionPlaneColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.SubDReflectionPlaneColorReduction, value); }
    }
    /// <summary>
    /// Mesh edge width in pixels
    /// </summary>
    public int MeshEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshEdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshEdgeThickness, value); }
    }
    /// <summary>
    /// Naked mesh edge width in pixels.}
    /// </summary>
    public int MeshNakedEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNakedEdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNakedEdgeThickness, value); }
    }
    /// <summary>
    /// Non-manifold mesh edge width in pixels
    /// </summary>
    public int MeshNonmanifoldEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNonmanifoldEdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNonmanifoldEdgeThickness, value); }
    }
    /// <summary>
    /// Mesh vertex size in pixels
    /// </summary>
    public int MeshVertexSize
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshVertexSize); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshVertexSize, value); }
    }
    /// <summary>
    /// The darken percentage of the color
    /// </summary>
    public int MeshEdgeColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshEdgeColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshEdgeColorReduction, value); }
    }
    /// <summary>
    /// The darken percentage of the color
    /// </summary>
    public int MeshNakedEdgeColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNakedEdgeColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNakedEdgeColorReduction, value); }
    }
    /// <summary>
    /// The darken percentage of the color
    /// </summary>
    public int MeshNonmanifoldEdgeColorReduction
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNonmanifoldEdgeColorReduction); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.MeshNonmanifoldEdgeColorReduction, value); }
    }
    /// <summary>
    /// Edge thickness scale
    /// </summary>
    public float SubDSmoothInteriorThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDSmoothInteriorThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDSmoothInteriorThicknessScale, value); }
    }
    /// <summary>
    /// Edge thickness scale
    /// </summary>
    public float SubDCreaseInteriorThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDCreaseInteriorThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDCreaseInteriorThicknessScale, value); }
    }
    /// <summary>
    /// Edge thickness scale
    /// </summary>
    public float SubDNonManifoldThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDNonManifoldThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDNonManifoldThicknessScale, value); }
    }
    /// <summary>
    /// Edge thickness scale
    /// </summary>
    public float SubDBoundaryThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDBoundaryThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDBoundaryThicknessScale, value); }
    }
    /// <summary>
    /// Edge thickness (pixels).
    /// </summary>
    // This isn't an int for some reason, even though the UI acts like it is and it would be more consistent
    public float SubDSmoothInteriorEdgeThickness
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDSmoothInteriorEdgeThickness); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDSmoothInteriorEdgeThickness, value); }
    }
    /// <summary>
    /// Edge thickness (pixels).
    /// </summary>
    // This isn't an int for some reason, even though the UI acts like it is and it would be more consistent
    public float SubDCreaseInteriorEdgeThickness
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDCreaseInteriorEdgeThickness); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDCreaseInteriorEdgeThickness, value); }
    }
    /// <summary>
    /// Edge thickness (pixels).
    /// </summary>
    // This isn't an int for some reason, even though the UI acts like it is and it would be more consistent
    public float SubDNonManifoldEdgeThickness
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDNonManifoldEdgeThickness); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDNonManifoldEdgeThickness, value); }
    }
    /// <summary>
    /// Edge thickness (pixels).
    /// </summary>
    // This isn't an int for some reason, even though the UI acts like it is and it would be more consistent
    public float SubDBoundaryEdgeThickness
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDBoundaryEdgeThickness); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SubDBoundaryEdgeThickness, value); }
    }

    public enum SubDThicknessUse : int
    {
      ObjectWidth = 0,
      Pixels = 1
    }

    /// <summary>
    /// Thickness usage, pixel thickness or a scale thickness
    /// </summary>
    public SubDThicknessUse SubDThicknessUsage
    {
      get { return (SubDThicknessUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDThicknessUsage); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDThicknessUsage, (int)value); }
    }
    public SubDThicknessUse SubDSmoothInteriorThicknessUsage
    {
      get { return (SubDThicknessUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDSmoothInteriorThicknessUsage); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDSmoothInteriorThicknessUsage, (int)value); }
    }
    public SubDThicknessUse SubDCreaseInteriorThicknessUsage
    {
      get { return (SubDThicknessUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDCreaseInteriorThicknessUsage); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDCreaseInteriorThicknessUsage, (int)value); }
    }
    public SubDThicknessUse SubDNonManifoldThicknessUsage
    {
      get { return (SubDThicknessUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDNonManifoldThicknessUsage); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDNonManifoldThicknessUsage, (int)value); }
    }
    public SubDThicknessUse SubDBoundaryThicknessUsage
    {
      get { return (SubDThicknessUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDBoundaryThicknessUsage); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SubDBoundaryThicknessUsage, (int)value); }
    }
    /// <summary>
    /// Apply pattern to the edge
    /// </summary>
    public bool SubDSmoothInteriorApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDSmoothInteriorApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDSmoothInteriorApplyPattern, value); }
    }
    /// <summary>
    /// Apply pattern to the edge
    /// </summary>
    public bool SubDCreaseInteriorApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDCreaseInteriorApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDCreaseInteriorApplyPattern, value); }
    }
    /// <summary>
    /// Apply pattern to the edge
    /// </summary>
    public bool SubDNonManifoldApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDNonManifoldApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDNonManifoldApplyPattern, value); }
    }
    /// <summary>
    /// Apply pattern to the edge
    /// </summary>
    public bool SubDBoundaryApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDBoundaryApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDBoundaryApplyPattern, value); }
    }
    /// <summary>
    /// Apply Turnh on or off the reflection plane axis line
    /// </summary>
    public bool SubDReflectionPlaneAxisLineOn
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDReflectionPlaneAxisLineOn); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDReflectionPlaneAxisLineOn, value); }
    }
    /// <summary>
    /// Edge color
    /// </summary>
    public Color SubDSmoothInteriorEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDSmoothInteriorEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDSmoothInteriorEdgeColor, value); }
    }
    /// <summary>
    /// Edge color
    /// </summary>
    public Color SubDCreaseInteriorEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDCreaseInteriorEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDCreaseInteriorEdgeColor, value); }
    }
    /// <summary>
    /// Edge color
    /// </summary>
    public Color SubDNonManifoldEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDNonManifoldEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDNonManifoldEdgeColor, value); }
    }
    /// <summary>
    /// Edge color
    /// </summary>
    public Color SubDBoundaryEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDBoundaryEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDBoundaryEdgeColor, value); }
    }
    /// <summary>
    /// Reflection axis line color
    /// </summary>
    public Color SubDReflectionAxisLineColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDReflectionAxisLineColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDReflectionAxisLineColor, value); }
    }
    /// <summary>
    /// Reflection plane color
    /// </summary>
    public Color SubDReflectionPlaneColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDReflectionPlaneColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.SubDReflectionPlaneColor, value); }
    }
    /// <summary>
    /// Sets the mesh edge color
    /// </summary>
    public Color MeshEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshEdgeColor, value); }
    }
    /// <summary>
    /// Sets the naked edge color
    /// </summary>
    public Color MeshNakedEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshNakedEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshNakedEdgeColor, value); }
    }
    /// <summary>
    /// Sets the nonmanifold edge color
    /// </summary>
    public Color MeshNonmanifoldEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshNonmanifoldEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.MeshNonmanifoldEdgeColor, value); }
    }

    /// <summary>
    /// Height above the world XY plane in model units
    /// </summary>
    public double CustomGroundPlaneAltitude
    {
      get { return GetDouble(UnsafeNativeMethods.DisplayAttributesDouble.CustomGroundPlaneAltitude); }
      set { SetDouble(UnsafeNativeMethods.DisplayAttributesDouble.CustomGroundPlaneAltitude, value); }
    }
    /// <summary>
    /// Turn the custom ground plane on or off
    /// </summary>
    public bool CustomGroundPlaneOn
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomGroundPlaneOn); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomGroundPlaneOn, value); }
    }
    /// <summary>
    /// Makes the ground plane transparent, but allows shadows to still be cast on it.
    /// </summary>
    public bool CustomGroundPlaneShadowOnly
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomGroundPlaneShadowOnly); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomGroundPlaneShadowOnly, value); }
    }

    /// <summary>
    /// Turns on auto-elevation that moves Ground Plane to the lowest point of the objects in the model.
    /// </summary>
    public bool CustomGroundPlaneAutomaticAltitude
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomGroundPlaneAutomaticAltitude); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CustomGroundPlaneAutomaticAltitude, value); }
    }
    /// <summary>
    /// Set visibility of SubD smooth edges.
    /// </summary>
    public bool ShowSubDEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDEdges, value); }
    }
    /// <summary>
    /// Set visibility of SubD creased edges.
    /// </summary>
    public bool ShowSubDCreases
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDCreases); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDCreases, value); }
    }
    /// <summary>
    /// Set visibility of SubD naked edges.
    /// </summary>
    public bool ShowSubDBoundary
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDBoundary); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDBoundary, value); }
    }
    /// <summary>
    /// Turn on/off color differentiation of SubD symmetry children.
    /// </summary>
    public bool ShowSubDNonmanifoldEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDNonmanifoldEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowSubDNonmanifoldEdges, value); }
    }
    public bool ShowSubDReflectionPlanePreview
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDReflectedPreview); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SubDReflectedPreview, value); }
    }
    /// <summary>
    /// Display mesh edges on/off
    /// </summary>
    public bool ShowMeshEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshEdges, value); }
    }
    /// <summary>
    /// Display mesh naked edges on/off
    /// </summary>
    public bool ShowMeshNakedEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshNakedEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshNakedEdges, value); }
    }

    /// <summary>
    /// Display mesh manifold edges on/off
    /// </summary>
    public bool ShowMeshNonmanifoldEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshNonmanifoldEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowMeshNonmanifoldEdges, value); }
    }
    /// <summary>
    /// Draw lights using light color
    /// </summary>
    public bool UseLightColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseLightColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.UseLightColor, value); }
    }
    /// <summary>
    /// When a clipping plane intersects a 3-D object and the section is closed, the section is filled.
    /// </summary>
    public bool ShowClippingFills
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingShowXSurface); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingShowXSurface, value); }
    }
    /// <summary>
    /// Shows the edges between the clipping plane and clipped objects.
    /// </summary>
    public bool ShowClippingEdges
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingShowXEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingShowXEdges, value); }
    }
    /// <summary>
    /// Shades the selected clipping plane.
    /// </summary>
    public bool ClippingShadeSelectedPlane
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingShowCP); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingShowCP, value); }
    }
    /// <summary>
    /// Clips the highlight wires. Shaded selections always clip.
    /// </summary>
    public bool ClipSelectionHighlight
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingClipSelected); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ClippingClipSelected, value); }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ClippingPlaneFillColorUse : int
    {
      /// <summary>
      /// Follows how the object displays in the viewport.
      /// </summary>
      ViewportColor = 0,
      /// <summary>
      /// Uses the object's render material.
      /// </summary>
      RenderMaterialColor = 1,
      /// <summary>
      /// Uses the clipping plane's color or layer color property.
      /// </summary>
      PlaneMaterialColor = 2,
      /// <summary>
      /// Solid color
      /// </summary>
      SolidColor = 3
    }

    /// <summary>
    /// Specifies how the color for the clipping plane object fill is determined.
    /// </summary>
    public ClippingPlaneFillColorUse ClippingPlaneFillColorUsage
    {
      get { return (ClippingPlaneFillColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingSurfaceUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingSurfaceUsage, (int)value); }
    }
    /// <summary>
    /// Clipping plane fill color
    /// </summary>
    public Color ClippingFillColor
    {
      get { return (Color)GetColor(UnsafeNativeMethods.DisplayAttrsColor.ClippingSurfaceColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.ClippingSurfaceColor, value); }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ClippingEdgeColorUse : int
    {
      /// <summary>
      /// Uses the clipping plane's color (object or layer).
      /// </summary>
      PlaneColor = 0,
      /// <summary>
      /// Solid color
      /// </summary>
      SolidColor = 1,
      /// <summary>
      /// Uses the object's color (object or layer).
      /// </summary>
      ObjectColor = 2
    }
    /// <summary>
    /// Specifies how the color for the Edges is determined
    /// </summary>
    public ClippingEdgeColorUse ClippingEdgeColorUsage
    {
      get { return (ClippingEdgeColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingEdgesUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingEdgesUsage, (int)value); }
    }
    /// <summary>
    /// Clipping edge color
    /// </summary>
    public Color ClippingEdgeColor
    {
      get { return (Color)GetColor(UnsafeNativeMethods.DisplayAttrsColor.ClippingEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.ClippingEdgeColor, value); }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ClippingShadeColorUse : int
    {
      /// <summary>
      /// Uses the clipping plane's color (object or layer).
      /// </summary>
      PlaneColor = 0,
      /// <summary>
      /// Uses the plane's render material (object or layer).
      /// </summary>
      PlaneMaterialColor = 1,
      /// <summary>
      /// Solid Color
      /// </summary>
      SolidColor = 2
    }
    /// <summary>
    /// Specifies how to shade the clipping plane
    /// </summary>
    public ClippingShadeColorUse ClippingShadeColorUsage
    {
      get { return (ClippingShadeColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingCPUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingCPUsage, (int)value); }
    }
    /// <summary>
    /// Clipping plane solid color
    /// </summary>
    public Color ClippingShadeColor
    {
      get { return (Color)GetColor(UnsafeNativeMethods.DisplayAttrsColor.ClippingCPColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.ClippingCPColor, value); }
    }
    /// <summary>
    /// Edge thickness in pixels.
    /// </summary>
    public int ClippingEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingEdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingEdgeThickness, value); }
    }
    /// <summary>
    /// Specifies the clipping plane transparency percentage.
    /// </summary>
    public int ClippingShadeTransparency
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingCPTrans); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.ClippingCPTrans, value); }
    }
    /// <summary>
    /// When enabled, the appearances of clipping fills and edges are based on objects' section style properties.
    /// </summary>
    public bool UseSectionStyles
    {
      get { return (1 == GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.ClipSectionUsage)); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.ClipSectionUsage, value ? 1 : 0); }
    }

    /// <summary>
    /// The width of the control polygon lines in pixels.
    /// </summary>
    public int ControlPolygonWireThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.CPWireThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.CPWireThickness, value); }
    }
    /// <summary>
    /// PointStyle for the control polygon. Supported values are ControlPoint, RoundControlPoint, VariableDot, and RoundDot
    /// </summary>
    public PointStyle ControlPolygonStyle
    {
      get { return (PointStyle)GetInt(UnsafeNativeMethods.DisplayAttributesInt.CVStyle); }
      set
      {
        if (value != PointStyle.RoundControlPoint && value != PointStyle.VariableDot && value != PointStyle.RoundDot)
          value = PointStyle.ControlPoint;

        SetInt(UnsafeNativeMethods.DisplayAttributesInt.CVStyle, (int)value);
      }
    }
    /// <summary>
    /// The control point size in pixels.
    /// </summary>
    public int ControlPolygonGripSize
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.CVSize); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.CVSize, value); }
    }

    /// <summary>
    /// LockedObjectTransparency.
    /// </summary>
    public int LockedObjectTransparency
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.LockedTrans); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.LockedTrans, value); }
    }

    public enum LockedObjectUse : int
    {
      /// <summary>
      /// Uses the object's specified attributes.
      /// </summary>
      UseObjectAttributes = 0,
      /// <summary>
      /// Use specified lock color
      /// </summary>
      SpecifyColor = 1,
      /// <summary>
      /// Use settings specified in Appearance: Colors Options.
      /// </summary>
      UseAppSettings = 2
    }

    /// <summary>
    /// Set asource of display attributes for locked objects
    /// </summary>
    public LockedObjectUse LockedObjectUsage
    {
      get { return (LockedObjectUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.LockedUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.LockedUsage, (int)value); }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum DynamicDisplayUse
    {
      /// <summary>
      /// Use system default settings.
      /// </summary>
      UseAppSettings = 0,
      /// <summary>
      /// Reduces the display of objects to their bounding boxes. This can speed up the display on large models.
      /// </summary>
      DisplayObjectBoundingBox = 1
    }
    /// <summary>
    /// Sets the appearance of objects in the display
    /// </summary>
    public DynamicDisplayUse DynamicDisplayUsage
    {
      get { return (DynamicDisplayUse)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.BBoxMode); }
      set { SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.BBoxMode, (int)value); }
    }
    /// <summary>
    /// Shades entire object with highlight color.
    /// </summary>
    public bool HighlightSurfaces
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.HighlightSurfaces); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.HighlightSurfaces, value); }
    }
    /// <summary>
    /// Use dotted / solid lines
    /// </summary>
    public bool ControlPolygonUseSolidLines
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPSolidLines); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPSolidLines, value); }
    }
    /// <summary>
    /// Specifies a color for the control polygon.
    /// </summary>
    /// <value>true = Use a specified color for all control polygons.</value>
    /// <value>false = Use the color specified in the object's Properties.</value>
    public bool ControlPolygonUseFixedSingleColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPSingleColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPSingleColor, value); }
    }
    /// <summary>
    /// Shows the control points while the control polygon is displayed.
    /// </summary>
    public bool ControlPolygonShowPoints
    {
      get { return !GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHidePoints); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHidePoints, !value); }
    }
    /// <summary>
    /// Shows the object while the control polygon is displayed.
    /// </summary>
    public bool ControlPolygonShowSurface
    {
      get { return !GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHideSurface); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHideSurface, !value); }
    }
    /// <summary>
    /// Shows the control polygon and only shows the control points.
    /// </summary>
    public bool ControlPolygonShow
    {
      get { return !GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHidden); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHidden, !value); }
    }
    /// <summary>
    /// Highlights the segments of the control polygon on either side of the control points.
    /// </summary>
    public bool ControlPolygonHighlight
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHighlight); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.CPHighlight, value); }
    }
    /// <summary>
    /// Set locked appearance
    /// </summary>
    /// <value>true = Locked objects appear transparent</value>
    /// <value>false = Locked objects appear solid</value>
    public bool GhostLockedObjects
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.GhostLockedObjects); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.GhostLockedObjects, value); }
    }
    /// <summary>
    /// Applies the settings for locked objects to locked layers.
    /// </summary>
    public bool LayersFollowLockUsage
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.LayersFollowLockUsage); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.LayersFollowLockUsage, value); }
    }
    /// <summary>
    /// Control polygon color
    /// </summary>
    public Color ControlPolygonColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.CPColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.CPColor, value); }
    }
    /// <summary>
    /// Locked Object Color
    /// </summary>
    public Color LockedColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.LockedColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.LockedColor, value); }
    }

    /// <summary>
    /// Enable shadows
    /// </summary>
    public bool ShadowsOn
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.Shadows); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.Shadows, value); }
    }
    /// <summary>
    /// Shadow intensity (percentage 0-100)
    /// </summary>
    public int ShadowIntensity
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.ShadowIntensity); }
      set 
      {
        if (value < 0) value = 0;
        else if (value > 100) value = 100;
        SetInt(UnsafeNativeMethods.DisplayAttributesInt.ShadowIntensity, value); 
      }
    }
    /// <summary>
    /// Value from 1 to 16384 indicating how much memory is to be allocated. Actual memory use
    /// is ShadowMemoryUsage*ShadowMemoryUsage*4.
    /// </summary>
    public int ShadowMemoryUsage
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.ShadowMemoryUsage); }
      set
      {
        if (value < 1) value = 1;
        else if (value > 16384) value = 16384;
        SetInt(UnsafeNativeMethods.DisplayAttributesInt.ShadowMemoryUsage, value);
      }
    }
    /// <summary>
    /// Skylight shadow quality, from 0 (lowest) to 8 (highest)
    /// </summary>
    public int SkylightShadowQuality
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.SkylightQuality); }
      set
      {
        if (value < 0) value = 0;
        else if (value > 8) value = 8;
        SetInt(UnsafeNativeMethods.DisplayAttributesInt.SkylightQuality, value);
      }
    }

    // might expose this stuff in the future
    private const int CubeShadows = 50;
    private const int DrmShadows = 100;
    private enum ShadowMapType : uint
    {
      None = 0,
      Normal = 1,
      Sampled = 2,
      PCF = 3,
      Dithered = 4,

      GI = 5,

      NormalCube = Normal + CubeShadows,
      SampledCube = Sampled + CubeShadows,
      PCFCube = PCF + CubeShadows,
      SMDitheredCube = Dithered + CubeShadows,

      Force32bit = 0xFFFFFFFF
    }
    /// <summary>
    /// Soft edge quality, from 0 (none/faster) to 12 (softer/slower)
    /// </summary>
    public int ShadowSoftEdgeQuality
    {
      get 
      {
        if (GetInt(UnsafeNativeMethods.DisplayAttributesInt.ShadowMapType) == (int)ShadowMapType.Normal)
          return 0;
        else
        {
          return GetInt(UnsafeNativeMethods.DisplayAttributesInt.NumSamples);
        }
      }
      set
      {
        if (value < 0) value = 0;
        if (value > 12) value = 12;

        if (value == 0)
        {
          SetInt(UnsafeNativeMethods.DisplayAttributesInt.ShadowMapType, (int)ShadowMapType.Normal);
          SetInt(UnsafeNativeMethods.DisplayAttributesInt.NumSamples, 0);
        }
        else
        {
          SetInt(UnsafeNativeMethods.DisplayAttributesInt.ShadowMapType, (int)ShadowMapType.Sampled);
          SetInt(UnsafeNativeMethods.DisplayAttributesInt.NumSamples, value);
        }
      }
    }
    /// <summary>
    /// Set blurring from 0 (no blurring) to 16 (maximum blurring)
    /// </summary>
    public double ShadowEdgeBlur
    {
      get { return GetDouble(UnsafeNativeMethods.DisplayAttributesDouble.ShadowBlur); }
      set 
      {
        if (value < 0.0f) value = 0.0f;
        if (value > 16.0f) value = 16.0f;
        SetDouble(UnsafeNativeMethods.DisplayAttributesDouble.ShadowBlur, value); 
      }
    }
    /// <summary>
    /// ShadowBiasX (Self shadowing artifacts) from 0 (dirty) to 50 (cleaner).
    /// </summary>
    public double ShadowBiasX
    {
      get { return GetDouble(UnsafeNativeMethods.DisplayAttributesDouble.ShadowBiasX); }
      set 
      {
        if (value < 0.0) value = 0.0;
        if (value > 50.0) value = 50.0;
        SetDouble(UnsafeNativeMethods.DisplayAttributesDouble.ShadowBiasX, value); }
    }
    /// <summary>
    /// Transparency tolerance from 0 (never cast shadows) to 100 (always case shadows)
    /// </summary>
    public int ShadowTransparencyTolerance
    {
      get { return (int)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.TransparencyTolerance); }
      set
      {
        if (value < 0) value = 0;
        if (value > 100) value = 100;
        SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.TransparencyTolerance, value);
      }
    }
    /// <summary>
    /// Camera-based shadow clipping radius
    /// </summary>
    public float ShadowClippingRadius
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.ShadowClippingRadius); }
      set
      {
        int current = GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.ShadowClippingUsage);
        if (value == 0.0)
          current &= 0xF0;
        else
          current |= 0x01;

        SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.ShadowClippingRadius, value);
      }
    }

    /// <summary>
    /// If true, shadows ignore user-defined clipping planes
    /// </summary>
    public bool ShadowsIgnoreUserDefinedClippingPlanes
    {
      get { return (0 != (0x10 & GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.ShadowClippingUsage))); }
      set 
      {
        int current = GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.ShadowClippingUsage);
        if (value) current |= 0x10;
        if (!value) current &= ~0x10;
        SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.ShadowClippingUsage, current); 
      }
    }

    /// <summary>
    /// Size of axes as a percentage of the grid extents.
    /// </summary>
    public int AxesSizePercentage
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.AxesPercentage); }
      set
      {
        if (value < 0) value = 0;
        else if (value > 100) value = 100;
        SetInt(UnsafeNativeMethods.DisplayAttributesInt.AxesPercentage, value);
      }
    }

    /// <summary>
    /// Transparency of the grid, percentage (0-100)
    /// </summary>
    public int GridTransparency
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.GridTrans); }
      set
      {
        if (value < 0) value = 0;
        else if (value > 100) value = 100;
        SetInt(UnsafeNativeMethods.DisplayAttributesInt.GridTrans, value);
      }
    }

    /// <summary>
    /// Transparency of the grid plane, percentage (0-100)
    /// </summary>
    public int GridPlaneTransparency
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.GridPlaneTrans); }
      set
      {
        if (value < 0) value = 0;
        else if (value > 100) value = 100;
        SetInt(UnsafeNativeMethods.DisplayAttributesInt.GridPlaneTrans, value);
      }
    }
    public enum GridPlaneVisibilityMode : int
    {
      /// <summary>
      /// Show only when grid is on
      /// </summary>
      ShowOnlyIfGridVisible = 0,
      /// <summary>
      /// Show always
      /// </summary>
      AlwaysShow = 1
    }
    /// <summary>
    /// Set when to show the grid plane
    /// </summary>
    public GridPlaneVisibilityMode GridPlaneVisibility
    {
      get { return (GridPlaneVisibilityMode)GetInt(UnsafeNativeMethods.DisplayAttributesInt.PlaneVisibility); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.PlaneVisibility, (int)value); }
    }

    public enum WorldAxesIconColorUse : int
    {
      /// <summary>
      /// Use default setting
      /// </summary>
      UseApplicationSettings = 0,
      /// <summary>
      /// Set colors for grid axes in Appearance: Colors Options
      /// </summary>
      SameAsGridAxesColors = 1,
      /// <summary>
      /// Use specified custom colors
      /// </summary>
      Custom = 2
    }
    public WorldAxesIconColorUse WorldAxesIconColorUsage
    {
      get { return (WorldAxesIconColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.WorldAxesColor); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.WorldAxesColor, (int)value); }
    }

    /// <summary>
    /// If true, use the grid thin line color in App settings
    /// </summary>
    public bool PlaneUsesGridColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.PlaneUsesGridColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.PlaneUsesGridColor, value); }
    }

    /// <summary>
    /// The color of the grid plane
    /// </summary>
    public Color GridPlaneColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.GridPlaneColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.GridPlaneColor, value); }
    }

  }
}

#endif
