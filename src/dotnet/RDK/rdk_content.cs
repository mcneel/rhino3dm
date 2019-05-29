#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Rhino.Render.Fields;
using Rhino.Runtime.InteropWrappers;
using System.Collections;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Render.UI
{
  /// <summary>
  /// Implement this interface in your user control to get UserInterfaceSection
  /// event notification.
  /// </summary>
  [Obsolete]
  public interface IUserInterfaceSection
  {
    /// <summary>
    /// Called by UserInterfaceSection when the selected content changes or a
    /// content field property value changes.
    /// </summary>
    /// <param name="userInterfaceSection">
    /// The UserInterfaceSection object that called this interface method.
    /// </param>
    /// <param name="renderContentList">
    /// The currently selected list of content items to edit.
    /// </param>
    void UserInterfaceDisplayData(UserInterfaceSection userInterfaceSection, RenderContent[] renderContentList);
    /// <summary>
    /// The UserInterfaceSection object that called this interface method.
    /// </summary>
    /// <param name="userInterfaceSection">
    /// The UserInterfaceSection object that called this interface method.
    /// </param>
    /// <param name="expanding">
    /// Will be true if the control has been createExpanded or false if it was
    /// collapsed.
    /// </param>
    void OnUserInterfaceSectionExpanding(UserInterfaceSection userInterfaceSection, bool expanding);
    /// <summary>
    /// Return true if the section should be hidden, else return false.
    /// </summary>
    /// <remarks>
    /// This is not to be confused with IsShown(). Hidden tells the holder
    /// whether or not to hide the section. IsShown reports the current
    /// physical state of the control.
    /// </remarks>
    /// <remarks>
    /// This method is called continuously by the holder and should be
    /// implemented to be as fast as possible.  It is recommended that the
    /// state be calculated and cached in <see cref="UserInterfaceDisplayData"/>
    /// and the cached value returned here.
    /// </remarks>
    bool Hidden { get; }
  }

  /// <summary>
  /// Custom user interface section manager
  /// </summary>
  [Obsolete]
  public class UserInterfaceSection
  {
    #region Internals
    /// <summary>
    /// Internal constructor
    /// </summary>
    /// <param name="serialNumber">C++ pointer serial number returned by the C interface wrapper.</param>
    /// <param name="window">The control created and embedded in the expandable tab control in the content browser.</param>
    internal UserInterfaceSection(int serialNumber, object window)
    {
      m_serial_number = serialNumber;
      m_window = window;
      if (serialNumber > 0) g_user_interface_section_dictionary.Add(serialNumber, this);
      SetHooks();
    }
    /// <summary>
    /// C++ pointer serial number returned by the C interface wrapper.
    /// </summary>
    internal int SerialNumber { get { return m_serial_number; } }
    #endregion Internals

    #region private members
    /// <summary>
    /// The control created and embedded in the expandable tab control in the content browser.
    /// </summary>
    private object m_window;
    /// <summary>
    /// C++ pointer serial number returned by the C interface wrapper.
    /// </summary>
    private readonly int m_serial_number;
    /// <summary>
    /// Search hint helper
    /// </summary>
    private int m_search_hint = -1;
    /// <summary>
    /// UserInterfaceSection instance dictionary, the constructor adds objects to the dictionary
    /// and the C++ destructor callback removes them when they get destroyed.
    /// </summary>
    static private readonly Dictionary<int, UserInterfaceSection> g_user_interface_section_dictionary = new Dictionary<int, UserInterfaceSection>();
    #endregion private members

    #region C++ function callbacks
    /// <summary>
    /// Set C++ callback function pointers
    /// </summary>
    private static void SetHooks()
    {
      // Need to set the hook using a static member variable otherwise it gets garbage
      // collected to early on shutdown and the C++ code attempts to make a callback
      // on a garbage collected function pointer.
      g_delete_this_proc = DeleteThisProc;
      g_display_data_proc = DisplayDataProc;
      g_on_expand_callback = OnExpandProc;
      g_is_hidden_callback = IsHiddenProc;
      UnsafeNativeMethods.Rdk_ContentUiSectionSetCallbacks(g_delete_this_proc, g_display_data_proc, g_on_expand_callback, g_is_hidden_callback);
    }
    /// <summary>
    /// Delegate used by Imports.cs for internal C++ method callbacks
    /// </summary>
    /// <param name="serialNumber">Runtime C++ memory pointer serial number.</param>
    internal delegate int SerialNumberCallback(int serialNumber);
    /// <summary>
    /// Delegate used by Imports.cs for internal C++ method callbacks
    /// </summary>
    /// <param name="serialNumber">Runtime C++ memory pointer serial number.</param>
    /// <param name="b"></param>
    internal delegate void SerialNumberBoolCallback(int serialNumber, bool b);
    /// <summary>
    /// Called by the C++ destructor when a user interface section object is destroyed.
    /// </summary>
    private static SerialNumberCallback g_delete_this_proc;
    /// <summary>
    /// Called by the C++ SDK when it is time to initialize a user interface section.
    /// </summary>
    private static SerialNumberCallback g_display_data_proc;
    /// <summary>
    /// Called by the C++ SDK when a user interface section is being createExpanded
    /// or collapsed.
    /// </summary>
    private static SerialNumberBoolCallback g_on_expand_callback;
    /// <summary>
    /// Called by the C++ SDK to determine if a user interface section should be hidden
    /// </summary>
    private static SerialNumberCallback g_is_hidden_callback;
    /// <summary>
    /// C++ user interface destructor callback, remove the object from the runtime
    /// dictionary and dispose if it if possible.
    /// </summary>
    /// <param name="serialNumber"></param>
    private static int DeleteThisProc(int serialNumber)
    {
      var ui_section = FromSerialNumber(serialNumber);
      if (ui_section == null)
        return 0;
      if (ui_section.m_window is IDisposable) (ui_section.m_window as IDisposable).Dispose();
      ui_section.m_window = null;
      g_user_interface_section_dictionary.Remove(serialNumber);
      return 1;
    }
    /// <summary>
    /// Called when it is safe to initialize the control window.
    /// </summary>
    /// <param name="serialNumber"></param>
    static private int DisplayDataProc(int serialNumber)
    {
      UserInterfaceSection ui_section;
      IUserInterfaceSection i_ui_section;
      if (!UiFromSerialNumber(serialNumber, out ui_section, out i_ui_section))
        return 0;
      var render_content_list = ui_section.GetContentList();
      i_ui_section.UserInterfaceDisplayData(ui_section, render_content_list);
      return 1;
    }
    /// <summary>
    /// Called when a user interface section is being createExpanded or collapsed.
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <param name="expanding"></param>
    static private void OnExpandProc(int serialNumber, bool expanding)
    {
      UserInterfaceSection ui_section;
      IUserInterfaceSection i_ui_section;
      if (UiFromSerialNumber(serialNumber, out ui_section, out i_ui_section))
        i_ui_section.OnUserInterfaceSectionExpanding(ui_section, expanding);
    }

    /// <summary>
    /// Called when it is safe to initialize the control window.
    /// </summary>
    /// <param name="serialNumber"></param>
    private static int IsHiddenProc(int serialNumber)
    {
      var value = 0;
      UserInterfaceSection ui_section;
      IUserInterfaceSection i_ui_section;
      if (UiFromSerialNumber(serialNumber, out ui_section, out i_ui_section))
        value = i_ui_section.Hidden ? 1 : 0;
      return value;
    }

    #endregion C++ function callbacks

    #region Public properties
    /// <summary>
    /// The RenderContent object that created this user interface object.
    /// </summary>
    public RenderContent RenderContent
    {
      get
      {
        var pointer = UnsafeNativeMethods.Rdk_CoreContent_RenderContentFromUISection(SerialNumber, ref m_search_hint);
        var found = RenderContent.FromPointer(pointer);
        return found;
      }
    }
    /// <summary>
    /// The user control associated with this user interface object.
    /// </summary>
    public object Window { get { return m_window; } }
    #endregion Public properties

    #region Public methods
    /// <summary>
    /// Find the UserInterfaceSection that created the specified instance of a
    /// window.
    /// </summary>
    /// <param name="window">
    /// If window is not null then look for the UserInterfaceSection that
    /// created the window.
    /// </param>
    /// <returns>
    /// If a UserInterfaceSection object is found containing a reference to
    /// the requested window then return the object otherwise return null.
    /// </returns>
    public static UserInterfaceSection FromWindow(object window)
    {
      if (null != window)
        foreach (var section in g_user_interface_section_dictionary)
          if (window.Equals(section.Value.Window)) return section.Value;
      return null;
    }
    /// <summary>
    /// Returns a list of currently selected content items to be edited.
    /// </summary>
    /// <returns>Returns a list of currently selected content items to be edited.</returns>
    public RenderContent[] GetContentList()
    {
      var id_list = GetContentIdList();
      var render_content_list = new List<RenderContent>(id_list.Length);
      var doc = RhinoDoc.ActiveDoc;
      foreach (var guid in id_list)
      {
        var content = RenderContent.FromId(doc, guid);
        if (null != content) render_content_list.Add(content);
      }
      return render_content_list.ToArray();
    }
    /// <summary>
    /// Show or hide this content section.
    /// </summary>
    /// <param name="visible">If true then show the content section otherwise hide it.</param>
    public void Show(bool visible)
    {
      // This function is obsolete. Sections must hide and show by using Hidden [ANDYLOOK]
      var serial_number = SerialNumber;
      UnsafeNativeMethods.Rdk_CoreContent_UiSectionShow(serial_number, ref m_search_hint, visible);
    }
    /// <summary>
    /// Expand or collapse this content section.
    /// </summary>
    /// <param name="expand">If true then expand the content section otherwise collapse it.</param>
    public void Expand(bool expand)
    {
      var serial_number = SerialNumber;
      UnsafeNativeMethods.Rdk_CoreContent_UiSectionExpand(serial_number, ref m_search_hint, expand);
    }
    #endregion Public methods

    #region Private properties
    /// <summary>
    /// Dereference the serial number as a C++ pointer, used for direct access to the C++ object.
    /// </summary>
    private IntPtr Pointer
    {
      get
      {
        var pointer = UnsafeNativeMethods.Rdk_CoreContent_AddFindContentUISectionPointer(SerialNumber, ref m_search_hint);
        return pointer;
      }
    }
    #endregion Private properties

    #region Private Methods
    /// <summary>
    /// Returns a list of currently selected content item Id's to be edited.
    /// </summary>
    /// <returns>Returns a list of currently selected content item Id's to be edited.</returns>
    private Guid[] GetContentIdList()
    {
      using (var id_list = new SimpleArrayGuid())
      {
        var pointer_to_id_list = id_list.NonConstPointer();
        var serial_number = SerialNumber;
        UnsafeNativeMethods.Rdk_CoreContent_UiSectionContentIdList(serial_number, ref m_search_hint, pointer_to_id_list);
        return id_list.ToArray();
      }
    }
    /// <summary>
    /// Look up a runtime instance of an user interface object by serial number.
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <returns></returns>
    static private UserInterfaceSection FromSerialNumber(int serialNumber)
    {
      UserInterfaceSection found;
      g_user_interface_section_dictionary.TryGetValue(serialNumber, out found);
      return found;
    }
    /// <summary>
    /// Look up a runtime instance of an user interface object by serial number
    /// and check the user interface Window object for a IUserInterfaceSection
    /// instance.
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <param name="uiSection"></param>
    /// <param name="iUiSection"></param>
    /// <returns>
    /// Returns true if both uiSection and iUiSection are non null otherwise;
    /// return false.
    /// </returns>
    static private bool UiFromSerialNumber(int serialNumber, out UserInterfaceSection uiSection, out IUserInterfaceSection iUiSection)
    {
      iUiSection = null;
      uiSection = FromSerialNumber(serialNumber);
      if (null == uiSection) return false;
      iUiSection = uiSection.Window as IUserInterfaceSection;
      return (null != iUiSection);
    }
    #endregion Private Methods
  }
}

namespace Rhino.Render
{
  [Flags]
  public enum RenderContentStyles : int
  {
    /// <summary>
    /// No defined styles
    /// </summary>
    None = 0,
    /// <summary>
    /// Texture UI includes an auto texture summary section. See AddAutoParameters().
    /// </summary>
    TextureSummary = 0x0001,
    /// <summary>
    /// Editor displays an instant preview before preview cycle begins.
    /// </summary>
    QuickPreview = 0x0002,
    /// <summary>
    /// Content's preview imagery can be stored in the preview cache.
    /// </summary>
    PreviewCache = 0x0004,
    /// <summary>
    /// Content's preview imagery can be rendered progressively.
    /// </summary>
    ProgressivePreview = 0x0008,
    /// <summary>
    /// Texture UI includes an auto local mapping section for textures. See AddAutoParameters()
    /// </summary>
    LocalTextureMapping = 0x0010,
    /// <summary>
    /// Texture UI includes a graph section.
    /// </summary>
    GraphDisplay = 0x0020,
    /// <summary>
    /// Content supports UI sharing between contents of the same type id.
    /// </summary>
    [Obsolete]
    SharedUI = 0x0040,
    /// <summary>
    /// Texture UI includes an adjustment section.
    /// </summary>
    Adjustment = 0x0080,
    /// <summary>
    /// Content uses fields to facilitate data storage and undo support. See Fields()
    /// </summary>
    Fields = 0x0100,
    /// <summary>
    /// Content supports editing in a modal editor.
    /// </summary>
    ModalEditing = 0x0200,
    /// <summary>
    /// The content's fields are dynamic. Dynamic fields can be created during loading.
    /// </summary>
    DynamicFields = 0x0400,
  }

