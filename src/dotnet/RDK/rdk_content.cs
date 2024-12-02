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
using Rhino.Runtime;

namespace Rhino.Render.UI
{
  /// <summary>
  /// Implement this interface in your user control to get UserInterfaceSection
  /// event notification.
  /// </summary>
  [Obsolete("Obsolete")]
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
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
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
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
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
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    bool Hidden { get; }
  }

  /// <summary>
  /// Custom user interface section manager
  /// </summary>
  [Obsolete("Obsolete")]
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

      if (ui_section.m_window is IDisposable dis)
        dis.Dispose();

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
      if (!UiFromSerialNumber(serialNumber, out var ui_section, out var i_ui_section))
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
      if (UiFromSerialNumber(serialNumber, out var ui_section, out var i_ui_section))
        i_ui_section.OnUserInterfaceSectionExpanding(ui_section, expanding);
    }

    /// <summary>
    /// Called when it is safe to initialize the control window.
    /// </summary>
    /// <param name="serialNumber"></param>
    private static int IsHiddenProc(int serialNumber)
    {
      if (UiFromSerialNumber(serialNumber, out var _, out var i_ui_section))
        return i_ui_section.Hidden ? 1 : 0;

      return 0;
    }

    #endregion C++ function callbacks

    #region Public properties
    /// <summary>
    /// The RenderContent object that created this user interface object.
    /// </summary>
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
    public RenderContent RenderContent
    {
      get
      {
        var pointer = UnsafeNativeMethods.Rdk_CoreContent_RenderContentFromUISection(SerialNumber, ref m_search_hint);
        var found = RenderContent.FromPointer(pointer, null);
        return found;
      }
    }
    /// <summary>
    /// The user control associated with this user interface object.
    /// </summary>
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
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
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    public static UserInterfaceSection FromWindow(object window)
    {
      if (null != window)
      {
        foreach (var section in g_user_interface_section_dictionary)
        {
          if (window.Equals(section.Value.Window))
            return section.Value;
        }
      }

      return null;
    }
    /// <summary>
    /// Returns a list of currently selected content items to be edited.
    /// </summary>
    /// <returns>Returns a list of currently selected content items to be edited.</returns>
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
    public RenderContent[] GetContentList()
    {
      var id_list = GetContentIdList();
      var render_content_list = new List<RenderContent>(id_list.Length);
      var doc = RhinoDoc.ActiveDoc;
      foreach (var guid in id_list)
      {
        var content = RenderContent.FromId(doc, guid);
        if (null != content)
          render_content_list.Add(content);
      }
      return render_content_list.ToArray();
    }
    /// <summary>
    /// Show or hide this content section.
    /// </summary>
    /// <param name="visible">If true then show the content section otherwise hide it.</param>
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
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
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
    public void Expand(bool expand)
    {
      var serial_number = SerialNumber;
      UnsafeNativeMethods.Rdk_CoreContent_UiSectionExpand(serial_number, ref m_search_hint, expand);
    }
    #endregion Public methods

