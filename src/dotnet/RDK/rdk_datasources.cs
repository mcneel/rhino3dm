#pragma warning disable 1591

using Rhino.Display;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Render;
using Rhino.Runtime.InteropWrappers;
using Rhino.Geometry;

namespace Rhino.Render.DataSources
{
  public enum Modes
  {
    Unset,
    Grid, // Big thumbnails like Explorer icon mode.
    List, // Small thumbnails and info on right like Explorer report mode.
    Tree, // Tree mode.
  };

  public enum Shapes
  {
    Square, // Square thumbnails. Used for materials and textures.
    Wide,   // Wide thumbnails. Used for environments.
  };

  public enum Sizes
  {
    Unset,
    Tiny,
    Small,
    Medium,
    Large,
  };



  sealed class PreviewSettings : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public PreviewSettings(IntPtr pPreviewSettings)
    {
      m_cpp = pPreviewSettings;
    }

    ~PreviewSettings()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public Modes Mode()
    {

      if (m_cpp != IntPtr.Zero)
      {
        int r_value = UnsafeNativeMethods.RdkPreviewSettings_Mode(m_cpp);

        if (r_value == 0)
          return Modes.Unset;

        if (r_value == 1)
          return Modes.Grid;

        if (r_value == 2)
          return Modes.List;

        if (r_value == 3)
          return Modes.Tree;
      }

      return Modes.Unset;
    }

    public void SetMode(Modes m)
    {
      UnsafeNativeMethods.RdkPreviewSettings_SetMode(m_cpp, (int)m);
    }

    public Sizes Size()
    {
      if (m_cpp != IntPtr.Zero)
      {
        int r_value = UnsafeNativeMethods.RdkPreviewSettings_Size(m_cpp);

        if (r_value == 0)
          return Sizes.Unset;

        if (r_value == 1)
          return Sizes.Tiny;

        if (r_value == 2)
          return Sizes.Small;

        if (r_value == 3)
          return Sizes.Medium;

        if (r_value == 4)
          return Sizes.Large;

      }

      return Sizes.Unset;
    }

    public void SetSize(Sizes s)
    {
      UnsafeNativeMethods.RdkPreviewSettings_SetSize(m_cpp, (int)s);
    }

