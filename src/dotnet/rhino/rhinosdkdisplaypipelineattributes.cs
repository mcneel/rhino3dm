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
      if (m_ptr_attributes!=IntPtr.Zero && !m_dontdelete)
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

    public enum CurveThicknessUse : int
    {
      ObjectWidth = 0,
      Pixels = 1
    }

    /// <summary>
    /// Sets usage, pixel thickness (CurveThickness) or a scale thickness (CurveThicknessScale)
    /// </summary>
    public void SetCurveThicknessUsage(CurveThicknessUse usage)
    {
      SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.CurveThicknessUsage, (int)usage);
    }
    /// <summary>
    /// Gets current usage, pixel thickness (CurveThickness) or a scale thickness (CurveThicknessScale)
    /// </summary>
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

    public enum SurfaceEdgeThicknessUse : int
    {
      ObjectWidth = 0,
      Pixels = 1,
    }

    /// <summary>
    /// Helper function for setting the SurfaceEdgeThicknessFlags
    /// </summary>
    /// <returns></returns>
    public SurfaceEdgeThicknessUse GetSurfaceEdgeThicknessUsage()
    {
      SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)(GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage) & (int)SurfaceEdgeThicknessFlags.EdgeFixedWidth);
      if (currentUsage == SurfaceEdgeThicknessFlags.EdgeFixedWidth) return SurfaceEdgeThicknessUse.Pixels;
      return SurfaceEdgeThicknessUse.ObjectWidth;
    }

    /// <summary>
    /// Helper function for getting the SurfaceEdgeThicknessFlags
    /// </summary>
    /// <returns></returns>
    public void SetSurfaceEdgeThicknessUsage(SurfaceEdgeThicknessUse use)
    {
      SurfaceEdgeThicknessFlags currentUsage = (SurfaceEdgeThicknessFlags)GetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage);
      if (SurfaceEdgeThicknessUse.ObjectWidth == use)
        currentUsage &= ~SurfaceEdgeThicknessFlags.EdgeFixedWidth;
      else
        currentUsage |= SurfaceEdgeThicknessFlags.EdgeFixedWidth;
      SetByte(UnsafeNativeMethods.DisplayPipelineAttributesByte.SurfaceThicknessUsage, (int)currentUsage);
    }

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
    public void GetSurfaceIsoApplyPattern(out bool u, out bool v, out bool w)
    {
      u = v = w = false;
      UnsafeNativeMethods.CDisplayPipelineAttributes_GetIsoApplyPattern(NonConstPointer(), ref u, ref v, ref w);
    }

    public bool SurfaceIsoShowForFlatFaces
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowFlatSurfaceIsos); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowFlatSurfaceIsos, value); }
    }
    /// <summary>
    /// 
    /// </summary>
    public bool SurfaceIsoThicknessUsed
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoThicknessUsed); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoThicknessUsed, value); }
    }
 
    /// <summary>
    /// Turn pattern application on or off
    /// </summary>
    public bool SurfaceEdgeApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceEdgeApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceEdgeApplyPattern, value); }
    }

    /// <summary>
    /// Turn pattern application on or off
    /// </summary>
    public bool SurfaceNakedEdgeApplyPattern
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceNakedEdgeApplyPattern); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SurfaceNakedEdgeApplyPattern, value); }
    }

    /// <summary>
    /// Turn Surface Edge visibility on or off
    /// </summary>
    public bool ShowSurfaceEdge
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowEdges, value);  }
    }

    /// <summary>
    /// Turn Surface Naked Edge visibility on or off
    /// </summary>
    public bool ShowSurfaceNakedEdge
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowNakedEdges); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.ShowNakedEdges, value); }
    }

    public enum SurfaceEdgeColorUse : int
    {
      ObjectColor = 0,
      IsocurveColor = 1,
      SingleColorForAll = 2
    }
    public SurfaceEdgeColorUse SurfaceEdgeColorUsage
    { 
      get { return (SurfaceEdgeColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorUsage, (int)value);  }
    }

    public enum SurfaceNakedEdgeColorUse : int
    {
      UseSurfaceEdgeSettings = 0,
      ObjectColor = 1,
      IsoCurveColor = 2,
      SingleColorForAll = 3
    }

    public SurfaceNakedEdgeColorUse SurfaceNakedEdgeColorUsage
    {
      get { return (SurfaceNakedEdgeColorUse)GetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorUsage); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorUsage, (int)value); }
    }

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
    public void SetSurfaceIsoColorUsage(SurfaceIsoColorUse use)
    {
      SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed, SurfaceIsoColorUse.SpecifiedUV == use);
      SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor, SurfaceIsoColorUse.SingleColorForAll == use);
    }
    /// <summary>
    /// Helper function for getting SurfaceIsoColorsUsed and SurfaceSingleIsoColor
    /// </summary>
    public SurfaceIsoColorUse GetSurfaceIsoColorUsage()
    {
      if (GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor)) return SurfaceIsoColorUse.SingleColorForAll;
      else if (GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed)) return SurfaceIsoColorUse.SpecifiedUV;
      return SurfaceIsoColorUse.ObjectColor;
    }

    public bool SurfaceIsoSingleColor
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleIsoColor, value);  }
    }

    public bool SurfaceIsoColorsUsed
    {
      get { return GetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed); }
      set { SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.IsoColorsUsed, value); }
    }

    public Color SurfaceEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.EdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.EdgeColor, value); }
    }

    public Color SurfaceNakedEdgeColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.NakedEdgeColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.NakedEdgeColor, value); }
    }

    public Color SurfaceIsoUVColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoColor, value); }
    }

    public Color SurfaceIsoUColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoUColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoUColor, value); }
    }

    public Color SurfaceIsoVColor
    {
      get { return GetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoVColor); }
      set { SetColor(UnsafeNativeMethods.DisplayAttrsColor.IsoVColor, value); }
    }

    public int SurfaceEdgeColorReductionPercent
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorReduction); }
      set { if (value < 0) value = 0; if (value > 100) value = 100; SetInt(UnsafeNativeMethods.DisplayAttributesInt.EdgeColorReduction, value); }
    }

    public int SurfaceNakedEdgeColorReductionPercent
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorReduction); }
      set { if (value < 0) value = 0; if (value > 100) value = 100; SetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeColorReduction, value); }
    }

    public int SurfaceNakedEdgeThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.NakedEdgeThickness, value); }
    }

    public int SurfaceIsoThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoThickness, value); }
    }

    public int SurfaceIsoUThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoUThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoUThickness, value); }
    }
    public int SurfaceIsoVThickness
    {
      get { return GetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoVThickness); }
      set { SetInt(UnsafeNativeMethods.DisplayAttributesInt.IsoVThickness, value); }
    }

    public float SurfaceEdgeThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceEdgeThicknessScale);  }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceEdgeThicknessScale, value);  }
    }
    public float SurfaceNakedEdgeThicknessScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceNakedEdgeThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceNakedEdgeThicknessScale, value); }
    }
    public float SurfaceIsoThicknessUScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoUThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoUThicknessScale, value); }
    }
    public float SurfaceIsoThicknessVScale
    {
      get { return GetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoVThicknessScale); }
      set { SetFloat(UnsafeNativeMethods.DisplayPipelineAttributesFloat.SurfaceIsoVThicknessScale, value); }
   }
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
          bool single_color = (value!=Color.Empty);
          m_parent.SetBool(UnsafeNativeMethods.DisplayPipelineAttributesBool.SingleMeshWireColor, single_color);
          if( single_color )
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

    public enum LightingSchema
    {
      None = 0,
      DefaultLighting = 1,
      SceneLighting = 2,
      CustomLighting = 3,
      AmbientOcclusion = 4
    }

    //ELightingScheme m_eLightingScheme;
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

  }
}
#endif