//    #region Private properties
//    /// <summary>
//    /// Dereference the serial number as a C++ pointer, used for direct access to the C++ object.
//    /// </summary>
//    private IntPtr Pointer
//    {
//      get
//      {
//        var pointer = UnsafeNativeMethods.Rdk_CoreContent_AddFindContentUISectionPointer(SerialNumber, ref m_search_hint);
//        return pointer;
//      }
//    }
//    #endregion Private properties

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
      g_user_interface_section_dictionary.TryGetValue(serialNumber, out UserInterfaceSection found);
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
  /// <since>5.1</since>
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
    /// Render content UI includes a 'Name' and 'Type' section.
    /// </summary>
    NameTypeSection = 0x0800,
    /// <summary>
    /// OBSOLETE: The UI is always shared between render contents of the same type id.
    /// </summary>
    [Obsolete("No longer needed")]
    SharedUI = 0x0040,
    /// <summary>
    /// Texture UI includes an adjustment section.
    /// </summary>
    Adjustment = 0x0080,
    /// <summary>
    /// Render content uses fields to facilitate data storage and undo support. See Fields()
    /// </summary>
    Fields = 0x0100,
    /// <summary>
    /// Render content supports editing in a modal editor. OBSOLETE: All contents support modal editing.
    /// </summary>
    ModalEditing = 0x0200,
    /// <summary>
    /// The render content's fields are dynamic. OBSOLETE: Dynamic fields can co-exist with static fields.
    /// </summary>
    DynamicFields = 0x0400,
  }

  /// <summary>
  /// Content Guids of RenderContent provided by the RDK SDK.
  /// 
  /// These Guids can be used to check against RenderContent.TypeId.
  /// </summary>
  public static class ContentUuids
  {
    /// <since>6.0</since>
    public static Guid BasicMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBasicMaterialType);
    /// <since>6.0</since>
    public static Guid BlendMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBlendMaterialType);
    /// <since>6.0</since>
    public static Guid CompositeMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcCompositeMaterialType);
    /// <since>6.0</since>
    public static Guid PlasterMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPlasterMaterialType);
    /// <since>6.0</since>
    public static Guid MetalMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcMetalMaterialType);
    /// <since>6.0</since>
    public static Guid PaintMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPaintMaterialType);
    /// <since>6.0</since>
    public static Guid PlasticMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPlasticMaterialType);
    /// <since>6.0</since>
    public static Guid GemMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcGemMaterialType);
    /// <since>6.0</since>
    public static Guid GlassMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcGlassMaterialType);
    /// <since>6.0</since>
    public static Guid PictureMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPictureMaterialType);
    /// <since>6.0</since>
    public static Guid DefaultMaterialInstance => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcDefaultMaterialInstance);
    /// <summary>
    /// Rhino V8 Blend material type Guid
    /// </summary>
    /// <since>8.0</since>
    public static Guid V8BlendMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcV8BlendMaterialType);
    /// <since>7.0</since>
    public static Guid PhysicallyBasedMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPhysicallyBasedMaterialType);
    /// <since>7.0</since>
    public static Guid DoubleSidedMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcDoubleSidedMaterialType);
    /// <since>7.0</since>
    public static Guid EmissionMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcEmissionMaterialType);
    /// <since>7.5</since>
    public static Guid DisplayAttributeMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcDisplayAttributeMaterialType);
    /// <since>6.0</since>
    public static Guid RealtimeDisplayMaterialType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcRealtimeDisplayMaterialType);
    /// <since>6.0</since>
    public static Guid BasicEnvironmentType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBasicEnvironmentType);
    /// <since>6.0</since>
    public static Guid DefaultEnvironmentInstance => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcDefaultEnvironmentInstance);
    /// <since>6.0</since>
    public static Guid Texture2DCheckerTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.Rc2DCheckerTextureType);
    /// <since>6.0</since>
    public static Guid Texture3DCheckerTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.Rc3DCheckerTextureType);
    /// <since>6.0</since>
    public static Guid AdvancedDotTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcAdvancedDotTextureType);
    /// <since>6.0</since>
    public static Guid BitmapTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBitmapTextureType);
    /// <since>6.0</since>
    public static Guid BlendTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBlendTextureType);
    /// <since>6.0</since>
    public static Guid CubeMapTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcCubeMapTextureType);
    /// <since>6.0</since>
    public static Guid ExposureTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcExposureTextureType);
    /// <since>6.0</since>
    public static Guid FBmTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcFBmTextureType);
    /// <since>6.0</since>
    public static Guid GradientTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcGradientTextureType);
    /// <since>6.0</since>
    public static Guid GraniteTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcGraniteTextureType);
    /// <since>6.0</since>
    public static Guid GridTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcGridTextureType);
    /// <since>6.0</since>
    public static Guid HDRTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcHDRTextureType);
    /// <since>6.0</since>
    public static Guid EXRTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcEXRTextureType);
    /// <since>6.0</since>
    public static Guid MarbleTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcMarbleTextureType);
    /// <since>6.0</since>
    public static Guid MaskTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcMaskTextureType);
    /// <since>6.0</since>
    public static Guid NoiseTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcNoiseTextureType);
    /// <since>6.0</since>
    public static Guid PerlinMarbleTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPerlinMarbleTextureType);
    /// <since>6.0</since>
    public static Guid PerturbingTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPerturbingTextureType);
    /// <since>6.0</since>
    public static Guid ProjectionChangerTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcProjectionChangerTextureType);
    /// <since>6.0</since>
    public static Guid ResampleTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcResampleTextureType);
    /// <since>6.0</since>
    public static Guid SingleColorTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcSingleColorTextureType);
    /// <since>6.0</since>
    public static Guid SimpleBitmapTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcSimpleBitmapTextureType);
    /// <since>6.0</since>
    public static Guid StuccoTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcStuccoTextureType);
    /// <since>6.0</since>
    public static Guid TextureAdjustmentTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcTextureAdjustmentTextureType);
    /// <since>6.0</since>
    public static Guid TileTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcTileTextureType);
    /// <since>6.0</since>
    public static Guid TurbulenceTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcTurbulenceTextureType);
    /// <since>6.0</since>
    public static Guid WavesTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcWavesTextureType);
    /// <since>6.0</since>
    public static Guid WoodTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcWoodTextureType);
    /// <since>8.0</since>
    public static Guid AddTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcAddTextureType);
    /// <since>8.0</since>
    public static Guid MultiplyTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcMultiplyTextureType);
    /// <since>8.0</since>
    public static Guid PhysicalSkyTextureType => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcPhysicalSkyTextureType);
    /// <since>6.0</since>
    public static Guid HatchBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcHatchBumpTexture);
    /// <since>6.0</since>
    public static Guid CrossHatchBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcCrossHatchBumpTexture);
    /// <since>6.0</since>
    public static Guid LeatherBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcLeatherBumpTexture);
    /// <since>6.0</since>
    public static Guid WoodBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcWoodBumpTexture);
    /// <since>6.0</since>
    public static Guid SpeckleBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcSpeckleBumpTexture);
    /// <since>6.0</since>
    public static Guid GritBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcGritBumpTexture);
    /// <since>6.0</since>
    public static Guid DotBumpTexture => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcDotBumpTexture);
    /// <since>6.0</since>
    public static Guid BasicMaterialCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBasicMaterialCCI);
    /// <since>6.0</since>
    public static Guid BlendMaterialCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBlendMaterialCCI);
    /// <since>6.0</since>
    public static Guid CompositeMaterialCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcCompositeMaterialCCI);
    /// <since>6.0</since>
    public static Guid BasicEnvironmentCCI => UnsafeNativeMethods.RhRdkUuids_GetUuid(UnsafeNativeMethods.Rdk_UuidIds.RcBasicEnvironmentCCI);
  }

  /// <since>6.0</since>
  public enum DynamicIconUsage
  {
    /// <summary>
    /// Used in a tree control (e.g., TreeGridView).
    /// </summary>
    TreeControl,
    /// <summary>
    /// Used in a sub-node control (\see CRhRdkSubNodeCtrl)
    /// </summary>
    SubnodeControl,
    /// <summary>
    /// Used in a content control (\see CRhRdkContentCtrl)
    /// </summary>
    ContentControl,
    /// <summary>
    /// Used in a custom user interface. The background will be plain white.
    /// </summary>
    General,
  };

  /// <summary>
  /// Attributes for RenderContent
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class CustomRenderContentAttribute : Attribute
  {
    /// <since>6.0</since>
    public CustomRenderContentAttribute(
      string renderEngineGuid = "", bool imageBased = false, string category = "General",
      bool is_elevated = false, bool is_built_in = false, bool is_private = false
      ) : this(
            renderEngineGuid, imageBased, category,
            is_elevated, is_built_in, is_private,
            false, false, false)
    {
    }

    /// <since>6.16</since>
    public CustomRenderContentAttribute(
      string renderEngineGuid, bool imageBased, string category,
      bool is_elevated, bool is_built_in, bool is_private,
      bool is_linear, bool is_hdrcapable, bool is_normalmap
      )
    {
      try
      {
        if (!string.IsNullOrEmpty(renderEngineGuid))
        {
          RenderEngineId = new Guid(renderEngineGuid);
        }
      }
      catch (Exception)
      {
      }

      ImageBased = imageBased;
      IsLinear = is_linear;
      IsHdrCapable = is_hdrcapable;
      IsNormalMap = is_normalmap;
      Category = category;
      IsElevated = is_elevated;
      IsBuiltIn = is_built_in;
      IsPrivate = is_private;
    }

    /// <since>6.0</since>
    public Guid RenderEngineId
    {
      private set;
      get;
    } = Guid.Empty;

    /// <since>6.0</since>
    public bool ImageBased { get; set; } = false;
    /// <since>6.16</since>
    public bool IsLinear { get; set; } = false;
    /// <since>6.16</since>
    public bool IsHdrCapable { get; set; } = false;
    /// <since>6.16</since>
    public bool IsNormalMap { get; set; } = false;
    /// <since>6.0</since>
    public string Category { get; set; } = "General";
    /// <since>6.0</since>
    public bool IsElevated { get; set; } = false;
    /// <since>6.0</since>
    public bool IsBuiltIn { get; set; } = false;
    /// <since>6.0</since>
    public bool IsPrivate { get; set; } = false;
  }

  /// <summary>
  /// Defines constant values for all render content kinds, such as material,
  /// environment or texture.
  /// </summary>
  /// <since>5.1</since>
  [Flags]
  public enum RenderContentKind : int
  {
    /// <summary>
    /// Specifies no RenderContent kind.
    /// </summary>
    None = UnsafeNativeMethods.CRhRdkContentKindConst.None,
    /// <summary>
    /// Specifies a material RenderContent kind.
    /// </summary>
    Material = UnsafeNativeMethods.CRhRdkContentKindConst.Material,
    /// <summary>
    /// Specifies an environment RenderContent kind.
    /// </summary>
    Environment = UnsafeNativeMethods.CRhRdkContentKindConst.Environment,
    /// <summary>
    /// Specifies a texture RenderContent kind.
    /// </summary>
    Texture = UnsafeNativeMethods.CRhRdkContentKindConst.Texture,
  }

  /// <summary>
  /// Defines the collection type to iterate. 
  /// </summary>
  /// <since>6.0</since>
  public enum it_strategy
  {
    /// <summary>
    /// This type represents all the render contents in the database. 
    /// </summary>
    ContentDataBase,
    /// <summary>
    /// This type represents the selected render content collection.
    /// </summary>
    ContentSelection
  }

  /// <summary>
  /// Defines the proxy type of the render content 
  /// </summary>
  /// <since>6.0</since>
  public enum ProxyTypes
  {
    /// <summary>
    /// No proxy type specified.
    /// </summary>
    None, 
    /// <summary>
    /// A proxy that represents a single material in the CRhinoDoc::m_material_table
    /// </summary>
    Single, 
    /// <summary>
    /// A proxy that represents multiple materials - all similar.
    /// </summary>
    Multi, 
    /// <summary>
    /// A proxy that represents textures, either in the texture table, or attached to materials or environments.
    /// </summary>
    Texture
  };

  /// <summary>
  /// Models a collection of kinds.
  /// </summary>
  public sealed class RenderContentKindList : IDisposable
  {
    private IntPtr m_cpp;
    private readonly bool m_delete = false;

    /// <since>6.1</since>
    public IntPtr CppPointer { get { return m_cpp; } }

    /// <summary>
    /// Construct an empty kind list
    /// </summary>
    /// <since>6.7</since>
    public RenderContentKindList()
    {
      m_delete = true;
      m_cpp = UnsafeNativeMethods.CRhRdkContentKindList_New();
    }

    /// <summary>
    /// Construct a kind list from another.
    /// </summary>
    /// <param name="kind_list">Kind list to copy</param>
    /// <since>6.7</since>
    public RenderContentKindList(RenderContentKindList kind_list)
    {
      m_delete = true;
      m_cpp = UnsafeNativeMethods.CRhRdkContentKindList_CopyNew(kind_list.CppPointer);
    }

    /// <summary>
    /// Construct from native pointer - internal use only.
    /// </summary>
    /// <param name="pRdkRenderContentKindList">C++ pointer to a kind list.</param>
    /// <since>6.1</since>
    public RenderContentKindList(IntPtr pRdkRenderContentKindList)
    {
      m_cpp = pRdkRenderContentKindList;
    }

    /// <summary>
    /// Kind list finalizer
    /// </summary>
    ~RenderContentKindList ()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose a kind list.
    /// </summary>
    /// <since>6.1</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        if (m_delete)
          UnsafeNativeMethods.CRhRdkContentKindList_Delete(m_cpp);

        m_cpp = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Add a kind to a kind list.
    /// </summary>
    /// <param name="kind">The RenderContent kind to add.</param>
    /// <since>6.7</since>
    public void Add(RenderContentKind kind)
    {
      UnsafeNativeMethods.CRhRdkContentKindList_Add(m_cpp, (int)kind);
    }

    /// <summary>
    /// The number of kinds in the collection
    /// </summary>
    /// <returns>The number of kinds in the collection</returns>
    /// <since>6.1</since>
    public int Count()
    {
      return UnsafeNativeMethods.CRhRdkContentKindList_Count(m_cpp);
    }

    /// <summary>
    /// The single kind in the list. If the list does not contain exactly one kind, returns 'None'.
    /// </summary>
    /// <returns>A RenderContentKind</returns>
    /// <since>6.1</since>
    public RenderContentKind SingleKind()
    {
      switch (UnsafeNativeMethods.CRhRdkContentKindList_SingleKind(m_cpp))
      {
      case 1: return RenderContentKind.Material;
      case 2: return RenderContentKind.Environment;
      case 4: return RenderContentKind.Texture;
      }

      return RenderContentKind.None;
    }

    /// <summary>
    /// Query whether or not the collection contains a particular kind designation.
    /// </summary>
    /// <param name="kind"></param>
    /// <returns>true if the kind is in the collection.</returns>
    /// <since>6.3</since>
    public bool Contains(RenderContentKind kind)
    {
      return UnsafeNativeMethods.CRhRdkContentKindList_Contains(m_cpp, (uint)kind);
    }
  }

  /// <summary>
  /// Content collection filter value
  /// </summary>
  /// <since>6.9</since>
  public enum FilterContentByUsage
  {
    /// <summary>
    /// No filter in use.
    /// </summary>
    None,
    /// <summary>
    /// Display only used contents.
    /// </summary>
    Used,
    /// <summary>
    /// Display only unused contents.
    /// </summary>
    Unused,
    /// <summary>
    /// Display only contents used by selected objects.
    /// </summary>
    UsedSelected,
  }

  /// <summary>
  /// A collection of Render content
  /// </summary>
  public sealed class RenderContentCollection : IDisposable, IEnumerable
  {
    private IntPtr m_cpp;
    private readonly bool m_delete_cpp_pointer;

    /// <summary>
    /// Internal function to get native pointer
    /// </summary>
    /// <since>6.0</since>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Construct an empty collection of RenderContent objects
    /// </summary>
    /// <since>6.0</since>
    public RenderContentCollection()
    {
      m_cpp = UnsafeNativeMethods.CRhRdkContentArray_New();
      m_delete_cpp_pointer = true;
    }

    /// <summary>
    /// Internal function to create collection from native pointer
    /// </summary>
    /// <param name="nativePtr">Native pointer</param>
    /// <since>6.0</since>
    public RenderContentCollection(IntPtr nativePtr)
    {
      m_cpp = nativePtr;
      m_delete_cpp_pointer = false;
    }

    /// <summary>
    /// Finalizer for RenderContentCollection
    /// </summary>
    ~RenderContentCollection()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose function for RenderContentCollection
    /// </summary>
    /// <since>6.0</since>
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

    /// <summary>
    /// Gets usage filter type for collection
    /// </summary>
    /// <returns>Usage filter type for collection</returns>
    /// <since>6.9</since>
    public FilterContentByUsage GetFilterContentByUsage()
    {
      switch (UnsafeNativeMethods.IRhRdkContentCollection_GetFilterContentByUsage(m_cpp))
      {
      default:
      case 0: return FilterContentByUsage.None;
      case 1: return FilterContentByUsage.Used;
      case 2: return FilterContentByUsage.Unused;
      case 3: return FilterContentByUsage.UsedSelected;
      }
    }

    /// <summary>
    /// See SetForcedVaries
    /// </summary>
    /// <returns>Forced varies</returns>
    /// <since>6.9</since>
    public bool GetForcedVaries()
    {
      return UnsafeNativeMethods.IRhRdkContentCollection_GetForcedVaries(m_cpp);
    }

    /// <summary>
    /// Set the collection to 'varies'.
    /// Only valid if the collection is a 'selection' collection.
    /// Useful for clients that only support single content selections.
    /// </summary>
    /// <param name="b">Varies if true</param>
    /// <since>6.9</since>
    public void SetForcedVaries(bool b)
    {
      UnsafeNativeMethods.IRhRdkContentCollection_SetForcedVaries(m_cpp, b);
    }

    /// <summary>
    /// Sets a search pattern for filtering contents. This is not actually used by the iterator,
    /// but is stored for use by any UI that wants to filter contents based on a search string
    /// by using the function RhRdkCheckSearchPattern()
    /// </summary>
    /// <param name="pattern">The search pattern. See RhRdkCheckSearchPattern() for details</param>
    /// <since>6.0</since>
    public void SetSearchPattern(string pattern)
    {
      var sh = new StringWrapper(pattern);
      var p_string = sh.ConstPointer;

      UnsafeNativeMethods.IRhRdkContentCollection_SetSearchPattern(m_cpp, p_string);
    }

    /// <summary>
    /// See SetSearchPattern
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public string GetSearchPattern()
    {
      using (var sh = new StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.IRhRdkContentCollection_GetSearchPattern(m_cpp, p_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets the first tag
    /// </summary>
    /// <returns>The first tag</returns>
    /// <since>6.13</since>
    public string FirstTag()
    {
      using (var sh = new StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.IRhRdkContentCollection_FirstTag(m_cpp, p_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Call FirstTag first - this gets the next tag
    /// </summary>
    /// <returns>The next tag</returns>
    /// <since>6.13</since>
    public string NextTag()
    {
      using (var sh = new StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.IRhRdkContentCollection_NextTag(m_cpp, p_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// To be documented
    /// </summary>
    /// <param name="c"></param>
    /// <param name="includeChildren"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool ContentNeedsPreviewThumbnail(Rhino.Render.RenderContent c, bool includeChildren)
    {
      if (c != null)
        return UnsafeNativeMethods.IRhRdkContentCollection_ContentNeedsPreviewThumbnailWithChildren(m_cpp, c.NonConstPointer(), includeChildren);

      return false;
    }

    /// <summary>
    /// Remove an array of contents from the collection.
    /// </summary>
    /// <param name="collection">Collection of contents to remove</param>
    /// <since>6.0</since>
    public void Remove(Rhino.Render.RenderContentCollection collection)
    {
      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Remove(CppPointer, collection.CppPointer);
      }
    }

    /// <summary>
    /// Add an array of non-const contents to the collection.
    /// </summary>
    /// <param name="collection">The array of contents to add</param>
    /// <since>6.0</since>
    public void Add(Rhino.Render.RenderContentCollection collection)
    {
      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Add(CppPointer, collection.CppPointer);
      }
    }

    /// <summary>
    /// Set an array of const contents as the collection.
    /// </summary>
    /// <param name="collection">The array of contents to set.</param>
    /// <since>6.0</since>
    public void Set(Rhino.Render.RenderContentCollection collection)
    {
      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Set(CppPointer, collection.CppPointer);
      }
    }

    /// <summary>
    /// Clear the collection.
    /// </summary>
    /// <since>6.0</since>
    public void Clear()
    {
      if (CppPointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhRdkContentCollection_Clear(CppPointer);
      }
    }

    /// <summary>
    /// Append a RenderContent to the collection
    /// </summary>
    /// <param name="content">The array of contents to append.</param>
    /// <since>6.0</since>
    public void Append(Rhino.Render.RenderContent content)
    {
      if (content == null)
        return;

      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkContentArray_Add(m_cpp, content.CppPointer);
      }
    }

    /// <summary>
    /// The number of items in the collection.
    /// </summary>
    /// <returns>The number of items in the collection.</returns>
    /// <since>6.0</since>
    public int Count()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.CRhRdkContentArray_Count(m_cpp);
      }

      return -1;
    }

    /// <summary>
    /// Gets an iterator for the collection
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public ContentCollectionIterator Iterator()
    {
      ContentCollectionIterator value = null;

      if (CppPointer != IntPtr.Zero)
      {
        value = new ContentCollectionIterator(UnsafeNativeMethods.IRhRdkContentCollection_IIterator(CppPointer));
      }

      return value;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public RenderContent Find_Sel(Guid uuid)
    {
      RenderContent found = null;

      var iterator = Iterator();
      if (iterator != null)
      {
        var content = iterator.First();
        while (content != null)
        {
          if (content.Id.ToString().CompareTo(uuid.ToString()) == 0)
          {
            found = content;
            break;
          }

          content = iterator.Next();
        }

        iterator.DeleteThis();
      }

      return found;
    }

    /// <summary>
    /// Content at index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public RenderContent ContentAt(int index)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContent = UnsafeNativeMethods.CRhRdkContentArray_ContentAt(m_cpp, index);
        if (pContent != IntPtr.Zero)
        {
          return RenderContent.FromPointer(pContent, null);
        }
      }

      return null;
    }

    /// <since>6.0</since>
    public IEnumerator GetEnumerator()
    {
      var iterator = Iterator();
      if (iterator != null)
      {
        // 4th June 2024 John Croudy, https://mcneel.myjetbrains.com/youtrack/issue/RH-82358
        // Use try..finally to make sure the iterator gets deleted even if the caller breaks out of the loop.
        try
        {
          var content = iterator.First();
          while (content != null)
          {
            yield return content;
            content = iterator.Next();
          }
        }
        finally
        {
          iterator.DeleteThis();
        }
      }
    }
  }

  /// <summary>
  /// An iterator for the RenderContentCollection
  /// </summary>
  public sealed class ContentCollectionIterator : IDisposable
  {
    private IntPtr m_cpp;

    /// <since>6.0</since>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <since>6.0</since>
    public ContentCollectionIterator(IntPtr pCollection)
    {
      m_cpp = pCollection;
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~ContentCollectionIterator()
    {
      Dispose(false);
    }

    /// <since>6.0</since>
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

    /// <since>6.0</since>
    public void DeleteThis()
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.IIterator_DeleteThis(m_cpp);
      }
    }

    /// <since>6.0</since>
    public RenderContent First()
    {
      RenderContent content = null;

      if (m_cpp != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(UnsafeNativeMethods.IIterator_First(m_cpp), null);
      }

      return content;
    }

    /// <since>6.0</since>
    public RenderContent Next()
    {
      RenderContent content = null;

      if (m_cpp != IntPtr.Zero)
      {
        content = RenderContent.FromPointer(UnsafeNativeMethods.IIterator_Next(m_cpp), null);
      }

      return content;
    }
  }

  /// <summary>
  /// Base class for all RenderContent - RenderMaterial, RenderTexture and RenderEnvironment
  /// 
  /// Contents have a unique type id which is the same for all instances of the same class and an instance id
  /// which is unique for each instance.They know how to provide a shader for rendering, how to read and write
  /// their state as XML and how to create their own user interfaces.
  /// 
  /// There are two flavors of content in the RDK -- temporary and persistent.It is very important to understand
  /// the distinction between a temporary content instance and a persistent content instance, and the fact that a
  /// temporary instance (and all its children) can become persistent.Persistent content is registered with a
  /// document and is usually(but not always) owned by it.
  /// 
  /// Temporary contents get created and deleted very often during the normal operation of the RDK.In fact, just
  /// about anything the user clicks on might result in a temporary content being created and deleted again.They
  /// are created by the content browser, the thumbnail rendering, and so on.They are 'free floating' and are
  /// owned by whomever created them.They do not appear in the modeless UI, they do not get saved in the 3dm
  /// file, and they can freely be deleted again after use.
  /// 
  /// Contrast this with persistent contents which are attached to a document.They are always owned by RDK,
  /// appear in the modeless UI and get saved in the 3dm file. Pointers to persistent contents should never
  /// be stored by clients; you should only store their instance ids and look them up again using
  /// RenderContent.FromId. They can be deleted only after detaching them from the document.
  /// 
  /// RenderContent::Create is the highest-level function for creating a content.It creates it,
  /// initializes it, adds it to the document and sends many events.It even records undo.You cannot call this
  /// method from just anywhere. It must only be called by 'UI code'; scripts or buttons on a dialog.It results
  /// in a persistent (usually top-level) content being attached to the document and appearing in all the RDK UI
  /// elements that display contents, like the thumbnail and tree views.If you call this method and specify a
  /// parent and child-slot name, your new content will be attached to the document-resident parent as a child
  /// and the UI will be updated accordingly.
  /// 
  /// The important point is that everything is temporary while the content structure is being built. Only
  /// after the whole structure is complete will the top-level parent be attached to the document making the
  /// whole structure persistent.
  /// 
  /// </summary>
  public abstract class RenderContent : IDisposable
  {
    #region Kinds (internal)
    internal static string KindString(RenderContentKind kinds)
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
          sb.Append(';');
        }
        sb.Append("environment");
      }

      if ((kinds & RenderContentKind.Texture) == RenderContentKind.Texture)
      {
        if (sb.Length != 0)
        {
          sb.Append(';');
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
    static internal RenderContentKind ConvertContentKind(UnsafeNativeMethods.CRhRdkContentKindConst kind)
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

    /// <summary>
    /// If this is a material, checks if it's in use and the rhino object is selected.
    /// Otherwise returns false.
    /// </summary>
    /// <returns></returns>
    internal bool IsInUseBySelectedObject()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_IsInUseBySelectedObject(CppPointer);
    }

    /// <summary>
    /// Checks if this render content has the specified id. This is different to checking the id
    /// directly because it also checks the members of texture proxies.
    /// </summary>
    internal bool HasId(Guid id)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_HasId(ConstPointer(), id);
    }
    #endregion

    #region statics
    /// <since>5.1</since>
    /// <deprecated>7.9</deprecated>
    public enum ShowContentChooserFlags : int
    {
      /// <summary>
      /// Deprecated
      /// </summary>
      None              = 0x0000,
      /// <summary>
      /// Deprecated
      /// </summary>
      HideNewTab = 0x0001,
      /// <summary>
      /// Deprecated
      /// </summary>
      HideExistingTab = 0x0002,
      /// <summary>
      /// Deprecated
      /// </summary>
      MultipleSelection = 0x0004,
    };

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="type">The type of the content to create.</param>
    /// <param name="flags">Flags for future use (please always pass ShowContentChooserFlags::None).</param>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>5.1</since>
    /// <deprecated>7.9</deprecated>
    //[Obsolete("This method is deprecated")]
    public static RenderContent Create(Guid type, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      IntPtr ptr_content = UnsafeNativeMethods.Rdk_Globals_CreateContentByType(type, IntPtr.Zero, String.Empty, (int)flags, doc.RuntimeSerialNumber);
      return ptr_content == IntPtr.Zero ? null : FromPointer(ptr_content, null);
    }

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="type">The type of the content to create.</param>
    /// <param name="flags">Flags for future use (please always pass ShowContentChooserFlags::None).</param>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>5.1</since>
    /// <deprecated>7.9</deprecated>
    //[Obsolete("This method is deprecated")]
    public static RenderContent Create(Type type, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      return Create(type.GUID, flags, doc);
    }

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="type">The type of the content to create.</param>
    /// <param name="parent">Parent is the parent content. If not null, this must be an RDK-owned content that is
    /// attached to the document (either top-level or child). The new content then becomes its child.
    /// If null, the new content is added to the top-level document content list instead.</param>
    /// <param name="childSlotName">ChildSlotName is the unique child identifier to use for the new content when creating it as a child of parent (i.e., when parent is not null)</param>
    /// <param name="flags">Flags for future use (please always pass ShowContentChooserFlags::None).</param>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>5.1</since>
    /// <deprecated>7.9</deprecated>
    //[Obsolete("This method is deprecated")]
    public static RenderContent Create(Guid type, RenderContent parent, String childSlotName, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      IntPtr ptr_content = UnsafeNativeMethods.Rdk_Globals_CreateContentByType(type, parent.ConstPointer(), childSlotName, (int)flags, doc.RuntimeSerialNumber);
      return ptr_content == IntPtr.Zero ? null : FromPointer(ptr_content, parent);
    }

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="type">The type of the content to create.</param>
    /// <param name="parent">The parent content. If not null, this must be an RDK-owned content that is
    /// attached to the document (either top-level or child). The new content then becomes its child.
    /// If null, the new content is added to the top-level document content list instead.</param>
    /// <param name="childSlotName">ChildSlotName is the unique child identifier to use for the new content when creating it as a child of parent (i.e., when parent is not null)</param>
    /// <param name="flags">Flags for future use (please always pass ShowContentChooserFlags::None).</param>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>5.1</since>
    /// <deprecated>7.9</deprecated>
    //[Obsolete("This method is deprecated")]
    public static RenderContent Create(Type type, RenderContent parent, String childSlotName, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      return Create(type.GUID, parent, childSlotName, flags, doc);
    }

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <param name="type">The type of the content to create.</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>7.9</since>
    public static RenderContent Create(RhinoDoc doc, Guid type)
    {
      IntPtr ptr_content = UnsafeNativeMethods.Rdk_Globals_CreateContentByType(type, IntPtr.Zero, String.Empty, 0, doc.RuntimeSerialNumber);
      return ptr_content == IntPtr.Zero ? null : FromPointer(ptr_content, null);
    }

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <param name="type">The type of the content to create.</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>7.9</since>
    public static RenderContent Create(RhinoDoc doc, Type type)
    {
      return Create(doc, type.GUID);
    }

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <param name="type">The type of the content to create.</param>
    /// <param name="parent">Parent is the parent content. If not null, this must be an RDK-owned content that is
    /// attached to the document (either top-level or child). The new content then becomes its child.
    /// If null, the new content is added to the top-level document content list instead.</param>
    /// <param name="childSlotName">ChildSlotName is the unique child identifier to use for the new content when creating it as a child of parent (i.e., when parent is not null)</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>7.9</since>
    public static RenderContent Create(RhinoDoc doc, Guid type, RenderContent parent, string childSlotName)
    {
      IntPtr ptr_content = UnsafeNativeMethods.Rdk_Globals_CreateContentByType(type, parent.ConstPointer(), childSlotName, 0, doc.RuntimeSerialNumber);
      return ptr_content == IntPtr.Zero ? null : FromPointer(ptr_content, parent);
    }

    /// <summary>
    /// Constructs a new content of the specified type and attaches it to a document.
    /// This function cannot be used to create temporary content that you delete after use.
    /// Content created by this function is owned by RDK and appears in the content editor.
    /// To create a temporary content which is owned by you, call RenderContentType.NewContentFromTypeId().
    /// </summary>
    /// <param name="doc">The Rhino document to attach the new render content to.</param>
    /// <param name="type">The type of the content to create.</param>
    /// <param name="parent">The parent content. If not null, this must be an RDK-owned content that is
    /// attached to the document (either top-level or child). The new content then becomes its child.
    /// If null, the new content is added to the top-level document content list instead.</param>
    /// <param name="childSlotName">ChildSlotName is the unique child identifier to use for the new content when creating it as a child of parent (i.e., when parent is not null)</param>
    /// <returns>A new document-resident render content.</returns>
    /// <since>7.9</since>
    public static RenderContent Create(RhinoDoc doc, Type type, RenderContent parent, string childSlotName)
    {
      return Create(doc, type.GUID, parent, childSlotName);
    }

    /// <summary>
    /// Call RegisterContent in your plug-in's OnLoad function in order to register all of the
    /// custom RenderContent classes in your assembly.
    /// </summary>
    /// <param name="plugin"></param>
    /// <returns>array of render content types registered on success. null on error.</returns>
    /// <since>5.1</since>
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
    /// <since>5.1</since>
    public static Type[] RegisterContent(Assembly assembly, Guid pluginId)
    {
      // Check the Rhino plug-in for a RhinoPlugIn with the specified Id.
      var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);

      // RhinoPlugIn not found so bail, all content gets associated with a plug-in!
      if (plugin == null) return null;

      // Get a list of the publicly exported class types from the requested assembly.
      var exported_types = assembly.GetExportedTypes();

      // Scan the exported class types for RenderContent derived classes.
      var content_types = new List<Type>();
      var render_content_type = typeof(RenderContent);
      for (var i = 0; i < exported_types.Length; i++)
      {
        var exported_type = exported_types[i];

        // If abstract class or not derived from RenderContent or does not contain a public constructor then skip it.
        if (exported_type.IsAbstract || !exported_type.IsSubclassOf(render_content_type) || exported_type.GetConstructor(new Type[] { }) == null)
          continue;

        // Check the class type for a GUID custom attribute.
        var attr = exported_type.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);

        // If the class does not have a GUID custom attribute then throw an exception.
        if (attr.Length < 1)
          throw new InvalidDataException("Class \"" + exported_type + "\" must include a GUID attribute");

        // Add the class type to the content list.
        content_types.Add(exported_type);
      }

      // If this plug-in does not contain any valid RenderContent derived objects then bail.
      if (content_types.Count == 0) return null;

      // Make sure that content types have not already been registered.
      foreach (var content_type in content_types)
      {
        if (RdkPlugIn.RenderContentTypeIsRegistered(content_type))
          return null; // Bail out because this type was previously registered.
      }

      // Get the RdkPlugIn associated with this RhinoPlugIn, if it is not in
      // the RdkPlugIn dictionary it will get added if possible.
      var rdk_plug_in = RdkPlugIn.GetRdkPlugIn(plugin);

      // Plug-in not found or there was a problem adding it to the dictionary.
      if (rdk_plug_in == null) return null;

      // Append the RdkPlugIn registered content type list.
      rdk_plug_in.AddRegisteredContentTypes(content_types);

      // Process the valid class type list and register each class with the appropriate C++ RDK class factory.
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
    /// Loads content from a library file.  Does not add the content to the document.  Use RhinoDoc.RenderMaterials.Add or similar.
    /// </summary>
    /// <param name="filename">full path to the file to be loaded.</param>
    /// <returns>The loaded content or null if an error occurred.</returns>
    /// <since>5.1</since>
    public static RenderContent LoadFromFile(String filename)
    {
      var p_content = UnsafeNativeMethods.Rdk_RenderContent_LoadContentFromFile(filename);
      if (p_content == IntPtr.Zero)
        return null;

      var new_content = FromPointer(p_content, null);
      new_content.AutoDelete = true;
      return new_content;
    }

    /// <summary>
    /// Used by SaveToFile
    /// </summary>
    /// <since>8.6</since>
    public enum EmbedFilesChoice : int 
    { 
      /// <summary>
      /// Never embed support files in the content file
      /// </summary>
      NeverEmbed = 0,
      /// <summary>
      /// Always embed support files in the content file
      /// </summary>
      AlwaysEmbed = 1, 
      /// <summary>
      /// Show a UI
      /// </summary>
      AskUser = 2 
    }

    /// <summary>
    /// Saves content to a file - RMTL, RENV or RTEX.
    /// </summary>
    /// <param name="filename">Full path to the file to be saved.</param>
    /// <param name="embedFilesChoice"></param>
    /// <returns>The loaded content or null if an error occurred.</returns>
    /// <since>8.6</since>
    public bool SaveToFile(String filename, EmbedFilesChoice embedFilesChoice)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_SaveContentToFile(ConstPointer(), filename, (int)embedFilesChoice);
    }


    /// <summary>
    /// Add a material, environment or texture to the internal RDK document lists as
    /// top level content.  The content must have been returned from
    /// RenderContent::MakeCopy, NewContentFromType or a similar function that returns
    /// a non-document content.
    /// Obsolete - use RhinoDoc.RenderMaterials.Add or similar.
    /// </summary>
    /// <param name="renderContent">The render content.</param>
    /// <returns>true on success.</returns>
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Use RhinoDoc.RenderMaterials.Add")]
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
    /// Obsolete - use RhinoDoc.RenderMaterials.Add or similar.
    /// </summary>
    /// <param name="document">The document to attach the render content to.</param>
    /// <param name="renderContent">The render content.</param>
    /// <returns>true on success.</returns>
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Use RhinoDoc.RenderMaterials.Add")]
    public static bool AddPersistentRenderContent(RhinoDoc document, RenderContent renderContent)
    {
      // Should this be moved to the document?
      renderContent.AutoDelete = false;

      if (document == null)
        throw new ArgumentNullException(nameof(document));

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
    /// <since>5.7</since>
    public static RenderContent FromId(RhinoDoc document, Guid id)
    {
      var doc_sn = document != null ? document.RuntimeSerialNumber : 0u;
      var render_content = UnsafeNativeMethods.Rdk_FindContentInstance(doc_sn, id);
      return FromPointer(render_content, null);
    }

    /// <summary>
    /// Create a copy of the render content. All content is the same, except for the
    /// instance Id.
    /// </summary>
    /// <returns>The new RenderContent</returns>
    /// <since>6.0</since>
    public RenderContent MakeCopy()
    {
      var render_content = UnsafeNativeMethods.Rdk_RenderContent_MakeCopy(ConstPointer());

      var copy = FromPointer(render_content, this);

      // DHNL 2015.12.17 set AutoDelete so copy gets properly disposed of when temporary copy
      // goes out of scope and hasn't been added as persistent content (RH-32132).
      // Note that copy can be null.
      if (copy != null)
        copy.AutoDelete = true;

      return copy;
    }

    internal delegate void OnMakeCopyCallback(int serialNumber, IntPtr pNewContent);
    internal static OnMakeCopyCallback g_on_make_copy = OnMakeCopyImpl;
    [MonoPInvokeCallback(typeof(OnMakeCopyCallback))]
    private static void OnMakeCopyImpl(int serialNumber, IntPtr pNewContent)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null) 
        return;

      var newContent = RenderContent.FromPointer(pNewContent, null);

      content.OnMakeCopy(ref newContent);
    }

    /// <summary>
    /// Override this function to supplement the standard copying behavour for your RenderContent.
    /// </summary>
    /// <param name="newContent">Is the content that will be returned from MakeCopy.</param>
    protected virtual void OnMakeCopy(ref RenderContent newContent)
    {
    }

    /// <summary>
    /// Create a .NET object of the appropriate type and attach it to the
    /// requested C++ pointer
    /// </summary>
    /// <param name="renderContent"></param>
    /// <param name="relatedContent"></param>
    /// <returns></returns>
    internal static RenderContent FromPointer(IntPtr renderContent, RenderContent relatedContent)
    {
      // If null C++ pointer then bail.
      if (renderContent == IntPtr.Zero)
        return null;

      // Get the runtime memory serial number for the requested item.
      var serial_number = UnsafeNativeMethods.CRhCmnRenderContent_IsRhCmnDefined(renderContent);

      // If the object has been created and not disposed of then return it.
      if (serial_number > 0) return FromSerialNumber(serial_number);

      // Could not find the object in the runtime list so check to see if the C++ pointer is a CRhRdkTexture pointer.
      var p_texture = UnsafeNativeMethods.Rdk_RenderContent_DynamicCastToTexture(renderContent);
      if (p_texture != IntPtr.Zero)
      {
        return new NativeRenderTexture(p_texture, relatedContent);
      }

      // Check to see if the C++ pointer is a CRhRdkMaterial pointer.
      var p_material = UnsafeNativeMethods.Rdk_RenderContent_DynamicCastToMaterial(renderContent);
      if (p_material != IntPtr.Zero) 
      {
        return new NativeRenderMaterial(p_material, relatedContent);
      }

      // Check to see if the C++ pointer is a CRhRdkEnvironment pointer.
      var p_environment = UnsafeNativeMethods.Rdk_RenderContent_DynamicCastToEnvironment(renderContent);
      if (p_environment != IntPtr.Zero)
      {
        return new NativeRenderEnvironment(p_environment, relatedContent);
      }

      // This should never, ever, happen.
      throw new InvalidCastException("renderContent Pointer is not of a recognized type");
    }

    /// <since>6.0</since>
    /// <deprecated>6.4</deprecated>
    [Obsolete("Use other FromXml method")]
    public static RenderContent FromXml(String xml)
    {
      return FromXml(xml, null);
    }

    /// <summary>
    /// Creates a new content from the XML data.  The resulting content will not be attached to the document.
    /// </summary>
    /// <param name="xml">The input XML data.</param>
    /// <param name="doc">The document that the content will be associated with for units, linear workflow purposes.</param>
    /// <returns></returns>
    /// <since>6.4</since>
    public static RenderContent FromXml(String xml, RhinoDoc doc)
    {
      IntPtr pContent = UnsafeNativeMethods.Rdk_RenderContent_FromXml(xml, doc==null ? 0 : doc.RuntimeSerialNumber);
      if (pContent == IntPtr.Zero)
        return null;

      return RenderContent.FromPointer(pContent, null);
    }

    /// <summary>
    /// Generate a render content preview
    /// </summary>
    /// <param name="lwf">Linear Workflow</param>
    /// <param name="c">Render Content</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="bSuppressLocalMapping">Suppress Local Mapping</param>
    /// <param name="pjs">Preivew Job Signature</param>
    /// <param name="pa">Preivew Appearance</param>
    /// <param name="result">Reference to PreviewRenderResult value</param>
    /// <returns>The Bitmap of the render content preview</returns>
    public static System.Drawing.Bitmap GenerateRenderContentPreview(LinearWorkflow lwf, RenderContent c, int width, int height, bool bSuppressLocalMapping, PreviewJobSignature pjs, PreviewAppearance pa, ref Utilities.PreviewRenderResult result)
    {
      if (lwf == null || c == null || pjs == null || pa == null)
        return null;

      result = Utilities.PreviewRenderResult.Nothing;

      uint rValue = 0;

      IntPtr pDib = UnsafeNativeMethods.Rdk_Globals_GenerateRenderedContentPreview(lwf.CppPointer, c.CppPointer, width, height, bSuppressLocalMapping, pjs.CppPointer, pa.CppPointer, ref rValue);

      if(rValue == 0)
        result = Utilities.PreviewRenderResult.Rendering;
      else if(rValue == 1)
        result = Utilities.PreviewRenderResult.CacheOK;
      else if(rValue == 2)
        result = Utilities.PreviewRenderResult.CacheFail;
      else if(rValue == 3)
        result = Utilities.PreviewRenderResult.Nothing;

      if (pDib == IntPtr.Zero)
        return null;

      if (rValue == (uint)Utilities.PreviewRenderResult.Rendering || rValue == (uint)Utilities.PreviewRenderResult.CacheFail || rValue == (uint)Utilities.PreviewRenderResult.Nothing)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    /// <summary>
    /// Generate a quick render content preview
    /// </summary>
    /// <param name="c">Render Content</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="psc">PreviewSceneServer</param>
    /// <param name="bSuppressLocalMapping">SuppressLocalMapping</param>
    /// <param name="reason"> ContentChanged = 0, ViewChanged = 1, RefreshDisplay = 2, Other = 99</param>
    /// <param name="result">Rhino.Command.Result value for successfull quick image creation</param>
    /// <returns>The Bitmap of the quick render content preview</returns>
    public static System.Drawing.Bitmap GenerateQuickContentPreview(RenderContent c, int width, int height, PreviewSceneServer psc, bool bSuppressLocalMapping, int reason, ref Rhino.Commands.Result result)
    {
      uint rValue = 0;
      result = Rhino.Commands.Result.Nothing;

      if (c == null)
        return null;

      IntPtr pPreviewSceneServer = IntPtr.Zero;
      if (psc != null)
        pPreviewSceneServer = psc.CppPointer;

      IntPtr pDib = UnsafeNativeMethods.Rdk_Globals_GenerateQuickContentPreview(c.CppPointer, width, height, pPreviewSceneServer, bSuppressLocalMapping, reason, ref rValue);

      if(rValue == 0)
        result = Rhino.Commands.Result.Nothing;
      else if(rValue == 1)
        result = Rhino.Commands.Result.Success;

      if (pDib == IntPtr.Zero)
        return null;

      if (rValue == 0)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    /// <summary>Specifies optional buttons for ShowContentInstanceBrowser().</summary>
    public enum ContentInstanceBrowserButtons
    {
      /// <summary>No optional buttons.</summary>
      None = 0,
      /// <summary>Include New button.</summary>
      NewButton = 1,
      /// <summary>Include Edit button.</summary>
      EditButton = 2
    }

    /// <summary>
    /// Allows the user to choose a content by displaying the Content Instance Browser dialog.
    /// The dialog will have OK, Cancel and Help buttons, and optional New and Edit buttons.
    /// <param name="doc">Specifies the document to use.</param>
    /// <param name="instance_id">Sets the initially selected content and receives the instance id of the chosen content.</param>
    /// <param name="kinds">Specifies the kind(s) of content that should be displayed in the browser.</param>
    /// <param name="buttons">Specifies which optional buttons to display.</param>
    /// Returns true if the user chooses a content or false if the dialog is cancelled.
    /// </summary>
    public static bool ShowContentInstanceBrowser(RhinoDoc doc, ref Guid instance_id, RenderContentKind kinds, ContentInstanceBrowserButtons buttons)
    {
      return UnsafeNativeMethods.Rdk_Globals_ShowContentInstanceBrowser(doc.RuntimeSerialNumber, ref instance_id, (uint)kinds, (uint)buttons);
    }

    internal static ChangeContexts ChangeContextFromExtraRequirementsSetContext(ExtraRequirementsSetContexts sc)
    {
      switch (sc)
      {
      case ExtraRequirementsSetContexts.UI:   return ChangeContexts.UI;
      case ExtraRequirementsSetContexts.Drop: return ChangeContexts.Drop;
      default: break;
      }

      return ChangeContexts.Program;
    }

    internal static ExtraRequirementsSetContexts ExtraRequirementsSetContextFromChangeContext(ChangeContexts cc)
    {
      switch (cc)
      {
        case ChangeContexts.UI:   return ExtraRequirementsSetContexts.UI;
        case ChangeContexts.Drop: return ExtraRequirementsSetContexts.Drop;
        default: break;
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
        default: break;
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
        default: break;
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
    static object g_custom_content_dictionary_lock = new object();
    /// <summary>
    /// Rhino.Render.Fields FieldDictionary which provides access to setting
    /// and retrieving field values.
    /// </summary>
    private FieldDictionary m_field_dictionary;
    /// <summary>
    /// Rhino.Render.Fields FieldDictionary which provides access to setting
    /// and retrieving field values.
    /// </summary>
    /// <since>5.1</since>
    public FieldDictionary Fields
    {
      get { return m_field_dictionary ?? (m_field_dictionary = new FieldDictionary(this)); }
    }

    /// <since>6.0</since>
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
    /// <since>5.7</since>
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
    /// <since>5.7</since>
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
    /// They should always derive from the subclasses of this class.
    /// </summary>
    internal RenderContent()
    {
      // This constructor is being called because we have a custom .NET subclass
      if (IsCustomClassDefintion())
      {
        lock(g_custom_content_dictionary_lock)
        {
          RuntimeSerialNumber = System.Threading.Interlocked.Increment(ref g_current_serial_number);
          g_custom_content_dictionary.Add(RuntimeSerialNumber, this);
        }
      }
      // Find the plug-in that registered this class type
      var type = GetType();
      var type_id = type.GUID;
      RdkPlugIn.GetRenderContentType(type_id, out Guid plugin_id);
      if (Guid.Empty == plugin_id) return;

      // Get information from custom class attributes
      var render_engine = Guid.Empty;
      var image_based = false;
      var category = "";
      var is_elevated = false;
      var is_built_in = false;
      var is_private = false;
      var is_normalmap = false;
      var is_hdrcapable= false;
      var is_linear = false;

      var attr = type.GetCustomAttributes(typeof(CustomRenderContentAttribute), false);
      if (attr.Length > 0)
      {
        if (attr[0] is CustomRenderContentAttribute custom)
        {
          image_based = custom.ImageBased;
          render_engine = custom.RenderEngineId;
          category = custom.Category;
          is_elevated = custom.IsElevated;
          is_built_in = custom.IsBuiltIn;
          is_private = custom.IsPrivate;
          is_normalmap = custom.IsNormalMap;
          is_hdrcapable = custom.IsHdrCapable;
          is_linear = custom.IsLinear;
        }
      }

      // Create C++ pointer of the appropriate type.
      var returned_serial_number = -1;
      if (this is RenderTexture)
        returned_serial_number = UnsafeNativeMethods.CRhCmnTexture_New(RuntimeSerialNumber, image_based, render_engine, plugin_id, type_id, category, is_elevated, is_built_in, is_private, is_linear, is_hdrcapable, is_normalmap);
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
    /// Name for your content type. i.e., "My .net Texture"
    /// </summary>
    /// <since>5.1</since>
    public abstract String TypeName { get; }
    /// <summary>
    /// Description for your content type. i.e., "Procedural checker pattern"
    /// </summary>
    /// <since>5.1</since>
    public abstract String TypeDescription { get; }

    /// <summary>
    /// Display name for this content.
    /// </summary>
    /// <since>7.8</since>
    public String DisplayName
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.DisplayName); }
    }

    /// <summary>
    /// Instance 'raw' name for this content.
    /// </summary>
    /// <since>5.1</since>
    public String Name
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.RawName); }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetInstanceName(ConstPointer(), value);
      }
    }

    ///<summary>
    /// Set instance name for this content
    /// <param name="name"></param>
    /// <param name="renameEvents"></param>
    /// <param name="ensureNameUnique"></param>
    ///</summary>
    /// <since>7.0</since>
    public void SetName(string name, bool renameEvents, bool ensureNameUnique)
    {
      UnsafeNativeMethods.Rdk_RenderContent_SetInstanceNameEx(ConstPointer(), name, renameEvents, ensureNameUnique);
    }

    /// <summary>
    /// Notes for this content.
    /// </summary>
    /// <since>5.1</since>
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
    /// <since>6.4</since>
    public String Tags {
      get { return GetString (UnsafeNativeMethods.RenderContent_StringIds.Tags); }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetTags (NonConstPointer(), value);
      }
    }

    /// <summary>
    /// Returns the localized display name of the child slot name
    /// </summary>
    /// <since>7.0</since>
    public String ChildSlotDisplayName
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.ChildSlotDisplayName); }
    }

    /// <summary>
    /// Category for this content.
    /// </summary>
    /// <since>6.7</since>
    public String Category
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.Category); }
    }

    /// <summary>
    /// Instance identifier for this content.
    /// </summary>
    /// <since>5.1</since>
    public Guid Id
    {
      get
      {
        return UnsafeNativeMethods.Rdk_RenderContent_InstanceId(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetInstanceId(ConstPointer(), value);
        if (this is INativeContent native)
        {
          native.Id = value;
        }
      }
    }

    /// <summary>
    /// Type identifier for this content
    /// </summary>
    /// <since>6.0</since>
    public Guid TypeId => UnsafeNativeMethods.Rdk_RenderContent_TypeId(ConstPointer());

    /// <summary>
    /// **** This method is for proxies and will be marked obsolete in the future ****
    ///
    /// The only place a single proxy can be displayed is in the
    /// New Content Control main thumbnail. All other attempts to
    /// use a single proxy in a UI require it to be replaced with
    /// the corresponding multi proxy. Single proxies override this
    /// to find the corresponding multi proxy.
    /// </summary>
    /// <returns>The render content.</returns>
    /// <since>6.9</since>
    public RenderContent ForDisplay()
    {
      IntPtr pContent = UnsafeNativeMethods.Rdk_RenderContent_ForDisplay(ConstPointer());
      if (pContent != IntPtr.Zero)
        return RenderContent.FromPointer(pContent, this);
      
      return null;
    }

    /// <summary>
    ///  Query whether or not the content or any of its ancestors is a reference content.
    /// </summary>
    /// <returns>true if the content is a reference, else false</returns>
    /// <since>6.9</since>
    public bool IsReference()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_IsReference(ConstPointer());
    }

    /// <summary>
    /// Group ID of the content
    /// </summary>
    /// <since>6.26</since>
    public Guid GroupId
    {
      get
      {
        return UnsafeNativeMethods.Rdk_RenderContent_GroupId(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetGroupId(ConstPointer(), value);
      }
    }

    /// <summary>
    ///  Create an 'instance' of the content hierarchy and group the new hierarchy with this hierarchy.
    ///  If the instance is subsequently attached to the same document, the state of all members
    ///  of the group will be kept synchronized. With the exception of the instance ids, all
    ///  state is exactly preserved in the new instance hierarchy.
    ///  \note The grouping will have no effect until the new instance is attached to the same document.
    /// 
    /// </summary>
    /// <returns>A grouped instance of the content hierarchy.</returns>
    /// <since>6.26</since>
    public RenderContent MakeGroupInstance()
    {
      var render_content = UnsafeNativeMethods.Rdk_RenderContent_MakeGroupInstance(ConstPointer());

      var copy = FromPointer(render_content, this);
      if (copy != null)
      { 
        copy.AutoDelete = true;
      }

      return copy;
    }

    /// <summary>
    /// Remove this content from any instance group it may be a member of.
    /// Does not record undo but does send the OnContentGroupIdChanged event.
    /// </summary>
    /// <returns>true if content was ungrouped, \e false if it was not part of a group.</returns>
    /// <since>6.26</since>
    public bool Ungroup()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_Ungroup(ConstPointer());
    }

    /// <summary>
    /// Remove this content and all its children from any instance groups they may be a member of.
    /// Does not record undo but does send the OnContentGroupIdChanged event.
    /// </summary>
    /// <returns>true if a content was ungrouped, \e false if no content or child was part of a group.</returns>
    /// <since>6.26</since>
    public bool UngroupRecursive()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_UngroupRecursive(ConstPointer());
    }

    /// <summary>
    /// Remove this content and all its children from any instance groups they may be a member of.
    /// If any content in the same document is left alone in the group, that content is also ungrouped.
    /// Records undo and sends events OnContentChanged and OnContentGroupIdChanged.
    /// \note This method is designed to be called from a content UI and is intended for RDK internal
    /// use but may be used as an expert user override.
    /// </summary>
    /// <returns>true if a content was ungrouped, \e false if no content or child was part of a group.</returns>
    /// <since>6.26</since>
    public bool SmartUngroupRecursive()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_SmartUngroupRecursive(ConstPointer());
    }

    /// <summary>
    ///  UseCount returns how many times the content is used
    /// </summary>
    /// <since>6.9</since>
    public int UseCount()
    {
      return UnsafeNativeMethods.Rdk_RenderContent_UseCount(ConstPointer());
    }

    /// <summary>
    /// This method is deprecated and no longer called. For more information see <see cref="CalculateRenderHash"/>
    /// </summary>
    /// <param name="hash"></param>
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    [CLSCompliant(false)]
    [Obsolete("This method is deprecated and no longer called. For more information see CalculateRenderHash")]
    public void SetRenderHash(uint hash)
    {
    }

    /// <summary>
    /// This method is deprecated and no longer called. For more information see <see cref="CalculateRenderHash"/>
    /// </summary>
    /// <returns>bool</returns>
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
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
    [Obsolete("Use CalculateRenderHash2")]
    protected virtual uint CalculateRenderHash(ulong rcrcFlags)
    {
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.Rdk_RenderContent_CallCalculateRenderCRCBase(const_pointer, rcrcFlags, "");
    }

    /// <summary>
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="excludeParameterNames"></param>
    /// <returns></returns>
    [CLSCompliant(false)]
    protected virtual uint CalculateRenderHash2(CrcRenderHashFlags flags, string[] excludeParameterNames)
    {
      var const_pointer = ConstPointer();
      if (const_pointer == IntPtr.Zero)
        return 0;

      var names = string.Join(";", excludeParameterNames);

      return UnsafeNativeMethods.Rdk_RenderContent_CallCalculateRenderCRCBase(const_pointer, (ulong)flags, names);
    }

    /// <summary>
    /// Render hash for the content hierarchy. It iterates over children and includes a caching
    /// mechanism which means the hash value can be retrieved quickly if it hasn't changed.
    /// The cache is invalidated when Changed() is called.
    /// 
    /// You can override the <see cref="CalculateRenderHash"/> method to provide a custom hash value.
    /// </summary>
    /// <since>6.0</since>
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
    /// By default, RenderHash recurses into children when computing the render hash.
    /// However, some applications may require children to be excluded from the render hash calculation.
    /// Call this method to enable or disable recursing into children.
    /// see <see cref="RenderHash"/>
    /// </summary>
    /// <param name="recursive"></param>
    /// <since>7.0</since>
    public void SetIsRenderHashRecursive(bool recursive)
    {
      UnsafeNativeMethods.Rdk_RenderContent_SetIsRenderCRCRecursive(ConstPointer(), recursive);
    }

    /// <summary>
    /// This method is deprecated in favor of the one that takes CrcRenderHashFlags.
    /// </summary>
    /// <since>6.0</since>
    /// <deprecated>8.0</deprecated>
    [CLSCompliant(false)]
    public uint RenderHashExclude(TextureRenderHashFlags flags, string excludeParameterNames)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_RenderCRC_ExcludeParamNames(
             ConstPointer(), (ulong)flags, excludeParameterNames, IntPtr.Zero);
    }

    /// <summary>
    /// As RenderHash, but allows you to specify flags and exclude specific parameters.
    /// </summary>
    /// <param name="flags">Flags to finely control the render hash.</param>
    /// <param name="excludeParameterNames">Semicolon-delimited string of parameter names to exclude.</param>
    /// <returns>The render hash.</returns>
    /// <since>6.2</since>
    [CLSCompliant(false)]
    public uint RenderHashExclude(CrcRenderHashFlags flags, string excludeParameterNames)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_RenderCRC_ExcludeParamNames(
             ConstPointer(), (ulong)flags, excludeParameterNames, IntPtr.Zero);
    }

    /// <summary>
    /// As RenderHash, but allows you to specify flags and exclude specific parameters.
    /// Use this version of the function to calculate a render hash when you have the linear workflow
    /// information and you are not running on the main thread. Access to LinearWorkflow data requires
    /// document access. CrcRenderHashFlags.ExcludeLinearWorkflow must be specified.
    /// </summary>
    /// <param name="flags">Flags to finely control the render hash.</param>
    /// <param name="excludeParameterNames">Semicolon-delimited string of parameter names to exclude.</param>
    /// <param name="lw">Linear Workflow to use.</param>
    /// <returns>The render hash.</returns>
    /// <since>6.2</since>
    [CLSCompliant(false)]
    public uint RenderHashExclude(CrcRenderHashFlags flags, string excludeParameterNames, LinearWorkflow lw)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_RenderCRC_ExcludeParamNames(
             ConstPointer(), (ulong)flags, excludeParameterNames, (lw==null) ? IntPtr.Zero : lw.CppPointer);
    }

    internal delegate uint RenderCrcCallback(int serialNumber, ulong rcrcFlags, IntPtr pString_excludeParameterNames);
    internal static readonly RenderCrcCallback g_on_render_crc = OnRenderCrc;
    [MonoPInvokeCallback(typeof(RenderCrcCallback))]
    private static uint OnRenderCrc(int serialNumber, ulong rcrcFlags, IntPtr pString_excludeParameterNamese)
    {
      var render_content = FromSerialNumber(serialNumber);
      if (render_content == null)
        return 0;

      var method = render_content.GetType().GetMethod("CalculateRenderHash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      if (method.DeclaringType != typeof(RenderContent))
      {
        //There is a virtual override on this class for CalculateRenderHash, which means we have to call
        //it - otherwise we can use the newer CalculateRenderHash2.
#pragma warning disable 618
        return render_content.CalculateRenderHash(rcrcFlags);
#pragma warning restore 618
      }

      var excludeParameterNames = StringWrapper.GetStringFromPointer(pString_excludeParameterNamese);

      string[] names = Array.Empty<string>();
      if (excludeParameterNames.Length != 0)
      {
        names = excludeParameterNames.Split(';');
      }

      return render_content != null ? render_content.CalculateRenderHash2((CrcRenderHashFlags)rcrcFlags, names) : 0;
    }

    /// <summary>
    /// Returns true if this content has no parent, false if it is the child of another content.
    /// </summary>
    /// <since>5.1</since>
    public bool TopLevel
    {
      get { return UnsafeNativeMethods.Rdk_RenderContent_IsTopLevel(ConstPointer()); }
    }

    /// <summary>
    /// Returns true if this content is a resident of one of the persistent lists.
    /// </summary>
    /*public*/ // Hiding for the time being. It may be better to just have a Document property
    bool InDocument
    {
      get { return UnsafeNativeMethods.Rdk_RenderContent_IsInDocument(ConstPointer()); }
    }

    /// <summary>
    /// Gets or sets the render content's 'hidden' state. This feature only works for top-level render contents
    /// because it hides the entire hierarchy. It is normally used for 'implementation detail' render contents.
    /// For expert use only. Hidden render contents are never shown in the UI, with the exception of the Object
    /// (or Layer) Material Properties UI which always shows whatever is assigned to the object (or layer).
    /// In the Object (or Layer) Material Properties UI, if the user drops down the list, hidden render contents
    /// are not listed. Hidden render contents, being part of the document content list, will be listed by any
    /// scripts or other code that iterates over the document render content list. It is recommended that you set
    /// IsHidden once when you create your render content and leave it on to prevent flicker or slow performance.
    /// </summary>
    /// <since>5.1</since>
    public bool Hidden
    {
      get { return UnsafeNativeMethods.Rdk_RenderContent_IsHidden(ConstPointer()); }
      set { UnsafeNativeMethods.Rdk_RenderContent_SetIsHidden(NonConstPointer(), value); }
    }

    /// <summary>
    /// Contents can be created as 'auto-delete' by certain commands such as 'Picture'.
    /// These contents are automatically hidden from the user when the associated Rhino object
    /// is deleted. They are later deleted when the document is saved.
    /// </summary>
    /// <since>6.15</since>
    public bool IsHiddenByAutoDelete
    {
      get { return UnsafeNativeMethods.Rdk_RenderContent_IsHiddenByAutoDelete(ConstPointer()); }
    }

    /// <summary>
    /// Determines if the content can be edited.
    /// </summary>
    /// <since>6.0</since>
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
    /// <since>6.0</since>
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
    /// <since>6.0</since>
    public ProxyTypes ProxyType
    {
      get
      {
        switch (UnsafeNativeMethods.Rdk_RenderContent_ProxyType(ConstPointer()))
        {
        default:
        case 0: return ProxyTypes.None;
        case 1: return ProxyTypes.Single;
        case 2: return ProxyTypes.Multi;
        case 3: return ProxyTypes.Texture;
        }
      }
    }

    /// <summary>
    /// Returns the top content in this parent/child chain.
    /// </summary>
    /// <since>5.11</since>
    public RenderContent Parent
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_Parent(ConstPointer());
        return FromPointer(p_content, this);
      }
    }

    /// <summary>
    /// Return First child of this content or null if none.
    /// </summary>
    /// <since>6.0</since>
    public RenderContent FirstChild
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_FirstChild(ConstPointer());
        return FromPointer(p_content, this);
      }
    }

    /// <summary>
    /// Return First sibling of this content or null if none.
    /// </summary>
   /// <since>6.0</since>
   public RenderContent NextSibling
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_NextSibling(ConstPointer());
        return FromPointer(p_content, this);
      }
    }

    /// <summary>
    /// Returns the top content in this parent/child chain.
    /// </summary>
    /// <since>5.1</since>
    public RenderContent TopLevelParent
    {
      get
      {
        var p_content = UnsafeNativeMethods.Rdk_RenderContent_TopLevelParent(ConstPointer());
        return FromPointer(p_content, this);
      }
    }

    /// <summary>
    /// Obsolete. Do not use. You should use DocumentOwner instead.
    /// </summary>
    /// <since>5.10</since>
    [Obsolete("Use DocumentOwner")]
    public RhinoDoc Document
    {
      get
      {
        var doc_sn = UnsafeNativeMethods.Rdk_RenderContent_DocumentOwner(ConstPointer());
        return RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      }
    }

    /// <summary>
    /// If this render content is owned by a document, the document will be returned.
    /// This is the same as getting the document the render content is attached to.
    /// Otherwise returns null.
    /// </summary>
    /// <since>7.13</since>
    public RhinoDoc DocumentOwner
    {
      get
      {
        var doc_sn = UnsafeNativeMethods.Rdk_RenderContent_DocumentOwner(ConstPointer());
        return RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      }
    }

    /// <summary>
    /// Obsolete. Do not use. You should use DocumentOwner instead.
    /// </summary>
    /// <since>6.0</since>
    [Obsolete("Use DocumentOwner")]
    public RhinoDoc DocumentRegistered
    {
      get
      {
        var doc_sn = UnsafeNativeMethods.Rdk_RenderContent_DocumentOwner(ConstPointer());
        return RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      }
    }

    /// <summary>
    /// If this render content is associated with a document in any way, the document will be returned. This includes copies of
    /// render contents that were attached to a document when the copy was made. Otherwise returns null.
    /// </summary>
    /// <since>6.0</since>
    public RhinoDoc DocumentAssoc
    {
      get
      {
        var doc_sn = UnsafeNativeMethods.Rdk_RenderContent_DocumentAssoc(ConstPointer());
        return RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetDocumentAssoc(NonConstPointer(), (null != value) ? value.RuntimeSerialNumber : (uint)0);
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
    /// <since>5.7</since>
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
    /// <since>5.7</since>
    /// <deprecated>6.13</deprecated>
    [Obsolete("Obsolete, use Edit a version that returns a RenderContent", false)]
    public bool OpenInModalEditor()
    {
      if (!InDocument)
        return false;

      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.Rdk_RenderContent_OpenInMainEditor(const_pointer);
    }

    /// <summary>
    /// This method allows a render content hierarchy to be edited using a modal (AKA 'pop-up') editor.
    /// If the original render content is in a document, it will remain there, and the edited one will be
    /// 'free-floating'. Therefore it is the caller's responsibility to do any replacement in the document
    /// if required. The returned new content will be owned by the caller.
    /// </summary>
    /// <returns>
    /// Returns an edited version of the content hierarchy if successful, else null.
    /// The method always edits the entire hierarchy. It places a copy of the hierarchy in the editor
    /// and selects the copied item that corresponds to this one. Therefore, editing a child will return
    /// a top-level render content, not a child.
    /// </returns>
    /// <since>6.13</since>
    public RenderContent Edit()
    {
      var const_pointer = ConstPointer();
      var new_content_ptr = UnsafeNativeMethods.Rdk_RenderContent_Edit(const_pointer);
      var new_content = RenderContent.FromPointer(new_content_ptr, this);
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

    /// <since>6.0</since>
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
    /// Begins a change or batch of changes. It may also make a
    /// copy of the content state allowing <see cref="EndChange"/> to send an
    /// event with the old and new contents. Calls to this method are counted;
    /// you must call EndChange() once for every call to BeginChange().
    /// Note:
    ///   If Changed() was called between the calls to BeginChange() and
    ///   EndChange(), the last call to EndChange() may cause the ContentChanged
    ///   event to be sent.
    /// </summary>
    /// <param name="changeContext">
    /// the change context. If this is ChangeContexts.UI, ChangeContexts.Program,ChangeContexts.Drop or ChangeContexts.Tree,
    /// the content will be copied. EndChange() will then send the copy as 'old' in the OnContentChanged event.
    /// <see cref="EndChange"/> <see cref="ContentChanged"/>
    /// </param>
    /// <since>6.0</since>
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
    ///   EndChange(), the last call to EndChange() will raise the <see cref="ContentChanged"/> event.
    /// </summary>
    /// <since>6.0</since>
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
      const int idxEnvironment = 3;

      var type = idxInvalid;
      if (this is RenderMaterial)
        type = idxMaterial;
      else if (this is RenderTexture)
        type = idxTexture;
      else if (this is RenderEnvironment)
        type = idxEnvironment;

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
    /// <since>5.1</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete ("Use AddUserInterfaceSection(Rhino.UI.Controls.ICollapsibleSection) below instead.  This function will not work on the Mac and is not type-safe.")]
    public UI.UserInterfaceSection AddUserInterfaceSection(Type classType, string caption, bool createExpanded, bool createVisible)
    {
      if (OnAddUiSectionsUIId == Guid.Empty)
        throw new Exception("AddUserInterfaceSection can only be called from OnAddUserInterfaceSections");

      Type win32_interface = classType.GetInterface("IWin32Window");
      if (win32_interface == null)
        throw new ArgumentException("classType must implement IWin32Window interface", nameof(classType));

      ConstructorInfo constructor = classType.GetConstructor(Type.EmptyTypes);
      if (!classType.IsPublic || constructor == null)
        throw new ArgumentException("panelType must be a public class and have a parameterless constructor", nameof(classType));

      object[] attr = classType.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1)
        throw new ArgumentException("classType must have a GuidAttribute", nameof(classType));

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
      if (null != new_ui_section)
        return new_ui_section;

      if (control is IDisposable dis)
        dis.Dispose();

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
    /// <since>5.1</since>
    public bool AddAutomaticUserInterfaceSection(string caption, int id)
    {
      if (OnAddUiSectionsUIId == Guid.Empty)
      {
        throw new Exception("AddAutomaticUserInterfaceSection can only be called from OnAddUserInterfaceSections");
      }

      return UnsafeNativeMethods.Rdk_CoreContent_AddAutomaticUISection(NonConstPointer(), OnAddUiSectionsUIId, caption, id);
    }

    static readonly List<Rhino.UI.Controls.ICollapsibleSection> m_sections_to_keep_alive =
                new List<Rhino.UI.Controls.ICollapsibleSection>();

    /// <since>6.0</since>
    public bool AddUserInterfaceSection(Rhino.UI.Controls.ICollapsibleSection section)
    {
      if (OnAddUiSectionsUIId == Guid.Empty)
      {
        throw new Exception("AddUserInterfaceSection can only be called from OnAddUserInterfaceSections");
      }

      m_sections_to_keep_alive.Add(section);

      // Style the section according to rhino
      if (HostUtils.RunningOnWindows)
      {
        var service = HostUtils.GetPlatformService<Rhino.UI.IEtoStylePageService>();
        service?.StyleEtoControls(section);
      }

      return UnsafeNativeMethods.Rdk_CoreContent_AddUISection(NonConstPointer(), OnAddUiSectionsUIId, section.CppPointer);
    }

    /// <summary>
    /// This allows a content to have more than one UI for the same content type.
    /// </summary>
    /// <returns>
    /// Default is zero and is ignored.
    /// </returns>
    /// <since>7.1</since>
    [CLSCompliant(false)]
    public virtual ulong GetUiHash()
    {
      return 0;
    }

    /// <since>6.10</since>
    public DataSources.ContentFactory Factory()
    {
      IntPtr pFactory = UnsafeNativeMethods.Rdk_RenderContent_Factory(ConstPointer());

      if (pFactory != IntPtr.Zero)
        return new DataSources.ContentFactory(pFactory);

      return null;
    }

    /// <since>7.0</since>
    /// DO NOT CALL THIS FUNCTION IN NEW CODE. IT WILL BE DEPRECATED ASAP.
    public bool GetUnderlyingInstances(ref RenderContentCollection collection) // TODO: JOHNC GetUnderlyingInstances
    {
      return UnsafeNativeMethods.Rdk_RenderContent_GetUnderlyingInstances(ConstPointer(), collection.CppPointer);
    }

    /// <since>6.0</since>
    public virtual bool IsContentTypeAcceptableAsChild(Guid type, String childSlotName)
    {
      if (IsNativeWrapper())
        return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsContentTypeAcceptableAsChild(ConstPointer(), type, childSlotName);

      return 1 == UnsafeNativeMethods.Rdk_RenderContent_CallIsContentTypeAcceptableAsChildBase(ConstPointer(), type, childSlotName);
    }

    /// <since>6.1</since>
    public virtual bool IsFactoryProductAcceptableAsChild(DataSources.ContentFactory factory, String childSlotName)
    {
      return 1 == UnsafeNativeMethods.Rdk_RenderContent_IsFactoryProductAcceptableAsChild(ConstPointer(), factory.CppPointer, childSlotName);
    }

    /// <summary>
    /// Modify your content so that it is converted from meters into the units of the unit system.
    /// No need to call the base class when you override this, and no need to recurse into children.
    /// </summary>
    /// <since>7.0</since>
    public virtual void ConvertUnits(UnitSystem from, UnitSystem to)
    {
    }

    /// <summary>
    /// Query the content instance for the value of a given named parameter.
    /// If you do not support this parameter, call the base class.
    /// </summary>
    /// <param name="parameterName">Name of the parameter</param>
    /// <returns>IConvertible. Note that you can't directly cast from object, instead you have to use the Convert mechanism.</returns>
    /// <since>5.7</since>
    [CLSCompliant(false)]
    public virtual object GetParameter(String parameterName)
    {
      var value = new Variant();

      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderContent_GetVariantParameter(ConstPointer(),
                                              parameterName, value.NonConstPointer());
      }
      else
      {
        string key = BindingKey(parameterName, null);
        if (m_bound_parameters.TryGetValue(key, out BoundField bound_field))
        {
          value.SetValue(bound_field.Field.ValueAsObject());
        }
        else
        {
          UnsafeNativeMethods.Rdk_RenderContent_CallGetVariantParameterBase(ConstPointer(),
                                                parameterName, value.NonConstPointer());
        }
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
    /// <since>5.7</since>
    /// <deprecated>6.0</deprecated>
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
    /// <since>6.0</since>
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
        if (m_bound_parameters.TryGetValue(key, out BoundField bound_field))
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
    /// <since>5.12</since>
    public bool IsLocked
    {
      get
      {
        return UnsafeNativeMethods.Rdk_RenderContent_GetLocked(ConstPointer()) != 0;
      }
      set
      {
        if (InDocument)
          throw new Exception("IsLocked setter must be called prior to adding content to the document");

        if (value)
        {
          UnsafeNativeMethods.Rdk_RenderContent_SetLocked(NonConstPointer());
        }
      }
    }

    /// <summary>
    /// Extra requirements are a way of specifying extra functionality on parameters in the automatic UI.
    /// Override this function to specify additional functionality for automatic UI sections or the texture summary.
    /// See IAutoUIExtraRequirements.h in the C++ RDK SDK for string definitions for the parameter names.
    /// Call the base class from your override if you do not support the extra requirement parameter.
    /// Please do not call this function. It is only retained for backward compatibility. You should instead
    /// call GetExtraRequirementParameter().
    /// </summary>
    /// <param name="contentParameterName">The parameter or field internal name to which this query applies.</param>
    /// <param name="extraRequirementParameter">The extra requirement parameter, as listed in IAutoUIExtraRequirements.h in the C++ RDK SDK.</param>
    /// <returns>
    /// The value of the requested extra requirement parameter or null if the parameter does not exist.
    /// Current supported return values are (int, bool, float, double, string, Guid, Color, Vector3d, Point3d, DateTime).
    /// </returns>
    /// <since>5.7</since>
    public virtual object GetChildSlotParameter(String contentParameterName, String extraRequirementParameter)
    {
      var value = new Variant();

      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderContent_GetExtraRequirementParameter(ConstPointer(),
                            contentParameterName, extraRequirementParameter, value.NonConstPointer());
      }
      else
      {
        var key = BindingKey(contentParameterName, extraRequirementParameter);
        if (m_bound_parameters.TryGetValue(key, out BoundField bound_field))
        {
          value.SetValue(bound_field.Field.ValueAsObject());
        }
        else
        {
          UnsafeNativeMethods.Rdk_RenderContent_CallGetExtraRequirementParameterBase(ConstPointer(),
                              contentParameterName, extraRequirementParameter, value.NonConstPointer());
        }
      }
      return value;
    }

    /// <summary>
    /// Extra requirements are a way of specifying extra functionality on parameters in the automatic UI.
    /// See IAutoUIExtraRequirements.h in the C++ RDK SDK for string definitions for the parameter names.
    /// </summary>
    /// <param name="contentParameterName">The parameter or field internal name to which this query applies.</param>
    /// <param name="extraRequirementParameter">The extra requirement parameter, as listed in IAutoUIExtraRequirements.h in the C++ RDK SDK.</param>
    /// <returns>
    /// The value of the requested extra requirement parameter or null if the parameter does not exist.
    /// Current supported return values are (int, bool, float, double, string, Guid, Color, Vector3d, Point3d, DateTime).
    /// </returns>
    /// <since>7.9</since>
    public object GetExtraRequirementParameter(string contentParameterName, string extraRequirementParameter)
    {
      return GetChildSlotParameter(contentParameterName, extraRequirementParameter);
    }

    /// <summary>
    /// Extra requirements are a way of specifying extra functionality on parameters in the automatic UI.
    /// Override this function to support values being set from automatic UI sections or the texture summary.
    /// See IAutoUIExtraRequirements.h in the C++ RDK SDK for string definitions for the parameter names.
    /// Call the base class from your override if you do not support the extra requirement parameter.
    /// Please do not call this function. It is only retained for backward compatibility. You should instead
    /// call SetExtraRequirementParameter().
    /// </summary>
    /// <param name="contentParameterName">The parameter or field internal name to which this query applies.</param>
    /// <param name="extraRequirementParameter">The extra requirement parameter, as listed in IAutoUIExtraRequirements.h in the C++ RDK SDK.</param>
    /// <param name="value">The value to set this extra requirement parameter. You will typically use System.Convert to convert the value to the type you need.</param>
    /// <param name="sc">The context of this operation.</param>
    /// <returns>true if successful, else false.</returns>
    /// <since>5.7</since>
    public virtual bool SetChildSlotParameter(String contentParameterName, String extraRequirementParameter, object value, ExtraRequirementsSetContexts sc)
    {
      if (value is Variant v)
      {
        if (IsNativeWrapper())
        {
          return 1 == UnsafeNativeMethods.Rdk_RenderContent_SetExtraRequirementParameter(ConstPointer(), contentParameterName, extraRequirementParameter, v.ConstPointer(), (int)sc);
        }

        var key = BindingKey(contentParameterName, extraRequirementParameter);
        if (m_bound_parameters.TryGetValue(key, out BoundField bound_field))
        {
          bound_field.Field.Set(v);
          return true;
        }

        return 1 == UnsafeNativeMethods.Rdk_RenderContent_CallSetExtraRequirementParameterBase(ConstPointer(), contentParameterName, extraRequirementParameter, v.ConstPointer(), (int)sc);
      }

      return false;
    }

    /// <summary>
    /// Extra requirements are a way of specifying extra functionality on parameters in the automatic UI.
    /// See IAutoUIExtraRequirements.h in the C++ RDK SDK for string definitions for the parameter names.
    /// </summary>
    /// <param name="contentParameterName">The parameter or field internal name to which this query applies.</param>
    /// <param name="extraRequirementParameter">The extra requirement parameter, as listed in IAutoUIExtraRequirements.h in the C++ RDK SDK.</param>
    /// <param name="value">The value to set this extra requirement parameter. You will typically use System.Convert to convert the value to the type you need.</param>
    /// <param name="sc">The context of this operation.</param>
    /// <returns>true if successful, else false.</returns>
    /// <since>7.9</since>
    public bool SetExtraRequirementParameter(string contentParameterName, string extraRequirementParameter, object value, ExtraRequirementsSetContexts sc)
    {
      return SetChildSlotParameter(contentParameterName, extraRequirementParameter, value, sc);
    }

    /// <summary>
    /// Gets the on-ness property for the texture in the specified child slot.
    /// </summary>
    /// <param name="childSlotName">The child slot name of the texture.</param>
    /// <returns>true if successful, else false.</returns>
    /// <since>5.7</since>
    public bool ChildSlotOn(String childSlotName)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_GetChildSlotOn(ConstPointer(), childSlotName);
    }

    /// <summary>
    /// Sets the on-ness property for the texture in the specified child slot.
    /// </summary>
    /// <param name="childSlotName">The child slot name of the texture.</param>
    /// <param name="bOn">Value for the on-ness property.</param>
    /// <param name="cc">Context of the change</param>
    /// <since>5.7</since>
    public void SetChildSlotOn(String childSlotName, bool bOn, ChangeContexts cc)
    {
      SetExtraRequirementParameter(childSlotName, "texture-on", new Variant(bOn), ExtraRequirementsSetContextFromChangeContext(cc));
    }

    /// <summary>
    /// Gets the amount property for the texture in the specified child slot.
    /// </summary>
    /// <param name="childSlotName">The child slot name of the texture.</param>
    /// <returns>The requested amount value. Values are typically from 0.0 to 100.0</returns>
    /// <since>5.7</since>
    public double ChildSlotAmount(String childSlotName)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_GetChildSlotAmount(ConstPointer(), childSlotName);
    }

    /// <summary>
    /// Sets the amount property for the texture in the specified child slot.
    /// </summary>
    /// <param name="childSlotName">The child slot name of the texture.</param>
    /// <param name="amount">The texture amount. Values are typically from 0.0 to 100.0</param>
    /// <param name="cc">The context of the change.</param>
    /// <since>5.7</since>
    public void SetChildSlotAmount(String childSlotName, double amount, ChangeContexts cc)
    {
      SetExtraRequirementParameter(childSlotName, "texture-amount", new Variant(amount), ExtraRequirementsSetContextFromChangeContext(cc));
    }

    /// <summary>
    /// Gets the PreviewSceneServer of the content
    /// </summary>
    /// <param name="ssd">SceneServerData</param>
    /// <return>Returns the contents PreviewSceneServer</return>
    /// <since>6.0</since>
    public PreviewSceneServer NewPreviewSceneServer(SceneServerData ssd)
    {
      IntPtr pPreviewSceneServer = UnsafeNativeMethods.Rdk_RenderContent_NewPreviewSceneServer(ConstPointer(), ssd.CppPointer);
      if (pPreviewSceneServer != IntPtr.Zero)
        return new PreviewSceneServer(ssd, pPreviewSceneServer);

      return null;
    }

    /// <summary>
    /// Return values for MatchData function
    /// </summary>
    /// <since>5.7</since>
    public enum MatchDataResult : int
    {
      /// <summary>
      /// None
      /// </summary>
      None = 0,
      /// <summary>
      /// Some
      /// </summary>
      Some = 1,
      /// <summary>
      /// All
      /// </summary>
      All = 2,
    };

    /// <summary>
    /// Implement to transfer data from another content to this content during creation.
    /// </summary>
    /// <param name="oldContent">An old content object from which the implementation may harvest data.</param>
    /// <returns>Information about how much data was matched.</returns>
    /// <since>6.0</since>
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

    /// <since>5.7</since>
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
    /// <since>5.1</since>
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
      /// OBSOLETE.
      /// </summary>
      FieldInit = 6,
      /// <summary>
      /// Change occurred during serialization (loading).
      /// </summary>
      Serialize = 7,
      /// <summary>
      /// Change occurred as a result of 'real-time' user activity in the (content) UI.
      /// The content's preview, UI, group members and registerable properties are not updated.
      /// </summary>
      RealTimeUI = 8,
      /// <summary>
      /// Change occurred as a result of executing a script.
      /// </summary>
      Script = 9,
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

    /// <since>6.0</since>
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
    /// <since>5.1</since>
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
    /// <since>5.1</since>
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

    /// <summary>
    /// Automatic Dynamic Field support.
    /// Dynamic fields are typically created in the constructor of RenderContent and they are therefore created
    /// automatically whenever the content is created. However, some advanced users require the fields to be created in response
    /// to some user action which occurs much later. This creates the problem that the fields do not exist
    /// by default and therefore cannot be loaded when the document is loaded. These methods are provided to solve that
    /// problem by making it possible to automatically create the dynamic fields on loading if they don't already exist.
    /// Dynamic fields that have this auto-create-on-load behavior are referred to as automatic dynamic fields.
    /// Dynamic fields that do not require the advanced automatic feature can still be created by using
    /// these methods (recommended), or they can be created manually (legacy usage).
    /// You must call this before creating any dynamic fields. Calls to this method are counted; you must call
    /// EndCreateDynamicFields() once for every call to BeginCreateDynamicFields().
    /// </summary>
    /// <param name="automatic"> automatic specifies if the dynamic fields are automatic. If so, they will be created automatically
    /// during loading of the document.</param>
    /// <since>7.1</since>
    public void BeginCreateDynamicFields(bool automatic)
    {
      UnsafeNativeMethods.Rdk_RenderContent_BeginCreateDynamicFields(ConstPointer(), automatic);
    }

    /// <summary>
    /// Create a dynamic field with an initial value and min and max limits.
    /// </summary>
    /// <param name="internalName"> is the internal name of the field. Not localized.</param>
    /// <param name="localName"> is the localized user-friendly name of the field.</param>
    /// <param name="englishName"> is the English user-friendly name of the field.</param>
    /// <param name="value"> is the initial value of the field.</param>
    /// <param name="minValue"> is the minimum value of the field. Must be the same type as vValue.</param>
    /// <param name="maxValue"> is the maximum value of the field. Must be the same type as vValue.</param>
    /// <param name="sectionId"> is used for filtering fields between sections. Zero if not needed.</param>
    /// <since>7.1</since>
    public bool CreateDynamicField(string internalName, string localName, string englishName, object value, object minValue, object maxValue, int sectionId)
    {
      var varValue = new Variant(value);
      var varMin   = new Variant(minValue);
      var varMax   = new Variant(maxValue);

      return UnsafeNativeMethods.Rdk_RenderContent_CreateDynamicField(ConstPointer(),
             internalName, localName, englishName,
             varValue.ConstPointer(), varMin.ConstPointer(), varMax.ConstPointer(), sectionId);
    }

    /// <summary>
    /// You must call this after creating dynamic fields. Calls to this method are counted; you must call
    /// BeginCreateDynamicFields() once for every call to EndCreateDynamicFields().
    /// </summary>
    /// <since>7.1</since>
    public void EndCreateDynamicFields()
    {
      UnsafeNativeMethods.Rdk_RenderContent_EndCreateDynamicFields(ConstPointer());
    }

    /// <since>5.1</since>
    public RenderContent FindChild(String childSlotName)
    {
      var p_const_this = ConstPointer();
      var p_child = UnsafeNativeMethods.Rdk_RenderContent_FindChild(p_const_this, childSlotName);
      return FromPointer(p_child, this);
    }

    /// <summary>
    /// Set another content as a child of this content. This content may or may
    /// not be attached to a document.  If this content already has a child
    /// with the specified child slot name, that child will be deleted.  If
    /// this content is not attached to a document, the child will be added
    /// without sending any events.  If this content is attached to a document,
    /// the necessary events will be sent to update the UI.
    /// Note:
    ///   Do not call this method to add children in your constructor. If you want to
    ///   add default children, you should override Initialize() and add them there.
    /// </summary>
    /// <param name="renderContent">
    /// Child content to add to this content.
    /// If renderContent is null, the function will fail.
    /// If renderContent is already attached to a document, the function will fail.
    /// If renderContent is already a child of this or another content, the function will fail.
    /// </param>
    /// <param name="childSlotName">
    /// The name that will be assigned to this child slot. The child slot name
    /// cannot be an empty string. If it is, the function will fail.
    /// </param>
    /// <returns>
    /// Returns true if the content was added or the child slot with this name was modified,
    /// otherwise returns false.
    /// </returns>
    /// <since>6.0</since>
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
    /// <since>5.10</since>
    /// <deprecated>6.0</deprecated>
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

    /// <since>5.6</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("This method is obsolete and simply calls SetChild")]
    public bool AddChild(RenderContent renderContent)
    {
      if (renderContent == null)
        throw new ArgumentNullException(nameof(renderContent));

      var slot_name = renderContent.ChildSlotName;
      return SetChild(renderContent, slot_name, ChangeContexts.Program);
    }

    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("This method is obsolete and simply calls SetChild")]
    public bool AddChild(RenderContent renderContent, String childSlotName)
    {
      return SetChild(renderContent, childSlotName,  ChangeContexts.Program);
    }

    /// <since>5.10</since>
    public bool DeleteChild(string childSlotName, ChangeContexts changeContexts)
    {
      // Changing a child slot needs to be wrapped by a BeginChange and EndChange call.
      BeginChange(changeContexts);
      var pointer = NonConstPointer();
      var success = UnsafeNativeMethods.Rdk_RenderContent_DeleteChild(pointer, childSlotName);
      EndChange();
      return success;
    }

    /// <since>5.10</since>
    public void DeleteAllChildren(ChangeContexts changeContexts)
    {
      // Changing a child slot needs to be wrapped by a BeginChange and EndChange call.
      BeginChange(changeContexts);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderContent_DeleteAllChildren(pointer);
      EndChange();
    }

    /// <since>5.6</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("This method is obsolete and simply calls SetChild")]
    public bool ChangeChild(RenderContent oldContent, RenderContent newContent)
    {
      if (oldContent == null)
        return false;

      return SetChild(newContent, oldContent.ChildSlotName, ChangeContexts.Program);
    }

    /// <since>5.1</since>
    public String ChildSlotName
    {
      get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.ChildSlotName); }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetChildSlotName(ConstPointer(), value);
      }
    }

    /// <since>6.0</since>
    public string[] GetEmbeddedFilesList()
    {
      using (var list = new ClassArrayString())
      {
        var p_non_const_list = list.NonConstPointer();
        UnsafeNativeMethods.Rdk_RenderContent_GetEmbeddedFiles(ConstPointer(), p_non_const_list);
        return list.ToArray();
      }
    }

    /// <since>6.1</since>
    /// <deprecated>7.2</deprecated>
    [Obsolete("This method should not be called.")]
    public bool Initialize()
    {
       return UnsafeNativeMethods.Rdk_RenderContent_Initialize(NonConstPointer());
    }

    /// <since>6.1</since>
    /// <deprecated>7.2</deprecated>
    [Obsolete("This method should not be called.")]
    public void Uninitialize()
    {
      UnsafeNativeMethods.Rdk_RenderContent_Uninitialize(NonConstPointer());
    }
    /// <since>6.13</since>
    public bool Replace(RenderContent newcontent)
    {
      return UnsafeNativeMethods.Rdk_RenderContent_ReplaceContentInDocument(DocumentOwner.RuntimeSerialNumber, CppPointer, newcontent.CppPointer);
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
        var type = RdkPlugIn.GetRenderContentType(typeId, out Guid plugin_id);

        // If the requested typeId was found and is derived from the requested
        // type then create an instance of the class.
        if (null != type && !type.IsAbstract && type.IsSubclassOf(isSubclassOf))
          return Activator.CreateInstance(type) as RenderContent;
      }
      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
      }
      return null;
    }

    ///// <summary>
    ///// Called after content has been allocated and before it has been added
    ///// to the document. Override this method when you need to call virtual
    ///// class methods that should not be called from the constructor.
    ///// </summary>
    ///// <returns>
    ///// Return true to allow the content to be added to the document or false
    ///// if it should be destroyed.
    ///// </returns>
    //public virtual bool Initialize()
    //{
    //  return true;
    //}

    internal delegate int IsFactoryProductAcceptableAsChildCallback(int serialNumber, IntPtr contentFactoryPointer, IntPtr pString_childSlotName);
    internal static IsFactoryProductAcceptableAsChildCallback IsFactoryProductAcceptableAsChildHook = OnIsFactoryProductAcceptableAsChild;
    [MonoPInvokeCallback(typeof(IsFactoryProductAcceptableAsChildCallback))]
    static int OnIsFactoryProductAcceptableAsChild(int serialNumber, IntPtr contentFactoryPointer, IntPtr pString_childSlotName)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null)
        return 1;

      var child_slot_name = StringWrapper.GetStringFromPointer(pString_childSlotName);
      using (var string_holder = new StringHolder())
      {
        var pointer = string_holder.NonConstPointer();
        UnsafeNativeMethods.Rdk_Factory_Kind(contentFactoryPointer, pointer);
        var factory_kind = string_holder.ToString();
        var id = UnsafeNativeMethods.Rdk_Factory_TypeId(contentFactoryPointer);
        var result = content.IsFactoryProductAcceptableAsChild(id, factory_kind, child_slot_name);

        return result ? 1 : 0;
      }
    }

    /// <summary>
    /// Override this method to restrict the type of acceptable child content.
    /// The default implementation of this method returns true if the factory kind is 'texture'.
    /// </summary>
    /// <param name="kindId"></param>
    /// <param name="factoryKind"></param>
    /// <param name="childSlotName">
    /// </param>
    /// <returns>
    /// Return true only if content with the specified kindId can be accepted as a child in the specified child slot.
    /// </returns>
    /// <since>5.11</since>
    virtual public bool IsFactoryProductAcceptableAsChild(Guid kindId, string factoryKind, string childSlotName)
    {
      return factoryKind == "texture";
    }

    internal delegate int IsContentTypeAcceptableAsChildCallback(int serialNumber, Guid type, IntPtr pString_childSlotName);
    internal static IsContentTypeAcceptableAsChildCallback m_IsContentTypeAcceptableAsChild = OnIsContentTypeAcceptableAsChild;
    [MonoPInvokeCallback(typeof(IsContentTypeAcceptableAsChildCallback))]
    static int OnIsContentTypeAcceptableAsChild(int serialNumber, Guid type, IntPtr pString_childSlotName)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && pString_childSlotName != IntPtr.Zero)
          return content.IsContentTypeAcceptableAsChild(type, StringWrapper.GetStringFromPointer(pString_childSlotName)) ? 1 : 0;
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate void GetParameterCallback(int serialNumber, IntPtr pStringName, IntPtr pVariant);
    internal static GetParameterCallback m_GetParameter = OnGetParameter;
    [MonoPInvokeCallback(typeof(GetParameterCallback))]
    static void OnGetParameter(int serialNumber, IntPtr pStringName, IntPtr pVariant)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);

        if (content != null && pStringName != IntPtr.Zero && pVariant != IntPtr.Zero)
        {
          var parameter_name = StringWrapper.GetStringFromPointer(pStringName);
          var rc = content.GetParameter(parameter_name);
          if (!(rc is Variant v))
          {
            v = new Variant(rc);
          }
          v.CopyToPointer(pVariant);
          return;
        }
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      if (pVariant != IntPtr.Zero)
      {
        var v = new Variant();
        v.CopyToPointer(pVariant);
      }
    }

    static private void InternalEmbeddedFilesCallback(int serialNumber, IntPtr result)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null)
        return;

      var list = content.FilesToEmbed;
      if (list == null)
        return;

      string delimited_string = "";
      foreach (var s in list)
      {
        if (!string.IsNullOrEmpty(s))
        {
          if (delimited_string == "")
          {
            delimited_string = s;
          }
          else
          {
            delimited_string = delimited_string + ";" + s;
          }
        }
      }

      UnsafeNativeMethods.ON_wString_Set(result, delimited_string);
    }

    internal delegate void SetEmbeddedFilesCallback(int serialNumber, IntPtr result);
    internal static SetEmbeddedFilesCallback g_embedded_files_callback = OnEmbeddedFilesCallback;
    [MonoPInvokeCallback(typeof(SetEmbeddedFilesCallback))]
    static void OnEmbeddedFilesCallback(int serialNumber, IntPtr result)
    {
      // 22nd November 2022 John Croudy, https://mcneel.myjetbrains.com/youtrack/issue/RH-71480
      // Some plug-ins might throw an exception from RenderContent.FilesToEmbed; handle it.
      try
      {
        InternalEmbeddedFilesCallback(serialNumber, result);
      }
      catch (Exception e)
      {
        UnsafeNativeMethods.ON_wString_Set(result, "");
        HostUtils.ExceptionReport(e);
      }
    }

    internal delegate void SetGetFilenameCallback(int serialNumber, IntPtr result);
    internal static SetGetFilenameCallback g_get_filename_callback = OnGetFilenameCallback;
    [MonoPInvokeCallback(typeof(SetGetFilenameCallback))]
    static void OnGetFilenameCallback(int serialNumber, IntPtr result)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null)
        return;

      var filename = content.Filename;
      if (filename == null)
        return;
      
      UnsafeNativeMethods.ON_wString_Set(result, filename);
    }

    internal delegate bool SetSetFilenameCallback(int serialNumber, IntPtr pFilename);
    internal static SetSetFilenameCallback g_set_filename_callback = OnSetFilenameCallback;
    [MonoPInvokeCallback(typeof(SetSetFilenameCallback))]
    static bool OnSetFilenameCallback(int serialNumber, IntPtr pFilename)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null)
        return false;

      var filename = StringWrapper.GetStringFromPointer(pFilename);

      content.Filename = filename;

      return true;
    }

    internal delegate void SetMetersToUnitsCallback(int serialNumber);
    internal static SetMetersToUnitsCallback g_meters_to_units_callback = OnMetersToUnitsCallback;
    [MonoPInvokeCallback(typeof(SetMetersToUnitsCallback))]
    static void OnMetersToUnitsCallback(int serialNumber)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null)
        return;

      if (null != content.DocumentAssoc)
      {
        content.ConvertUnits(UnitSystem.Meters, content.DocumentAssoc.ModelUnitSystem);
      }
    }

    internal delegate void SetUnitsToMetersCallback(int serialNumber);
    internal static SetUnitsToMetersCallback g_units_to_meters_callback = OnUnitsToMetersCallback;
    [MonoPInvokeCallback(typeof(SetUnitsToMetersCallback))]
    static void OnUnitsToMetersCallback(int serialNumber)
    {
      var content = FromSerialNumber(serialNumber);
      if (content == null)
        return;

      if (null != content.DocumentAssoc)
      {
        content.ConvertUnits(content.DocumentAssoc.ModelUnitSystem, UnitSystem.Meters);
      }
    }

    /// <summary>
    /// A string array of full paths to files used by the content that may be
    /// embedded in .3dm files and library files (.rmtl, .renv, .rtex). The
    /// default implementation returns an empty string list. Override this to
    /// return the file name or file names used by your content. This is
    /// typically used by textures that reference files containing the texture
    /// imagery. 
    /// </summary>
    /// <since>5.12</since>
    public virtual IEnumerable<string> FilesToEmbed { get { return new String[0]; } }
    
    /// <summary>
    /// If the content is file based, this function can be overridden to deal with setting/getting the 
    /// filename.  Corresponds to IRhRdkFileBasedContent in the C++ SDK
    /// </summary>
    /// <since>7.4</since>
    public virtual string Filename
    {
      get
      {
        if (IsNativeWrapper())
        {
          using (var sw = new StringWrapper())
          {
            var p_string = sw.NonConstPointer;
            bool ret = UnsafeNativeMethods.Rdk_RenderContent_Filename(ConstPointer(), p_string);
            if (ret)
            {
              return sw.ToString();
            }
          }
        }

        return "";
      }
      set
      {
        if (IsNativeWrapper())
        {
          UnsafeNativeMethods.Rdk_RenderContent_SetFilename(NonConstPointer(), value);
        }
      }
    }

    internal delegate int SetParameterCallback(int serialNumber, IntPtr name, IntPtr value);
    internal static SetParameterCallback m_SetParameter = OnSetParameter;
    [MonoPInvokeCallback(typeof(SetParameterCallback))]
    static int OnSetParameter(int serialNumber, IntPtr pString_name, IntPtr value)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && pString_name != IntPtr.Zero && value != IntPtr.Zero)
        {
          var v = Variant.CopyFromPointer(value);
          return (content.SetParameter(StringWrapper.GetStringFromPointer(pString_name), v) ? 1 : 0);
        }
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
      return 0;
    }


    internal delegate int GetExtraRequirementParameterCallback(int serialNumber, IntPtr pString_paramName, IntPtr pString_extraRequirementName, IntPtr value);
    internal static GetExtraRequirementParameterCallback m_GetExtraRequirementParameter = OnGetExtraRequirementParameter;
    [MonoPInvokeCallback(typeof(GetExtraRequirementParameterCallback))]
    static int OnGetExtraRequirementParameter(int serialNumber, IntPtr pString_paramName, IntPtr pString_extraRequirementName, IntPtr value)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && pString_paramName != IntPtr.Zero && pString_extraRequirementName != IntPtr.Zero && value != IntPtr.Zero)
        {
          var parameter_name = StringWrapper.GetStringFromPointer(pString_paramName);
          var extra_requirement = StringWrapper.GetStringFromPointer(pString_extraRequirementName);
          var rc = content.GetExtraRequirementParameter(parameter_name, extra_requirement);

          if (!(rc is Variant v))
            v = new Variant(rc);
          v.CopyToPointer(value);
          return !v.IsNull ? 1 : 0;
        }
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate int SetContentIconCallback(int serialNumber, int width, int height, IntPtr dibOut, int fromBaseClass);
    internal static SetContentIconCallback SetContentIcon = OnSetContentIcon;
    [MonoPInvokeCallback(typeof(SetContentIconCallback))]
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
        content.VirtualIcon(new Size(width, height), out Bitmap bitmap);
        if (null == bitmap)
          return 0;

        if (!content.m_bitmap_to_icon_dictionary.TryGetValue(bitmap, out IntPtr handle))
        {
          handle = bitmap.GetHbitmap();
          content.m_bitmap_to_icon_dictionary.Add(bitmap, handle);
        }
        UnsafeNativeMethods.CRhinoDib_SetFromHBitmap(dibOut, handle, true);

        return 1;
      }

      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
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
    /// <since>6.0</since>
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

    /// <since>6.0</since>
    virtual public bool Icon(Size size, out Bitmap bitmap)
    {
      bitmap = null;

      using (var rhinodib = new RhinoDib())
      {
        if (0 == UnsafeNativeMethods.Rdk_RenderContent_GetIcon(ConstPointer(), size.Width, size.Height, rhinodib.NonConstPointer))
          return false;

        bitmap = rhinodib.ToBitmap();

        return true;
      }
    }

    /// <since>6.0</since>
    virtual public bool DynamicIcon(Size size, out Bitmap bitmap, DynamicIconUsage usage)
    {
      bitmap = null;

      using (var rhinodib = new RhinoDib())
      {
        if (0 == UnsafeNativeMethods.Rdk_RenderContent_GetDynamicIcon(ConstPointer(), size.Width, size.Height, rhinodib.NonConstPointer, (int)usage))
          return false;

        bitmap = rhinodib.ToBitmap();

        return true;
      }
    }
    
    internal delegate int SetExtraRequirementParameterCallback(int serialNumber, IntPtr pString_paramName, IntPtr pString_extraRequirementName, IntPtr value, int sc);
    internal static SetExtraRequirementParameterCallback m_SetExtraRequirementParameter = OnSetExtraRequirementParameter;
    [MonoPInvokeCallback(typeof(SetExtraRequirementParameterCallback))]
    static int OnSetExtraRequirementParameter(int serialNumber, IntPtr pString_paramName, IntPtr pString_extraRequirementName, IntPtr value, int sc)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        if (content != null && pString_paramName != IntPtr.Zero && value != IntPtr.Zero && pString_extraRequirementName != IntPtr.Zero)
        {
          var v = Variant.CopyFromPointer(value);
          return content.SetExtraRequirementParameter(StringWrapper.GetStringFromPointer(pString_paramName),
                                      StringWrapper.GetStringFromPointer(pString_extraRequirementName), v,
                                     (ExtraRequirementsSetContexts)sc) ? 1 : 0;
        }
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate int HarvestDataCallback(int serialNumber, IntPtr oldContent);
    internal static HarvestDataCallback HarvestData = g_on_harvest_data;
    [MonoPInvokeCallback(typeof(HarvestDataCallback))]
    static int g_on_harvest_data(int serialNumber, IntPtr oldContent)
    {
      try
      {
        var content = FromSerialNumber(serialNumber);
        var old = FromPointer(oldContent, null);
        if (content != null && old != null)
          return (int)content.MatchData(old);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
      return (int)MatchDataResult.None;
    }

    internal delegate void AddUiSectionsCallback(int serialNumber, Guid editorId);
    internal static AddUiSectionsCallback m_AddUISections = OnAddUISections;
    internal static Guid OnAddUiSectionsUIId = Guid.Empty;
    [MonoPInvokeCallback(typeof(AddUiSectionsCallback))]
    static void OnAddUISections(int serialNumber, Guid UIId)
    {
      OnAddUiSectionsUIId = UIId;
      try
      {
        var content = FromSerialNumber(serialNumber);
        content?.OnAddUserInterfaceSections();
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
      OnAddUiSectionsUIId = Guid.Empty;
    }

    internal delegate int GetDefaultsFromUserCallback(int serialNumber);
    internal static GetDefaultsFromUserCallback GetDefaultsFromUser = g_on_get_defaults_from_user;
    [MonoPInvokeCallback(typeof(GetDefaultsFromUserCallback))]
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
        HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal delegate void RenderContentOnCppDtorCallback(int serialNumber);
    internal static RenderContentOnCppDtorCallback OnCppDtor = OnCppDtorRhCmnRenderContent;
    [MonoPInvokeCallback(typeof(RenderContentOnCppDtorCallback))]
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
        HostUtils.ExceptionReport(ex);
      }
    }

    internal delegate uint RenderContentBitFlagsCallback(int serialNumber, uint flags);
    internal static RenderContentBitFlagsCallback BitFlags = OnContentBitFlags;
    [MonoPInvokeCallback(typeof(RenderContentBitFlagsCallback))]
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
        HostUtils.ExceptionReport(ex);
      }
      return flags;
    }

    /// <since>7.5</since>
    public RenderContentStyles Styles
    {
      get { return (RenderContentStyles)UnsafeNativeMethods.Rdk_RenderContent_BitFlags(ConstPointer()); }
    }

    private RenderContentStyles m_styles_to_add = RenderContentStyles.None;
    private RenderContentStyles m_styles_to_remove = RenderContentStyles.None;

    /// <summary>
    /// ModifyRenderContentStyles
    /// </summary>
    /// <param name="stylesToAdd"></param>
    /// <param name="stylesToRemove"></param>
    protected void ModifyRenderContentStyles(RenderContentStyles stylesToAdd, RenderContentStyles stylesToRemove)
    {
      m_styles_to_add = stylesToAdd;
      m_styles_to_remove = stylesToRemove;
    }

    internal delegate void GetRenderContentStringCallback(int serialNumber, int string_id, IntPtr pON_wString);
    internal static GetRenderContentStringCallback GetRenderContentString = OnGetRenderContentString;
    [MonoPInvokeCallback(typeof(GetRenderContentStringCallback))]
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
        HostUtils.ExceptionReport(ex);
      }
    }

    internal delegate IntPtr GetShaderCallback(int serialNumber, Guid renderEngineId, IntPtr privateData);
    internal static GetShaderCallback m_GetShader = OnGetShader;
    [MonoPInvokeCallback(typeof(GetShaderCallback))]
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
        HostUtils.ExceptionReport(ex);
      }
      return IntPtr.Zero;
    }

    internal delegate ulong RenderContentUiHashCallback(int serialNumber);
    internal static readonly RenderContentUiHashCallback m_GetUiHash = OnRenderContentUiHash;
    [MonoPInvokeCallback(typeof(RenderContentUiHashCallback))]
    private static ulong OnRenderContentUiHash(int serialNumber)
    {
      var render_content = FromSerialNumber(serialNumber);

      return render_content != null ? render_content.GetUiHash() : 0;
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
    [MonoPInvokeCallback(typeof(ContentAddedCallback))]
    private static void OnContentAdded(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseAttachReason reason)
    {
      if (m_content_added_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        m_content_added_event.SafeInvoke(doc, new RenderContentEventArgs(doc, content, ReasonFromAttachReason(reason)));
      }
    }
    internal static EventHandler<RenderContentEventArgs> m_content_added_event;

    private static ContentRenamedCallback m_OnContentRenamed;
    [MonoPInvokeCallback(typeof(ContentRenamedCallback))]
    private static void OnContentRenamed(IntPtr pContent)
    {
      if (g_content_renamed_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = content?.DocumentOwner;
        g_content_renamed_event.SafeInvoke(doc, new RenderContentEventArgs(doc, content));
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_renamed_event;

    private static ContentDeletingCallback m_OnContentDeleting;
    [MonoPInvokeCallback(typeof(ContentDeletingCallback))]
    private static void OnContentDeleting(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseDetachReason reason)
    {
      if (g_content_deleting_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        g_content_deleting_event.SafeInvoke(doc, new RenderContentEventArgs(doc, content, ReasonFromDetachReason(reason)));
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_deleting_event;

    private static ContentDeletingCallback m_OnContentDeleted;
    [MonoPInvokeCallback(typeof(ContentDeletingCallback))]
    private static void OnContentDeleted(uint docSerialNumber, IntPtr pContent, UnsafeNativeMethods.RdkEventWatcherBaseDetachReason reason)
    {
      if (g_content_deleted_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        g_content_deleted_event.SafeInvoke(doc, new RenderContentEventArgs(doc, content, ReasonFromDetachReason(reason)));
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_deleted_event;

    private static ContentReplacingCallback m_OnContentReplacing;
    [MonoPInvokeCallback(typeof(ContentReplacingCallback))]
    private static void OnContentReplacing(uint docSerialNumber, IntPtr pContent)
    {
      if (g_content_replacing_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        g_content_replacing_event.SafeInvoke(doc, new RenderContentEventArgs(doc, content));
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_replacing_event;

    private static ContentReplacedCallback m_OnContentReplaced;
    [MonoPInvokeCallback(typeof(ContentReplacedCallback))]
    private static void OnContentReplaced(uint docSerialNumber, IntPtr pContent)
    {
      if (g_content_replaced_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        g_content_replaced_event.SafeInvoke(doc, new RenderContentEventArgs(doc, content));
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_replaced_event;

    private static ContentChangedCallback m_OnContentChanged;
    [MonoPInvokeCallback(typeof(ContentChangedCallback))]
    private static void OnContentChanged(IntPtr newContent, IntPtr oldContent, int cc)
    {
      if (g_content_changed_event != null)
      {
        var content = FromPointer(newContent, null);
        var old_content = FromPointer(oldContent, null);
        var doc = content?.DocumentOwner;
        g_content_changed_event.SafeInvoke(doc, new RenderContentChangedEventArgs(doc, content, old_content, (ChangeContexts)cc));
      }
    }
    static EventHandler<RenderContentChangedEventArgs> g_content_changed_event;

    private static ContentUpdatePreviewCallback m_OnContentUpdatePreview;
    [MonoPInvokeCallback(typeof(ContentUpdatePreviewCallback))]
    private static void OnContentUpdatePreview(IntPtr pContent)
    {
      if (g_content_update_preview_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = content?.DocumentOwner;
        g_content_update_preview_event.SafeInvoke(doc, new RenderContentEventArgs(doc, content));
      }
    }
    static EventHandler<RenderContentEventArgs> g_content_update_preview_event;

    /// <summary>
    /// Used to monitor render content addition to the document.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentEventArgs> ContentAdded
    {
      add
      {
        if (m_content_added_event == null)
        {
          m_OnContentAdded = OnContentAdded;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentAddedEventCallback(m_OnContentAdded, HostUtils.m_rdk_ew_report);
        }
        m_content_added_event += value;
      }
      remove
      {
        m_content_added_event -= value;
        if (m_content_added_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentAddedEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentAdded = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content renaming in the document.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentEventArgs> ContentRenamed
    {
      add
      {
        if (g_content_renamed_event == null)
        {
          m_OnContentRenamed = OnContentRenamed;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentRenamedEventCallback(m_OnContentRenamed, HostUtils.m_rdk_ew_report);
        }
        g_content_renamed_event += value;
      }
      remove
      {
        g_content_renamed_event -= value;
        if (g_content_renamed_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentRenamedEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentRenamed = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content deletion from the document.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentEventArgs> ContentDeleting
    {
      add
      {
        if (g_content_deleting_event == null)
        {
          m_OnContentDeleting = OnContentDeleting;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletingEventCallback(m_OnContentDeleting, HostUtils.m_rdk_ew_report);
        }
        g_content_deleting_event += value;
      }
      remove
      {
        g_content_deleting_event -= value;
        if (g_content_deleting_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletingEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentDeleting = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content deletion from the document.
    /// </summary>
    /// <since>6.0</since>
    public static event EventHandler<RenderContentEventArgs> ContentDeleted
    {
      add
      {
        if (g_content_deleted_event == null)
        {
          m_OnContentDeleted = OnContentDeleted;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletedEventCallback(m_OnContentDeleted, HostUtils.m_rdk_ew_report);
        }
        g_content_deleted_event += value;
      }
      remove
      {
        g_content_deleted_event -= value;
        if (g_content_deleted_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentDeletedEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentDeleted = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content replacing in the document.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentEventArgs> ContentReplacing
    {
      add
      {
        if (g_content_replacing_event == null)
        {
          m_OnContentReplacing = OnContentReplacing;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacingEventCallback(m_OnContentReplacing, HostUtils.m_rdk_ew_report);
        }
        g_content_replacing_event += value;
      }
      remove
      {
        g_content_replacing_event -= value;
        if (g_content_replacing_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacingEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentReplacing = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content replacing in the document.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentEventArgs> ContentReplaced
    {
      add
      {
        if (g_content_replaced_event == null)
        {
          m_OnContentReplaced = OnContentReplaced;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacedEventCallback(m_OnContentReplaced, HostUtils.m_rdk_ew_report);
        }
        g_content_replaced_event += value;
      }
      remove
      {
        g_content_replaced_event -= value;
        if (g_content_replaced_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentReplacedEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentReplaced = null;
        }
      }
    }

    /// <summary>
    /// Used to monitor render content modifications.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentChangedEventArgs> ContentChanged
    {
      add
      {
        if (g_content_changed_event == null)
        {
          m_OnContentChanged = OnContentChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentChangedEventCallback(m_OnContentChanged, HostUtils.m_rdk_ew_report);
        }
        g_content_changed_event += value;
      }
      remove
      {
        g_content_changed_event -= value;
        if (g_content_changed_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentChangedEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentChanged = null;
        }
      }
    }

    /// <summary>
    /// This event is raised when a preview has been rendered
    /// </summary>
    public static event EventHandler<PreviewRenderedEventArgs> PreviewRendered
    {
      add
      {
        if (g_on_preview_rendered == null)
        {
          g_on_preview_rendered = OnPreviewRendered;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnPreviewRenderedEventCallback(g_on_preview_rendered, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_on_preview_rendered_event += value;
      }
      remove
      {
        g_on_preview_rendered_event -= value;
        if (g_on_preview_rendered == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnPreviewRenderedEventCallback(null, Rhino.Runtime.HostUtils.m_rdk_ew_report);
          g_on_preview_rendered = null;
        }
      }
    }
    internal delegate void OnPreviewRenderedCallback(IntPtr pDib, uint quality, IntPtr pjs);
    internal static OnPreviewRenderedCallback g_on_preview_rendered;
    static EventHandler<PreviewRenderedEventArgs> g_on_preview_rendered_event;
    [MonoPInvokeCallback(typeof(OnPreviewRenderedCallback))]
    private static void OnPreviewRendered(IntPtr pDib, uint quality, IntPtr p_pjs)
    {
      if (g_on_preview_rendered_event != null)
      {
        var args = new PreviewRenderedEventArgs();

        PreviewJobSignature pjs = new PreviewJobSignature(p_pjs);

        var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(UnsafeNativeMethods.CRhinoDib_Copy(pDib), true);

        args.PreviewJobSignature = pjs;
        args.Bitmap = bitmap;

        Utilities.PreviewQuality q = Utilities.PreviewQuality.None;

        if(quality == 0)
          q = Utilities.PreviewQuality.None;
        else if (quality == 1)
          q = Utilities.PreviewQuality.Low;
        else if (quality == 2)
          q = Utilities.PreviewQuality.Medium;
        else if (quality == 3)
          q = Utilities.PreviewQuality.IntermediateProgressive;
        else if (quality == 4)
          q = Utilities.PreviewQuality.Full;

        args.Quality = q;

        g_on_preview_rendered_event.SafeInvoke(null, args);
      }
    }

    /// <summary>
    /// This event is raised when a field value is modified.
    /// </summary>
    /// <since>5.11</since>
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
    internal delegate void OnContentFieldChangedCallback(int serialNumber, IntPtr pString_name, IntPtr value, int cc);
    internal static OnContentFieldChangedCallback g_on_content_field_changed;
    static EventHandler<RenderContentFieldChangedEventArgs> g_content_field_changed_event;
    [MonoPInvokeCallback(typeof(OnContentFieldChangedCallback))]
    private static void OnContentFieldChanged(int serialNumber, IntPtr pString_name, IntPtr value, int cc)
    {
      if (g_content_field_changed_event != null)
      {
        if (pString_name == IntPtr.Zero) return;
        var content = FromSerialNumber(serialNumber);
        if (content == null) return;
        //var v = Variant.CopyFromPointer(value);
        //var old_value = v.AsObject();
        var name_string = StringWrapper.GetStringFromPointer(pString_name);
        var args = new RenderContentFieldChangedEventArgs(content, name_string, (ChangeContexts) cc);
        g_content_field_changed_event.SafeInvoke(content, args);
      }
    }

    /// <summary>
    /// Used to monitor render content preview updates.
    /// </summary>
    /// <since>5.7</since>
    public static event EventHandler<RenderContentEventArgs> ContentUpdatePreview
    {
      add
      {
        if (g_content_update_preview_event == null)
        {
          m_OnContentUpdatePreview = OnContentUpdatePreview;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentUpdatePreviewEventCallback(m_OnContentUpdatePreview, HostUtils.m_rdk_ew_report);
        }
        g_content_update_preview_event += value;
      }
      remove
      {
        g_content_update_preview_event -= value;
        if (g_content_update_preview_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentUpdatePreviewEventCallback(null, HostUtils.m_rdk_ew_report);
          m_OnContentUpdatePreview = null;
        }
      }
    }

    static EventHandler<RenderContentEventArgs> g_current_environment_change_event;
    private static CurrentContentChangedCallback m_OnCurrentEnvironmentChange;
    [MonoPInvokeCallback(typeof(CurrentContentChangedCallback))]
    private static void OnCurrentEnvironmentChange(uint docSerialNumber, int kind, int usage, IntPtr pContent)
    {
      if (g_current_environment_change_event != null)
      {
        var content = FromPointer(pContent, null);
        var doc = content?.DocumentOwner;
        var args = new RenderContentEventArgs(doc, content, (RenderSettings.EnvironmentUsage)usage);
        g_current_environment_change_event.SafeInvoke(doc, args);
      }
    }

    /// <summary>
    /// Event fired when changes to current environments have been made.
    /// This will be one of Background, Reflection or Skylighting
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
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentCurrencyChangedEventCallback(m_OnCurrentEnvironmentChange, HostUtils.m_rdk_ew_report);
        }
        g_current_environment_change_event += value;
      }
      remove
      {
        g_current_environment_change_event -= value;
        if (g_current_environment_change_event == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetContentCurrencyChangedEventCallback(null, HostUtils.m_rdk_ew_report);
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
      lock (g_custom_content_dictionary_lock)
      {
        g_custom_content_dictionary.TryGetValue(serialNumber, out RenderContent rc);
        return rc;
      }
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
    /// <summary>
    /// Finalizer
    /// </summary>
    ~RenderContent()
    {
      Dispose(false);
    }

    /// <since>5.1</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    /// <param name="disposing"></param>
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
        lock (g_custom_content_dictionary_lock)
        {
          g_custom_content_dictionary.Remove(serialNumber);
        }
      }

      if (HostUtils.RunningOnWindows)
      {
        // TODO_RDK_MAC the dictionary handles cannot be deleted on
        // the Mac. This needs to be thought through carefully. The current
        // problem is that we cannot yet create a CRhinoDib from a C# Bitmap.

        foreach (var item in m_bitmap_to_icon_dictionary)
        {
          UnsafeNativeMethods.DeleteObject(item.Value);
        }
        m_bitmap_to_icon_dictionary.Clear();
      }
      
    }
    #endregion
  }

  /// <since>6.2</since>
  [CLSCompliant(false)]
  [Flags]
  public enum CrcRenderHashFlags : ulong
  {
    /// <summary>
    /// Normal render CRC; nothing is excluded.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Linear Workflow color and texture changes are not included.
    /// </summary>
    ExcludeLinearWorkflow = 1,
    /// <summary>
    /// Local mapping is excluded (only used by Textures).
    /// </summary>
    ExcludeLocalMapping = 2,
    /// <summary>
    /// Units are excluded (only used by Textures).
    /// </summary>
    ExcludeUnits = 4,
    /// <summary>
    /// Reserved for future use
    /// </summary>
    Reserved2              = 8,
    /// <summary>
    /// Use this flag when simulating
    /// </summary>
    ForSimulation          = ExcludeLinearWorkflow,
    /// <summary>
    /// Use this flag when you want to calculate the CRC independantly of any effect the document has on the content.
    /// </summary>
    ExcludeDocumentEffects = ExcludeLinearWorkflow | ExcludeUnits | Reserved2
  }

  /// <summary>
  /// Event args for RenderContent
  /// </summary>
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

    internal RenderContentEventArgs(RhinoDoc doc, RenderContent content, RenderSettings.EnvironmentUsage usage)
    {
      m_content = content;
      Document = doc;
      EnvironmentUsageEx = usage;
    }

    /// <since>5.7</since>
    public RenderContent Content { get { return m_content; } }

    /// <since>6.0</since>
    public RhinoDoc Document { get; private set; }

    /// <summary>
    /// Not when used in CurrentEnvironmentChanged (defaults to None).
    /// </summary>
    /// <since>6.0</since>
    public RenderContentChangeReason Reason { get; private set; } = RenderContentChangeReason.None;

    /// <summary>
    /// Meaningful for CurrentEnvironmentChanged event. Will be one of Background, ReflectionAndRefraction or Skylighting.
    /// </summary>
    /// <since>6.11</since>
    [Obsolete("Call EnvironmentUsageEx instead")]
    public RenderEnvironment.Usage EnvironmentUsage { get; private set; } = RenderEnvironment.Usage.None;

    /// <summary>
    /// Meaningful for CurrentEnvironmentChanged event. Will be one of Background, Reflection or Skylighting.
    /// </summary>
    /// <since>8.0</since>
    public RenderSettings.EnvironmentUsage EnvironmentUsageEx { get; private set; } = RenderSettings.EnvironmentUsage.Background;
  }

  /// <summary>
  /// EventArgs for the RenderContentChanged event
  /// </summary>
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
    /// <since>5.7</since>
    public RenderContent.ChangeContexts ChangeContext { get { return m_cc; } }
    private readonly RenderContent m_old_content;
    /// <since>6.0</since>
    public RenderContent OldContent { get { return m_old_content; } }
  }

  /// <summary>
  /// Enumeration denoting type of change for attach or detach.
  /// </summary>
  /// <since>6.0</since>
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

  /// <summary>
  /// EventArgs for the RenderContentFieldChanged event
  /// </summary>
  public class RenderContentFieldChangedEventArgs : RenderContentChangedEventArgs
  {
    internal RenderContentFieldChangedEventArgs(RenderContent content, string fieldName, RenderContent.ChangeContexts cc)
      : base(content.DocumentOwner, content, null, cc)
    {
      m_field_name = fieldName;
    }
    /// <since>5.11</since>
    public string FieldName { get { return m_field_name; } }
    private readonly string m_field_name;
  }

  /// <summary>
  /// PreviewRenderedEventArgs is raised when a content preview has been rendered
  /// </summary>
  [CLSCompliant(true)] 
  public class PreviewRenderedEventArgs : EventArgs
  {
    PreviewJobSignature m_preview_job_signature;
    Bitmap m_bitmap;
    Utilities.PreviewQuality m_quality;

    /// <summary>
    /// The Preview Job Signature associated with the rendered preview
    /// </summary>
    /// <since>8.8</since>
    public PreviewJobSignature PreviewJobSignature { get { return m_preview_job_signature; } set { m_preview_job_signature = value;}}

    /// <summary>
    /// The Bitmap of the rendered preview
    /// </summary>
    /// <since>8.8</since>
    public Bitmap Bitmap { get { return m_bitmap; } set { m_bitmap = value;}}
    
    /// <summary>
    /// The Bitmap of the rendered preview
    /// </summary>
    /// <since>8.8</since>
    public Utilities.PreviewQuality Quality { get { return m_quality; } set { m_quality = value;}}
  }

  //*public*/
  //class RenderContentTypeEventArgs : EventArgs
  //{
  //  readonly Guid m_content_type;
  //  internal RenderContentTypeEventArgs(Guid type) { m_content_type = type; }
  //  public Guid Content { get { return m_content_type; } }
  //}
  //
  //*public*/
  //class RenderContentKindEventArgs : EventArgs
  //{
  //  //readonly RenderContentKind m_kind;
  //  //internal RenderContentKindEventArgs(RenderContentKind kind) { m_kind = kind; }
  //  //public RenderContentKind Content { get { return m_kind; } }
  //}
  //
  //*public*/
  //class CurrentRenderContentChangedEventArgs : RenderContentEventArgs
  //{
  //  internal CurrentRenderContentChangedEventArgs(RhinoDoc doc, RenderContent content, RenderContentKind kind)
  //    : base(doc, content)
  //  {
  //    //m_kind = kind;
  //  }
  //
  //  //readonly RenderContentKind m_kind;
  //  //public RenderContentKind Kind { get { return m_kind; } }
  //}

  namespace ParameterNames
  {
    /// <summary>
    /// Helper class with properties containing the names of fields available in our PBR implementation.
    /// </summary>
    public static class PhysicallyBased
    {
      /// <since>7.0</since>
      public static string BaseColor { get { return ChildSlotNames.PhysicallyBased.BaseColor; } }
      /// <since>7.0</since>
      public static string BRDF { get { return "pbr-brdf"; } }
      /// <since>7.0</since>
      public static string Subsurface { get { return ChildSlotNames.PhysicallyBased.Subsurface; } }
      /// <since>7.0</since>
      public static string SubsurfaceScatteringColor { get { return ChildSlotNames.PhysicallyBased.SubsurfaceScatteringColor; } }
      /// <since>7.0</since>
      public static string SubsurfaceScatteringRadius { get { return ChildSlotNames.PhysicallyBased.SubsurfaceScatteringRadius; } }
      /// <since>7.0</since>
      public static string Specular { get { return ChildSlotNames.PhysicallyBased.Specular; } }
      /// <since>7.0</since>
      public static string SpecularTint { get { return ChildSlotNames.PhysicallyBased.SpecularTint; } }
      /// <since>7.0</since>
      public static string Metallic { get { return ChildSlotNames.PhysicallyBased.Metallic; } }
      /// <since>7.0</since>
      public static string Roughness { get { return ChildSlotNames.PhysicallyBased.Roughness; } }
      /// <since>7.0</since>
      public static string Anisotropic { get { return ChildSlotNames.PhysicallyBased.Anisotropic; } }
      /// <since>7.0</since>
      public static string AnisotropicRotation { get { return ChildSlotNames.PhysicallyBased.AnisotropicRotation; } }
      /// <since>7.0</since>
      public static string Sheen { get { return ChildSlotNames.PhysicallyBased.Sheen; } }
      /// <since>7.0</since>
      public static string SheenTint { get { return ChildSlotNames.PhysicallyBased.SheenTint; } }
      /// <since>7.0</since>
      public static string Clearcoat { get { return ChildSlotNames.PhysicallyBased.Clearcoat; } }
      /// <since>7.0</since>
      public static string ClearcoatRoughness { get { return ChildSlotNames.PhysicallyBased.ClearcoatRoughness; } }
      /// <since>7.0</since>
      public static string ClearcoatBump { get { return ChildSlotNames.PhysicallyBased.ClearcoatBump; } }
      /// <since>7.0</since>
      public static string OpacityIor { get { return ChildSlotNames.PhysicallyBased.OpacityIor; } }
      /// <since>7.0</since>
      public static string Opacity { get { return ChildSlotNames.PhysicallyBased.Opacity; } }
      /// <since>7.0</since>
      public static string OpacityRoughness { get { return ChildSlotNames.PhysicallyBased.OpacityRoughness; } }
      /// <since>7.0</since>
      public static string Emission { get { return ChildSlotNames.PhysicallyBased.Emission; } }
      /// <since>7.0</since>
      public static string Displacement { get { return ChildSlotNames.PhysicallyBased.Displacement; } }
      /// <since>7.0</since>
      public static string Bump { get { return ChildSlotNames.PhysicallyBased.Bump; } }
      /// <since>7.0</since>
      public static string AmbientOcclusion { get { return ChildSlotNames.PhysicallyBased.AmbientOcclusion; } }
      /// <since>7.0</since>
      public static string Alpha { get { return ChildSlotNames.PhysicallyBased.Alpha; } }
    }
  }

  namespace ChildSlotNames
  {
    /// <summary>
    /// Helper class with properties containing the names of children available in our PBR implementation.
    /// </summary>
    public static class PhysicallyBased
    {
      /// <since>7.0</since>
      public static string BaseColor { get { return FromTextureType(DocObjects.TextureType.PBR_BaseColor); } }
      /// <since>7.0</since>
      public static string Subsurface { get { return FromTextureType(DocObjects.TextureType.PBR_Subsurface); } }
      /// <since>7.0</since>
      public static string SubsurfaceScatteringColor { get { return FromTextureType(DocObjects.TextureType.PBR_SubsurfaceScattering); } }
      /// <since>7.0</since>
      public static string SubsurfaceScatteringRadius { get { return FromTextureType(DocObjects.TextureType.PBR_SubsurfaceScatteringRadius); } }
      /// <since>7.0</since>
      public static string Specular { get { return FromTextureType(DocObjects.TextureType.PBR_Specular); } }
      /// <since>7.0</since>
      public static string SpecularTint { get { return FromTextureType(DocObjects.TextureType.PBR_SpecularTint); } }
      /// <since>7.0</since>
      public static string Metallic { get { return FromTextureType(DocObjects.TextureType.PBR_Metallic); } }
      /// <since>7.0</since>
      public static string Roughness { get { return FromTextureType(DocObjects.TextureType.PBR_Roughness); } }
      /// <since>7.0</since>
      public static string Anisotropic { get { return FromTextureType(DocObjects.TextureType.PBR_Anisotropic); } }
      /// <since>7.0</since>
      public static string AnisotropicRotation { get { return FromTextureType(DocObjects.TextureType.PBR_Anisotropic_Rotation); } }
      /// <since>7.0</since>
      public static string Sheen { get { return FromTextureType(DocObjects.TextureType.PBR_Sheen); } }
      /// <since>7.0</since>
      public static string SheenTint { get { return FromTextureType(DocObjects.TextureType.PBR_SheenTint); } }
      /// <since>7.0</since>
      public static string Clearcoat { get { return FromTextureType(DocObjects.TextureType.PBR_Clearcoat); } }
      /// <since>7.0</since>
      public static string ClearcoatRoughness { get { return FromTextureType(DocObjects.TextureType.PBR_ClearcoatRoughness); } }
      /// <since>7.0</since>
      public static string ClearcoatBump { get { return FromTextureType(DocObjects.TextureType.PBR_ClearcoatBump); } }
      /// <since>7.0</since>
      public static string OpacityIor { get { return FromTextureType(DocObjects.TextureType.PBR_OpacityIor); } }
      /// <since>7.0</since>
      public static string Opacity { get { return FromTextureType(DocObjects.TextureType.Opacity); } }
      /// <since>7.0</since>
      public static string OpacityRoughness { get { return FromTextureType(DocObjects.TextureType.PBR_OpacityRoughness); } }
      /// <since>7.0</since>
      public static string Emission { get { return FromTextureType(DocObjects.TextureType.PBR_Emission); } }
      /// <since>7.0</since>
      public static string Displacement { get { return FromTextureType(DocObjects.TextureType.PBR_Displacement); } }
      /// <since>7.0</since>
      public static string Bump { get { return FromTextureType(DocObjects.TextureType.Bump); } }
      /// <since>7.0</since>
      public static string AmbientOcclusion { get { return FromTextureType(DocObjects.TextureType.PBR_AmbientOcclusion); } }
      /// <since>7.0</since>
      public static string Alpha { get { return FromTextureType(DocObjects.TextureType.PBR_Alpha); } }

      /// <since>7.0</since>
      public static string FromTextureType(Rhino.DocObjects.TextureType textureType)
      {
        using (var sh = new StringHolder())
        {
          var p_string = sh.NonConstPointer();
          if (UnsafeNativeMethods.CRhRdkMaterial_PhysicallyBased_ChildSlotName_FromTextureType((int)textureType, p_string))
          {
            return sh.ToString();
          }
        }

        Debug.Assert(false);
        return "";
      }
    }
  }
}

internal interface INativeContent
{
  Guid Document { get; set; }
  Guid Id { get; set; }
}

#endif