    public bool ShowUnits()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RdkPreviewSettings_ShowUnits(m_cpp);
      }

      return false;
    }

    public bool ShowLabels()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RdkPreviewSettings_ShowLabels(m_cpp);
      }

      return false;
    }

    public MetaData MetaDataForContent(RenderContent content)
    {
      if (m_cpp != IntPtr.Zero)
      {
        return new MetaData(UnsafeNativeMethods.RdkPreviewSettings_MetaDataForContent(m_cpp, content.CppPointer));
      }

      return null;
    }

    public void AddMetaData(MetaData md)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RdkPreviewSettings_AddMetaData(m_cpp, md.CppPointer);
      }
    }

    public void AddMetaData(MetaDataProxy mdp)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RdkPreviewSettings_AddMetaData(m_cpp, mdp.CppPointer);
      }
    }

    public void ResetMetaDataIterator()
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RdkPreviewSettings_ResetMetaDataIterator(m_cpp);
      }
    }

    public MetaData NextMetaData()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pMD = UnsafeNativeMethods.RdkPreviewSettings_NextMetaData(m_cpp);

        if(pMD != IntPtr.Zero)
          return new MetaData(pMD);
      }

      return null;
    }

    public void ClearMetaData()
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RdkPreviewSettings_ClearMetaData(m_cpp);
      }
    }
  }

  sealed class ContentEditorSettings : IDisposable
  {
    public enum Layouts
    {
      Horizontal,
      Vertical,
    };

    public enum Splitters
    {
      HA,
      HB,
      HC,
      VA,
      VB,
      VC
    };

    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentEditorSettings(IntPtr pPreviewSettings)
    {
      m_cpp = pPreviewSettings;
    }

    ~ContentEditorSettings()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public bool ShowPreviewPane()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RdkContentEditorSettings_ShowPreviewPane(m_cpp);
      }

      return false;
    }

    public bool ChevronEngaged()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RdkContentEditorSettings_ChevronEngaged(m_cpp);
      }

      return false;
    }

    public RenderContentKind Kind()
    {
      if (m_cpp != IntPtr.Zero)
      {
        uint kind = UnsafeNativeMethods.RdkContentEditorSettings_Kind(m_cpp);

        if (kind == 0)
          return RenderContentKind.None;
        if (kind == 1)
          return RenderContentKind.Material;
        if (kind == 2)
          return RenderContentKind.Environment;
        if (kind == 3)
          return RenderContentKind.Texture;

      }

      return RenderContentKind.None;
    }

    public Guid EditorId()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RdkContentEditorSettings_EditorId(m_cpp);
      }
      return Guid.Empty;
    }

    public ContentEditorSettings.Layouts Layout()
    {
      if (m_cpp != IntPtr.Zero)
      {
        int layout_value = UnsafeNativeMethods.RdkContentEditorSettings_Layout(m_cpp);
        if (layout_value == 0)
          return ContentEditorSettings.Layouts.Horizontal;
        if (layout_value == 1)
          return ContentEditorSettings.Layouts.Vertical;
      }
      return ContentEditorSettings.Layouts.Horizontal;
    }

    public int SplitterPos(ContentEditorSettings.Splitters splitter)
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RdkContentEditorSettings_SplitterPos(m_cpp, (uint)splitter);
      }
      return -1;
    }

    public void SetSplitterPos(ContentEditorSettings.Splitters splitter, int pos)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RdkContentEditorSettings_SetSplitterPos(m_cpp, (uint)splitter, pos);
      }
    }

  }

  public sealed class MetaData : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public MetaData(IntPtr pMetaData)
    {
      m_cpp = pMetaData;
    }

    ~MetaData()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public string Geometry()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr rc = UnsafeNativeMethods.MetaData_Geometry(m_cpp);
        return rc == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(rc);
      }

      return string.Empty;
    }

    public Guid ContentInstanceId()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.MetaData_ContentInstanceId(m_cpp);
      }

      return Guid.Empty;
    }
  }

  public enum AssignBys
  {
    Unset,
    Layer,  // Object material(s) assigned by layer.
    Parent, // Object material(s) assigned by object's parent.
    Object, // Object material(s) assigned by object.
    Varies, // Object materials are assigned differently.
    PlugIn, // Object material assign-by is managed by the current render plug-in.
  };

  sealed class NewContentControlAssignBy : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public NewContentControlAssignBy(IntPtr pAssignBy)
    {
      m_cpp = pAssignBy;
    }

    ~NewContentControlAssignBy()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public AssignBys Get()
    {
      int assign = UnsafeNativeMethods.NewContentControlAssignBy_Get(m_cpp);

      if (assign == 0)
        return AssignBys.Unset;

      if (assign == 1)
        return AssignBys.Layer;

      if (assign == 2)
        return AssignBys.Parent;

      if (assign == 3)
        return AssignBys.Object;

      if (assign == 4)
        return AssignBys.Varies;

      if (assign == 5)
        return AssignBys.PlugIn;

      return AssignBys.Unset;
    }

    public void Set(AssignBys assign)
    {
      int assign_by = 0;

      if (assign == AssignBys.Layer)
        assign_by = 1;

      if (assign == AssignBys.Parent)
        assign_by = 2;

      if (assign == AssignBys.Object)
        assign_by = 3;

      if (assign == AssignBys.Varies)
        assign_by = 4;

      if (assign == AssignBys.PlugIn)
        assign_by = 5;

      UnsafeNativeMethods.NewContentControlAssignBy_Set(m_cpp, assign_by);
    }
  }

  public enum Usage
  {
    kBackground = 1, // Specifies the background environment.
    kReflection = 2, // Specifies the custom reflective environment. Also used for refraction.
    kSkylighting = 4, // Specifies the custom skylighting environment.
    kAny = 7, // Only used in special cases. Not valid for calls to On(), Get(), etc.
  };

  sealed class RdkCurrentEnvironment : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkCurrentEnvironment(IntPtr pCurrentEnvironment)
    {
      m_cpp = pCurrentEnvironment;
    }

    ~RdkCurrentEnvironment()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderContent GetEnv()
    {
      RenderContent content = null;
      IntPtr pContent = UnsafeNativeMethods.RdkCurrentEnvironment_GetEnv(m_cpp, 1, 1);
      content = RenderContent.FromPointer(pContent);

      return content;
    }

  }

  //IRhRdkBackEnd
  sealed class RdkBackEnd : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkBackEnd(IntPtr pBackEnd)
    {
      m_cpp = pBackEnd;
    }

    ~RdkBackEnd()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkBackEnd_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

  }

  //IRhRdkRenderSettingsBackEnd
  sealed class RdkRenderSettingsBackEnd : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkRenderSettingsBackEnd(IntPtr pBackEnd)
    {
      m_cpp = pBackEnd;
    }

    ~RdkRenderSettingsBackEnd()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkRenderSettingsBackEnd_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public void OnApply()
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkRenderSettingsBackEnd_OnApply(m_cpp);
      }
    }

    public void OnCancel()
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkRenderSettingsBackEnd_OnCancel(m_cpp);
      }
    }

    public void ResetToDefaults()
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkRenderSettingsBackEnd_ResetToDefaults(m_cpp);
      }
    }

  }

  public sealed class RhinoSettings : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RhinoSettings(IntPtr pRhinoSettings)
    {
      m_cpp = pRhinoSettings;
    }

    ~RhinoSettings()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderSettings GetRenderSettings()
    {
      IntPtr pRenderSetting = UnsafeNativeMethods.RhinoSettings_ON_3dmRenderSettings(m_cpp);
      if (pRenderSetting != IntPtr.Zero)
        return new RenderSettings(pRenderSetting);

      return null;
    }

    public void SetRenderSettings(RenderSettings renderSettings)
    {
      UnsafeNativeMethods.RhinoSettings_Set_ON_3dmRenderSettings(m_cpp, renderSettings.ConstPointer());
    }

    public RhinoView ActiveView()
    {
      IntPtr pRhinoView = UnsafeNativeMethods.RhinoSettings_ActiveView(m_cpp);
      if (pRhinoView != IntPtr.Zero)
        return RhinoView.FromIntPtr(pRhinoView);

      return null;
    }

    public Rhino.DocObjects.ViewInfo RenderingView()
    {
      IntPtr pRhinoView = UnsafeNativeMethods.RhinoSettings_RenderingView(m_cpp);
      if (pRhinoView != IntPtr.Zero)
        return new Rhino.DocObjects.ViewInfo(pRhinoView, true);

      return null;
    }

    public List<System.Drawing.Size> GetCustomRenderSizes()
    {
      var list = new List<System.Drawing.Size>();

      IntPtr pArray = UnsafeNativeMethods.RhinoSettings_GetSizes(m_cpp);
      if (pArray == IntPtr.Zero)
        return list;

      int i = 0, width = 0, height = 0;
      while (UnsafeNativeMethods.Rdk_CustomRenderSizes_GetSize(pArray, i++, ref width, ref height))
      {
        list.Add(new System.Drawing.Size(width, height));
      }

      UnsafeNativeMethods.Rdk_CustomRenderSizes_DeleteArray(pArray);
      return list;
    }

    public bool ViewSupportsShading(RhinoView view)
    {
      IntPtr ptr_rhino_view = UnsafeNativeMethods.CRhinoView_FromRuntimeSerialNumber(view.RuntimeSerialNumber);
      if (IntPtr.Zero == ptr_rhino_view)
        return false;

      return  UnsafeNativeMethods.RhinoSettings_ViewSupportsShading(m_cpp, ptr_rhino_view);
    }

    public bool GroundPlaneOnInViewDisplayMode(RhinoView view)
    {
      IntPtr ptr_rhino_view = UnsafeNativeMethods.CRhinoView_FromRuntimeSerialNumber(view.RuntimeSerialNumber);
      if (IntPtr.Zero == ptr_rhino_view)
        return false;

      return UnsafeNativeMethods.RhinoSettings_GroundPlaneOnInViewDisplayMode(m_cpp, ptr_rhino_view);
    }

    public void SetGroundPlaneOnInViewDisplayMode(RhinoView view, bool bOn)
    {
      IntPtr ptr_rhino_view = UnsafeNativeMethods.CRhinoView_FromRuntimeSerialNumber(view.RuntimeSerialNumber);
      if (IntPtr.Zero == ptr_rhino_view)
        UnsafeNativeMethods.RhinoSettings_SetGroundPlaneOnInViewDisplayMode(m_cpp, ptr_rhino_view, bOn);
    }
  }

  // RdkContentUIs : IRhRdkContentUIs
  sealed class RdkContentUIs : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkContentUIs(IntPtr pContentUIs)
    {
      m_cpp = pContentUIs;
    }

    ~RdkContentUIs()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public void Changed()
    {
      UnsafeNativeMethods.RdkContentUIs_Changed(m_cpp);
    }

    public ContentUIArray GetUIs()
    {
      ContentUIArray contentUIArray = new ContentUIArray();
      UnsafeNativeMethods.RdkContentUIs_GetUIs(m_cpp, contentUIArray.CppPointer);
      return contentUIArray;
    }

  }

  // RdkContentUI : IRhRdkContentUI
  sealed class RdkContentUI : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkContentUI(IntPtr pContentUI)
    {
      m_cpp = pContentUI;
    }

    ~RdkContentUI()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public Guid Uuid()
    {
      return UnsafeNativeMethods.RdkContentUI_Uuid(m_cpp);
    }

    public bool IsShown()
    {
      return UnsafeNativeMethods.RdkContentUI_IsShown(m_cpp);
    }

    public bool IsCreated()
    {
      return UnsafeNativeMethods.RdkContentUI_IsCreated(m_cpp);
    }

    public IntPtr ContentUIHolder()
    {
      return UnsafeNativeMethods.RdkContentUI_ContentUIHolder(m_cpp);
    }

    public void DeleteThis()
    {
      UnsafeNativeMethods.RdkContentUI_DeleteThis(m_cpp);
    }
  }


  /// <summary>
  /// ContentUIArray 
  /// </summary>
  sealed class ContentUIArray : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// ContentUIArray constructor
    /// </summary>
    public ContentUIArray()
    {
      m_cpp = UnsafeNativeMethods.ContentUIArray_New();
    }

    /// <summary>
    /// ContentUIArray destructor
    /// </summary>
    ~ContentUIArray()
    {
      Dispose(false);
    }

    /// <summary>
    /// ContentUIArray dispose
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// ContentUIArray dispose
    /// </summary>
    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.ContentUIArray_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }


    /// <summary>
    /// Get the number of instances
    /// </summary>
    /// <returns>returns the number of instances</returns>
    public int Count()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.ContentUIArray_Count(m_cpp);
      }

      return 0;
    }

    /// <summary>
    /// Get the instance at index
    /// </summary>
    /// <param name="index">The index of the instance</param>
    /// <returns>returns the instance at index</returns>
    public RdkContentUI At(int index)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.ContentUIArray_At(m_cpp, index);
        return new RdkContentUI(pContentUI);
      }

      return null;
    }
  }

  // IRhRdkSelectionNavigator
  public sealed class RdkSelectionNavigator : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkSelectionNavigator(IntPtr pRhinoSettings)
    {
      m_cpp = pRhinoSettings;
    }

    ~RdkSelectionNavigator()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Clear the navigator.
    /// </summary>
    public void Clear()
    {
      UnsafeNativeMethods.RdkSelectionNavigator_Clear(m_cpp);
    }

    /// <summary>
    /// Add a selection of contents at the current position.
		/// Clears the navigator ahead of the current position.
    /// </summary>
    /// <param name="selectedContentArray">The selected content</param>
    public void Add(Rhino.Render.RenderContentCollection selectedContentArray)
    {
      UnsafeNativeMethods.RdkSelectionNavigator_Add(m_cpp, selectedContentArray.CppPointer);
    }

    /// <summary>
    /// Check the backwards status of the navigator
    /// </summary>
    /// <returns>True if it is possible to navigate backwards, else false</returns>
    public bool CanGoBackwards()
    {
      return UnsafeNativeMethods.RdkSelectionNavigator_CanGoBackwards(m_cpp);
    }

    /// <summary>
    /// Check the forwards status of the navigator
    /// </summary>
    /// <returns>True if it is possible to navigate forwards, else false</returns>
    public bool CanGoForwards()
    {
      return UnsafeNativeMethods.RdkSelectionNavigator_CanGoForwards(m_cpp);
    }

    /// <summary>
    /// Navigate backwards if possible
    /// </summary>
    /// <param name="selectedContentArray">selectedContentArray is the new selection after navigating backwards</param>
    /// <returns>true on success, else false</returns>
    public bool GoBackwards(ref Rhino.Render.RenderContentCollection selectedContentArray)
    {
      return UnsafeNativeMethods.RdkSelectionNavigator_GoBackwards(m_cpp, selectedContentArray.CppPointer);
    }

    /// <summary>
    /// Navigate forwards if possible
    /// </summary>
    /// <param name="selectedContentArray">selectedContentArray is the new selection after navigating forwards</param>
    /// <returns>true on success, else false</returns>
    public bool GoForwards(ref Rhino.Render.RenderContentCollection selectedContentArray)
    {
      return UnsafeNativeMethods.RdkSelectionNavigator_GoForwards(m_cpp, selectedContentArray.CppPointer);
    }

  }
}