  /// <summary>
  /// Content Guids of RenderContent provided by the RDK SDK.
  /// 
  /// These Guids can be used to check against RenderContent.TypeId.
  /// </summary>
  public static class ContentUuids {
    public static Guid BasicMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBasicMaterialType);
    public static Guid BlendMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBlendMaterialType);
    public static Guid CompositeMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcCompositeMaterialType);
    public static Guid PlasterMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcPlasterMaterialType);
    public static Guid MetalMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcMetalMaterialType);
    public static Guid PaintMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcPaintMaterialType);
    public static Guid PlasticMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcPlasticMaterialType);
    public static Guid GemMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcGemMaterialType);
    public static Guid GlassMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcGlassMaterialType);
    public static Guid PictureMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcPictureMaterialType);
    public static Guid DefaultMaterialInstance => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcDefaultMaterialInstance);
    
    
    public static Guid RealtimeDisplayMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcRealtimeDisplayMaterialType);
    
    
    public static Guid BasicEnvironmentType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBasicEnvironmentType);
    public static Guid DefaultEnvironmentInstance => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcDefaultEnvironmentInstance);
    
    
    public static Guid Texture2DCheckerTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.Rc2DCheckerTextureType);
    public static Guid Texture3DCheckerTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.Rc3DCheckerTextureType);
    public static Guid AdvancedDotTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcAdvancedDotTextureType);
    public static Guid BitmapTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBitmapTextureType);
    public static Guid BlendTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBlendTextureType);
    public static Guid CubeMapTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcCubeMapTextureType);
    public static Guid ExposureTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcExposureTextureType);
    public static Guid FBmTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcFBmTextureType);
    public static Guid GradientTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcGradientTextureType);
    public static Guid GraniteTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcGraniteTextureType);
    public static Guid GridTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcGridTextureType);
    public static Guid HDRTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcHDRTextureType);
    public static Guid EXRTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcEXRTextureType);
    public static Guid MarbleTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcMarbleTextureType);
    public static Guid MaskTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcMaskTextureType);
    public static Guid NoiseTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcNoiseTextureType);
    public static Guid PerlinMarbleTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcPerlinMarbleTextureType);
    public static Guid PerturbingTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcPerturbingTextureType);
    public static Guid ProjectionChangerTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcProjectionChangerTextureType);
    public static Guid ResampleTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcResampleTextureType);
    public static Guid SingleColorTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcSingleColorTextureType);
    public static Guid SimpleBitmapTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcSimpleBitmapTextureType);
    public static Guid StuccoTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcStuccoTextureType);
    public static Guid TextureAdjustmentTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcTextureAdjustmentTextureType);
    public static Guid TileTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcTileTextureType);
    public static Guid TurbulenceTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcTurbulenceTextureType);
    public static Guid WavesTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcWavesTextureType);
    public static Guid WoodTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcWoodTextureType);
    
    
    public static Guid HatchBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcHatchBumpTexture);
    public static Guid CrossHatchBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcCrossHatchBumpTexture);
    public static Guid LeatherBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcLeatherBumpTexture);
    public static Guid WoodBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcWoodBumpTexture);
    public static Guid SpeckleBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcSpeckleBumpTexture);
    public static Guid GritBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcGritBumpTexture);
    public static Guid DotBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcDotBumpTexture);
    
    
    public static Guid BasicMaterialCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBasicMaterialCCI);
    public static Guid BlendMaterialCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBlendMaterialCCI);
    public static Guid CompositeMaterialCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcCompositeMaterialCCI);
    public static Guid BasicEnvironmentCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.RenderContent_UuidIds.RcBasicEnvironmentCCI);
  }

  public enum DynamicIconUsage 
  {
    /// <summary>
    /// Dynamic icon appears in a tree control.
    /// </summary>
    TreeControl = 0, 
    /// <summary>
    /// Dynamic icon appears in a sub-node control (\see CRhRdkSubNodeCtrl)
    /// </summary>
    SubnodeControl = 1, 
    /// <summary>
    /// Dynamic icon appears in a content control  (\see CRhRdkContentCtrl)
    /// </summary>
    ContentControl = 2, 
  };

  [AttributeUsage(AttributeTargets.Class)]
  /*public*/
  public sealed class CustomRenderContentAttribute : Attribute
  {
    private readonly Guid m_renderengine_id;

    public CustomRenderContentAttribute(string renderEngineGuid = "", bool imageBased = false, string category = "General", bool is_elevated = false, bool is_built_in = false, bool is_private = false)
    {
      m_renderengine_id = Guid.Empty;

      try
      {
        if (!string.IsNullOrEmpty(renderEngineGuid))
        {
          m_renderengine_id = new Guid(renderEngineGuid);
        }
      }
      catch (Exception)
      {
      }

      ImageBased = imageBased;
      Category = category;
      IsElevated = is_elevated;
      IsBuiltIn = is_built_in;
      IsPrivate = is_private;
    }

    public Guid RenderEngineId
    {
      get { return m_renderengine_id; }
    }

    public bool ImageBased { get; set; }
    public string Category { get; set; }
    public bool IsElevated { get; set; }
    public bool IsBuiltIn { get; set; }
    public bool IsPrivate { get; set; }
  }

  /// <summary>
  /// Defines constant values for all render content kinds, such as material,
  /// environment or texture.
  /// </summary>
  [Flags]
  public enum RenderContentKind : int
  {
    None = UnsafeNativeMethods.CRhRdkContentKindConst.None,
    Material = UnsafeNativeMethods.CRhRdkContentKindConst.Material,
    Environment = UnsafeNativeMethods.CRhRdkContentKindConst.Environment,
    Texture = UnsafeNativeMethods.CRhRdkContentKindConst.Texture,
  }

  /// <summary>
  /// Defines the collection type to iterate. 
  /// </summary>
  public enum it_strategy
  { 
    ContentDataBase, // This type represents all the render contents in the database. 
    ContentSelection // This type represents the selected render content collection.
  }

  /// <summary>
  /// Defines the proxy type of the render content 
  /// </summary>
  public enum ProxyTypes
  {
    None, Single, Multi
  };

  public sealed class RenderContentKindList : IDisposable
  {
    private IntPtr m_cpp;
    private bool delete = false;

    public IntPtr CppPointer {
      get { return m_cpp; }
    }

    public RenderContentKindList()
    {
      delete = true;
      m_cpp = UnsafeNativeMethods.CRhRdkContentKindList_New();
    }

    public RenderContentKindList(RenderContentKindList kind_list)
    {
      delete = true;
      m_cpp = UnsafeNativeMethods.CRhRdkContentKindList_CopyNew(kind_list.CppPointer);
    }

    public RenderContentKindList (IntPtr pRdkRenderContentKindList)
    {
      delete = false;
      m_cpp = pRdkRenderContentKindList;
    }

    ~RenderContentKindList ()
    {
      Dispose (false);
    }

    public void Dispose ()
    {
      Dispose (true);
      GC.SuppressFinalize (this);
    }

    void Dispose (bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        if (delete)
          UnsafeNativeMethods.CRhRdkContentKindList_Delete(m_cpp);

        m_cpp = IntPtr.Zero;
      }
    }

    public void Add(RenderContentKind kind)
    {
      UnsafeNativeMethods.CRhRdkContentKindList_Add(m_cpp, (int)kind);
    }

    public int Count()
    {
      return UnsafeNativeMethods.CRhRdkContentKindList_Count (m_cpp);
    }

    public RenderContentKind SingleKind ()
    {
      uint kind = UnsafeNativeMethods.CRhRdkContentKindList_SingleKind (m_cpp);

      if (kind == 0)
        return RenderContentKind.None;
      if (kind == 1)
        return RenderContentKind.Material;
      if (kind == 2)
        return RenderContentKind.Environment;
      if (kind == 4)
        return RenderContentKind.Texture;
      
      return RenderContentKind.None;
    }

    public bool Contains(RenderContentKind kind)
    {
      return UnsafeNativeMethods.CRhRdkContentKindList_Contains (m_cpp, (uint)kind);
    }

  }

  /// <summary>
  /// Content collection filter value
  /// </summary>
  public enum FilterContentByUsage
  {
    /// <summary>
    /// No filter in use
    /// </summary>
    None,
    /// <summary>
    /// Display only used contents
    /// </summary>
    Used,
    /// <summary>
    /// Display only unused contents
    /// </summary>
    Unused,
  }

  public sealed class RenderContentCollection : IDisposable, IEnumerable
  {
    private IntPtr m_cpp;
    private bool m_delete_cpp_pointer;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderContentCollection()
    {
      m_cpp = UnsafeNativeMethods.CRhRdkContentArray_New();
      m_delete_cpp_pointer = true;
    }

    public RenderContentCollection(IntPtr nativePtr)
    {
      m_cpp = nativePtr;
      m_delete_cpp_pointer = false;
    }

    ~RenderContentCollection()
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
        if (m_delete_cpp_pointer)
          UnsafeNativeMethods.CRhRdkContentArray_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public FilterContentByUsage GetFilterContentByUsage()
    {
      int filter_value = UnsafeNativeMethods.IRhRdkContentCollection_GetFilterContentByUsage(m_cpp);

      FilterContentByUsage filter = FilterContentByUsage.None;

      if (filter_value == 0)
        filter = FilterContentByUsage.None;
      if (filter_value == 1)
        filter = FilterContentByUsage.Used;
      if (filter_value == 2)
        filter = FilterContentByUsage.Unused;

      return filter;
    }

    public bool GetForcedVaries()
    {
      return UnsafeNativeMethods.IRhRdkContentCollection_GetForcedVaries(m_cpp);
    }

    public void SetForcedVaries(bool b)
    {
      UnsafeNativeMethods.IRhRdkContentCollection_SetForcedVaries(m_cpp, b);
    }

    public void SetSearchPattern(string pattern)
    {
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(pattern);
      var p_string = sh.ConstPointer;

      UnsafeNativeMethods.IRhRdkContentCollection_SetSearchPattern(m_cpp, p_string);
    }

    public string GetSearchPattern()
    {
      using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.IRhRdkContentCollection_GetSearchPattern(m_cpp, p_string);
        return sh.ToString();
      }
    }

    public string FirstTag()
    {
      using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.IRhRdkContentCollection_FirstTag(m_cpp, p_string);
        return sh.ToString();
      }
    }

    public string NextTag()
    {
      using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.IRhRdkContentCollection_NextTag(m_cpp, p_string);
        return sh.ToString();
      }
    }

    public void Remove(Rhino.Render.RenderContentCollection collection)
    {
      IntPtr value = IntPtr.Zero;

      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Remove(CppPointer, collection.CppPointer);
      }
    }

    public void Add(Rhino.Render.RenderContentCollection collection)
    {
      IntPtr value = IntPtr.Zero;

      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Add(CppPointer, collection.CppPointer);
      }
    }

    public void Set(Rhino.Render.RenderContentCollection collection)
    {
      IntPtr value = IntPtr.Zero;

      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Set(CppPointer, collection.CppPointer);
      }
    }

    public void Clear()
    {
      IntPtr value = IntPtr.Zero;

      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Clear(CppPointer);
      }
    }

    public void Append(Rhino.Render.RenderContent content)
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkContentArray_Add(m_cpp, content.CppPointer);
      }
    }

    public int Count()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.CRhRdkContentArray_Count(m_cpp);
      }

      return -1;
    }

    public ContentCollectionIterator Iterator()
    {
      ContentCollectionIterator value = null;

      if (CppPointer != IntPtr.Zero)
      {
        value = new ContentCollectionIterator(UnsafeNativeMethods.IRhRdkContentCollection_IIterator(CppPointer));
      }

      return value;
    }

    public RenderContent Find_Sel(Guid uuid)
    {
      RenderContent content = null;

      ContentCollectionIterator iterator = Iterator();
      content = iterator.First();

      while (content != null)
      {
        if (content.Id.ToString().CompareTo(uuid.ToString()) == 0)
        {
          iterator.DeleteThis();
          return content;
        }
        content = iterator.Next();
      }

      iterator.DeleteThis();
      return null;
    }

    public RenderContent ContentAt(int index)
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContent = UnsafeNativeMethods.CRhRdkContentArray_ContentAt(m_cpp, index);
        RenderContent content = RenderContent.FromPointer(pContent);
        return content;
      }

      return null;
    }

    public IEnumerator GetEnumerator()
    {
      RenderContent content = null;
      ContentCollectionIterator iterator = Iterator();

      content = iterator.First();

      while (content != null)
      {
        yield return content;
        content = iterator.Next();
      }

      iterator.DeleteThis();
    }
  }

  public sealed class ContentCollectionIterator : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentCollectionIterator(IntPtr pCollection)
    {
      m_cpp = pCollection;
    }

    ~ContentCollectionIterator()
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
        UnsafeNativeMethods.IIterator_DeleteThis(m_cpp);
      }
    }

    public void DeleteThis()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.IIterator_DeleteThis(m_cpp);
      }
    }

    public RenderContent First()
    {
      RenderContent content = null;

      if (m_cpp != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(UnsafeNativeMethods.IIterator_First(m_cpp));
      }

      return content;
    }

    public RenderContent Next()
    {
      RenderContent content = null;

      if (m_cpp != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(UnsafeNativeMethods.IIterator_Next(m_cpp));
      }

      return content;
    }
  }

  public abstract class RenderContent : IDisposable
  {
    #region Kinds (internal)
    internal static String KindString(RenderContentKind kinds)
    {
      var sb = new System.Text.StringBuilder();

      if ((kinds & RenderContentKind.Material) == RenderContentKind.Material)
      {
        sb.Append("material");
      }

      if ((kinds & RenderContentKind.Environment) == RenderContentKind.Environment)
      {
        if (sb.Length != 0)
        {
          sb.Append(";");
        }
        sb.Append("environment");
      }

      if ((kinds & RenderContentKind.Texture) == RenderContentKind.Texture)
      {
        if (sb.Length != 0)
        {
          sb.Append(";");
        }
        sb.Append("texture");
      }
      return sb.ToString();
    }

    /// <summary>
    /// Convert unmanaged UnsafeNativeMethods.CRhRdkContentKindConst to managed
    /// RenderContentKind.
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    static internal RenderContentKind ConvertConentKind(UnsafeNativeMethods.CRhRdkContentKindConst kind)
    {
      var result = RenderContentKind.None;
      if (0 != (UnsafeNativeMethods.CRhRdkContentKindConst.Material & kind))
        result |= RenderContentKind.Material;
      if (0 != (UnsafeNativeMethods.CRhRdkContentKindConst.Environment & kind))
        result |= RenderContentKind.Environment;
      if (0 != (UnsafeNativeMethods.CRhRdkContentKindConst.Texture & kind))
        result |= RenderContentKind.Texture;
      return result;
    }
    #endregion

    #region statics
    public enum ShowContentChooserFlags : int
    {
      None = 0x0000,
      HideNewTab = 0x0001,
      HideExistingTab = 0x0002,
    };

    /// <summary>
    /// Constructs a new content of the specified type and add it to the persistent content list.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromType().
    /// </summary>
    /// <param name="type">Is the type of the content to add.</param>
    /// <param name="flags">Options for the tab.</param>
    /// <param name="doc">The current Rhino document.</param>
    /// <returns>A new persistent render content.</returns>
    public static RenderContent Create(Guid type, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      IntPtr ptr_content = UnsafeNativeMethods.Rdk_Globals_CreateContentByType(type, IntPtr.Zero, String.Empty, (int)flags, doc.RuntimeSerialNumber);
      return ptr_content == IntPtr.Zero ? null : FromPointer(ptr_content);
    }

    /// <summary>
    /// Constructs a new content of the specified type and add it to the persistent content list.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromType().
    /// </summary>
    /// <param name="type">Is the type of the content to add.</param>
    /// <param name="flags">Options for the tab.</param>
    /// <param name="doc">The current Rhino document.</param>
    /// <returns>A new persistent render content.</returns>
    public static RenderContent Create(Type type, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      return Create(type.GUID, flags, doc);
    }

    /// <summary>
    /// Constructs a new content of the specified type and add it to the persistent content list.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromType().
    /// </summary>
    /// <param name="type">is the type of the content to add.</param>
    /// <param name="parent">Parent is the parent content. If not NULL, this must be an RDK-owned content that is
    /// in the persistent content list (either top-level or child). The new content then becomes its child.
    /// If NULL, the new content is added to the top-level content list instead.</param>
    /// <param name="childSlotName">ChildSlotName is the unique child identifier to use for the new content when creating it as a child of pParent (i.e., when pParent is not NULL)</param>
    /// <param name="flags">Options for the tab.</param>
    /// <param name="doc">The current Rhino document.</param>
    /// <returns>A new persistent render content.</returns>
    public static RenderContent Create(Guid type, RenderContent parent, String childSlotName, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      IntPtr ptr_content = UnsafeNativeMethods.Rdk_Globals_CreateContentByType(type, parent.ConstPointer(), childSlotName, (int)flags, doc.RuntimeSerialNumber);
      return ptr_content == IntPtr.Zero ? null : FromPointer(ptr_content);
    }

    /// <summary>
    /// Constructs a new content of the specified type and add it to the persistent content list.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromType().
    /// </summary>
    /// <param name="type">is the type of the content to add.</param>
    /// <param name="parent">Parent is the parent content. If not NULL, this must be an RDK-owned content that is
    /// in the persistent content list (either top-level or child). The new content then becomes its child.
    /// If NULL, the new content is added to the top-level content list instead.</param>
    /// <param name="childSlotName">ChildSlotName is the unique child identifier to use for the new content when creating it as a child of pParent (i.e., when pParent is not NULL)</param>
    /// <param name="flags">Options for the tab.</param>
    /// <param name="doc">The current Rhino document.</param>
    /// <returns>A new persistent render content.</returns>
    public static RenderContent Create(Type type, RenderContent parent, String childSlotName, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      return Create(type.GUID, parent, childSlotName, flags, doc);
    }

    /// <summary>
    /// Call RegisterContent in your plug-in's OnLoad function in order to register all of the
    /// custom RenderContent classes in your assembly.
    /// </summary>
    /// <param name="plugin"></param>
    /// <returns>array of render content types registered on success. null on error.</returns>
    public static Type[] RegisterContent(PlugIns.PlugIn plugin)
    {
      return RegisterContent(plugin.Assembly, plugin.Id);
    }
    /// <summary>
    /// Call RegisterContent in your plug-in's OnLoad function in order to register all of the
    /// custom RenderContent classes in your assembly.
    /// </summary>
    /// <param name="assembly">
    /// Assembly where custom content is defined, this may be a plug-in assembly
    /// or another assembly referenced by the plug-in.
    /// </param>
    /// <param name="pluginId">Parent plug-in for this assembly.</param>
    /// <returns>array of render content types registered on success. null on error.</returns>
    public static Type[] RegisterContent(Assembly assembly, Guid pluginId)
    {
      // Check the Rhino plug-in for a RhinoPlugIn with the specified Id
      var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
      // RhinoPlugIn not found so bail, all content gets associated with a plug-in!
      if (plugin == null) return null;
      // Get a list of the publicly exported class types from the requested assembly
      var exported_types = assembly.GetExportedTypes();
      // Scan the exported class types for RenderContent derived classes
      var content_types = new List<Type>();
      var render_content_type = typeof(RenderContent);
      for (var i = 0; i < exported_types.Length; i++)
      {
        var exported_type = exported_types[i];
        // If abstract class or not derived from RenderContent or does not contain a public constructor then skip it
        if (exported_type.IsAbstract || !exported_type.IsSubclassOf(render_content_type) || exported_type.GetConstructor(new Type[] { }) == null)
          continue;
        // Check the class type for a GUID custom attribute
        var attr = exported_type.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
        // If the class does not have a GUID custom attribute then throw an exception
        if (attr.Length < 1) throw new InvalidDataException("Class \"" + exported_type + "\" must include a GUID attribute");
        // Add the class type to the content list
        content_types.Add(exported_type);
      }
      // If this plug-in does not contain any valid RenderContent derived objects then bail
      if (content_types.Count == 0) return null;

      // make sure that content types have not already been registered
      foreach (var content_type in content_types)
        if (RdkPlugIn.RenderContentTypeIsRegistered(content_type))
          return null; // Bail out because this type was previously registered

      // Get the RdkPlugIn associated with this RhinoPlugIn, if it is not in
      // the RdkPlugIn dictionary it will get added if possible.
      var rdk_plug_in = RdkPlugIn.GetRdkPlugIn(plugin);

      // Plug-in not found or there was a problem adding it to the dictionary
      if (rdk_plug_in == null) return null;

      // Append the RdkPlugIn registered content type list
      rdk_plug_in.AddRegisteredContentTypes(content_types);

      // Process the valid class type list and register each class with the
      // appropriate C++ RDK class factory
      var texture_type = typeof(RenderTexture);
      var material_type = typeof(RenderMaterial);
      var environment_type = typeof(RenderEnvironment);
      foreach (var content_type in content_types)
      {
        var id = content_type.GUID;
        if (content_type.IsSubclassOf(texture_type))
          UnsafeNativeMethods.Rdk_AddTextureFactory(id);
        if (content_type.IsSubclassOf(material_type))
          UnsafeNativeMethods.Rdk_AddMaterialFactory(id);
        if (content_type.IsSubclassOf(environment_type))
          UnsafeNativeMethods.Rdk_AddEnvironmentFactory(id);
      }

      // Return an array of the valid content types
      return content_types.ToArray();
    }

    /// <summary>
    /// Loads content from a library file.  Does not add the content to the persistent content list.
    /// Use AddPersistantContent to add it to the list.
    /// </summary>
    /// <param name="filename">full path to the file to be loaded.</param>
    /// <returns>The loaded content or null if an error occurred.</returns>
    public static RenderContent LoadFromFile(String filename)
    {
      var p_content = UnsafeNativeMethods.Rdk_RenderContent_LoadContentFromFile(filename);
      if (p_content == IntPtr.Zero)
        return null;

      var new_content = FromPointer(p_content);
      new_content.AutoDelete = true;
      return new_content;
    }
    /// <summary>
    /// Add a material, environment or texture to the internal RDK document lists as
    /// top level content.  The content must have been returned from
    /// RenderContent::MakeCopy, NewContentFromType or a similar function that returns
    /// a non-document content.
    /// </summary>
    /// <param name="renderContent">The render content.</param>
    /// <returns>true on success.</returns>
    [Obsolete("Use RhinoDoc.RenderMaterials.Attach")]
    public static bool AddPersistentRenderContent(RenderContent renderContent)
    {
      var doc = RhinoDoc.ActiveDoc;
      return AddPersistentRenderContent(doc, renderContent);
    }
    /// <summary>
    /// Add a material, environment or texture to the internal RDK document lists as
    /// top level content.  The content must have been returned from
    /// RenderContent::MakeCopy, NewContentFromType or a similar function that returns
    /// a non-document content.
    /// </summary>
    /// <param name="document">The document to attach the render content to.</param>
    /// <param name="renderContent">The render content.</param>
    /// <returns>true on success.</returns>
    [Obsolete("Use RhinoDoc.RenderMaterials.Attach")]
    public static bool AddPersistentRenderContent(RhinoDoc document, RenderContent renderContent)
    {
      // Should this be moved to the document?
      renderContent.AutoDelete = false;
      if (document == null) throw new ArgumentNullException("document");
      var serial_number = document.RuntimeSerialNumber;
      return 1 == UnsafeNativeMethods.Rdk_Globals_AddPersistentContent(serial_number, renderContent.ConstPointer());
    }


    /// <summary>
    /// Search for a content object based on its Id
    /// </summary>
    /// <param name="document">
    /// The Rhino document containing the content.
    /// </param>
    /// <param name="id">
    /// Id of the content instance to search for.
    /// </param>
    /// <returns>
    /// Returns the content object with the specified Id if it is found
    /// otherwise it returns null.
    /// </returns>
    public static RenderContent FromId(RhinoDoc document, Guid id)
    {
      var doc_sn = document != null ? document.RuntimeSerialNumber : 0u;
      var render_content = UnsafeNativeMethods.Rdk_FindContentInstance(doc_sn, id);
      return FromPointer(render_content);
    }

    /// <summary>
    /// Create a copy of the render content. All content is the same, except for the
    /// instance Id.
    /// </summary>
    /// <returns>The new RenderContent</returns>
    public RenderContent MakeCopy()
    {
      var render_content = UnsafeNativeMethods.Rdk_RenderContent_MakeCopy(ConstPointer());

      var copy = FromPointer(render_content);
      // DHNL 2015.12.17 set AutoDelete so copy gets properly disposed of when temporary copy
      // goes out of scope and hasn't been added as persistent content (RH-32132)
      // Note that copy can be null
      if(copy != null)
        copy.AutoDelete = true;
      return copy;
    }

    /// <summary>
    /// Create a .NET object of the appropriate type and attach it to the
    /// requested C++ pointer
    /// </summary>
    /// <param name="renderContent"></param>
    /// <returns></returns>
    internal static RenderContent FromPointer(IntPtr renderContent)
    {
      // If null C++ pointer then bail
      if (renderContent == IntPtr.Zero)
        return null;
      // Get the runtime memory serial number for the requested item
      var serial_number = UnsafeNativeMethods.CRhCmnRenderContent_IsRhCmnDefined(renderContent);
      // If the object has been created and not disposed of then return it
      if (serial_number > 0) return FromSerialNumber(serial_number);
      // Could not find the object in the runtime list so check to see if the C++
      // pointer is a CRhRdkTexture pointer 
      var p_texture = UnsafeNativeMethods.Rdk_RenderContent_DynamicCastToTexture(renderContent);
      // Is a RenderTexture so create one and attach it to the requested C++ pointer
      if (p_texture != IntPtr.Zero) return new NativeRenderTexture(p_texture);
      // Check to see if the C++ pointer is a CRhRdkMaterial pointer
      var p_material = UnsafeNativeMethods.Rdk_RenderContent_DynamicCastToMaterial(renderContent);
      // It is a RenderMaterial so create one and attach it to the requested C++ pointer
      if (p_material != IntPtr.Zero) return new NativeRenderMaterial(p_material);
      // Check to see if the C++ pointer is a CRhRdkEnviornmen pointer
      var p_environment = UnsafeNativeMethods.Rdk_RenderContent_DynamicCastToEnvironment(renderContent);
      // It is a RenderEnviornment so create one and attach it to the requested C++ pointer
      if (p_environment != IntPtr.Zero) return new NativeRenderEnvironment(p_environment);
      //This should never, ever, happen.
      throw new InvalidCastException("renderContent Pointer is not of a recognized type");
    }

    [Obsolete]
    public static RenderContent FromXml(String xml)
    {
      return FromXml(xml, null);
    }

    public static RenderContent FromXml(String xml, RhinoDoc doc)
    {
      IntPtr pContent = UnsafeNativeMethods.Rdk_RenderContent_FromXml(xml, doc==null ? 0 : doc.RuntimeSerialNumber);
      if (pContent == IntPtr.Zero)
        return null;

      return RenderContent.FromPointer(pContent);
    }

    internal static ChangeContexts ChangeContextFromExtraRequirementsSetContext(ExtraRequirementsSetContexts sc) // Static.
    {
      switch (sc)
      {
        case ExtraRequirementsSetContexts.UI: return ChangeContexts.UI;
        case ExtraRequirementsSetContexts.Drop: return ChangeContexts.Drop;
      }

      return ChangeContexts.Program;
    }

    internal static ExtraRequirementsSetContexts ExtraRequirementsSetContextFromChangeContext(ChangeContexts cc) // Static.
    {
      switch (cc)
      {
        case ChangeContexts.UI: return ExtraRequirementsSetContexts.UI;
        case ChangeContexts.Drop: return ExtraRequirementsSetContexts.Drop;
      }

      return ExtraRequirementsSetContexts.Program;
    }
    static internal RenderContentChangeReason ReasonFromAttachReason(UnsafeNativeMethods.RdkEventWatcherBaseAttachReason reason)
    {
      switch (reason)
      {
        case UnsafeNativeMethods.RdkEventWatcherBaseAttachReason.Attach:
          return RenderContentChangeReason.Attach;
        case UnsafeNativeMethods.RdkEventWatcherBaseAttachReason.Change:
          return RenderContentChangeReason.ChangeAttach;
        case UnsafeNativeMethods.RdkEventWatcherBaseAttachReason.Open:
          return RenderContentChangeReason.Open;
        case UnsafeNativeMethods.RdkEventWatcherBaseAttachReason.Undo:
          return RenderContentChangeReason.AttachUndo;
      }
      throw new Exception("Unknown RdkEventWatcherBaseAttachReason type");
    }
    static internal RenderContentChangeReason ReasonFromDetachReason(UnsafeNativeMethods.RdkEventWatcherBaseDetachReason reason)
    {
      switch (reason)
      {
        case UnsafeNativeMethods.RdkEventWatcherBaseDetachReason.Detach:
          return RenderContentChangeReason.Detach;
        case UnsafeNativeMethods.RdkEventWatcherBaseDetachReason.Change:
          return RenderContentChangeReason.ChangeDetach;
        case UnsafeNativeMethods.RdkEventWatcherBaseDetachReason.Delete:
          return RenderContentChangeReason.Delete;
        case UnsafeNativeMethods.RdkEventWatcherBaseDetachReason.Undo:
          return RenderContentChangeReason.DetachUndo;
      }
      throw new Exception("Unknown RdkEventWatcherBaseDetachReason type");
    }


    #endregion

    // -1 == Disposed content
    /// <summary>
    /// Serial number of the created object, valid values:
    ///   -1  == OnDeleteRhCmnRenderContent() was called with this serial number
    ///   >0  == Value set by the constructor
    /// </summary>
    internal int RuntimeSerialNumber;// = 0; initialized by runtime
    /// <summary>
    /// The next allocation serial number
    /// </summary>
    static int g_current_serial_number = 1;
    /// <summary>
    /// I think this is the index to start the search from if we have an
    /// idea as to where to start looking.
    /// </summary>
    private int m_search_hint = -1;
    /// <summary>
    /// Contains a list of objects with a m_runtime_serial_number > 0,
    /// OnDeleteRhCmnRenderContent() will remove objects from the dictionary
    /// by m_runtime_serial_number.
    /// </summary>
    static readonly Dictionary<int, RenderContent> g_custom_content_dictionary = new Dictionary<int, RenderContent>();
    /// <summary>
    /// Rhino.Render.Fields FieldDictionary which provides access to setting
    /// and retrieving field values.
    /// </summary>
    private FieldDictionary m_field_dictionary;
    /// <summary>
    /// Rhino.Render.Fields FieldDictionary which provides access to setting
    /// and retrieving field values.
    /// </summary>
    public FieldDictionary Fields
    {
      get { return m_field_dictionary ?? (m_field_dictionary = new FieldDictionary(this)); }
        }

    public IntPtr CppPointer
    {
        get { return ConstPointer(); }
    }

class BoundField
    {
      public BoundField(Field field, ChangeContexts cc)
      {
        Field = field;
        ChangeContexts = cc;
      }
      public Field Field { get; set; }
      public ChangeContexts ChangeContexts { get; set; }
    }
    readonly Dictionary<string, BoundField> m_bound_parameters = new Dictionary<string, BoundField>();

    /// <summary>
    /// Use bindings to automatically wire parameters to fields
    /// </summary>
    /// <param name="parameterName"></param>
    /// <param name="childSlotName"></param>
    /// <param name="field"></param>
    /// <param name="setEvent"></param>
    public void BindParameterToField(string parameterName, string childSlotName, Field field, ChangeContexts setEvent)
    {
      var key = BindingKey(parameterName, childSlotName);
      m_bound_parameters[key] = new BoundField(field, setEvent);
    }

    /// <summary>
    /// Use bindings to automatically wire parameters to fields
    /// </summary>
    /// <param name="parameterName"></param>
    /// <param name="field"></param>
    /// <param name="setEvent"></param>
    public void BindParameterToField(string parameterName, Field field, ChangeContexts setEvent)
    {
      string key = BindingKey(parameterName, null);
      m_bound_parameters[key] = new BoundField(field, setEvent);
    }

    static string BindingKey(string parameterName, string childSlotName)
    {
      if (string.IsNullOrEmpty(childSlotName))
        return parameterName;
      return parameterName + "~~" + childSlotName;
    }

    /// <summary>
    /// Check to see if the class is defined by RhinoCommon or some other
    /// assembly.
    /// </summary>
    /// <returns>
    /// If the class assembly type is equal to the RhinoCommon assembly then
    /// return true  indicating native content otherwise return false
    /// indicating custom content.
    /// </returns>
    bool ClassDefinedInRhinoCommon()
    {
      var render_content = typeof(RenderContent);
      var class_type = GetType();
      return render_content.Assembly.Equals(class_type.Assembly);
    }
    /// <summary>
    /// Check to see if the class type assembly is something other than
    /// RhinoCommon.
    /// </summary>
    /// <returns>
    /// Return true if the class definition resides in an assembly other than
    /// RhinoCommon otherwise return false because it is native content.
    /// </returns>
    bool IsCustomClassDefintion()
    {
      return !ClassDefinedInRhinoCommon();
    }

    /// <summary>
    /// internal because we don't want people to ever directly subclass RenderContent.
    /// They should always derive from the subclasses of this class
    /// </summary>
    internal RenderContent()
    {
      // This constructor is being called because we have a custom .NET subclass
      if (IsCustomClassDefintion())
      {
        RuntimeSerialNumber = g_current_serial_number++;
        g_custom_content_dictionary.Add(RuntimeSerialNumber, this);
      }
      // Find the plug-in that registered this class type
      var type = GetType();
      var type_id = type.GUID;
      Guid plugin_id;
      RdkPlugIn.GetRenderContentType(type_id, out plugin_id);
      if (Guid.Empty == plugin_id) return;

      // Get information from custom class attributes
      var render_engine = Guid.Empty;
      var image_based = false;
      var category = "";
      var is_elevated = false;
      var is_built_in = false;
      var is_private = false;

      var attr = type.GetCustomAttributes(typeof(CustomRenderContentAttribute), false);
      if (attr.Length > 0)
      {
        var custom = attr[0] as CustomRenderContentAttribute;
        if (custom != null)
        {
          image_based = custom.ImageBased;
          render_engine = custom.RenderEngineId;
          category = custom.Category;
          is_elevated = custom.IsElevated;
          is_built_in = custom.IsBuiltIn;
          is_private = custom.IsPrivate;
        }
      }

      // Crete C++ pointer of the appropriate type
      var returned_serial_number = -1;
      if (this is RenderTexture)
        returned_serial_number = UnsafeNativeMethods.CRhCmnTexture_New(RuntimeSerialNumber, image_based, render_engine, plugin_id, type_id, category, is_elevated, is_built_in, is_private);
      else if (this is RenderMaterial)
        returned_serial_number = UnsafeNativeMethods.CRhCmnMaterial_New(RuntimeSerialNumber, image_based, render_engine, plugin_id, type_id, category, is_elevated, is_built_in, is_private);
      else if (this is RenderEnvironment)
        returned_serial_number = UnsafeNativeMethods.CRhCmnEnvironment_New(RuntimeSerialNumber, image_based, render_engine, plugin_id, type_id, category, is_elevated, is_built_in, is_private);
      else
        throw new InvalidCastException("Content is of unknown type");
      if (returned_serial_number != RuntimeSerialNumber)
        throw new Exception("Error creating new content pointer");

      AutoDelete = true;
    }
    /// <summary>
    /// Internal method used to get string values from the C++ SDK
    /// </summary>
    /// <param name="which">Id of string value to get</param>
    /// <returns>Returns the requested string value.</returns>
    internal string GetString(UnsafeNativeMethods.RenderContent_StringIds which)
    {
      var p_const_this = ConstPointer();
      using (var sh = new StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.Rdk_RenderContent_GetString(p_const_this, p_string, which);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Name for your content type.  ie. "My .net Texture"
    /// </summary>
    public abstract String TypeName { get; }
    /// <summary>
    /// Description for your content type.  ie.  "Procedural checker pattern"
    /// </summary>
    public abstract String TypeDescription { get; }

    /// <summary>
    /// Instance name for this content.
    /// </summary>
    public String Name
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.Name); }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetInstanceName(ConstPointer(), value);
      }
    }

    /// <summary>
    /// Notes for this content.
    /// </summary>
    public String Notes
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.Notes); }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetNotes(ConstPointer(), value);
      }
    }

    /// <summary>
    /// Tags for this content.
    /// </summary>
    public String Tags {
      get { return GetString (UnsafeNativeMethods.RenderContent_StringIds.Tags); }
      set {
        UnsafeNativeMethods.Rdk_RenderContent_SetTags (NonConstPointer (), value);
      }
    }

    /// <summary>
    /// Category for this content.
    /// </summary>
    public String Category
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.Category); }
    }

    /// <summary>
    /// Instance identifier for this content.
    /// </summary>
    public Guid Id
    {
      get
      {
        return UnsafeNativeMethods.Rdk_RenderContent_InstanceId(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetInstanceId(ConstPointer(), value);
      }
    }

    /// <summary>
    /// Type identifier for this content
    /// </summary>
    public Guid TypeId => UnsafeNativeMethods.Rdk_RenderContent_TypeId(ConstPointer());

    /// <summary>
    /// **** This method is for proxies and will be marked obsolete in V7 ****
    ///
    /// The only place a single proxy can be displayed is in the
    /// New Content Control main thumbnail. All other attempts to
    /// use a single proxy in a UI require it to be replaced with
    /// the corresponding multi proxy. Single proxies override this
    /// to find the corresponding multi proxy.
    /// </summary>
    /// <returns>The cotnent.</returns>
    public RenderContent ForDisplay()
    {
      IntPtr pContent = UnsafeNativeMethods.Rdk_RenderContent_ForDisplay(ConstPointer());

      if (pContent != IntPtr.Zero)
        return RenderContent.FromPointer(pContent);
      
      return null;
    }

    /// <summary>
    ///  Query whether or not the content or any of its ancestors is a reference content.
    /// </summary>
    /// <returns>true if the content is a reference, else false</returns>
    public bool IsReference()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_IsReference(ConstPointer());
    }

    /// <summary>
    ///  UseCount returns how many times the content is used
    /// </summary>
    public int UseCount()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_UseCount(ConstPointer());
    }

    /// <summary>
    /// This method is deprecated and no longer called. For more information
    /// see <see cref="CalculateRenderHash"/>
    /// </summary>
    /// <param name="hash"></param>
    [CLSCompliant(false)]
    [Obsolete("This method is deprecated and no longer called. For more information see CalculateRenderHash")]
    public void SetRenderHash(uint hash)
    {
    }

    /// <summary>
    /// This method is deprecated and no longer called. For more information
    /// see <see cref="CalculateRenderHash"/>
    /// </summary>
    /// <returns>bool</returns>
    [Obsolete("This method is deprecated and no longer called. For more information see CalculateRenderHash")]
    public bool IsRenderHashCached()
    {
      return false;
    }

    /// <summary>
    /// Override this method to calculate the render hash of the state that
    /// affects how the content is rendered.  Does not include children or
    /// perform any caching. Render hash values are now automatically cached by
    /// the content framework and you do not have to worry about caching. You
    /// also do not have to worry about iterating into children.  This method
    /// is now only called internally by the framework, use the RenderHash
    /// property to get the current hash value.
    /// </summary>
    /// <returns></returns>
    [CLSCompliant(false)]
    protected virtual uint CalculateRenderHash(ulong rcrcFlags)
    {
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.Rdk_RenderContent_CallCalculateRenderCRCBase(const_pointer, rcrcFlags);
    }


    /// <summary>
    /// Render hash for the content hierarchy. It iterates children and includes
    /// a caching mechanism which means the hash value can be retrieved quickly
    /// if it hasn't changed. The cache is invalidated when Changed() is called.
    /// 
    /// You can override the <see cref="CalculateRenderHash"/> method to provide
    /// a custom hash value.
    /// </summary>
    [CLSCompliant(false)]
    public uint RenderHash
    {
      get
      {
        var const_pointer = ConstPointer();
        return UnsafeNativeMethods.Rdk_RenderContent_RenderCRC(const_pointer, 0);
      }
    }

    /// <summary>
    /// As RenderHash, but ignore parameter names given.
    /// </summary>
    /// <param name="flags">Flags to ignore</param>
    /// <param name="excludeParameterNames">semicolon-delimited string</param>
    /// <returns>Render hash</returns>
    [CLSCompliant(false)]
    public uint RenderHashExclude(TextureRenderHashFlags flags, string excludeParameterNames)
    {
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.Rdk_RenderContent_RenderCRC_ExcludeParamNames(const_pointer, (ulong)flags, excludeParameterNames);
    }

    /// <summary>
    /// As RenderHash, but ignore parameter names given.
    /// </summary>
    /// <param name="flags">Flags to ignore</param>
    /// <param name="excludeParameterNames">semicolon-delimited string</param>
    /// <returns>Render hash</returns>
    [CLSCompliant(false)]
    public uint RenderHashExclude(CrcRenderHashFlags flags, string excludeParameterNames)
    {
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.Rdk_RenderContent_RenderCRC_ExcludeParamNames(const_pointer, (ulong)flags, excludeParameterNames);
    }


    internal delegate uint RenderCrcCallback(int serialNumber, ulong rcrcFlags);
    internal static readonly RenderCrcCallback g_on_render_crc = OnRenderCrc;
    private static uint OnRenderCrc(int serialNumber, ulong rcrcFlags)
    {
      var render_content = FromSerialNumber(serialNumber);

      return render_content != null ? render_content.CalculateRenderHash(rcrcFlags) : 0;
    }



    /// <summary>
    /// Returns true if this content has no parent, false if it is the child of another content.
    /// </summary>
    public bool TopLevel
    {
      get { return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsTopLevel(ConstPointer()); }
    }

    // Hiding for the time being. It may be better to just have a Document property
    /// <summary>
    /// Returns true if this content is a resident of one of the persistent lists.
    /// </summary>
    /*public*/
    bool InDocument
    {
      get { return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsInDocument(ConstPointer()); }
    }

    /// <summary>
    /// Determines if the content has the hidden flag set.
    /// </summary>
    public bool Hidden
    {
      get { return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsHidden(ConstPointer()); }
      set { UnsafeNativeMethods.Rdk_RenderContent_SetIsHidden(NonConstPointer(), value); }
    }

    /// <summary>
    /// Contents can be created as 'auto-delete' by certain commands such as 'PictureFrame'.
    /// These contents are automatically hidden from the user when the associated Rhino object
    /// is deleted. They are later deleted when the document is saved.
    /// </summary>
    public bool IsHiddenByAutoDelete
    {
      get { return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsHiddenByAutoDelete(ConstPointer()); }
    }

    /// <summary>
    /// Determines if the content can be edited.
    /// </summary>
    public bool CanBeEdited
    {
      get
      {
        return UnsafeNativeMethods.Rdk_RenderContent_CanBeEdited(ConstPointer());
      }
    }

    /// <summary>
    /// Checks if render content is default instance.
    /// </summary>
    public bool IsDefaultInstance
    {
      get
      {
        return UnsafeNativeMethods.Rdk_RenderContent_IsDefaultInstance(ConstPointer());
      }
    }

    /// <summary>
    /// Gets the proxy type of the render content
    /// </summary>
    public ProxyTypes ProxyType
    {
      get
      {
        uint value = UnsafeNativeMethods.Rdk_RenderContent_ProxyType(ConstPointer());

        ProxyTypes type = ProxyTypes.None;

        if (value == 0)
          type = ProxyTypes.None;

        if (value == 1)
          type = ProxyTypes.Single;

        if (value == 2)
          type = ProxyTypes.Multi;

        return type;
      }
    }


    /// <summary>
    /// Returns the top content in this parent/child chain.
    /// </summary>
    public RenderContent Parent
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_Parent(ConstPointer());
        return FromPointer(p_content);
      }
    }

    /// <summary>
    /// Return First child of this content or nullptr if none.
    /// </summary>
    public RenderContent FirstChild
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_FirstChild(ConstPointer());
        return FromPointer(p_content);
      }
    }

    /// <summary>
    /// Return First sibling of this content or nullptr if none.
    /// </summary>
   public RenderContent NextSibling
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_NextSibling(ConstPointer());
        return FromPointer(p_content);
      }
    }

    /// <summary>
    /// Returns the top content in this parent/child chain.
    /// </summary>
    public RenderContent TopLevelParent
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_TopLevelParent(ConstPointer());
        return FromPointer(p_content);
      }
    }

    /// <summary>
    /// If this content is in a document content list, the document will be returned.  Otherwise null.
    /// </summary>
    public RhinoDoc Document
    {
      get
      {
        var doc_sn = UnsafeNativeMethods.Rdk_RenderContent_DocumentOwner(ConstPointer());
        return RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      }
    }

    /// <summary>
    /// If this content is used by a document, including not in the content lists (for example, as a decal),
    /// the document will be returned.  Otherwise null.
    /// </summary>
    public RhinoDoc DocumentRegistered
    {
      get
      {
        var doc_sn = UnsafeNativeMethods.Rdk_RenderContent_DocumentRegistered(ConstPointer());
        return RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      }
    }

    /// <summary>
    /// If this content is associated by a document in any way, the document will be returned.  This includes copies of
    /// contents that were initially in the document. Otherwise null.
    /// </summary>
    public RhinoDoc DocumentAssoc
    {
      get
      {
        var doc_sn = UnsafeNativeMethods.Rdk_RenderContent_DocumentAssoc(ConstPointer());
        return RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetDocumentAssoc(NonConstPointer(), value.RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// Call this method to open the content in the relevant thumbnail editor
    /// and select it for editing by the user. The content must be in the
    /// document or the call will fail.
    /// </summary>
    /// <returns>
    /// Returns true on success or false on error.
    /// </returns>
    public bool OpenInEditor()
    {
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.Rdk_RenderContent_OpenInMainEditor(const_pointer);
    }

    /// <summary>
    /// Call this method to open the content in the a modal version of the editor.
    /// The content must be in the document or the call will fail.
    /// </summary>
    /// <returns>
    /// Returns true on success or false on error.
    /// </returns>
    [Obsolete("Obsolete, use Edit a version that returns a RenderContent", false)]
    public bool OpenInModalEditor()
    {
      if (!InDocument)
        return false;

      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.Rdk_RenderContent_OpenInMainEditor(const_pointer);
    }

    /// <summary>
    /// Call this method to open the content in the a modal version of the editor.
    /// The content must be in the document or the call will fail.
    /// </summary>
    /// <returns>
    /// Returns the edited content on succees or null on error.
    /// </returns>
    public RenderContent Edit()
    {
      if (!InDocument)
        return null;

      var const_pointer = ConstPointer();
      var new_content_ptr = UnsafeNativeMethods.Rdk_RenderContent_Edit(const_pointer);
      var new_content = RenderContent.FromPointer(new_content_ptr);
      if (null != new_content)
      {
        new_content.AutoDelete = true;
      }
      return new_content;
    }

    #region Serialization

    // See if we can fit this into the standard .NET serialization method (ISerializable)
    /*public bool ReadFromXml(String inputXml)
    {
      return 1 == UnsafeNativeMethods.Rdk_RenderContent_ReadFromXml(NonConstPointer(), inputXml);
    }*/

    public String Xml
    {
      get
      {
        return GetString(UnsafeNativeMethods.RenderContent_StringIds.Xml);
      }
    }

    #endregion

    /// <summary>
    /// Override to provide UI sections to display in the editor.
    /// </summary>
    protected virtual void OnAddUserInterfaceSections()
    {
      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_AddUISections(NonConstPointer(), OnAddUiSectionsUIId);
      }
      else
      {
        UnsafeNativeMethods.Rdk_CallAddUISectionsBase(NonConstPointer(), OnAddUiSectionsUIId);
      }
    }

    /// <summary>
    /// Begins a change or batch of changes. This returns a reference to the
    /// content which you should use to make your changes. It may also make a
    /// copy of the content state allowing <see cref="EndChange"/> to send an
    /// event with the old and new contents. Calls to this method are counted;
    /// you must call EndChange() once for every call to BeginChange().
    /// Note:
    ///   If Changed() was called between the calls to BeginChange() and
    ///   EndChange(), the last call to EndChange() may cause the ContentChanged
    ///   event to be sent.
    /// </summary>
    /// <param name="changeContext">
    /// the change context. If this is kUI, kProgram, kDrop or kTree, the
    /// content will be copied. EndChange() will then send the copy as 'old' in
    /// the OnContentChanged event.
    /// Note:
    ///   If you override this method, <b>please be sure to call the base
    ///   class</b>. <see cref="EndChange"/> <see cref="ContentChanged"/>
    /// </param>
    public void BeginChange(ChangeContexts changeContext)
    {
      var const_pointer = ConstPointer();
      UnsafeNativeMethods.CRhRdkContent_BeginChange(const_pointer, (int)changeContext);
    }


    /// <summary>
    /// Ends a change or batch of changes. Calls to this method are counted;
    /// you must call this method once for every call to <see cref="BeginChange"/>.
    /// Note:
    ///   If <see cref="BeginChange"/> was called with ChangeContexts.UI,
    ///   ChangeContexts.Program, ChangeContexts.Drop or ChangeContexts.UI.Tree
    ///   and Changed() was called between the calls to <see cref="BeginChange"/> and
    ///   EndChange(), the last call to EndChange() will raise the
    ///   <see cref="ContentChanged"/> event.
    /// </summary>
    public void EndChange()
    {
      var const_pointer = ConstPointer();
      UnsafeNativeMethods.CRhRdkContent_EndChange(const_pointer);
    }

    /// <summary>
    /// Override this method to prompt user for information necessary to
    /// create a new content object.  For example, if you are created a
    /// textured material you may prompt the user for a bitmap file name
    /// prior to creating the textured material.
    /// </summary>
    /// <returns>
    /// If true is returned the content is created otherwise the creation
    /// is aborted.
    /// </returns>
    protected virtual bool OnGetDefaultsInteractive()
    {
      return true;
    }

    /// <summary>
    /// Create a new custom content user interface instance for this
    /// RenderContext.
    /// </summary>
    /// <param name="classId">The class Type Guid which was created.</param>
    /// <param name="UIId">The UI to add the new UI to.</param>
    /// <param name="caption">The expandable tab caption</param>
    /// <param name="createExpanded">
    /// If this is true the tab will initially be expanded otherwise it will be
    /// collapsed.
    /// </param>
    /// <param name="createVisible">
    /// If this is true the tab will initially be visible otherwise it will be
    /// hidden.
    /// </param>
    /// <param name="window">The user control to embed in the expandable tab.</param>
    /// <returns>
    /// Returns the UserInterfaceSection object used to manage the new custom
    /// UI.
    ///  </returns>
    /*protected*/
    [Obsolete]
    UI.UserInterfaceSection NewUiPointer(Guid classId, Guid UIId, string caption, bool createExpanded, bool createVisible, object window)
    {
      const int idxInvalid = 0;
      const int idxMaterial = 1;
      const int idxTexture = 2;
      const int idxEnviornment = 3;
      var type = idxInvalid;
      if (this is RenderMaterial)
        type = idxMaterial;
      else if (this is RenderTexture)
        type = idxTexture;
      else if (this is RenderEnvironment)
        type = idxEnviornment;

      var h_wnd = Rhino.UI.Dialogs.Service.ObjectToWindowHandle(window, false);
      var serial_number = UnsafeNativeMethods.Rdk_CoreContent_AddNewContentUiSection(type, NonConstPointer(), UIId, classId, caption, h_wnd, createExpanded, createVisible);
      return ((serial_number < 1) ? null : new UI.UserInterfaceSection(serial_number, window));
    }

    /// <summary>
    /// Add a new .NET control to an content expandable tab section, the height
    /// of the createExpanded tabs client area will be the initial height of the
    /// specified control.
    /// </summary>
    /// <param name="classType">
    /// The control class to create and embed as a child window in the
    /// expandable tab client area.  This class type must be derived from
    /// IWin32Window or this method will throw an ArgumentException.  Implement
    /// the IUserInterfaceSection interface in your classType to get
    /// UserInterfaceSection notification.
    /// </param>
    /// <param name="caption">Expandable tab caption.</param>
    /// <param name="createExpanded">
    /// If this value is true then the new expandable tab section will
    /// initially be expanded, if it is false it will be collapsed.
    /// </param>
    /// <param name="createVisible">
    /// If this value is true then the new expandable tab section will
    /// initially be visible, if it is false it will be hidden.
    /// </param>
    /// <returns>
    /// Returns the UserInterfaceSection object used to manage the new 
    /// user control object.
    /// </returns>
    [Obsolete ("Use AddUserInterfaceSection(Rhino.UI.Controls.ICollapsibleSection) below instead.  This function will not work on the Mac and is not type-safe.")]
    public UI.UserInterfaceSection AddUserInterfaceSection(Type classType, string caption, bool createExpanded, bool createVisible)
    {
      if (OnAddUiSectionsUIId == Guid.Empty)
        throw new Exception("AddUserInterfaceSection can only be called from OnAddUserInterfaceSections");
      Type win32_interface = classType.GetInterface("IWin32Window");
      if( win32_interface==null )
        throw new ArgumentException("classType must implement IWin32Window interface", "classType");
      ConstructorInfo constructor = classType.GetConstructor(Type.EmptyTypes);
      if (!classType.IsPublic || constructor == null) throw new ArgumentException("panelType must be a public class and have a parameterless constructor", "classType");
      object[] attr = classType.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1) throw new ArgumentException("classType must have a GuidAttribute", "classType");

      var control = Activator.CreateInstance(classType);

      var prop = classType.GetProperty("BackColor");
      if (prop != null)
      {
        var argb = UnsafeNativeMethods.Rdk_ContentUiBackgroundColor();
        var color = Color.FromArgb(argb);
        prop.SetValue(control, color);
      }
      //if (ApplicationSettings.AppearanceSettings.UsePaintColors)
      //{
      //  var asControl = control as Control;
      //  if (null != asControl)
      //    asControl.BackColor = System.Drawing.SystemColors.ButtonFace;
      //      //ApplicationSettings.AppearanceSettings.GetPaintColor(ApplicationSettings.PaintColor.NormalEnd);
      //}
      var new_ui_section = NewUiPointer(classType.GUID, OnAddUiSectionsUIId, caption, createExpanded, createVisible, control);
      if (null != new_ui_section) return new_ui_section;
      if (control is IDisposable) (control as IDisposable).Dispose();
      return null;
    }

    /// <summary>
    /// Add a new automatic user interface section, Field values which include
    /// prompts will be automatically added to this section.
    /// </summary>
    /// <param name="caption">Expandable tab caption.</param>
    /// <param name="id">Tab id which may be used later on to reference this tab.</param>
    /// <returns>
    /// Returns true if the automatic tab section was added otherwise; returns
    /// false on error.
    /// </returns>
    public bool AddAutomaticUserInterfaceSection(string caption, int id)
    {
      if (OnAddUiSectionsUIId == Guid.Empty)
      {
        throw new Exception("AddAutomaticUserInterfaceSection can only be called from OnAddUserInterfaceSections");
      }

      return UnsafeNativeMethods.Rdk_CoreContent_AddAutomaticUISection(NonConstPointer(), OnAddUiSectionsUIId, caption, id);
    }

    static List<Rhino.UI.Controls.ICollapsibleSection> m_sections_to_keep_alive = new List<Rhino.UI.Controls.ICollapsibleSection>();

    public bool AddUserInterfaceSection(Rhino.UI.Controls.ICollapsibleSection section)
    {
      if (OnAddUiSectionsUIId == Guid.Empty)
      {
        throw new Exception("AddUserInterfaceSection can only be called from OnAddUserInterfaceSections");
      }

       m_sections_to_keep_alive.Add(section);

      return UnsafeNativeMethods.Rdk_CoreContent_AddUISection(NonConstPointer(), OnAddUiSectionsUIId, section.CppPointer);
    }

    public DataSources.ContentFactory Factory()
    {
      IntPtr pFactory = UnsafeNativeMethods.Rdk_RenderContent_Factory(ConstPointer());

      if (pFactory != IntPtr.Zero)
        return new DataSources.ContentFactory(pFactory);

      return null;
    }


    public virtual bool IsContentTypeAcceptableAsChild(Guid type, String childSlotName)
    {
      if (IsNativeWrapper())
        return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsContentTypeAcceptableAsChild(ConstPointer(), type, childSlotName);

      return 1 == UnsafeNativeMethods.Rdk_RenderContent_CallIsContentTypeAcceptableAsChildBase(ConstPointer(), type, childSlotName);
    }

    public virtual bool IsFactoryProductAcceptableAsChild(DataSources.ContentFactory factory, String childSlotName)
    {
      return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsFactoryProductAcceptableAsChild(ConstPointer(), factory.CppPointer, childSlotName);
    }

    /// <summary>
    /// Query the content instance for the value of a given named parameter.
    /// If you do not support this parameter, call the base class.
    /// </summary>
    /// <param name="parameterName">Name of the parameter</param>
    /// <returns>IConvertible. Note that you can't directly cast from object, instead you have to use the Convert mechanism.</returns>
    [CLSCompliant(false)]
    public virtual object GetParameter(String parameterName)
    {
      Variant value = new Variant();

      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderContent_GetVariantParameter(ConstPointer(), parameterName, value.NonConstPointer());
      }
      else
      {
        string key = BindingKey(parameterName, null);
        BoundField bound_field;
        if (m_bound_parameters.TryGetValue(key, out bound_field))
          value.SetValue(bound_field.Field.ValueAsObject());
        else
          UnsafeNativeMethods.Rdk_RenderContent_CallGetVariantParameterBase(ConstPointer(), parameterName, value.NonConstPointer());
      }

      return value.IsNull ? null : value;
    }

    /// <summary>
    /// Set the named parameter value for this content instance.
    /// If you do not support this parameter, call the base class.
    /// </summary>
    /// <param name="parameterName"></param>
    /// <param name="value"></param>
    /// <param name="changeContext"></param>
    /// <returns></returns>
    [Obsolete("Use SetParameter without ChangeContexts and Begin/EndChange")]
    public virtual bool SetParameter(String parameterName, object value, ChangeContexts changeContext)
    {
      BeginChange(changeContext);
      bool bRet = SetParameter(parameterName, value);
      EndChange();
      return bRet;
    }
    /// <summary>
    /// Set the named parameter value for this content instance.
    /// If you do not support this parameter, call the base class.
    /// </summary>
    /// <param name="parameterName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual bool SetParameter(String parameterName, object value)
    {
      var v = new Variant(value);
      if (v != null)
      {
        if (IsNativeWrapper())
        {
          return 1 == UnsafeNativeMethods.Rdk_RenderContent_SetVariantParameter(ConstPointer(), parameterName, v.ConstPointer());
        }

        var key = BindingKey(parameterName, null);
        BoundField bound_field;
        if (m_bound_parameters.TryGetValue(key, out bound_field))
        {
          bound_field.Field.Set(v);
          return true;
        }

        return 1 == UnsafeNativeMethods.Rdk_RenderContent_CallSetVariantParameterBase(ConstPointer(), parameterName, v.ConstPointer());
      }
      return false;
    }

    /// <summary>
    /// Set this property to true prior to adding content to the document to
    /// lock the content browser editing UI methods.  Setting this to true will
    /// keep the browser from allowing things like deleting, renaming or
    /// changing content.  This is useful for custom child content that you
    /// want to be editable but persistent.  Setting this after adding content
    /// to the document will cause an exception to be thrown.
    /// </summary>
    /// <exception cref="Exception">
    /// IsLocked must be called prior to adding content to the document
    /// </exception>
    public bool IsLocked
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderContent_GetSetLocked(pointer, 0);
        return (value != 0);
      }
      set
      {
        if (InDocument)
          throw new Exception("IsLocked must be called prior to adding content to the document");
        if (!value)
          return;
        var pointer = NonConstPointer();
        UnsafeNativeMethods.Rdk_RenderContent_GetSetLocked(pointer, 1);
      }
    }

    /// <summary>
    /// Extra requirements are a way of specifying extra functionality on parameters in the automatic UI.
    /// Implement this function to specify additional functionality for automatic UI sections or the texture summary.
    /// See IAutoUIExtraRequirements.h in the C++ RDK for string definitions for the parameter names.
    /// </summary>
    /// <param name="parameterName">The parameter or field internal name to which this query applies</param>
    /// <param name="childSlotName">The extra requirement parameter, as listed in IAutoUIExtraRequirements.h in the C++ RDK</param>
    /// <returns>
    /// Call the base class if you do not support the extra requirement parameter.
    /// Current supported return values are (int, bool, float, double, string, Guid, Color, Vector3d, Point3d, DateTime)
    /// </returns>
    public virtual object GetChildSlotParameter(String parameterName, String childSlotName)
    {
      var value = new Variant();
      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderContent_GetExtraRequirementParameter(ConstPointer(), parameterName, childSlotName, value.NonConstPointer());
      }
      else
      {
        var key = BindingKey(parameterName, childSlotName);
        BoundField bound_field;
        if( m_bound_parameters.TryGetValue(key, out bound_field) )
          value.SetValue(bound_field.Field.ValueAsObject());
        else
          UnsafeNativeMethods.Rdk_RenderContent_CallGetExtraRequirementParameterBase(ConstPointer(), parameterName, childSlotName, value.NonConstPointer());
      }
      return value;
    }

    /// <summary>
    /// Extra requirements are a way of specifying extra functionality on parameters in the automatic UI.
    /// Implement this function to support values being set from automatic UI sections or the texture summary.
    /// See IAutoUIExtraRequirements.h in the C++ RDK for string definitions for the parameter names.
    /// </summary>
    /// <param name="parameterName">The parameter or field internal name to which this query applies</param>
    /// <param name="childSlotName">The extra requirement parameter, as listed in IAutoUIExtraRequirements.h in the C++ RDK</param>
    /// <param name="value">The value to set this extra requirement parameter. You will typically use System.Convert to convert the value to the type you need</param>
    /// <param name="sc">The context of this operation.</param>
    /// <returns>Null variant if not supported.  Call the base class if you do not support the extra requirement paramter.</returns>
    public virtual bool SetChildSlotParameter(String parameterName, String childSlotName, object value, ExtraRequirementsSetContexts sc)
    {
      var v = value as Variant;
      if (v != null)
      {
        if (IsNativeWrapper())
        {
          return 1 == UnsafeNativeMethods.Rdk_RenderContent_SetExtraRequirementParameter(ConstPointer(), parameterName, childSlotName, v.ConstPointer(), (int)sc);
        }

        var key = BindingKey(parameterName, childSlotName);
        BoundField bound_field;
        if (m_bound_parameters.TryGetValue(key, out bound_field))
        {
          bound_field.Field.Set(v);
          return true;
        }
        return 1 == UnsafeNativeMethods.Rdk_RenderContent_CallSetExtraRequirementParameterBase(ConstPointer(), parameterName, childSlotName, v.ConstPointer(), (int)sc);
      }
      return false;
    }

    /// <summary>
    /// Gets the on-ness property for the texture in the specified child slot.
    /// </summary>
    /// <param name="childSlotName">Child slot name for the child</param>
    /// <returns></returns>
    public bool ChildSlotOn(String childSlotName)
    {
      var rc = GetChildSlotParameter(childSlotName, "texture-on");
      if (rc == null)
        return false;
      if (rc is bool)
        return (bool)rc;
      var iconvert = rc as IConvertible;
      if( iconvert!=null )
        return Convert.ToBoolean(iconvert);
      return false;
    }

    /// <summary>
    /// Sets the on-ness property for the texture in the specified child slot.
    /// </summary>
    /// <param name="childSlotName">Child slot name for the child</param>
    /// <param name="bOn">Value for the on-ness property.</param>
    /// <param name="cc">Context of the change</param>
    public void SetChildSlotOn(String childSlotName, bool bOn, ChangeContexts cc)
    {
      SetChildSlotParameter(childSlotName, "texture-on", new Variant(bOn), ExtraRequirementsSetContextFromChangeContext(cc));
    }

    /// <summary>
    /// Gets the amount property for the texture in the specified child slot.  Values are typically from 0.0 - 100.0
    /// </summary>
    /// <param name="childSlotName">Child slot name for the child</param>
    /// <returns></returns>
    public double ChildSlotAmount(String childSlotName)
    {
      var rc = GetChildSlotParameter(childSlotName, "texture-amount");
      if (rc == null)
        return 0;
      if (rc is double || rc is int || rc is float || rc is IConvertible)
        return Convert.ToDouble(rc);
      return 0;
    }

    /// <summary>
    /// Sets the amount property for the texture in the specified child slot.  Values are typically from 0.0 - 100.0
    /// </summary>
    /// <param name="childSlotName">Child slot name for the child</param>
    /// <param name="amount">Texture amount. Values are typically from 0.0 - 100.0</param>
    /// <param name="cc">Context of the change.</param>
    public void SetChildSlotAmount(String childSlotName, double amount, ChangeContexts cc)
    {
      SetChildSlotParameter(childSlotName, "texture-amount", new Variant(amount), ExtraRequirementsSetContextFromChangeContext(cc));
    }

    /// <summary>
    /// Gets the PreviewSceneServer of the content
    /// </summary>
    /// <param name="ssd">SceneServerData</param>
    /// <return>Returns the contents PreviewSceneServer</return>
    public PreviewSceneServer NewPreviewSceneServer(SceneServerData ssd)
    {
      IntPtr pPreviewSceneServer = UnsafeNativeMethods.Rdk_RenderContent_NewPreviewSceneServer(ConstPointer(), ssd.CppPointer);
      if (pPreviewSceneServer != IntPtr.Zero)
        return new PreviewSceneServer(pPreviewSceneServer);

      return null;
    }

    /// <summary>
    /// Return values for MatchData function
    /// </summary>
    public enum MatchDataResult : int
    {
      None = 0,
      Some = 1,
      All = 2,
    };

    /// <summary>
    /// Implement to transfer data from another content to this content during creation.
    /// </summary>
    /// <param name="oldContent">An old content object from which the implementation may harvest data.</param>
    /// <returns>Information about how much data was matched.</returns>
    public virtual
    MatchDataResult MatchData(RenderContent oldContent)
    {
      if (IsNativeWrapper())
        return (MatchDataResult)UnsafeNativeMethods.Rdk_RenderContent_HarvestData(ConstPointer(), oldContent.ConstPointer());

      return (MatchDataResult)UnsafeNativeMethods.Rdk_RenderContent_CallHarvestDataBase(ConstPointer(), oldContent.ConstPointer());
    }

    #region Operations

    //TODO
    /** Delete a child content.
  \param parentContent is the content whose child is to be deleted. This must be an
  RDK-owned content that is in the persistent content list (either top-level or child).
  \param wszChildSlotName is the child-slot name of the child to be deleted.
  \return \e true if successful, else \e false. */
    //RHRDK_SDK bool RhRdkDeleteChildContent(CRhRdkContent& parentContent, const wchar_t* wszChildSlotName);

    enum ChangeChildContentFlags : int
    {
      /// <summary>
      /// Allow (none) item to be displayed in dialog.
      /// </summary>
      AllowNone = 0x0001,
      /// <summary>
      /// Automatically open new content in thumbnail editor.
      /// </summary>
      AutoEdit = 0x0002,

      /// <summary>
      /// Mask to use to isolate harvesting flags.
      /// </summary>
      HarvestMask = 0xF000,
      /// <summary>
      /// Use Renderer Support option to decide about harvesting.
      /// </summary>
      HarvestUseOpt = 0x0000,
      /// <summary>
      /// Always copy similar parameters from old child.
      /// </summary>
      HarvestAlways = 0x1000,
      /// <summary>
      /// Never copy similar parameters from old child.
      /// </summary>
      HarvestNever = 0x2000,
    };
    //TODO
    /** Change a content's child by allowing the user to choose the new content type from a
      content browser dialog. The child is created if it does not exist, otherwise the old
      child is deleted and replaced by the new child.
      \param parentContent is the content whose child is to be manipulated. This must be an
      RDK-owned content that is in the persistent content list (either top-level or child).
      \param wszChildSlotName is the child-slot name of the child to be manipulated.
      \param allowedKinds determines which content kinds are allowed to be chosen from the content browser dialog.
      \param uFlags is a set of flags for controlling the content browser dialog.
      \return \e true if successful, \e false if it fails or if the user cancels. */

    //RHRDK_SDK bool RhRdkChangeChildContent(CRhRdkContent& parentContent, const wchar_t* wszChildSlotName,
    //                                      const CRhRdkContentKindList& allowedKinds,
    //                                     UINT uFlags = rdkccc_AllowNone | rdkccc_AutoEdit);
    #endregion

    internal enum ParameterTypes : int
    {
      Null = 0,
      Boolean = 1,
      Integer = 2,
      Float = 3,
      Double = 4,
      Color = 5,
      Vector2d = 6,
      Vector3d = 7,
      String = 8,
      Pointer = 9,
      Uuid = 10,
      Matrix = 11,
      Time = 12,
      Buffer = 13,
      Point4d = 14,
    }

    public enum ExtraRequirementsSetContexts
    {
      /// <summary>
      /// Setting extra requirement as a result of user activity.
      /// </summary>
      UI = 0,
      /// <summary>
      /// Setting extra requirement as a result of drag and drop.
      /// </summary>
      Drop = 1,
      /// <summary>
      /// Setting extra requirement as a result of other (non-user) program activity.
      /// </summary>
      Program = 2,
    }

    /// <summary>
    /// Context of a change to content parameters.
    /// </summary>
    public enum ChangeContexts
    {
      /// <summary>
      /// Change occurred as a result of user activity in the content's UI.
      /// </summary>
      UI = 0,
      /// <summary>
      /// Change occurred as a result of drag and drop.
      /// </summary>
      Drop = 1,
      /// <summary>
      /// Change occurred as a result of internal program activity.
      /// </summary>
      Program = 2,
      /// <summary>
      /// Change can be disregarded.
      /// </summary>
      Ignore = 3,
      /// <summary>
      /// Change occurred within the content tree (e.g., nodes reordered).
      /// </summary>
      Tree = 4,
      /// <summary>
      /// Change occurred as a result of an undo.
      /// </summary>
      Undo = 5,
      /// <summary>
      /// Change occurred as a result of a field initialization.
      /// </summary>
      FieldInit = 6,
      /// <summary>
      /// Change occurred during serialization (loading).
      /// </summary>
      Serialize = 7,
    }

    /// <summary>
    /// See C++ RDK documentation - this is a pass through function that gives access to your own
    /// native shader.  .NET clients will more likely simply check the type of their content and call their own
    /// shader access functions
    /// If you overide this function, you must ensure that you call "IsCompatible" and return IntPtr.Zero is that returns false.
    /// </summary>
    /// <param name="renderEngineId">The render engine requesting the shader.</param>
    /// <param name="privateData">A pointer to the render engine's own context object.</param>
    /// <returns>A pointer to the unmanaged shader.</returns>
    /*public virtual*/
    IntPtr GetShader(Guid renderEngineId, IntPtr privateData)
    {
      if (IsNativeWrapper())
      {
        return UnsafeNativeMethods.Rdk_RenderContent_GetShader(ConstPointer(), renderEngineId, privateData);
      }
      return IntPtr.Zero;
    }

    public virtual bool IsCompatible(Guid renderEngineId)
    {
      if (IsNativeWrapper())
      {
        return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsCompatible(ConstPointer(), renderEngineId);
      }
      return 1 == UnsafeNativeMethods.Rdk_RenderContent_CallIsCompatibleBase(ConstPointer(), renderEngineId);
    }

    #region Child content support
    /// <summary>
    /// A "child slot" is the specific "slot" that a child (usually a texture) occupies.
    /// This is generally the "use" of the child - in other words, the thing the child
    /// operates on.  Some examples are "color", "transparency".
    /// </summary>
    /// <param name="paramName">The name of a parameter field. Since child textures will usually correspond with some
    ///parameter (they generally either replace or modify a parameter over UV space) these functions are used to
    ///specify which parameter corresponded with child slot.  If there is no correspondence, return the empty
    ///string.</param>
    /// <returns>
    /// The default behavior for these functions is to return the input string.
    /// Sub-classes may (in the future) override these functions to provide different mappings.
    /// </returns>
    public string ChildSlotNameFromParamName(String paramName)
    {
      using (var sh = new StringHolder())
      {
        var p_string = sh.NonConstPointer();
        var p_const_this = ConstPointer();
        UnsafeNativeMethods.Rdk_RenderContent_ChildSlotNameFromParamName(p_const_this, paramName, p_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// A "child slot" is the specific "slot" that a child (usually a texture) occupies.
    /// This is generally the "use" of the child - in other words, the thing the child
    /// operates on.  Some examples are "color", "transparency".
    /// </summary>
    /// <param name="childSlotName">The named of the child slot to receive the parameter name for.</param>
    /// <returns>The default behavior for these functions is to return the input string.  Sub-classes may (in the future) override these functions to provide different mappings.</returns>
    public string ParamNameFromChildSlotName(String childSlotName)
    {
      using (var sh = new StringHolder())
      {
        var p_string = sh.NonConstPointer();
        var p_const_this = ConstPointer();
        UnsafeNativeMethods.Rdk_RenderContent_ParamNameFromChildSlotName(p_const_this, childSlotName, p_string);
        return sh.ToString();
      }
    }

    public RenderContent FindChild(String childSlotName)
    {
      var p_const_this = ConstPointer();
      var p_child = UnsafeNativeMethods.Rdk_RenderContent_FindChild(p_const_this, childSlotName);
      return FromPointer(p_child);
    }

    /// <summary>
    /// Set another content as a child of this content. This content may or may
    /// not be attached to a document.  If this content already has a child
    /// with the specified child slot name, that child will be deleted.  If
    /// this content is not attached to a document, the child will be added
    /// without sending any events.  If this content is attached to a document,
    /// the necessary events will be sent to update the UI.
    /// Note:
    ///   Do not call this method to add children in your constructor. If you
    ///   want to add default children, you should override Initialize() and add
    ///   them there.
    /// </summary>
    /// <param name="renderContent">
    /// Child content to add to this content. If pChild is NULL, the function
    /// will fail.  If pChild is already attached to a document, the function
    /// will fail.  If pChild is already a child of this or another content,
    /// the function will fail.
    /// </param>
    /// <param name="childSlotName">
    /// The name that will be assigned to this child slot. The child slot name
    /// cannot be an empty string. If it is, the function will fail.
    /// </param>
    /// <returns>
    /// Returns true if the content was added or the child slot with this name
    /// was modified otherwise; returns false.
    /// </returns>
    public bool SetChild(RenderContent renderContent, String childSlotName)
    {
      // Changing a child slot needs to be wrapped by a BeginChange and EndChange call
      var pointer = NonConstPointer();
      var child_pointer = null == renderContent ? IntPtr.Zero : renderContent.NonConstPointer();
      var success = UnsafeNativeMethods.Rdk_RenderContent_SetChild(pointer, child_pointer, childSlotName);
      // If successfully added to the child content list then make sure the newContent
      // pointer does not get deleted when the managed object is disposed of since the
      // content is now included in this objects child content list
      if (success && null != renderContent)
        renderContent.m_b_auto_delete = false;
      return success;
    }

    /// <summary>
    /// Set another content as a child of this content. This content may or may
    /// not be attached to a document.  If this content already has a child
    /// with the specified child slot name, that child will be deleted.  If
    /// this content is not attached to a document, the child will be added
    /// without sending any events.  If this content is attached to a document,
    /// the necessary events will be sent to update the UI.
    /// Note:
    ///   Do not call this method to add children in your constructor. If you
    ///   want to add default children, you should override Initialize() and add
    ///   them there.
    /// </summary>
    /// <param name="renderContent">
    /// Child content to add to this content. If pChild is NULL, the function
    /// will fail.  If pChild is already attached to a document, the function
    /// will fail.  If pChild is already a child of this or another content,
    /// the function will fail.
    /// </param>
    /// <param name="childSlotName">
    /// The name that will be assigned to this child slot. The child slot name
    /// cannot be an empty string. If it is, the function will fail.
    /// </param>
    /// <param name="changeContexts">
    /// </param>
    /// <returns>
    /// Returns true if the content was added or the child slot with this name
    /// was modified otherwise; returns false.
    /// </returns>
    [Obsolete ("Use SetChild without ChangeContexts and Begin/EndChange")]
    public bool SetChild(RenderContent renderContent, String childSlotName, ChangeContexts changeContexts)
    {
      // Changing a child slot needs to be wrapped by a BeginChange and EndChange call
      BeginChange(changeContexts);
      var pointer = NonConstPointer();
      var child_pointer = null == renderContent ? IntPtr.Zero : renderContent.NonConstPointer();
      var success = UnsafeNativeMethods.Rdk_RenderContent_SetChild(pointer, child_pointer, childSlotName);
      // If successfully added to the child content list then make sure the newContent
      // pointer does not get deleted when the managed object is disposed of since the
      // content is now included in this objects child content list
      if (success && null != renderContent)
        renderContent.m_b_auto_delete = false;
      EndChange();
      return success;
    }

    [Obsolete("This method is obsolete and simply calls SetChild")]
    public bool AddChild(RenderContent renderContent)
    {
      if (renderContent == null) throw new ArgumentNullException("renderContent");
      var slot_name = renderContent.ChildSlotName;
      return SetChild(renderContent, slot_name, ChangeContexts.Program);
    }

    [Obsolete("This method is obsolete and simply calls SetChild")]
    public bool AddChild(RenderContent renderContent, String childSlotName)
    {
      return SetChild(renderContent, childSlotName,  ChangeContexts.Program);
    }

    public bool DeleteChild(string childSlotName, ChangeContexts changeContexts)
    {
      // Changing a child slot needs to be wrapped by a BeginChange and EndChange call
      BeginChange(changeContexts);
      var pointer = NonConstPointer();
      var success = UnsafeNativeMethods.Rdk_RenderContent_DeleteChild(pointer, childSlotName);
      EndChange();
      return success;
    }

    public void DeleteAllChildren(ChangeContexts changeContexts)
    {
      // Changing a child slot needs to be wrapped by a BeginChange and EndChange call
      BeginChange(changeContexts);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderContent_DeleteAllChildren(pointer);
      EndChange();
    }

    [Obsolete("This method is obsolete and simply calls SetChild")]
    public bool ChangeChild(RenderContent oldContent, RenderContent newContent)
    {
      if (oldContent == null) return false;
      var slot_name = oldContent.ChildSlotName;
      return SetChild(newContent, slot_name, ChangeContexts.Program);
    }

    public String ChildSlotName
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.ChildSlotName); }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetChildSlotName(ConstPointer(), value);
      }
    }


    public string[] GetEmbeddedFilesList()
    {
        using (var list = new ClassArrayString())
        {
            var p_non_const_list = list.NonConstPointer();
            UnsafeNativeMethods.Rdk_RenderContent_GetEmbeddedFiles(ConstPointer(), p_non_const_list);
            return list.ToArray();
        }
    }

    public bool Initialize()
    {
       return UnsafeNativeMethods.Rdk_RenderContent_Initialize(NonConstPointer()) == 1 ? true : false;
    }

    public void Uninitialize()
    {
      UnsafeNativeMethods.Rdk_RenderContent_Uninitialize(NonConstPointer());
    }
    public bool Replace(RenderContent newcontent)
    {
      newcontent.AutoDelete = false;

      return UnsafeNativeMethods.Rdk_RenderContent_ReplaceContentInDocument(Document.RuntimeSerialNumber, CppPointer, newcontent.CppPointer);
    }

    #endregion

    #region C++->C# Callbacks

    /// <summary>
    /// Function pointer to pass to the C++ wrapper, there is one of these for 
    /// each of RenderMaterail, RenderTexture and RenderEnvironment.
    /// </summary>
    /// <param name="typeId">Class type GUID custom attribute</param>
    /// <returns>Returns a new C++ pointer of the requested content type.</returns>
    internal delegate IntPtr NewRenderContentCallbackEvent(Guid typeId);
    /// <summary>
    /// Create content from type Id, called by the NewRenderContentCallbackEvent
    /// methods to create a .NET object pointer of a specific type from a class
    /// type Guid.
    /// </summary>
    /// <param name="typeId">The class GUID property to look up</param>
    /// <param name="isSubclassOf">The created content must be this type</param>
    /// <returns>Valid content object if the typeId is found otherwise null.</returns>
    static internal RenderContent NewRenderContent(Guid typeId, Type isSubclassOf)
    {
      var render_content_type = typeof(RenderContent);
      // If the requested type is not derived from RenderContent
      if (!isSubclassOf.IsSubclassOf(render_content_type)) throw new InvalidCastException();
      // The class is at least derived from RenderContent so continue to try 
      // an create and instance of it.
      try
      {
        // Ask the RDK plug-in manager for the class Type to create and the
        // plug-in that registered the class type.
        Guid plugin_id;
        var type = RdkPlugIn.GetRenderContentType(typeId, out plugin_id);
        // If the requested typeId was found and is derived from the requested
        // type then create an instance of the class.
        if (null != type && !type.IsAbstract && type.IsSubclassOf(isSubclassOf))
          return Activator.CreateInstance(type) as RenderContent;
      }
      catch (Exception exception)
      {
        Runtime.HostUtils.ExceptionReport(exception);
      }
      return null;
    }

    /// <summary>
    /// Called after content has been allocated and before it has been added
    /// to the document.  Override this method when you need to call virtual
    /// class methods that should not be called from the constructor.
    /// </summary>
    /// <returns>
    /// Return true to allow the content to be added to the document or false
    /// if it should be destroyed.
    /// </returns>
    //public virtual bool Initialize()
    //{
    //  return true;
    //}

    internal delegate int IsFactoryProductAcceptableAsChildCallback(int serialNumber, IntPtr contentFactoryPointer, IntPtr childSlotName);
    internal static IsFactoryProductAcceptableAsChildCallback IsFactoryProductAcceptableAsChildHook = OnIsFactoryProductAcceptableAsChild;
    static int OnIsFactoryProductAcceptableAsChild(int serialNumber, IntPtr contentFactoryPointer, IntPtr childSlotName)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null) return 1;
      var child_slot_name = System.Runtime.InteropServices.Marshal.PtrToStringUni(childSlotName);
      using (var string_holder = new StringHolder())
      {
        var pointer = string_holder.NonConstPointer();
        UnsafeNativeMethods.Rdk_Factory_Kind(contentFactoryPointer, pointer);
        var factory_kind = string_holder.ToString();
        var id = UnsafeNativeMethods.Rdk_Factory_TypeId(contentFactoryPointer);
        var result = content.IsFactoryProductAcceptableAsChild(id, factory_kind, child_slot_name);
        return (result ? 1 : 0);
      }
    }

    /// <summary>
    /// Override this method to restrict the type of acceptable child content.
    /// The default implementation of this method just returns true.
    /// </summary>
    /// <param name="kindId"></param>
    /// <param name="factoryKind"></param>
    /// <param name="childSlotName">
    /// </param>
    /// <returns>
    /// Return true only if content with the specified kindId can be  accepted
    /// as a child in the specified child slot.
    /// </returns>
    virtual public bool IsFactoryProductAcceptableAsChild(Guid kindId, string factoryKind, string childSlotName)
    {
      return factoryKind=="texture";
    }


    internal delegate int IsContentTypeAcceptableAsChildCallback(int serialNumber, Guid type, IntPtr childSlotName);
    internal static IsContentTypeAcceptableAsChildCallback m_IsContentTypeAcceptableAsChild = OnIsContentTypeAcceptableAsChild;
    static int OnIsContentTypeAcceptableAsChild(int serialNumber, Guid type, IntPtr childSlotName)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && childSlotName != IntPtr.Zero)
          return content.IsContentTypeAcceptableAsChild(type, System.Runtime.InteropServices.Marshal.PtrToStringUni(childSlotName)) ? 1 : 0;
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate void GetParameterCallback(int serialNumber, IntPtr name, IntPtr pVariant);
    internal static GetParameterCallback m_GetParameter = OnGetParameter;
    static void OnGetParameter(int serialNumber, IntPtr name, IntPtr pVariant)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);

        if (content != null && name != IntPtr.Zero && pVariant != IntPtr.Zero)
        {
          var parameter_name = System.Runtime.InteropServices.Marshal.PtrToStringUni(name);
          var rc = content.GetParameter(parameter_name);
          var v = rc as Variant;
          if (v == null)
          {
            v = new Variant(rc);
          }
          v.CopyToPointer(pVariant);
          return;
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }

      if (pVariant != IntPtr.Zero)
      {
        var v = new Variant();
        v.CopyToPointer(pVariant);
      }
    }

    internal delegate void SetEmbeddedFilesCallback(int serialNumber, IntPtr result);
    internal static SetEmbeddedFilesCallback g_embedded_files_callback = OnEmbeddedFilesCallback;
    static void OnEmbeddedFilesCallback(int serialNumber, IntPtr result)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null)
        return;
      var list = content.FilesToEmbed;
      if (list == null)
        return;
      string delimited_string = null;
      foreach (var s in list)
        if (!string.IsNullOrEmpty(s))
          delimited_string = (delimited_string == null ? "" : delimited_string + ";") + s;
      UnsafeNativeMethods.ON_wString_Set(result, delimited_string);
    }

    /// <summary>
    /// A string array of full paths to files used by the content that may be
    /// embedded in .3dm files and library files (.rmtl, .renv, .rtex). The
    /// default implementation returns an empty string list. Override this to
    /// return the file name or file names used by your content. This is
    /// typically used by textures that reference files containing the texture
    /// imagery. 
    /// </summary>
    public virtual IEnumerable<string> FilesToEmbed { get { return new String[0]; } }

    internal delegate int SetParameterCallback(int serialNumber, IntPtr name, IntPtr value);
    internal static SetParameterCallback m_SetParameter = OnSetParameter;
    static int OnSetParameter(int serialNumber, IntPtr name, IntPtr value)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && name != IntPtr.Zero && value != IntPtr.Zero)
        {
          var v = Variant.CopyFromPointer(value);
          return (content.SetParameter(System.Runtime.InteropServices.Marshal.PtrToStringUni(name), v) ? 1 : 0);
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }


    internal delegate int GetExtraRequirementParameterCallback(int serialNumber, IntPtr paramName, IntPtr extraRequirementName, IntPtr value);
    internal static GetExtraRequirementParameterCallback m_GetExtraRequirementParameter = OnGetExtraRequirementParameter;
    static int OnGetExtraRequirementParameter(int serialNumber, IntPtr paramName, IntPtr extraRequirementName, IntPtr value)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && paramName != IntPtr.Zero && extraRequirementName != IntPtr.Zero && value != IntPtr.Zero)
        {
          var parameter_name = System.Runtime.InteropServices.Marshal.PtrToStringUni(paramName);
          var requirement_name = System.Runtime.InteropServices.Marshal.PtrToStringUni(extraRequirementName);
          var rc = content.GetChildSlotParameter(parameter_name, requirement_name);

          var v = rc as Variant;
          if (v == null)
            v = new Variant(rc);
          v.CopyToPointer(value);
          return (!v.IsNull ? 1 : 0);
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate int SetContentIconCallback(int serialNumber, int width, int height, IntPtr dibOut, int fromBaseClass);
    internal static SetContentIconCallback SetContentIcon = OnSetContentIcon;
    private static int OnSetContentIcon(int serialNumber, int width, int height, IntPtr dibOut, int fromBaseClass)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content == null) return 0;
        if (fromBaseClass > 0)
        {
          // Call into the C RDK to get the Icon
          var const_pointer = content.ConstPointer();
          return UnsafeNativeMethods.Rdk_RenderContent_GetVirtualIcon(const_pointer, width, height, dibOut, 1);
        }
        // Call the .NET RDK to get the bitmap
        Bitmap bitmap;
        content.VirtualIcon(new Size(width, height), out bitmap);
        if (null == bitmap) return 0;
        IntPtr handle;
        if (!content.m_bitmap_to_icon_dictionary.TryGetValue(bitmap, out handle))
        {
          handle = bitmap.GetHbitmap();
          content.m_bitmap_to_icon_dictionary.Add(bitmap, handle);
        }
        UnsafeNativeMethods.CRhinoDib_SetFromHBitmap(dibOut, handle, true);
        return 1;
      }

      catch (Exception exception)
      {
        Runtime.HostUtils.ExceptionReport(exception);
      }
      return 0;
    }
    private readonly Dictionary<Bitmap, IntPtr> m_bitmap_to_icon_dictionary = new Dictionary<Bitmap, IntPtr>();

    /// <summary>
    /// Icon to display in the content browser, this bitmap needs to be valid for
    /// the life of this content object, the content object that returns the bitmap
    /// is responsible for disposing of the bitmap.
    /// </summary>
    /// <param name="size">
    /// Requested icon size
    /// </param>
    /// <param name="bitmap">
    /// </param>
    /// <returns>
    /// Return Icon to display in the content browser.
    /// </returns>
    virtual public bool VirtualIcon(Size size, out Bitmap bitmap)
    {
      bitmap = null;
      var const_ptr_this = ConstPointer();
      using (var rhinodib = new RhinoDib())
      {
        var ptr_rhino_dib = rhinodib.NonConstPointer;
        var success = UnsafeNativeMethods.Rdk_RenderContent_GetVirtualIcon(const_ptr_this, size.Width, size.Height, ptr_rhino_dib, 1);
        if (success != 0)
          bitmap = rhinodib.ToBitmap();

        return (success != 0);
      }
    }

    virtual public bool Icon(Size size, out Bitmap bitmap)
    {
      bitmap = null;
      var const_ptr_this = ConstPointer();
      using (var rhinodib = new RhinoDib())
      {
        var ptr_rhino_dib = rhinodib.NonConstPointer;
        var success = UnsafeNativeMethods.Rdk_RenderContent_GetIcon(const_ptr_this, size.Width, size.Height, ptr_rhino_dib);

        if (success != 0)
          bitmap = rhinodib.ToBitmap();

        return (success != 0);
      }
    }

    virtual public bool DynamicIcon(Size size, out Bitmap bitmap, DynamicIconUsage usage)
    {
      bitmap = null;
      var const_ptr_this = ConstPointer();
      using (var rhinodib = new RhinoDib())
      {
        var ptr_rhino_dib = rhinodib.NonConstPointer;
        var success = UnsafeNativeMethods.Rdk_RenderContent_GetDynamicIcon(const_ptr_this, size.Width, size.Height, ptr_rhino_dib, (int)usage);

        if (success != 0)
          bitmap = rhinodib.ToBitmap();

        return (success != 0);
      }
    }
    
    internal delegate int SetExtraRequirementParameterCallback(int serialNumber, IntPtr paramName, IntPtr extraRequirementName, IntPtr value, int sc);
    internal static SetExtraRequirementParameterCallback m_SetExtraRequirementParameter = OnSetExtraRequirementParameter;
    static int OnSetExtraRequirementParameter(int serialNumber, IntPtr paramName, IntPtr extraRequirementName, IntPtr value, int sc)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && paramName != IntPtr.Zero && value != IntPtr.Zero && extraRequirementName != IntPtr.Zero)
        {
          var v = Variant.CopyFromPointer(value);
          return content.SetChildSlotParameter(System.Runtime.InteropServices.Marshal.PtrToStringUni(paramName),
                                      System.Runtime.InteropServices.Marshal.PtrToStringUni(extraRequirementName),
                                      v,
                                      (ExtraRequirementsSetContexts)sc) ? 1 : 0;
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate int HarvestDataCallback(int serialNumber, IntPtr oldContent);
    internal static HarvestDataCallback HarvestData = g_on_harvest_data;
    static int g_on_harvest_data(int serialNumber, IntPtr oldContent)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        var old = FromPointer(oldContent);
        if (content != null && old != null)
          return (int)content.MatchData(old);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return (int)MatchDataResult.None;
    }

    internal delegate void AddUiSectionsCallback(int serialNumber, Guid editorId);
    internal static AddUiSectionsCallback m_AddUISections = _OnAddUISections;
    internal static Guid OnAddUiSectionsUIId = Guid.Empty;
    static void _OnAddUISections(int serialNumber, Guid UIId)
    {
      OnAddUiSectionsUIId = UIId;
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null)
          content.OnAddUserInterfaceSections();
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      OnAddUiSectionsUIId = Guid.Empty;
    }

    internal delegate int GetDefaultsFromUserCallback(int serialNumber);
    internal static GetDefaultsFromUserCallback GetDefaultsFromUser = g_on_get_defaults_from_user;
    static int g_on_get_defaults_from_user(int serialNumber)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null)
          return content.OnGetDefaultsInteractive() ? 1 : 0;
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate void RenderContentOnCppDtorCallback(int serialNumber);
    internal static RenderContentOnCppDtorCallback OnCppDtor = OnCppDtorRhCmnRenderContent;
    static void OnCppDtorRhCmnRenderContent(int serialNumber)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null)
        {
          //This is about to get deleted by C++ - don't delete it in Dispose
          content.AutoDelete = false;
          content.Dispose(true);
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    internal delegate uint RenderContentBitFlagsCallback(int serialNumber, uint flags);
    internal static RenderContentBitFlagsCallback BitFlags = OnContentBitFlags;
    static uint OnContentBitFlags(int serialNumber, uint flags)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (null != content)
        {
          var styles_to_add = (uint)content.m_styles_to_add;
          var styles_to_remove = (uint)content.m_styles_to_remove;
          flags |= styles_to_add;
          flags &= ~styles_to_remove;
          return flags;
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return flags;
    }

    private RenderContentStyles m_styles_to_add = RenderContentStyles.None;
    private RenderContentStyles m_styles_to_remove = RenderContentStyles.None;

    protected void ModifyRenderContentStyles(RenderContentStyles stylesToAdd, RenderContentStyles stylesToRemove)
    {
      m_styles_to_add = stylesToAdd;
      m_styles_to_remove = stylesToRemove;
    }

    internal delegate void GetRenderContentStringCallback(int serialNumber, int string_id, IntPtr pON_wString);
    internal static GetRenderContentStringCallback GetRenderContentString = OnGetRenderContentString;
    static void OnGetRenderContentString(int serialNnumber, int string_id, IntPtr pOnWString)
    {
      try
      {
        var content = FromSerialNumber(serialNnumber);
        if (content == null) return;

        string str = "";
        var id = (UnsafeNativeMethods.RenderContent_StringIds)string_id;

        if (id == UnsafeNativeMethods.RenderContent_StringIds.TypeName)
        {
          str = content.TypeName;
        }
        else if (id == UnsafeNativeMethods.RenderContent_StringIds.TypeDescription)
        {
          str = content.TypeDescription;
        }
        else
        {
          Debug.Assert(false);
        }

        if (!string.IsNullOrEmpty(str))
          UnsafeNativeMethods.ON_wString_Set(pOnWString, str);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    internal delegate IntPtr GetShaderCallback(int serialNumber, Guid renderEngineId, IntPtr privateData);
    internal static GetShaderCallback m_GetShader = OnGetShader;
    static IntPtr OnGetShader(int serialNumber, Guid renderEngineId, IntPtr privateData)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null)
          return content.GetShader(renderEngineId, privateData);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return IntPtr.Zero;
    }

    #endregion

    #region events

    internal delegate void ContentAddedCallback(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseAttachReason reason);
    internal delegate void ContentRenamedCallback(IntPtr pContent);
    internal delegate void ContentDeletingCallback(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseDetachReason reson);
    internal delegate void ContentReplacingCallback(uint docSerialNumber, IntPtr pContent);
    internal delegate void ContentReplacedCallback(uint docSerialNumber, IntPtr pContent);
    internal delegate void ContentChangedCallback(IntPtr newContent, IntPtr oldContent, int changeContext);
    internal delegate void ContentUpdatePreviewCallback(IntPtr pContent);

    internal delegate void ContentTypeAddedCallback(Guid typeId);
    internal delegate void ContentTypeDeletingCallback(Guid typeId);
    internal delegate void ContentTypeDeletedCallback(int kind);

    internal delegate void CurrentContentChangedCallback(uint docSerialNumber, int kind, int usage, IntPtr pContent);

    private static ContentAddedCallback m_OnContentAdded;
    private static void OnContentAdded(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseAttachReason reason)
    {
      if (m_content_added_event != null)
      {
        try
        {
          var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
          m_content_added_event(doc, new RenderContentEventArgs(doc, FromPointer(pContent), ReasonFromAttachReason(reason)));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<RenderContentEventArgs> m_content_added_event;

    private static ContentRenamedCallback m_OnContentRenamed;
    private static void OnContentRenamed(IntPtr pContent)
    {
      if (g_content_renamed_event != null)
      {
        try
        {
          var content = FromPointer(pContent);
          var doc = content == null ? null : content.Document;
          g_content_renamed_event(doc, new RenderContentEventArgs(doc, FromPointer(pContent)));
        }
        catch (Exception ex) { Runtime.HostUtils.ExceptionReport(ex); }
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_renamed_event;

    private static ContentDeletingCallback m_OnContentDeleting;
    private static void OnContentDeleting(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseDetachReason reason)
    {
      if (g_content_deleting_event != null)
      {
        try
        {
          var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
          g_content_deleting_event(doc, new RenderContentEventArgs(doc, FromPointer(pContent), ReasonFromDetachReason(reason)));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_deleting_event;

    private static ContentDeletingCallback m_OnContentDeleted;
    private static void OnContentDeleted(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseDetachReason reason)
    {
      if (g_content_deleted_event != null)
      {
        try
        {
          var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
          g_content_deleted_event(doc, new RenderContentEventArgs(doc, FromPointer(pContent), ReasonFromDetachReason(reason)));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_deleted_event;

    private static ContentReplacingCallback m_OnContentReplacing;
    private static void OnContentReplacing(uint docSerialNumber, IntPtr pContent)
    {
      if (g_content_replacing_event != null)
      {
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        try { g_content_replacing_event(doc, new RenderContentEventArgs(doc, FromPointer(pContent))); }
        catch (Exception ex) { Runtime.HostUtils.ExceptionReport(ex); }
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_replacing_event;

    private static ContentReplacedCallback m_OnContentReplaced;
    private static void OnContentReplaced(uint docSerialNumber, IntPtr pContent)
    {
      if (g_content_replaced_event != null)
      {
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        try { g_content_replaced_event(doc, new RenderContentEventArgs(doc, FromPointer(pContent))); }
        catch (Exception ex) { Runtime.HostUtils.ExceptionReport(ex); }
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_replaced_event;

    private static ContentChangedCallback m_OnContentChanged;
    private static void OnContentChanged(IntPtr newContent, IntPtr oldContent, int cc)
    {
      if (g_content_changed_event != null)
      {
        try
        {
          var content = FromPointer(newContent);
          var old_content = FromPointer(oldContent);
          var doc = (null == content ? null : content.Document);
          g_content_changed_event(doc, new RenderContentChangedEventArgs(doc, content, old_content, (ChangeContexts)cc));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    static EventHandler<RenderContentChangedEventArgs> g_content_changed_event;

    private static ContentUpdatePreviewCallback m_OnContentUpdatePreview;
    private static void OnContentUpdatePreview(IntPtr pContent)
    {
      if (g_content_update_preview_event != null)
      {
        try
        {
          var content = FromPointer(pContent);
          var doc = (content == null ? null : content.Document);
          g_content_update_preview_event(doc, new RenderContentEventArgs(doc, content));
        }
        catch (Exception ex) { Runtime.HostUtils.ExceptionReport(ex); }
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_update_preview_event;

    /// <summary>
    /// Used to monitor render content addition to the document.
    /// </summary>
    public static event EventHandler<RenderContentEventArgs> ContentAdded
    {
      add
      {
        if (m_content_added_event == null)
        {
          m_OnContentAdded = OnContentAdded;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentAddedEventCallback(m_OnContentAdded, Runtime.HostUtils.m_rdk_ew_report);
        }
        m_content_added_event += value;
      }
      remove
      {
        m_content_added_event -= value;
        if (m_content_added_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentAddedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentAdded = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content renaming in the document.
    /// </summary>
    public static event EventHandler<RenderContentEventArgs> ContentRenamed
    {
      add
      {
        if (g_content_renamed_event == null)
        {
          m_OnContentRenamed = OnContentRenamed;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentRenamedEventCallback(m_OnContentRenamed, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_content_renamed_event += value;
      }
      remove
      {
        g_content_renamed_event -= value;
        if (g_content_renamed_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentRenamedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentRenamed = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content deletion from the document.
    /// </summary>
    public static event EventHandler<RenderContentEventArgs> ContentDeleting
    {
      add
      {
        if (g_content_deleting_event == null)
        {
          m_OnContentDeleting = OnContentDeleting;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletingEventCallback(m_OnContentDeleting, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_content_deleting_event += value;
      }
      remove
      {
        g_content_deleting_event -= value;
        if (g_content_deleting_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletingEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentDeleting = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content deletion from the document.
    /// </summary>
    public static event EventHandler<RenderContentEventArgs> ContentDeleted
    {
      add
      {
        if (g_content_deleted_event == null)
        {
          m_OnContentDeleted = OnContentDeleted;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletedEventCallback(m_OnContentDeleted, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_content_deleted_event += value;
      }
      remove
      {
        g_content_deleted_event -= value;
        if (g_content_deleted_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentDeleted = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content replacing in the document.
    /// </summary>
    public static event EventHandler<RenderContentEventArgs> ContentReplacing
    {
      add
      {
        if (g_content_replacing_event == null)
        {
          m_OnContentReplacing = OnContentReplacing;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacingEventCallback(m_OnContentReplacing, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_content_replacing_event += value;
      }
      remove
      {
        g_content_replacing_event -= value;
        if (g_content_replacing_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacingEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentReplacing = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content replacing in the document.
    /// </summary>
    public static event EventHandler<RenderContentEventArgs> ContentReplaced
    {
      add
      {
        if (g_content_replaced_event == null)
        {
          m_OnContentReplaced = OnContentReplaced;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacedEventCallback(m_OnContentReplaced, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_content_replaced_event += value;
      }
      remove
      {
        g_content_replaced_event -= value;
        if (g_content_replaced_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentReplaced = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content modifications.
    /// </summary>
    public static event EventHandler<RenderContentChangedEventArgs> ContentChanged
    {
      add
      {
        if (g_content_changed_event == null)
        {
          m_OnContentChanged = OnContentChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentChangedEventCallback(m_OnContentChanged, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_content_changed_event += value;
      }
      remove
      {
        g_content_changed_event -= value;
        if (g_content_changed_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentChanged = null;
        }
      }
    }
    /// <summary>
    /// This event is raised when a field value is modified.
    /// </summary>
    public static event EventHandler<RenderContentFieldChangedEventArgs> ContentFieldChanged
    {
      add
      {
        if (g_on_content_field_changed == null)
        {
          g_on_content_field_changed = OnContentFieldChanged;
          UnsafeNativeMethods.Rdk_SetOnContentFieldChangeCallback(g_on_content_field_changed);
        }
        g_content_field_changed_event += value;
      }
      remove
      {
        g_content_field_changed_event -= value;
        if (g_content_field_changed_event == null)
        {
          UnsafeNativeMethods.Rdk_SetOnContentFieldChangeCallback(null);
          g_content_field_changed_event = null;
        }
      }
    }
    internal delegate void OnContentFieldChangedCallback(int serialNumber, IntPtr name, IntPtr value, int cc);
    internal static OnContentFieldChangedCallback g_on_content_field_changed;
    static EventHandler<RenderContentFieldChangedEventArgs> g_content_field_changed_event;
    private static void OnContentFieldChanged(int serialNumber, IntPtr name, IntPtr value, int cc)
    {
      try
      {
        if (name == IntPtr.Zero) return;
        var content = FromSerialNumber(serialNumber);
        if (content == null) return;
        //var v = Variant.CopyFromPointer(value);
        //var old_value = v.AsObject();
        var name_string = System.Runtime.InteropServices.Marshal.PtrToStringUni(name);
        var args = new RenderContentFieldChangedEventArgs(content, name_string, (ChangeContexts) cc);
        g_content_field_changed_event(content, args);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }


    /// <summary>
    /// Used to monitor render content preview updates.
    /// </summary>
    public static event EventHandler<RenderContentEventArgs> ContentUpdatePreview
    {
      add
      {
        if (g_content_update_preview_event == null)
        {
          m_OnContentUpdatePreview = OnContentUpdatePreview;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentUpdatePreviewEventCallback(m_OnContentUpdatePreview, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_content_update_preview_event += value;
      }
      remove
      {
        g_content_update_preview_event -= value;
        if (g_content_update_preview_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentUpdatePreviewEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnContentUpdatePreview = null;
        }
      }
    }

    static EventHandler<RenderContentEventArgs> g_current_environment_change_event;
    private static CurrentContentChangedCallback m_OnCurrentEnvironmentChange;
    private static void OnCurrentEnvironmentChange(uint docSerialNumber, int kind, int usage, IntPtr pContent)
    {
      if (g_current_environment_change_event != null)
      {
        try
        {
          var content = FromPointer(pContent);
          var doc = content?.Document;
          g_current_environment_change_event(doc, new RenderContentEventArgs(doc, content, (RenderEnvironment.Usage)usage));
        }
        catch (Exception ex) { Runtime.HostUtils.ExceptionReport(ex); }
      }
    }
    /// <summary>
    /// Event fired when changes to current environments have been made.
    /// This will be one of Background, ReflectionAndRefraction or Skylighting
    /// Since 6.11
    /// </summary>
    /// <since>6.11</since>
    public static event EventHandler<RenderContentEventArgs> CurrentEnvironmentChanged
    {
      add
      {
        if (g_current_environment_change_event == null)
        {
          m_OnCurrentEnvironmentChange = OnCurrentEnvironmentChange;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentCurrencyChangedEventCallback(m_OnCurrentEnvironmentChange, Runtime.HostUtils.m_rdk_ew_report);
        }
        g_current_environment_change_event += value;
      }
      remove
      {
        g_current_environment_change_event -= value;
        if (g_current_environment_change_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentCurrencyChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
          m_OnCurrentEnvironmentChange = null;
        }
      }
    }
    #endregion

    #region pointer tracking

    private bool m_b_auto_delete;
    internal bool AutoDelete
    {
      get { return m_b_auto_delete; }
      set { m_b_auto_delete = value; }
    }

    internal static RenderContent FromSerialNumber(int serialNumber)
    {
      RenderContent rc;
      g_custom_content_dictionary.TryGetValue(serialNumber, out rc);
      return rc;
    }

    internal virtual IntPtr ConstPointer()
    {
      var p_content = UnsafeNativeMethods.Rdk_FindRhCmnContentPointer(RuntimeSerialNumber, ref m_search_hint);
      return p_content;
    }
    internal virtual IntPtr NonConstPointer()
    {
      var p_content = UnsafeNativeMethods.Rdk_FindRhCmnContentPointer(RuntimeSerialNumber, ref m_search_hint);
      return p_content;
    }

    internal bool IsNativeWrapper()
    {
      return ClassDefinedInRhinoCommon();
    }
    #endregion

    #region disposable implementation
    ~RenderContent()
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
      if (m_b_auto_delete)
      {
        //Calling into C++ dtor will re-enter the dispose function - ensure there is no double delete.
        m_b_auto_delete = false;
        UnsafeNativeMethods.Rdk_RenderContent_Uninitialize(NonConstPointer());  //Cmn render contents are always initialized
        UnsafeNativeMethods.Rdk_RenderContent_DeleteThis(NonConstPointer());
      }

      //Nothing below here must access the C++ implementation.
      if (null != m_field_dictionary)
      {
        //Field dictionary no longer actually owns anything  on the C++ side.
        //m_field_dictionary.InternalDispose();
        m_field_dictionary = null;
      }

      var serialNumber = RuntimeSerialNumber;
      if (serialNumber >= 0)
      {
        RuntimeSerialNumber = -1;
        g_custom_content_dictionary.Remove(serialNumber);
      }

      if (HostUtils.RunningOnWindows)
      {
        // TODO_RDK_MAC the dictionary handles cannot be deleted in 
        // in the Mac. This needs to be thought carefully. The current
        // problem is that we cannot yet create an CRhinoDib from C#
        // Bitmap.

        foreach (var item in m_bitmap_to_icon_dictionary)
        {
          UnsafeNativeMethods.DeleteObject(item.Value);
        }
        m_bitmap_to_icon_dictionary.Clear();
      }
      
    }
    #endregion
  }

  [CLSCompliant(false)]
  public enum CrcRenderHashFlags : ulong
  {
    Normal                 = 0, // Normal render CRC; nothing is excluded.
    ExcludeLinearWorkflow  = 1, // Linear Workflow color and texture changes are not included.
    ExcludeLocalMapping    = 2, // Local mapping is excluded (only used by Textures).
    Reserved1              = 4,
    Reserved2              = 8,
  }

  public class RenderContentEventArgs : EventArgs
  {
    readonly RenderContent m_content;

    internal RenderContentEventArgs(RhinoDoc doc, RenderContent content)
    {
      m_content = content;
      Document = doc;
    }
    internal RenderContentEventArgs(RhinoDoc doc, RenderContent content, RenderContentChangeReason reason)
    {
      m_content = content;
      Document = doc;
      Reason = reason;
    }
    internal RenderContentEventArgs(RhinoDoc doc, RenderContent content, RenderEnvironment.Usage usage)
    {
      m_content = content;
      Document = doc;
      EnvironmentUsage = usage;
    }
    public RenderContent Content { get { return m_content; } }
    public RhinoDoc Document { get; private set; }
    /// <summary>
    /// Not when used in CurrentEnvironmentChanged (defaults to None).
    /// </summary>
    public RenderContentChangeReason Reason { get; private set; } = RenderContentChangeReason.None;
    /// <summary>
    /// Meaningful for CurrentEnvironmentChanged event. Will be one of Background, ReflectionAndRefraction or Skylighting.
    /// 
    /// Since 6.11
    /// </summary>
    /// <since>6.11</since>
    public RenderEnvironment.Usage EnvironmentUsage { get; private set; } = RenderEnvironment.Usage.None;
  }

  public class RenderContentChangedEventArgs : RenderContentEventArgs
  {
    internal RenderContentChangedEventArgs(RhinoDoc doc, RenderContent content, RenderContent oldContent,
      RenderContent.ChangeContexts cc)
      : base(doc, content)
    {
      m_old_content = oldContent;
      m_cc = cc;
    }

    readonly RenderContent.ChangeContexts m_cc;
    public RenderContent.ChangeContexts ChangeContext { get { return m_cc; } }
    private readonly RenderContent m_old_content;
    public RenderContent OldContent { get { return m_old_content; } }
  }


  /// <summary>
  /// Enumeration denoting type of change for attach or detach
  /// </summary>
  public enum RenderContentChangeReason
  {
    /// <summary>
    /// No attach or detach change
    /// </summary>
    None,
    /// <summary>
    /// Content is being attached by the RhinoDoc.AttachContent() or RenderContent.AttachChild() methods.
    /// </summary>
    Attach,
    /// <summary>
    /// Content is being detached by the RenderContent.DeleteContent() method.
    /// </summary>
    Detach,
    /// <summary>
    /// Content is being attached while changing.
    /// </summary>
    ChangeAttach,
    /// <summary>
    /// Content is being detached while changing.
    /// </summary>
    ChangeDetach,
    /// <summary>
    /// Content is being attached during undo/redo
    /// </summary>
    AttachUndo,
    /// <summary>
    /// Content is being detached during undo/redo.
    /// </summary>
    DetachUndo,
    /// <summary>
    /// Content is being attached during open document
    /// </summary>
    Open,
    /// <summary>
    /// Content is being detached during normal deletion.
    /// </summary>
    Delete
  }

  public class RenderContentFieldChangedEventArgs : RenderContentChangedEventArgs
  {
    internal RenderContentFieldChangedEventArgs(RenderContent content, string fieldName, RenderContent.ChangeContexts cc)
      : base(content.Document, content, null, cc)
    {
      m_field_name = fieldName;
    }
    public string FieldName { get { return m_field_name; } }
    private readonly string m_field_name;
  }

  /*public*/
  class RenderContentTypeEventArgs : EventArgs
  {
    readonly Guid m_content_type;
    internal RenderContentTypeEventArgs(Guid type) { m_content_type = type; }
    public Guid Content { get { return m_content_type; } }
  }

  /*public*/
  class RenderContentKindEventArgs : EventArgs
  {
    readonly RenderContentKind m_kind;
    internal RenderContentKindEventArgs(RenderContentKind kind) { m_kind = kind; }
    //public RenderContentKind Content { get { return m_kind; } }
  }

  /*public*/
  class CurrentRenderContentChangedEventArgs : RenderContentEventArgs
  {
    internal CurrentRenderContentChangedEventArgs(RhinoDoc doc, RenderContent content, RenderContentKind kind)
      : base(doc, content)
    {
      m_kind = kind;
    }

    readonly RenderContentKind m_kind;
    //public RenderContentKind Kind { get { return m_kind; } }
  }
}

internal interface INativeContent
{
  Guid Document { get; set; }
}

#endif