// Displacement DynamicAccess
class DisplacementDynamicAccess : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  public DisplacementDynamicAccess()
  {
    m_cpp = UnsafeNativeMethods.RdkDisplacementDynamicAccess_New();
  }

  ~DisplacementDynamicAccess()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      //UnsafeNativeMethods.RdkDisplacementDynamicAccess_Delete(m_cpp);
      m_cpp = IntPtr.Zero;
    }
  }

  public DisplacementParameters DisplacementParameters(uint doc_serial)
  {
    DisplacementParameters parameters = null;

    if (m_cpp != IntPtr.Zero)
    {
      IntPtr pDP = UnsafeNativeMethods.RdkDisplacementDynamicAccess_GetDisplacementParameters(m_cpp, doc_serial);
      if (pDP != IntPtr.Zero)
        return new DisplacementParameters(pDP);
    }

    return parameters;
  }

  public EdgeSofteningParameters EdgeSofteningParameters(uint doc_serial)
  {
    EdgeSofteningParameters parameters = null;

    if (m_cpp != IntPtr.Zero)
    {
      IntPtr pEP = UnsafeNativeMethods.RdkDisplacementDynamicAccess_GetEdgeSofteningParameters(m_cpp, doc_serial);
      if (pEP != IntPtr.Zero)
        return new EdgeSofteningParameters(pEP);
    }

    return parameters;
  }

  public CurvePipingParameters CurvePipingParameters(uint doc_serial)
  {
    CurvePipingParameters parameters = null;

    if (m_cpp != IntPtr.Zero)
    {
      IntPtr pCP = UnsafeNativeMethods.RdkDisplacementDynamicAccess_GetCurvePipingParameters(m_cpp, doc_serial);
      if (pCP != IntPtr.Zero)
        return new CurvePipingParameters(pCP);
    }

    return parameters;
  }

  public ThickeningParameters ThickeningParameters(uint doc_serial)
  {
    ThickeningParameters parameters = null;

    if (m_cpp != IntPtr.Zero)
    {
      IntPtr pCP = UnsafeNativeMethods.RdkDisplacementDynamicAccess_GetThickeningParameters(m_cpp, doc_serial);
      if (pCP != IntPtr.Zero)
        return new ThickeningParameters(pCP);
    }

    return parameters;
  }

  public ShutLiningParameters ShutLiningParameters(uint doc_serial)
  {
    ShutLiningParameters parameters = null;

    if (m_cpp != IntPtr.Zero)
    {
      IntPtr pSP = UnsafeNativeMethods.RdkDisplacementDynamicAccess_ShutLiningParameters(m_cpp, doc_serial);
      if (pSP != IntPtr.Zero)
        return new ShutLiningParameters(pSP);
    }

    return parameters;
  }
}

class DisplacementParameters : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  public DisplacementParameters(IntPtr p)
  {
    m_cpp = p;
  }

  ~DisplacementParameters()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      m_cpp = IntPtr.Zero;
    }
  }

  public bool GetOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetOn(m_cpp);
    }

    return false;
  }

  public void SetOn(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetOn(m_cpp, on);
    }
  }

  public bool IsVariesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesOn(m_cpp);
    }
    return false;
  }

  public Guid GetTextureId()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetTextureId(m_cpp);
    }

    return Guid.Empty;
  }

  public void SetTextureId(Guid uuid)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetTextureId(m_cpp, uuid);
    }
  }

  public bool IsVariesTexture()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesTexture(m_cpp);
    }
    return false;
  }

  public bool IsEnabledTexture()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledTexture(m_cpp);
    }
    return false;
  }

  public int GetMappingChannel()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetMappingChannel(m_cpp);
    }
    return 0;
  }

  public void SetMappingChannel(int mapping)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetMappingChannel(m_cpp, mapping);
    }
  }

  public bool IsVariesMappingChannel()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesMappingChannel(m_cpp);
    }
    return false;
  }

  public bool IsEnabledMappingChannel()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledMappingChannel(m_cpp);
    }
    return false;
  }

  public double GetBlackPoint()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetBlackPoint(m_cpp);
    }
    return 0;
  }

  public void SetBlackPoint(double point)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetBlackPoint(m_cpp, point);
    }
  }

  public bool IsVariesBlackPoint()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesBlackPoint(m_cpp);
    }
    return false;
  }

  public bool IsEnabledBlackPoint()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledBlackPoint(m_cpp);
    }
    return false;
  }

  public double GetWhitePoint()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetWhitePoint(m_cpp);
    }
    return 0;
  }

  public void SetWhitePoint(double point)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetWhitePoint(m_cpp, point);
    }
  }

  public bool IsVariesWhitePoint()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesWhitePoint(m_cpp);
    }
    return false;
  }

  public bool IsEnabledWhitePoint()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledWhitePoint(m_cpp);
    }
    return false;
  }

  public int GetInitialQuality()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetInitialQuality(m_cpp);
    }
    return 0;
  }

  public void SetInitialQuality(int quality)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetInitialQuality(m_cpp, quality);
    }
  }

  public bool IsVariesInitialQuality()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesInitialQuality(m_cpp);
    }
    return false;
  }

  public bool IsEnabledInitialQuality()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledInitialQuality(m_cpp);
    }
    return false;
  }

  public int GetMaxFaces()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetMaxFaces(m_cpp);
    }
    return 0;
  }

  public void SetMaxFaces(int faces)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetMaxFaces(m_cpp, faces);
    }
  }

  public bool IsVariesMaxFaces()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesMaxFaces(m_cpp);
    }
    return false;
  }

  public bool IsEnabledMaxFaces()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledMaxFaces(m_cpp);
    }
    return false;
  }

  public int GetFairing()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetFairing(m_cpp);
    }
    return 0;
  }

  public void SetFairing(int fairing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetFairing(m_cpp, fairing);
    }
  }

  public bool IsVariesFairing()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesFairing(m_cpp);
    }
    return false;
  }

  public bool IsEnabledFairing()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledFairing(m_cpp);
    }
    return false;
  }

  public bool GetFairingOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetFairingOn(m_cpp);
    }
    return false;
  }

  public void SetFairingOn(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetFairingOn(m_cpp, on);
    }
  }

  public bool IsVariesFairingOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesFairingOn(m_cpp);
    }
    return false;
  }

  public bool IsEnabledFairingOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledFairingOn(m_cpp);
    }
    return false;
  }

  public bool GetMaxFacesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetMaxFacesOn(m_cpp);
    }
    return false;
  }

  public void SetMaxFacesOn(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetMaxFacesOn(m_cpp, on);
    }
  }

  public bool IsVariesMaxFacesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesMaxFacesOn(m_cpp);
    }
    return false;
  }

  public bool IsEnabledMaxFacesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledMaxFacesOn(m_cpp);
    }
    return false;
  }

  public double GetPostWeldAngle()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetPostWeldAngle(m_cpp);
    }
    return 0;
  }

  public void SetPostWeldAngle(double angle)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetPostWeldAngle(m_cpp, angle);
    }
  }

  public bool IsVariesPostWeldAngle()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesPostWeldAngle(m_cpp);
    }
    return false;
  }

  public bool IsEnabledPostWeldAngle()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledPostWeldAngle(m_cpp);
    }
    return false;
  }

  public int GetMeshMemoryLimit()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetMeshMemoryLimit(m_cpp);
    }
    return 0;
  }

  public void SetMeshMemoryLimit(int limit)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetMeshMemoryLimit(m_cpp, limit);
    }
  }

  public bool IsVariesMeshMemoryLimit()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesMeshMemoryLimit(m_cpp);
    }
    return false;
  }

  public bool IsEnabledMeshMemoryLimit()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledMeshMemoryLimit(m_cpp);
    }
    return false;
  }

  public int GetRefineSteps()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetRefineSteps(m_cpp);
    }
    return 0;
  }

  public void SetRefineSteps(int step)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetRefineSteps(m_cpp, step);
    }
  }

  public bool IsVariesRefineSteps()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesRefineSteps(m_cpp);
    }
    return false;
  }

  public bool IsEnabledRefineSteps()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledRefineSteps(m_cpp);
    }
    return false;
  }

  public double GetRefineSensitivity()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetRefineSensitivity(m_cpp);
    }
    return 0;
  }

  public void SetRefineSensitivity(double sensitivity)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetRefineSensitivity(m_cpp, sensitivity);
    }
  }

  public bool IsVariesRefineSensitivity()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesRefineSensitivity(m_cpp);
    }
    return false;
  }

  public bool IsEnabledRefineSensitivity()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledRefineSensitivity(m_cpp);
    }
    return false;
  }
}

class EdgeSofteningParameters : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  public EdgeSofteningParameters(IntPtr p)
  {
    m_cpp = p;
  }

  ~EdgeSofteningParameters()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      m_cpp = IntPtr.Zero;
    }
  }

  public bool GetOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkEdgeSofteningParameters_GetOn(m_cpp);
    }

    return false;
  }

  public void SetOn(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkEdgeSofteningParameters_SetOn(m_cpp, on);
    }
  }

  public bool IsVariesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkEdgeSofteningParameters_IsVariesOn(m_cpp);
    }
    return false;
  }

  public double GetSoftening()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetSoftening(m_cpp);
    }
    return 0;
  }

  public void SetSoftening(double softening)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetSoftening(m_cpp, softening);
    }
  }

  public bool IsVariesSoftening()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesSoftening(m_cpp);
    }
    return false;
  }

  public bool IsEnabledSoftening()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledSoftening(m_cpp);
    }
    return false;
  }

  public double GetEdgeAngleThreshold()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetEdgeAngleThreshold(m_cpp);
    }
    return 0;
  }

  public void SetEdgeAngleThreshold(double threshold)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetEdgeAngleThreshold(m_cpp, threshold);
    }
  }

  public bool IsVariesEdgeAngleThreshold()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesEdgeAngleThreshold(m_cpp);
    }
    return false;
  }

  public bool IsEnabledEdgeAngleThreshold()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledEdgeAngleThreshold(m_cpp);
    }
    return false;
  }

  public bool GetChamfer()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetChamfer(m_cpp);
    }
    return false;
  }

  public void SetChamfer(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetChamfer(m_cpp, on);
    }
  }

  public bool IsVariesChamfer()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesChamfer(m_cpp);
    }
    return false;
  }

  public bool IsEnabledChamfer()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledChamfer(m_cpp);
    }
    return false;
  }

  public bool GetFaceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetFaceted(m_cpp);
    }
    return false;
  }

  public void SetFaceted(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetFaceted(m_cpp, on);
    }
  }

  public bool IsVariesFaceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesFaceted(m_cpp);
    }
    return false;
  }

  public bool IsEnabledFaceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledFaceted(m_cpp);
    }
    return false;
  }

  public bool GetForceSoftening()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_GetForceSoftening(m_cpp);
    }
    return false;
  }

  public void SetForceSoftening(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkDisplacementParameters_SetForceSoftening(m_cpp, on);
    }
  }

  public bool IsVariesForceSoftening()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsVariesForceSoftening(m_cpp);
    }
    return false;
  }

  public bool IsEnabledForceSoftening()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkDisplacementParameters_IsEnabledForceSoftening(m_cpp);
    }
    return false;
  }
}

class CurvePipingParameters : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  public CurvePipingParameters(IntPtr p)
  {
    m_cpp = p;
  }

  ~CurvePipingParameters()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      m_cpp = IntPtr.Zero;
    }
  }

  public bool GetOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_GetOn(m_cpp);
    }

    return false;
  }

  public void SetOn(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkCurvePipingParameters_SetOn(m_cpp, on);
    }
  }

  public bool IsVariesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsVariesOn(m_cpp);
    }
    return false;
  }

  public double GetRadius()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_GetRadius(m_cpp);
    }

    return 0.0;
  }

  public void SetRadius(double radius)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkCurvePipingParameters_SetRadius(m_cpp, radius);
    }
  }

  public bool IsVariesRadius()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsVariesRadius(m_cpp);
    }
    return false;
  }

  public bool IsEnabledRadius()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsEnabledRadius(m_cpp);
    }
    return false;
  }

  public int GetSegments()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_GetSegments(m_cpp);
    }

    return 0;
  }

  public void SetSegments(int segments)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkCurvePipingParameters_SetSegments(m_cpp, segments);
    }
  }

  public bool IsVariesSegments()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsVariesSegments(m_cpp);
    }
    return false;
  }

  public bool IsEnabledSegments()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsEnabledSegments(m_cpp);
    }
    return false;
  }

  public bool GetFaceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return !UnsafeNativeMethods.RdkCurvePipingParameters_GetFaceted(m_cpp);
    }

    return false;
  }

  public void SetFaceted(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkCurvePipingParameters_SetFaceted(m_cpp, !on);
    }
  }

  public bool IsVariesFaceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsVariesFaceted(m_cpp);
    }
    return false;
  }

  public bool IsEnabledFaceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsEnabledFaceted(m_cpp);
    }
    return false;
  }

  public string GetCapType()
  {
    if (m_cpp != IntPtr.Zero)
    {
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(null);
      var p_string = sh.NonConstPointer;
      UnsafeNativeMethods.RdkCurvePipingParameters_GetCapType(m_cpp, p_string);
      return sh.ToString();
    }

    return String.Empty;
  }

  public void SetCapType(string cap_type)
  {
    if (m_cpp != IntPtr.Zero)
    {
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(cap_type);
      var p_string = sh.ConstPointer;
      UnsafeNativeMethods.RdkCurvePipingParameters_SetCapType(m_cpp, p_string);
    }
  }

  public bool IsVariesCapType()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsVariesCapType(m_cpp);
    }
    return false;
  }

  public bool IsEnabledCapType()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsEnabledCapType(m_cpp);
    }
    return false;
  }

  public int GetAccuracy()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_GetAccuracy(m_cpp);
    }

    return 0;
  }

  public void SetAccuracy(int accuracy)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkCurvePipingParameters_SetAccuracy(m_cpp, accuracy);
    }
  }

  public bool IsVariesAccuracy()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsVariesAccuracy(m_cpp);
    }
    return false;
  }

  public bool IsEnabledAccuracy()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkCurvePipingParameters_IsEnabledAccuracy(m_cpp);
    }
    return false;
  }
}

class ThickeningParameters : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  public ThickeningParameters(IntPtr p)
  {
    m_cpp = p;
  }

  ~ThickeningParameters()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      m_cpp = IntPtr.Zero;
    }
  }

  public bool GetOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_GetOn(m_cpp);
    }

    return false;
  }

  public void SetOn(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkThickeningParameters_SetOn(m_cpp, on);
    }
  }

  public bool IsVariesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsVariesOn(m_cpp);
    }
    return false;
  }

  public double GetDistance()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_GetDistance(m_cpp);
    }

    return 0.0;
  }

  public void SetDistance(double distance)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkThickeningParameters_SetDistance(m_cpp, distance);
    }
  }

  public bool IsVariesDistance()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsVariesDistance(m_cpp);
    }
    return false;
  }

  public bool IsEnabledDistance()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsEnabledDistance(m_cpp);
    }
    return false;
  }

  public bool GetSolid()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_GetSolid(m_cpp);
    }

    return false;
  }

  public void SetSolid(bool solid)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkThickeningParameters_SetSolid(m_cpp, solid);
    }
  }

  public bool IsVariesSolid()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsVariesSolid(m_cpp);
    }
    return false;
  }

  public bool IsEnabledSolid()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsEnabledSolid(m_cpp);
    }
    return false;
  }

  public bool GetOffsetOnly()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_GetOffsetOnly(m_cpp);
    }

    return false;
  }

  public void SetOffsetOnly(bool offset_only)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkThickeningParameters_SetOffsetOnly(m_cpp, offset_only);
    }
  }

  public bool IsVariesOffsetOnly()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsVariesOffsetOnly(m_cpp);
    }
    return false;
  }

  public bool IsEnabledOffsetOnly()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsEnabledOffsetOnly(m_cpp);
    }
    return false;
  }

  public bool GetBothSides()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_GetBothSides(m_cpp);
    }

    return false;
  }

  public void SetBothSides(bool both_sides)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkThickeningParameters_SetBothSides(m_cpp, both_sides);
    }
  }

  public bool IsVariesBothSides()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsVariesBothSides(m_cpp);
    }
    return false;
  }

  public bool IsEnabledBothSides()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkThickeningParameters_IsEnabledBothSides(m_cpp);
    }
    return false;
  }

}

// ShutLining
class ShutLiningParameters : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  public ShutLiningParameters(IntPtr p)
  {
    m_cpp = p;
  }

  ~ShutLiningParameters()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      m_cpp = IntPtr.Zero;
    }
  }

  public bool GetOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_GetOn(m_cpp);
    }

    return false;
  }

  public void SetOn(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkShutLiningParameters_SetOn(m_cpp, on);
    }
  }

  public bool VariesOn()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_VariesOn(m_cpp);
    }
    return false;
  }

  public bool DataExists()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_DataExists(m_cpp);
    }

    return false;
  }

  public bool Faceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_Faceted(m_cpp);
    }

    return false;
  }

  public void SetFaceted(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkShutLiningParameters_SetFaceted(m_cpp, on);
    }
  }

  public bool VariesFaceted()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_VariesFaceted(m_cpp);
    }
    return false;
  }

  public bool AutoUpdate()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_AutoUpdate(m_cpp);
    }

    return false;
  }

  public void SetAutoUpdate(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkShutLiningParameters_SetAutoUpdate(m_cpp, on);
    }
  }

  public bool VariesAutoUpdate()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_VariesAutoUpdate(m_cpp);
    }
    return false;
  }

  public bool ForceUpdate()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_ForceUpdate(m_cpp);
    }

    return false;
  }

  public void SetForceUpdate(bool on)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkShutLiningParameters_SetForceUpdate(m_cpp, on);
    }
  }

  public bool VariesForceUpdate()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_VariesForceUpdate(m_cpp);
    }
    return false;
  }

  public bool VariesCurves()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_VariesCurves(m_cpp);
    }
    return false;
  }

  public bool FindCurve(ref Guid uuid)
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_FindCurve(m_cpp, ref uuid);
    }

    return false;
  }

  public bool SetCurve(Guid uuidCurve, bool bEnabled, double radius, int profile, bool bPull, bool bIsBump)
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_SetCurve(m_cpp, uuidCurve, bEnabled, radius, profile, bPull, bIsBump);
    }

    return false;
  }

  public bool GetCurve(ref Guid uuidCurve, ref bool bEnabled, ref double radius, ref int profile, ref bool bPull, ref bool bIsBump)
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_GetCurve(m_cpp, ref uuidCurve, ref bEnabled, ref radius, ref profile, ref bPull, ref bIsBump);
    }

    return false;
  }

  public bool GetFirstCurve(ref Guid uuidCurve, ref bool bEnabled, ref double radius, ref int profile, ref bool bPull, ref bool bIsBump)
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_GetFirstCurve(m_cpp, ref uuidCurve, ref bEnabled, ref radius, ref profile, ref bPull, ref bIsBump);
    }

    return false;
  }

  public bool GetNextCurve(ref Guid uuidCurve, ref bool bEnabled, ref double radius, ref int profile, ref bool bPull, ref bool bIsBump)
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_GetNextCurve(m_cpp, ref uuidCurve, ref bEnabled, ref radius, ref profile, ref bPull, ref bIsBump);
    }

    return false;
  }

  public bool HasCurves()
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_HasCurves(m_cpp);
    }

    return false;
  }

  public bool RemoveCurve(Guid uuidCurve)
  {
    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.RdkShutLiningParameters_RemoveCurve(m_cpp, uuidCurve);
    }

    return false;
  }

  public void GetCurves(SimpleArrayGuid curveUuidsOut, bool bOnlyEnabled = false)
  {
    if (m_cpp != IntPtr.Zero)
    {
      UnsafeNativeMethods.RdkShutLiningParameters_GetCurves(m_cpp, curveUuidsOut.NonConstPointer(), bOnlyEnabled);
    }
  }
}




/// <summary>
/// SnapShotsClientDataArray 
/// </summary>
class SnapShotsClientDataArray : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  /// <summary>
  /// SnapShotsClientDataArray constructor
  /// </summary>
  public SnapShotsClientDataArray(IntPtr p)
  {
    m_cpp = p;
  }

  /// <summary>
  /// SnapShotsClientDataArray destructor
  /// </summary>
  ~SnapShotsClientDataArray()
  {
    Dispose(false);
  }

  /// <summary>
  /// SnapShotsClientDataArray dispose
  /// </summary>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// SnapShotsClientDataArray dispose
  /// </summary>
  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      m_cpp = IntPtr.Zero;
    }
  }


  /// <summary>
  /// Get the number of instances
  /// </summary>
  /// <returns>returns the number of instances</returns>
  public int Count()
  {
    IntPtr value = IntPtr.Zero;

    if (m_cpp != IntPtr.Zero)
    {
      return UnsafeNativeMethods.SnapShotsClientDataArray_Count(m_cpp);
    }

    return 0;
  }

  /// <summary>
  /// Get the instance at index
  /// </summary>
  /// <param name="index">The index of the instance</param>
  /// <returns>returns the instance at index</returns>
  public SnapShostClientData At(int index)
  {
    if (m_cpp != IntPtr.Zero)
    {
      IntPtr pContentUI = UnsafeNativeMethods.SnapShotsClientDataArray_At(m_cpp, index);
      return new SnapShostClientData(pContentUI);
    }

    return null;
  }
}

enum eSnapshotClientState : int
{
  csChecked = 0,
  csUnChecked = 1,
  csVaries = 2
};

class SnapShostClientData : IDisposable
{
  private IntPtr m_cpp;

  public IntPtr CppPointer
  {
    get { return m_cpp; }
  }

  public SnapShostClientData(IntPtr p)
  {
    m_cpp = p;
  }

  ~SnapShostClientData()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  void Dispose(bool bDisposing)
  {
    if (m_cpp != IntPtr.Zero)
    {
      m_cpp = IntPtr.Zero;
    }
  }

  public string Name()
  {
    string name = "";
    var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
    var p_string = sh.NonConstPointer;

    UnsafeNativeMethods.SnapShotsClientDataArray_Name(m_cpp, p_string);

    return sh.ToString();
  }

  public string Category()
  {
    string category = "";
    var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
    var p_string = sh.NonConstPointer;

    UnsafeNativeMethods.SnapShotsClientDataArray_Category(m_cpp, p_string);

    return sh.ToString();
  }

  public Guid Id()
  {
    return UnsafeNativeMethods.SnapShotsClientDataArray_Id(m_cpp);
  }

  public eSnapshotClientState GetState()
  {
    int value = UnsafeNativeMethods.SnapShotsClientDataArray_State_Get(m_cpp);

    if (value == 0)
      return eSnapshotClientState.csChecked;
    if (value == 1)
      return eSnapshotClientState.csUnChecked;
    if (value == 2)
      return eSnapshotClientState.csVaries;

    return eSnapshotClientState.csUnChecked;
  }

  public void SetState(eSnapshotClientState state)
  {
    UnsafeNativeMethods.SnapShotsClientDataArray_State_Set(m_cpp, (int)state);
  }
}

namespace Rhino.Render.DataSources
{
  public class ContentFactories : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentFactories(IntPtr pRdkContentFactories)
    {
      m_cpp = pRdkContentFactories;
    }

    ~ContentFactories()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public ContentFactory FirstFactory()
    {
      IntPtr content_factory_pointer = UnsafeNativeMethods.ContentFactories_FirstFactory(m_cpp);

      if (content_factory_pointer != IntPtr.Zero)
      {
        return new ContentFactory(content_factory_pointer);
      }

      return null;
    }

    public ContentFactory NextFactory()
    {
      IntPtr content_factory_pointer = UnsafeNativeMethods.ContentFactories_NextFactory(m_cpp);

      if (content_factory_pointer != IntPtr.Zero)
      {
        return new ContentFactory(content_factory_pointer);
      }

      return null;
    }

    public ContentFactory FindFactory(Guid uuid)
    {
      ContentFactory factory = FirstFactory();

      while (factory != null)
      {
        // Factory can be listed
        if (factory.ContentTypeId() == uuid)
          return factory;

        factory = NextFactory();
      }

      return null;
    }
  }

  public class ContentFactory : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentFactory(IntPtr pRdkContentFactory)
    {
      m_cpp = pRdkContentFactory;
    }

    ~ContentFactory()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    /// <summary>
    /// New Content returns a new content, which is Initialized with the Initialize() function.
    /// The content should be unitilized after use with the Unitialize function.
    /// </summary>
    /// <returns></returns>
    public RenderContent NewContent()
    {
      RenderContent content = null;
      IntPtr content_pointer = IntPtr.Zero;

      content_pointer = UnsafeNativeMethods.ContentFactory_NewContent(m_cpp);

      if (content_pointer != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(content_pointer);
      }

      return content;
    }

    public RenderContentKind Kind()
    {
      RenderContentKind kind = RenderContentKind.None;
      uint kind_value = UnsafeNativeMethods.ContentFactory_Kind(m_cpp);

      if (kind_value == 0)
        kind = RenderContentKind.None;

      if (kind_value == 1)
        kind = RenderContentKind.Material;

      if (kind_value == 2)
        kind = RenderContentKind.Environment;

      if (kind_value == 4)
        kind = RenderContentKind.Texture;

      return kind;
    }

    public Guid ContentTypeId()
    {
      return UnsafeNativeMethods.ContentFactory_ContentTypeId(m_cpp);
    }
    
  }

  class RdkContentFactoriesUtil
  {
    public static ContentFactories GetRdkContentFactories()
    {
      ContentFactories factories = null;
      IntPtr factories_pointer = IntPtr.Zero;

      factories_pointer = UnsafeNativeMethods.RdkRegisteredContentFactories();

      if (factories_pointer != IntPtr.Zero)
      {
        factories = new ContentFactories(factories_pointer);
      }

      return factories;
    }
  }

  class DecalDataSource : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public DecalDataSource(IntPtr pDecalDS)
    {
      m_cpp = pDecalDS;
    }

    ~DecalDataSource()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }


    public Decal DecalForRead(Rhino.DocObjects.RhinoObject[] aObj, int crc)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      IntPtr pDecal = UnsafeNativeMethods.DecalDataSource_DecalForRead(m_cpp, pArray, crc);

      if (pDecal != IntPtr.Zero)
      {
        return new Decal(pDecal);
      }

      return null;
    }

    public WriteDecal DecalForWrite(Rhino.DocObjects.RhinoObject[] aObj, int crc)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      IntPtr pDecal = UnsafeNativeMethods.DecalDataSource_DecalForWrite(m_cpp, pArray, crc);

      if (pDecal != IntPtr.Zero)
      {
        return new WriteDecal(pDecal);
      }

      return null;
    }

    public bool AddDecal(Rhino.DocObjects.RhinoObject[] aObj)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      return UnsafeNativeMethods.DecalDataSource_AddDecal(m_cpp, pArray);
    }

    public bool DeleteDecal(Rhino.DocObjects.RhinoObject[] aObj, int crc)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      return UnsafeNativeMethods.DecalDataSource_DeleteDecal(m_cpp, pArray, crc);
    }

    public bool MoveDecal(Rhino.DocObjects.RhinoObject[] aObj, int crc_from, int crc_to)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      return UnsafeNativeMethods.DecalDataSource_MoveDecal(m_cpp, pArray, crc_from, crc_to);
    }

    public void GetDecalsWithShownWidgets(Rhino.DocObjects.RhinoObject[] aObj, ref SimpleArrayInt crc_array)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      UnsafeNativeMethods.DecalDataSource_GetDecalsWithShownWidgets(m_cpp, pArray, crc_array.NonConstPointer());
    }

    public bool ToggleDecalWidget(Rhino.DocObjects.RhinoObject[] aObj, int crc)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      return UnsafeNativeMethods.DecalDataSource_ToggleDecalWidget(m_cpp, pArray, crc);
    }

    public bool IsPropertiesSupported()
    {
      return UnsafeNativeMethods.DecalDataSource_IsPropertiesSupported(m_cpp);
    }

    public bool ShowProperties(Rhino.DocObjects.RhinoObject[] aObj, ref int crc)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      return UnsafeNativeMethods.DecalDataSource_ShowProperties(m_cpp, pArray, ref crc);
    }

    public DecalItem First(Rhino.DocObjects.RhinoObject[] aObj)
    {
      var rhobjs = new Rhino.Runtime.InternalRhinoObjectArray(aObj);

      IntPtr pArray = rhobjs.NonConstPointer();
      IntPtr pItem = UnsafeNativeMethods.DecalDataSource_First(m_cpp, pArray);

      if (pItem != IntPtr.Zero)
      {
        return new DecalItem(pItem);
      }
      return null;
    }

    public DecalItem Next()
    {
      IntPtr pItem = UnsafeNativeMethods.DecalDataSource_Next(m_cpp);

      if (pItem != IntPtr.Zero)
      {
        return new DecalItem(pItem);
      }
      return null;
    }
  }

  class DecalItem : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public DecalItem(IntPtr pDecalItem)
    {
      m_cpp = pDecalItem;
    }

    ~DecalItem()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }


    public int CRC()
    {
      return UnsafeNativeMethods.DecalDataSource_DecalCRC(m_cpp);
    }

    public bool Gray()
    {
      return UnsafeNativeMethods.DecalDataSource_Gray(m_cpp);
    }
  }

  class WriteDecal : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public WriteDecal(IntPtr pDecalItem)
    {
      m_cpp = pDecalItem;
    }

    ~WriteDecal()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }


    public int CRCAfterCommit()
    {
      return UnsafeNativeMethods.WriteDecal_CRCAfterCommit(m_cpp);
    }

    public void SetOrigin(Point3d point)
    {
      UnsafeNativeMethods.WriteDecal_SetOrigin(m_cpp, point);
    }

    public void SetTextureInstanceId(Guid uuidInstance)
    {
      UnsafeNativeMethods.WriteDecal_SetTextureInstanceId(m_cpp, uuidInstance);
    }

    public void SetProjection(Rhino.Render.DecalProjection projection)
    {
      UnsafeNativeMethods.WriteDecal_SetProjection(m_cpp, (int)projection);
    }

    public void SetMapToInside(bool bMapToInside)
    {
      UnsafeNativeMethods.WriteDecal_SetMapToInside(m_cpp, bMapToInside);
    }

    public void SetTransparency(double dTransparency)
    {
      UnsafeNativeMethods.WriteDecal_SetTransparency(m_cpp, dTransparency);
    }

    public void SetVectorUp(Vector3d vec)
    {
      UnsafeNativeMethods.WriteDecal_SetVectorUp(m_cpp, vec);
    }

    public void SetVectorAcross(Vector3d vec)
    {
      UnsafeNativeMethods.WriteDecal_SetVectorAcross(m_cpp, vec);
    }

    public void SetHeight(double dHeight)
    {
      UnsafeNativeMethods.WriteDecal_SetHeight(m_cpp, dHeight);
    }

    public void SetRadius(double dRadius)
    {
      UnsafeNativeMethods.WriteDecal_SetRadius(m_cpp, dRadius);
    }

    public void SetHorzSweep(double sta, double end)
    {
      UnsafeNativeMethods.WriteDecal_SetHorzSweep(m_cpp, sta, end);
    }

    public void SetVertSweep(double sta, double end)
    {
      UnsafeNativeMethods.WriteDecal_SetVertSweep(m_cpp, sta, end);
    }

    public void SetUVBounds(double dMinU, double dMinV, double dMaxU, double dMaxV)
    {
      UnsafeNativeMethods.WriteDecal_SetUVBounds(m_cpp, dMinU, dMinV, dMaxU, dMaxV);
    }
  }

  [Obsolete("RdkModalEditContentBucket is obsolete", false)]
  public class RdkModalEditContentBucket : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkModalEditContentBucket(IntPtr pRdkModalEditContentBucket)
    {
      m_cpp = pRdkModalEditContentBucket;
    }

    ~RdkModalEditContentBucket()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderContentCollection ContentsIn()
    {
      IntPtr pIntPtr = UnsafeNativeMethods.RdkModalEditContentBucket_ContentsIn(m_cpp);
      if (pIntPtr != IntPtr.Zero)
      {
        return new RenderContentCollection(pIntPtr);
      }
      return null;
    }

    public void SetContentsOut(RenderContentCollection collection)
    {
      UnsafeNativeMethods.RdkModalEditContentBucket_SetContentsOut(m_cpp, collection.CppPointer);
    }
  }

  public class RdkEdit : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkEdit(IntPtr pRdkEdit)
    {
      m_cpp = pRdkEdit;
    }

    ~RdkEdit()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public bool Execute(RenderContentCollection collection)
    {
      return UnsafeNativeMethods.RdkEdit_Execute(m_cpp, collection.CppPointer);
    }
  }

  #region IRhinoUiEventInfo
  sealed class DecalEventInfo : IDisposable
  {
    public enum Operations
    {
      Add,
      Delete,
      Modify
    };

    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public DecalEventInfo(IntPtr pDecalEventInfo)
    {
      m_cpp = pDecalEventInfo;
    }

    ~DecalEventInfo()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public Decal Decal()
    {
      IntPtr pDecal = UnsafeNativeMethods.RdkDecalEventInfo_Decal(m_cpp);

      if (pDecal != IntPtr.Zero)
      {
        return new Render.Decal(pDecal);
      }
      return null;
    }

    public int OldCRC()
    {
      return UnsafeNativeMethods.RdkDecalEventInfo_OldCRC(m_cpp);
    }

    public Operations Operation()
    {
      int ret = UnsafeNativeMethods.RdkDecalEventInfo_Operation(m_cpp);

      Operations op = Operations.Add;
      
      if(ret == 0)
        op = Operations.Add;
      if (ret == 1)
        op = Operations.Delete;
      if (ret == 2)
        op = Operations.Modify;

      return op;
    }
  }

  sealed class ContentUpdatePreviewMarkersEventInfo : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentUpdatePreviewMarkersEventInfo(IntPtr pMetaData)
    {
      m_cpp = pMetaData;
    }

    ~ContentUpdatePreviewMarkersEventInfo()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderContentKind Kind()
    {
      RenderContentKind kind = RenderContentKind.None;

      if (m_cpp != IntPtr.Zero)
      {
        uint kind_value = UnsafeNativeMethods.ContentUpdatePreviewMarkersEventInfo_Kind(m_cpp);

        if (kind_value == 0)
          kind = RenderContentKind.None;
        if (kind_value == 1)
          kind = RenderContentKind.Material;
        if (kind_value == 2)
          kind = RenderContentKind.Environment;
        if (kind_value == 4)
          kind = RenderContentKind.Texture;

        return kind;
      }

      return kind;
    }
  }


  class ContentParamEventInfo : IDisposable
  {
    public enum Types : uint
    {
      Name,    // Content name changed.
      Notes,   // Content notes changed.
      Tags,    // Content tags changed.
      GroupId, // Content group id changed.
      Param,   // Some other content state / field changed.
    };

    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentParamEventInfo(IntPtr pContentEventInfo)
    {
      m_cpp = pContentEventInfo;
    }

    ~ContentParamEventInfo()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderContent Content()
    {
      RenderContent content = null;
      IntPtr content_pointer = IntPtr.Zero;

      content_pointer = UnsafeNativeMethods.ContentParamEventInfo_Content(m_cpp);

      if (content_pointer != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(content_pointer);
      }

      return content;
    }

    public Types Type()
    {
      Types type = Types.Name;
      uint type_value = UnsafeNativeMethods.ContentParamEventInfo_Type(m_cpp);

      if (type_value == 0)
        type = Types.Name;
      if (type_value == 1)
        type = Types.Notes;
      if (type_value == 2)
        type = Types.Tags;
      if (type_value == 3)
        type = Types.GroupId;
      if (type_value == 4)
        type = Types.Param;

      return type;
    }
  }

  class ContentDatabaseEventInfo : IDisposable
  {
    public enum Types : uint
    {
      Attached,   // A content was attached (usually to a document). 'Extra' must be cast to CRhRdkEventWatcher::AttachReason.
      Detaching,  // A content is about to be detached (usually from a document). 'Extra' must be cast to CRhRdkEventWatcher::DetachReason.
      Detached,   // A content was detached (usually from a document). 'Extra' must be cast to CRhRdkEventWatcher::DetachReason.
      Replacing,  // A content is about to be replaced (usually in a document). 'Extra' is not used.
      Replaced,   // A content was replaced (usually in a document). 'Extra' is not used.
      Blossom,    // A V4 material was changed into a real RDK material (usually in a document). 'Extra' is not used.
    };

    public enum AttachReason : uint
    {
      Attach, // Content is being attached by the CRhRdkDocument::AttachContent() or CRhRdkContent::SetChild() methods, or to a decal.
      Change, // Content is being attached while it is replacing another one.
      Undo  , // Content is being attached during undo/redo.
      Open  , // Content is being attached during open document.
      Edit  , // Content is being attached during modal editing.
    };

    public enum DetachReason : uint
    {
      Detach, // Content is being detached by the CRhRdkDocument::DetachContent() method, or from a decal.
      Change, // Content is being detached while it is being replaced.
      Undo  , // Content is being detached during undo/redo.
      Delete, // Content is being detached during normal deletion.
      Edit  , // Content is being detached during modal editing.
    };

    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentDatabaseEventInfo(IntPtr pContentEventInfo)
    {
      m_cpp = pContentEventInfo;
    }

    ~ContentDatabaseEventInfo()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderContent Content()
    {
      RenderContent content = null;
      IntPtr content_pointer = IntPtr.Zero;

      content_pointer = UnsafeNativeMethods.ContentDatabaseEventInfo_Content(m_cpp);

      if (content_pointer != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(content_pointer);
      }

      return content;
    }

    public Types Type()
    {
      Types type = Types.Attached;
      uint type_value = UnsafeNativeMethods.ContentDatabaseEventInfo_Type(m_cpp);

      if (type_value == 0)
        type = Types.Attached;
      if (type_value == 1)
        type = Types.Detaching;
      if (type_value == 2)
        type = Types.Detached;
      if (type_value == 3)
        type = Types.Replacing;
      if (type_value == 4)
        type = Types.Replaced;
      if (type_value == 5)
        type = Types.Blossom;
      
      return type;
    }

    public ulong Extra()
    {
      return UnsafeNativeMethods.ContentDatabaseEventInfo_Extra(m_cpp);
    }
  }

  class ContentUpdatePreviewEventInfo : IDisposable
  {

    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentUpdatePreviewEventInfo(IntPtr pContentEventInfo)
    {
      m_cpp = pContentEventInfo;
    }

    ~ContentUpdatePreviewEventInfo()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderContent Content()
    {
      RenderContent content = null;
      IntPtr content_pointer = IntPtr.Zero;

      content_pointer = UnsafeNativeMethods.ContentUpdatePreviewEventInfo_Content(m_cpp);

      if (content_pointer != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(content_pointer);
      }

      return content;
    }
  }

  class ImageFileInfo : IDisposable
  {

    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ImageFileInfo(IntPtr pImageFileInfo)
    {
      m_cpp = pImageFileInfo;
    }

    ~ImageFileInfo()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public String ImageInfo(uint doc_serial, String filename)
    {
      using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
      {
        using (var sf = new Rhino.Runtime.InteropWrappers.StringWrapper(filename))
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ImageFile_ImageInfo(m_cpp, doc_serial, sf.ConstPointer, p_string);
          return sh.ToString();
        }
      }
    }

    public System.Drawing.Bitmap Image(uint doc_serial, String filename, System.Drawing.Color tColor, bool bTransparency, double dTransparency, System.Drawing.Size s)
    {
      IntPtr pDib = IntPtr.Zero;

      using (var sf = new Rhino.Runtime.InteropWrappers.StringWrapper(filename))
      {
        pDib = UnsafeNativeMethods.ImageFile_Image(m_cpp, doc_serial, bTransparency, tColor.R, tColor.G, tColor.B, dTransparency, sf.ConstPointer, s.Width, s.Height);
      }

      if (pDib == IntPtr.Zero)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);

      return bitmap;
    }
    public int ImageWidth(uint doc_serial, String filename)
    {
      using (var sf = new Rhino.Runtime.InteropWrappers.StringWrapper(filename))
      {
        return UnsafeNativeMethods.ImageFile_ImageWidth(m_cpp, doc_serial, sf.ConstPointer);
      }
    }

    public int ImageHeight(uint doc_serial, String filename)
    {
      using (var sf = new Rhino.Runtime.InteropWrappers.StringWrapper(filename))
      {
        return UnsafeNativeMethods.ImageFile_ImageHeight(m_cpp, doc_serial, sf.ConstPointer);
      }
    }
  }

  // IRhRdkContentChildSlot
  sealed class RdkContentChildSlot : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkContentChildSlot(IntPtr pRdkContentChildSlot)
    {
      m_cpp = pRdkContentChildSlot;
    }

    ~RdkContentChildSlot()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public void ExecuteChangeTask()
    {
      UnsafeNativeMethods.ContentChildSlot_ExecuteChangeTask(m_cpp);
    }

    public void ExecuteRemoveTask()
    {
      UnsafeNativeMethods.ContentChildSlot_ExecuteRemoveTask(m_cpp);
    }

    public bool IsChildLocked()
    {
      return UnsafeNativeMethods.ContentChildSlot_IsChildLocked(m_cpp);
    }

    public bool AtLeastOneGoodChild()
    {
      return UnsafeNativeMethods.ContentChildSlot_AtLeastOneGoodChild(m_cpp);
    }

    public RenderContentCollection GetChildList()
    {
      RenderContentCollection collection = new RenderContentCollection();

      UnsafeNativeMethods.ContentChildSlot_GetChildList(m_cpp, collection.CppPointer);

      return collection;
    }
  }
  // RdkMenu
  internal abstract class RdkMenu
  {
    protected IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get
      {
        return m_cpp;
      }
    }

    public abstract IntPtr AddSubMenu(string caption);

    public abstract bool AddItem(string caption, ushort cmd, bool bEnabled);

    public abstract void AddSeparator();
  }

  // IRhRdkIORMenuData
  internal class RdkIORMenuData : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RdkIORMenuData(IntPtr pRdkIORMenuData)
    {
      m_cpp = pRdkIORMenuData;
    }

    ~RdkIORMenuData()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public bool BuildMenu(RdkMenu menu)
    {
      return UnsafeNativeMethods.IRhRdkIORMenuData_BuildMenu(m_cpp, menu.CppPointer);
    }

    public bool BuildMenu(string filename, RdkMenu menu)
    {
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(filename);
      var p_string = sh.ConstPointer;

      return UnsafeNativeMethods.IRhRdkIORMenuData_BuildMenuFilename(m_cpp, p_string, menu.CppPointer);
    }

    public bool GetIORForCmd(ushort cmd, ref double dIOR)
    {
      return UnsafeNativeMethods.IRhRdkIORMenuData_GetIORForCmd(m_cpp, cmd, ref dIOR);
    }
  }
}
#endregion

