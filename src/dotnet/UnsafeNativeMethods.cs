using System;
using Rhino.Geometry;
using System.Runtime.InteropServices;
using Rhino.Display;
using static Rhino.UI.Localization;

// 19 Dec. 2010 S. Baer
// Giulio saw a significant performance increase by marking this class with the
// SuppressUnmanagedCodeSecurity attribute. See MSDN for details
[System.Security.SuppressUnmanagedCodeSecurity]
internal partial class UnsafeNativeMethods
{
  [StructLayout(LayoutKind.Sequential)]
  public struct Point
  {
#if MONO_BUILD
    public long X;
    public long Y;

    public Point(long x, long y)
    {
      X = x;
      Y = y;
    }
#else
    public int X;
    public int Y;
    public Point(int x, int y)
    {
      X = x;
      Y = y;
    }
#endif
  }

#if RHINO3DM_BUILD
  static UnsafeNativeMethods()
  {
    Init();
  }

  private static bool g_paths_set = false;
  public static void Init()
  {
    if (!g_paths_set)
    {
      var assembly_name = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string dir_name = System.IO.Path.GetDirectoryName(assembly_name);

      switch(Environment.OSVersion.Platform)
      {
        case PlatformID.Win32NT:
          {
            string env_path = Environment.GetEnvironmentVariable("path");
            var sub_directory = Environment.Is64BitProcess ? "\\Win64" : "\\Win32";
            Environment.SetEnvironmentVariable("path", env_path + ";" + dir_name + sub_directory);
          }
          break;
        default:
          break; // This is solved on Mac by using a config file
      }
      g_paths_set = true;
    }
  }
#endif


#if RHINO_SDK
#if MONO_BUILD
  [DllImport("RhinoCore")]
#else
  [DllImport("user32.dll")]
#endif
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool GetCursorPos(out Point lpPoint);

  [DllImport("user32.dll", CharSet = CharSet.Auto)]
  internal static extern bool DestroyIcon(IntPtr handle);

  [DllImport("Gdi32.dll", CharSet = CharSet.Auto)]
  internal static extern bool DeleteObject(IntPtr handle);

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

  [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
  public static extern IntPtr GetModuleHandle(string lpModuleName);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoUiHooks_SetLocalizationLocaleId(Rhino.UI.Localization.SetCurrentLanguageIdDelegate hook);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_RhRegisterNamedCallbackProc([MarshalAs(UnmanagedType.LPWStr)]string name, IntPtr callback);


  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoCommonPlugInLoader_SetCallbacks(Rhino.Runtime.HostUtils.LoadPluginCallback loadplugin,
    Rhino.Runtime.HostUtils.LoadSkinCallback loadskin,
    Action buildlists,
    Rhino.Runtime.HostUtils.GetAssemblyIdCallback getassemblyid);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern bool ON_SpaceMorph_MorphGeometry(IntPtr pConstGeometry, double tolerance, [MarshalAs(UnmanagedType.U1)]bool quickpreview, [MarshalAs(UnmanagedType.U1)]bool preserveStructure, IntPtr callback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern bool ON_SpaceMorph_MorphPlane(ref Plane pPlane, double tolerance, [MarshalAs(UnmanagedType.U1)]bool quickpreview, [MarshalAs(UnmanagedType.U1)]bool preserveStructure, IntPtr callback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetPythonEvaluateCallback(Rhino.Runtime.HostUtils.EvaluateExpressionCallback callback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetTextFieldEvalCallback(Rhino.Runtime.HostUtils.EvaluateTextFieldCallback callback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetGetNowProc(Rhino.Runtime.HostUtils.GetNowCallback callback, Rhino.Runtime.HostUtils.GetFormattedTimeCallback formattedTimCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetSendLogMessageToCloudProc(Rhino.Runtime.HostUtils.SendLogMessageToCloudCallback callback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetSendToPdfProc(Rhino.FileIO.FilePdf.SendToPdfCallback callback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetLicenseManagerCallbacks(Rhino.Runtime.LicenseManager.InitializeCallback dinitLicenseManagerProc,
                                                             Rhino.Runtime.LicenseManager.EchoCallback echoProc,
                                                             Rhino.Runtime.LicenseManager.ShowValidationUiCallback showLicenseValidationProc,
                                                             Rhino.Runtime.LicenseManager.UuidCallback licenseUuidProc,
                                                             Rhino.Runtime.LicenseManager.GetLicenseCallback getLicense,
                                                             Rhino.Runtime.LicenseManager.GetCustomLicenseCallback getCustomLicense,
                                                             Rhino.Runtime.LicenseManager.AskUserForLicenseCallback askUserForLicense,
                                                             Rhino.Runtime.LicenseManager.GetRegisteredOwnerInfoCallback getRegisteredOwnerInfo,
                                                             Rhino.Runtime.LicenseManager.ShowExpiredMessageCallback showExpiredMessage,
                                                             Rhino.Runtime.LicenseManager.GetInternetTimeCallback getInternetTime
                                                            );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  internal static extern bool CRhMainFrame_Invoke(Rhino.InvokeHelper.InvokeAction invokeProc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoVisualAnalysisMode_SetCallbacks(Rhino.Display.VisualAnalysisMode.ANALYSISMODEENABLEUIPROC enableuiProc,
    Rhino.Display.VisualAnalysisMode.ANALYSISMODEOBJECTSUPPORTSPROC objectSupportProc,
    Rhino.Display.VisualAnalysisMode.ANALYSISMODESHOWISOCURVESPROC showIsoCurvesProc,
    Rhino.Display.VisualAnalysisMode.ANALYSISMODESETDISPLAYATTRIBUTESPROC displayAttributesProc,
    Rhino.Display.VisualAnalysisMode.ANALYSISMODEUPDATEVERTEXCOLORSPROC updateVertexColorsProc,
    Rhino.Display.VisualAnalysisMode.ANALYSISMODEDRAWRHINOOBJECTPROC drawRhinoObjectProc,
    Rhino.Display.VisualAnalysisMode.ANALYSISMODEDRAWGEOMETRYPROC drawGeometryProc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoPlugIn_SetCallbacks(int serialNumber,
    Rhino.PlugIns.PlugIn.OnLoadDelegate onloadCallback,
    Rhino.PlugIns.PlugIn.OnShutdownDelegate shutdownCallback,
    Rhino.PlugIns.PlugIn.OnGetPlugInObjectDelegate getpluginobjectCallback,
    Rhino.PlugIns.PlugIn.CallWriteDocumentDelegate callwriteCallback,
    Rhino.PlugIns.PlugIn.WriteDocumentDelegate writedocumentCallback,
    Rhino.PlugIns.PlugIn.ReadDocumentDelegate readdocumentCallback,
    Rhino.PlugIns.PlugIn.DisplayOptionsDialogDelegate displayOptionsDialog
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoPlugIn_SetCallbacks3(Rhino.PlugIns.PlugIn.OnAddPagesToOptionsDelegate addoptionpagesCallback,
                                                         Rhino.PlugIns.PlugIn.OnAddPagesToObjectPropertiesDelegate addobjectpropertiespagesCallback,
                                                         Rhino.PlugIns.PlugIn.OnPlugInProcDelegate plugInProcCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoPlugIn_SetCallbacks4(Rhino.PlugIns.PlugIn.ResetMessageBoxesDelegate resetMessageBoxesCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoPlugIn_SetLegacyCallbacks(Rhino.PlugIns.PlugIn.LoadSaveProfileDelegate profilefunc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoFileImportPlugIn_SetCallbacks(Rhino.PlugIns.FileImportPlugIn.AddFileType addfiletype,
    Rhino.PlugIns.FileImportPlugIn.ReadFileFunc readfile);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoFileExportPlugIn_SetCallbacks(Rhino.PlugIns.FileExportPlugIn.AddFileType addfiletype,
    Rhino.PlugIns.FileExportPlugIn.WriteFileFunc writefile);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoRenderPlugIn_SetCallbacks(
    Rhino.PlugIns.RenderPlugIn.RenderFunc render,
    Rhino.PlugIns.RenderPlugIn.RenderWindowFunc renderwindow,
    Rhino.PlugIns.RenderPlugIn.OnSetCurrrentRenderPlugInFunc onsetcurrent,
    Rhino.PlugIns.RenderPlugIn.OnRenderDialogPageFunc onRenderDialogPage,
    Rhino.PlugIns.RenderPlugIn.RenderMaterialUiEventHandler materialUi
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_Rdk_UiHolder_SetPlatformCallbacks(Rhino.UI.Controls.CollapsibleSectionHolderImpl.CREATEFROMCPPPROC cpp);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoRenderPlugIn_SetRdkCallbacks(
    Rhino.PlugIns.RenderPlugIn.SupportsFeatureCallback supportsFeatureCallback,
    Rhino.PlugIns.RenderPlugIn.PreferBasicContentCallback preferBasicContentCallback,
    Rhino.PlugIns.RenderPlugIn.AbortRenderCallback abortRenderCallback,
    Rhino.PlugIns.RenderPlugIn.AllowChooseContentCallback allowChooseContentCallback,
    Rhino.PlugIns.RenderPlugIn.CreateDefaultContentCallback createDefaultContentCallback,
    Rhino.PlugIns.RenderPlugIn.OutputTypesCallback outputTypesCallback,
    Rhino.PlugIns.RenderPlugIn.PreviewRenderTypeCallback previewRenderTypeCallback,
    Rhino.PlugIns.RenderPlugIn.CreateTexturePreviewCallback texturePreviewCallback,
    Rhino.PlugIns.RenderPlugIn.CreatePreviewCallback previewCallback,
    Rhino.PlugIns.RenderPlugIn.DecalCallback decalCallback,
    Rhino.PlugIns.RenderPlugIn.PlugInQuestionCallback plugInQuestionCallback,
    Rhino.PlugIns.RenderPlugIn.RegisterContentIoCallback registerContentIoCallback,
    Rhino.PlugIns.RenderPlugIn.RegisterCustomPlugInsCallback registerCustomPlugInsCallback,
    Rhino.PlugIns.RenderPlugIn.GetCustomRenderSaveFileTypesCallback getCustomRenderSaveFileTypesCallback,
    Rhino.PlugIns.RenderPlugIn.UiContentTypesCallback uiContentTypesCallback,
    Rhino.PlugIns.RenderPlugIn.SaveCusomtomRenderFileCallback saveCustomRenderFile,
    Rhino.PlugIns.RenderPlugIn.RenderSettingsSectionsCallback renderSettingsSections,
    Rhino.PlugIns.RenderPlugIn.PlugInIconCallback pluginiconcallback
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoPlugIn_SetUnknownUserDataCallback(Rhino.PlugIns.PlugIn.UnknownUserDataCallback fn);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetRdkInitializationCallbacks(Rhino.Runtime.HostUtils.InitializeRDKCallback init, Rhino.Runtime.HostUtils.ShutdownRDKCallback shut);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoDigitizerPlugIn_SetCallbacks(Rhino.PlugIns.DigitizerPlugIn.EnableDigitizerFunc enablefunc,
    Rhino.PlugIns.DigitizerPlugIn.UnitSystemFunc unitsystemfunc,
    Rhino.PlugIns.DigitizerPlugIn.PointToleranceFunc pointtolfunc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern IntPtr CRhinoSkin_New(Rhino.Runtime.Skin.ShowSplashCallback cb, [MarshalAs(UnmanagedType.LPWStr)]string name, IntPtr hicon);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoCommand_SetCallbacks(
    int commandSerialNumber,
    Rhino.Commands.Command.RunCommandCallback cb,
    Rhino.Commands.Command.DoHelpCallback dohelpCb,
    Rhino.Commands.Command.ContextHelpCallback contexthelpCb,
    Rhino.Commands.Command.ReplayHistoryCallback replayhistoryCb,
    Rhino.Commands.SelCommand.SelFilterCallback selCb,
    Rhino.Commands.SelCommand.SelSubObjectCallback selSubObjectCb
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoDisplayConduit_SetCallback(uint conduitSerialNumber, int which, Rhino.Display.DisplayPipeline.ConduitCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportcb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetReplaceColorDialogCallback(Rhino.UI.Dialogs.ColorDialogCallback cb);

  //In RhinoApp
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetEscapeKeyCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetKeyboardCallback(Rhino.RhinoApp.KeyboardHookEvent cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetInitAppCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetCloseAppCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetAppSettingsChangeCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  //In Command
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetBeginCommandCallback(Rhino.Commands.Command.CommandCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetEndCommandCallback(Rhino.Commands.Command.CommandCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetUndoEventCallback(Rhino.Commands.Command.UndoCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  //In RhinoDoc
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetCloseDocumentCallback(Rhino.RhinoDoc.DocumentCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetNewDocumentCallback(Rhino.RhinoDoc.DocumentCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetActiveDocumentCallback(Rhino.RhinoDoc.DocumentCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);
  
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CNetModelessUserInterfaceDocChanged_SetCallback(Rhino.RhinoDoc.DocumentCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetDocPropChangeCallback(Rhino.RhinoDoc.DocumentCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetUnitsChangedWithScaling(Rhino.RhinoDoc.UnitsChangedWithScalingCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetBeginOpenDocumentCallback(Rhino.RhinoDoc.DocumentIoCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetEndOpenDocumentCallback(Rhino.RhinoDoc.DocumentIoCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetOnAfterPostReadViewUpdateCallback(Rhino.RhinoDoc.DocumentIoCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetViewModifiedEventCallback(Rhino.Display.RhinoView.ViewCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetBeginSaveDocumentCallback(Rhino.RhinoDoc.DocumentIoCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetEndSaveDocumentCallback(Rhino.RhinoDoc.DocumentIoCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetAddObjectCallback(Rhino.RhinoDoc.RhinoObjectCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetDeleteObjectCallback(Rhino.RhinoDoc.RhinoObjectCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetReplaceObjectCallback(Rhino.RhinoDoc.RhinoObjectCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetUnDeleteObjectCallback(Rhino.RhinoDoc.RhinoObjectCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetPurgeObjectCallback(Rhino.RhinoDoc.RhinoObjectCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetSelectObjectCallback(Rhino.RhinoDoc.RhinoObjectSelectionCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetDeselectAllObjectsCallback(Rhino.RhinoDoc.RhinoDeselectAllObjectsCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetModifyObjectAttributesCallback(Rhino.RhinoDoc.RhinoModifyObjectAttributesCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetLayerTableEventCallback(Rhino.RhinoDoc.RhinoTableCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetLinetypeTableEventCallback(Rhino.RhinoDoc.RhinoTableCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetDimStyleTableEventCallback(Rhino.RhinoDoc.RhinoTableCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetTextureMappingEventCallback(Rhino.RhinoDoc.TextureMappingEventCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetIdefTableEventCallback(Rhino.RhinoDoc.RhinoTableCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetLightTableEventCallback(Rhino.RhinoDoc.RhinoTableCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetMaterialTableEventCallback(Rhino.RhinoDoc.RhinoTableCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetGroupTableEventCallback(Rhino.RhinoDoc.RhinoTableCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern uint CRhinoDoc_AddCustomUndoEvent(uint docSerialNumber, [MarshalAs(UnmanagedType.LPWStr)]string description,
                                                           Rhino.RhinoDoc.RhinoUndoEventHandlerCallback undoCb,
                                                           Rhino.RhinoDoc.RhinoDeleteUndoEventHandlerCallback deleteCb);

  //In RhinoView
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetCreateViewCallback(Rhino.Display.RhinoView.ViewCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetDestroyViewCallback(Rhino.Display.RhinoView.ViewCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetActiveViewCallback(Rhino.Display.RhinoView.ViewCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetRenameViewCallback(Rhino.Display.RhinoView.ViewCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetDetailEventCallback(Rhino.Display.RhinoPageView.PageViewCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetDisplayModeChangedEventCallback(Rhino.Display.DisplayPipeline.DisplayModeChangedCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetOnIDocUserStringChangedCallback(Rhino.RhinoDoc.UserStringChangedCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetOnIdleCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetTransformObjectsCallback(Rhino.RhinoDoc.RhinoTransformObjectsCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetOnMainLoopCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern uint CRhinoGetObject_GetObjects(IntPtr ptr, int min, int max, Rhino.Input.Custom.GetObject.GeometryFilterCallback cb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern uint CRhinoGetPoint_GetPoint(
    IntPtr ptr,
    [MarshalAs(UnmanagedType.U1)]bool onMouseUp,
    [MarshalAs(UnmanagedType.U1)]bool getPoint2D,
    Rhino.Input.Custom.GetPoint.MouseCallback mouseCb,
    Rhino.Input.Custom.GetPoint.DrawCallback drawCb,
    Rhino.Display.DisplayPipeline.ConduitCallback postDrawCb,
    Rhino.Input.Custom.GetTransform.CalculateXformCallack calcXformCb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern uint CRhinoGetXform_GetXform(IntPtr ptr, Rhino.Input.Custom.GetPoint.MouseCallback mouseCb,
                                                      Rhino.Input.Custom.GetPoint.DrawCallback drawCb,
                                                      Rhino.Display.DisplayPipeline.ConduitCallback postDrawCb,
                                                      Rhino.Input.Custom.GetTransform.CalculateXformCallack calcXformCb);

  //In RhinoObject
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoObject_SetCallbacks(Rhino.DocObjects.RhinoObject.RhinoObjectDuplicateCallback duplicate,
                                                        Rhino.DocObjects.RhinoObject.RhinoObjectDocNotifyCallback docNotify,
                                                        Rhino.DocObjects.RhinoObject.RhinoObjectActiveInViewportCallback activeInViewport,
                                                        Rhino.DocObjects.RhinoObject.RhinoObjectSelectionCallback selectionChange,
                                                        Rhino.DocObjects.RhinoObject.RhinoObjectTransformCallback transform,
                                                        Rhino.DocObjects.RhinoObject.RhinoObjectSpaceMorphCallback morph,
                                                        Rhino.DocObjects.RhinoObject.RhinoObjectDeletedCallback deleted);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoObject_SetPerObjectCallbacks(IntPtr ptrObject, IntPtr drawCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoObject_SetPickCallbacks(Rhino.DocObjects.RhinoObject.RhinoObjectPickCallback pick,
                                                        Rhino.DocObjects.RhinoObject.RhinoObjectPickedCallback pickedCallback);

  //RH_C_FUNCTION void CRhCmnMouseCallback_SetCallback(enum MouseCallbackType which, RHMOUSEEVENTCALLBACK_PROC func)
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnMouseCallback_SetCallback(MouseCallbackType which, Rhino.Display.RhinoView.MouseCallback callback);


  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoApp_RegisterGripsEnabler(Guid key, Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoGripsEnablerCallback turnonFunc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoObjectGrips_SetCallbacks(Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsResetCallback resetFunc,
    Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsResetCallback resetmeshFunc,
    Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsUpdateMeshCallback updatemeshFunc,
    Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsNewGeometryCallback newgeomFunc,
    Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsDrawCallback drawFunc,
    Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsNeighborGripCallback neighborgripFunc,
    Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsNurbsSurfaceGripCallback nurbssurfacegripFunc,
    Rhino.DocObjects.Custom.CustomObjectGrips.CRhinoObjectGripsNurbsSurfaceCallback nurbssurfaceFunc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnGripObject_SetCallbacks(Rhino.DocObjects.Custom.CustomGripObject.CRhinoObjectDestructorCallback destructorFunc,
    Rhino.DocObjects.Custom.CustomGripObject.CRhinoGripObjectWeightCallback getweightFunc,
    Rhino.DocObjects.Custom.CustomGripObject.CRhinoGripObjectSetWeightCallback setweightFunc);

  #region RDK Functions
  //int Rdk_Globals_ShowColorPicker(HWND hWnd, ON_4FVECTOR_STRUCT v, bool bUseAlpha, ON_4fPoint* pColor)
  // Z:\dev\github\mcneel\rhino\src4\DotNetSDK\rhinocommon\c_rdk\rdk_plugin.cpp line 1430
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern int Rdk_Globals_ShowColorPickerNewEx(
    IntPtr hWnd,
    Rhino.Display.Color4f vColor,
    [MarshalAs(UnmanagedType.U1)]bool bUseAlpha,
    ref Rhino.Display.Color4f pColor,
    Rhino.UI.Dialogs.OnColorChangedEvent colorCallback,
    IntPtr named_color_list);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern int Rdk_Globals_ShowColorPickerEx(
    IntPtr hWnd,
    Rhino.Display.Color4f vColor,
    [MarshalAs(UnmanagedType.U1)]bool bUseAlpha,
    ref Rhino.Display.Color4f pColor,
    IntPtr pointerToNamedArgs,
    Rhino.UI.Dialogs.OnColorChangedEvent colorCallback);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetTextureEvaluatorCallbacks(Rhino.Render.TextureEvaluator.GetColorCallback callbackFunc,
    Rhino.Render.TextureEvaluator.OnDeleteThisCallback ondeletethisCallback, Rhino.Render.TextureEvaluator.InitializeCallback initCallbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetNewTextureCallback(Rhino.Render.RenderContent.NewRenderContentCallbackEvent callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetNewMaterialCallback(Rhino.Render.RenderContent.NewRenderContentCallbackEvent callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetNewEnvironmentCallback(Rhino.Render.RenderContent.NewRenderContentCallbackEvent callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetNewTextureEvaluatorCallback(Rhino.Render.RenderTexture.GetNewTextureEvaluatorCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSimulateTextureCallback(Rhino.Render.RenderTexture.SimulateTextureCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RdkSetTextureGetVirtualIntCallback(Rhino.Render.RenderTexture.GetVirtualIntCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RdkSetTextureSetVirtualIntCallback(Rhino.Render.RenderTexture.SetVirtualIntCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RdkSetTextureGetVirtualVector3dCallback(Rhino.Render.RenderTexture.GetVirtual3DVectorCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RdkSetTextureSetVirtualVector3dCallback(Rhino.Render.RenderTexture.SetVirtual3DVectorCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSimulateMaterialCallback(Rhino.Render.RenderMaterial.SimulateMaterialCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSimulateEnvironmentCallback(Rhino.Render.RenderEnvironment.SimulateEnvironmentCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentIoDeleteThisCallback(Rhino.Render.RenderContentSerializer.DeleteThisCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentIoLoadMultipleCallback(Rhino.Render.RenderContentSerializer.LoadMultipleCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentIoLoadMultipleSupportCallback(Rhino.Render.RenderContentSerializer.LoadMultipleSupportedCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentIoLoadCallback(Rhino.Render.RenderContentSerializer.LoadCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentIoSaveCallback(Rhino.Render.RenderContentSerializer.SaveCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentIoStringCallback(Rhino.Render.RenderContentSerializer.GetRenderContentIoStringCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetTextureChildSlotNameCallback(Rhino.Render.RenderMaterial.TextureChildSlotNameCallback callbackFunc);

#pragma warning disable 612
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetCallback_CRMProvider_DeleteThis(Rhino.Render.CustomRenderMeshProvider.CrmProviderDeleteThisCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetCallback_CRMProvider_WillBuild(Rhino.Render.CustomRenderMeshProvider.CrmProviderWillBuildCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetCallback_CRMProvider_BBox(Rhino.Render.CustomRenderMeshProvider.CrmProviderBBoxCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetCallback_CRMProvider_Build(Rhino.Render.CustomRenderMeshProvider.CrmProviderBuildCallback callbackFunc);
#pragma warning restore 612

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_IMeshProvider_SetCallbacks(
    Rhino.Render.CustomRenderMeshes.RenderMeshProvider.RenderMeshProvider_OnDelete_Callback delete_func,
    Rhino.Render.CustomRenderMeshes.RenderMeshProvider.RenderMeshProvider_HasCustomRenderMeshes_Callback hasmeshes_func,
    Rhino.Render.CustomRenderMeshes.RenderMeshProvider.RenderMeshProvider_RenderMeshes_Callback meshes_func,
    Rhino.Render.CustomRenderMeshes.RenderMeshProvider.RenderMeshProvider_NonObjectIds_Callback nonobjectids_func,
    Rhino.Render.CustomRenderMeshes.RenderMeshProvider.RenderMeshProvider_Progress_Callback progress_func
    );

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetSunChangedEventCallback(Rhino.Render.Sun.RdkSunSettingsChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetUndoRedoEventCallback(Rhino.Render.UndoRedo.RdkUndoRedoCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetOnAddCustomUISectionsEventCallback(Rhino.Render.AddCustomUISections.RdkAddCustomUISectionsEventCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);


  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetUndoRedoEndedEventCallback(Rhino.Render.UndoRedo.RdkUndoRedoEndedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetGroundPlaneChangedEventCallback(Rhino.Render.GroundPlane.RdkGroundPlaneSettingsChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetRenderChannelsChangedEventCallback(Rhino.Render.RenderChannels.RdkRenderChannelsSettingsChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetSafeFrameChangedEventCallback(Rhino.Render.SafeFrame.RdkSafeFrameChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetSkylightChangedEventCallback(Rhino.Render.Skylight.RdkSkylightSettingsChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetOnRenderImageEventCallback(Rhino.Render.ImageFile.OnRenderImageCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCallback);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetOnRenderWindowClonedEventCallback(Rhino.Render.RenderWindow.ClonedEventCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCallback);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentListClearingEventCallback(Rhino.Render.RenderContentTableEventForwarder.ContentListClearingCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentListClearedEventCallback(Rhino.Render.RenderContentTableEventForwarder.ContentListClearedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentListLoadedEventCallback(Rhino.Render.RenderContentTableEventForwarder.ContentListLoadedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentOnCppDtorCallback(Rhino.Render.RenderContent.RenderContentOnCppDtorCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentBitFlagsCallback(Rhino.Render.RenderContent.RenderContentBitFlagsCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetContentStringCallback(Rhino.Render.RenderContent.GetRenderContentStringCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetAddUISectionsCallback(Rhino.Render.RenderContent.AddUiSectionsCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetDefaultsFromUserCallback(Rhino.Render.RenderContent.GetDefaultsFromUserCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_IsFactoryProductAcceptableAsChildCallback(Rhino.Render.RenderContent.IsFactoryProductAcceptableAsChildCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetIsContentTypeAcceptableAsChildCallback(Rhino.Render.RenderContent.IsContentTypeAcceptableAsChildCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentRenderCrcCallback(Rhino.Render.RenderContent.RenderCrcCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSetParameterCallback(Rhino.Render.RenderContent.SetParameterCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetEmbeddedFilesCallback(Rhino.Render.RenderContent.SetEmbeddedFilesCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetMetersToUnitsCallback(Rhino.Render.RenderContent.SetMetersToUnitsCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetUnitsToMetersCallback(Rhino.Render.RenderContent.SetUnitsToMetersCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetOnContentFieldChangeCallback(Rhino.Render.RenderContent.OnContentFieldChangedCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetParameterCallback(Rhino.Render.RenderContent.GetParameterCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetExtraRequirementParameterCallback(Rhino.Render.RenderContent.GetExtraRequirementParameterCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSetExtraRequirementParameterCallback(Rhino.Render.RenderContent.SetExtraRequirementParameterCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetFilenameCallback(Rhino.Render.RenderContent.SetGetFilenameCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSetFilenameCallback(Rhino.Render.RenderContent.SetSetFilenameCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetContentIconCallback(Rhino.Render.RenderContent.SetContentIconCallback callbackFunction);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetOnMakeCopyCallback(Rhino.Render.RenderContent.OnMakeCopyCallback callbackFunction);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetHarvestDataCallback(Rhino.Render.RenderContent.HarvestDataCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetShaderCallback(Rhino.Render.RenderContent.GetShaderCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetRenderContentGetUiHashCallback(Rhino.Render.RenderContent.RenderContentUiHashCallback callbackFunc);

  //Rhino.Render.UI.UserInterfaceSection is obsolete
#pragma warning disable 618
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_ContentUiSectionSetCallbacks(
                  Rhino.Render.UI.UserInterfaceSection.SerialNumberCallback deleteThisCallback,
                  Rhino.Render.UI.UserInterfaceSection.SerialNumberCallback displayDataCallback,
                  Rhino.Render.UI.UserInterfaceSection.SerialNumberBoolCallback onExpandCallback,
                  Rhino.Render.UI.UserInterfaceSection.SerialNumberCallback isHiddenCallback);
#pragma warning restore 618

  // UiSection
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RhinoUiSectionImpl_SetCallbacks(
           Rhino.UI.Controls.Delegates.MOVEPROC move,
           Rhino.RDK.Delegates.SET_BOOL_PROC show,
           Rhino.RDK.Delegates.VOID_PROC deletethis,
           Rhino.RDK.Delegates.GET_BOOL_PROC isShown,
           Rhino.RDK.Delegates.GET_BOOL_PROC isEnabled,
           Rhino.RDK.Delegates.SET_BOOL_PROC enable,
           Rhino.RDK.Delegates.GET_INT_PROC getHeight,
           Rhino.RDK.Delegates.GET_INT_PROC getInitialState,
           Rhino.RDK.Delegates.GET_BOOL_PROC isHidden,
           Rhino.RDK.Delegates.FACTORY_PROC getSection,
           Rhino.RDK.Delegates.SET_INTPTR_PROC setParent,
           Rhino.RDK.Delegates.GET_GUID_PROC id,
           Rhino.RDK.Delegates.GET_STRING_PROC englishCaption,
           Rhino.RDK.Delegates.GET_STRING_PROC localCaption,
           Rhino.RDK.Delegates.GET_BOOL_PROC collapsible,
           Rhino.RDK.Delegates.GET_INT_PROC backgroundColor,
           Rhino.RDK.Delegates.SET_INT_PROC setBackgroundColor,
           Rhino.RDK.Delegates.GET_INT_PTR_PROC getWindowPtr,
           Rhino.RDK.Delegates.SET_GUID_PROC onEvent,
           Rhino.RDK.Delegates.VOID_PROC onViewModelActivatedEvent,
           Rhino.RDK.Delegates.GET_GUID_PROC PlugInId,
           Rhino.RDK.Delegates.GET_STRING_PROC CommandOptName,
           Rhino.RDK.Delegates.GET_INT_PROC RunScript,
           Rhino.RDK.Delegates.GET_GUID_PROC cid,
           Rhino.RDK.Delegates.ON_ATTACHED_TO_HOLDER_PROC onattachedtoholderproc,
           Rhino.RDK.Delegates.ON_DETACHED_FROM_HOLDER_PROC ondetachedfromholderproc,
           Rhino.RDK.Delegates.UPDATE_VIEW_PROC updateviewproc);

  // UiSection
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RhinoUiViewModelImpl_SetCallbacks(
           Rhino.RDK.Delegates.FACTORY_PROC getViewModel,
           Rhino.RDK.Delegates.SET_GUID_EVENT_INFO_PROC onEvent,
           Rhino.RDK.Delegates.VOID_PROC deleteThis,
           Rhino.RDK.Delegates.SET_INTPTR_PROC requireddatasources);

  // GenericController
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RhinoGenericController_SetCallbacks(Rhino.RDK.Delegates.SET_GUID_EVENT_INFO_PROC onEvent,
           Rhino.RDK.Delegates.SET_INTPTR_PROC requireddatasources);

  // UiWithController
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_UiWithController_SetCallbacks(Rhino.RDK.Delegates.VOID_PROC deletethis,
           Rhino.RDK.Delegates.SET_INTPTR_PROC setcontroller,
           Rhino.RDK.Delegates.SET_GUID_PROC onevent,
           Rhino.RDK.Delegates.GET_GUID_PROC controllerid);
  // RdkMenu
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_Menu_SetCallbacks(Rhino.RDK.Delegates.ADD_SUBMENU_PROC addsubmenu,
           Rhino.RDK.Delegates.ADD_ITEM_PROC additem,
           Rhino.RDK.Delegates.ADD_SEPARATOR_PROC addseparator);

  // RdkDataSource
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_DataSource_SetCallbacks(Rhino.RDK.Delegates.SUPPORTED_UUID_DATA_PROC supporteduuiddata);

  // RdkColorDataSource
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_ColorDataSource_SetCallbacks(Rhino.RDK.Delegates.GET_COLOR_PROC getcolor,
           Rhino.RDK.Delegates.SET_COLOR_PROC setcolor,
           Rhino.RDK.Delegates.USES_ALPHA_PROC usesalpha);

  // UiDynamicAcces
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_UiDynamicAccess_SetCallbacks(
    Rhino.RDK.Delegates.ADD_SECTIONS_PRE_PROC addsectionspre,
    Rhino.RDK.Delegates.SET_INTPTR_PROC addsectionspost,
    Rhino.RDK.Delegates.NEW_CONTENT_CREATOR_USING_TYPE_BROWSER_PROC newcontentcreatorusingtypebrowser,
    Rhino.RDK.Delegates.CONTENT_EDITOR_TAB_ID_PROC contenteditortabid,
    Rhino.RDK.Delegates.SET_INTPTR_PROC addtextureadjustmentsection,
    Rhino.RDK.Delegates.SET_INTPTR_PROC addtwocolorsection,
    Rhino.RDK.Delegates.SET_INTPTR_PROC addlocalmapping2dsection,
    Rhino.RDK.Delegates.SET_INTPTR_PROC addlocalmapping3dsection,
    Rhino.RDK.Delegates.SET_INTPTR_PROC addgraphsection,
    Rhino.RDK.Delegates.OPEN_FLOATING_CONTENT_EDITOR_DIALOG_PROC OpenFloatingContentEditorDialog,
    Rhino.RDK.Delegates.GET_TAGS_WINDOW_PROC gettagswindow,
    Rhino.RDK.Delegates.SHOW_DECAL_MAPPING_STYLE_DLG_PROC ShowDecalMappingStyleDialog,
    Rhino.RDK.Delegates.SHOW_MODAL_SUN_DIALOG_PROC showmodalsundialog,
    Rhino.RDK.Delegates.SHOW_MODAL_CONTENT_EDITOR_DIALOG_PROC showmodalcontenteditordialog,
    Rhino.RDK.Delegates.SHOW_CHOOSE_LAYERS_DLG_PROC showchooselayersdlg,
    Rhino.RDK.Delegates.NEW_RENDER_FRAME_PROC newrenderframe,
    Rhino.RDK.Delegates.SHOW_TT_MAPPING_MESH_EDITOR_DOCKBAR showttmappingmesheditordockbar,
    Rhino.RDK.Delegates.UPDATE_TT_MAPPING_MESH_EDITOR_DOCKBAR updatettmappingmesheditordockbar,
    Rhino.RDK.Delegates.SHOW_RENDER_OPEN_FILE_DLG_PROC ShowRenderOpenFileDlg,
    Rhino.RDK.Delegates.SHOW_RENDER_SAVE_FILE_DLG_PROC ShowRenderSaveFileDlg,
    Rhino.RDK.Delegates.SHOW_CONTENT_TYPE_BROWSER_PROC ShowContentTypeBrowser,
    Rhino.RDK.Delegates.PROMPT_FOR_IMAGE_FILE_PARAMS_PROC PromptForImageFileParams,
    Rhino.RDK.Delegates.SHOW_NAMED_ITEM_EDIT_DLG_PROC ShowNamedItemEditDlg,
    Rhino.RDK.Delegates.SHOW_SMART_MERGE_NAME_COLLISION_DLG_PROC ShowSmartMergeNameCollisionDlg,
    Rhino.RDK.Delegates.SHOW_PREVIEW_PROPERTIES_DLG_PROC ShowPreviewPropertiesDlg,
    Rhino.RDK.Delegates.CHOOSE_CONTENT_PROC ChooseContent,
    Rhino.RDK.Delegates.PEP_PICK_POINT_ON_IMAGE_PROC PepPickPointOnImage,
    Rhino.RDK.Delegates.SHOW_LAYER_MATERIAL_DIALOG_PROC ShowLayerMaterialDialog,
    Rhino.RDK.Delegates.PROMPT_FOR_IMAGE_DRAG_OPTIONS_DLG_PROC PromptForImageDragOptionsDlg,
    Rhino.RDK.Delegates.PROMPT_FOR_OPEN_ACTIONS_DLG_PROC PromptForOpenActionsDlg,
    Rhino.RDK.Delegates.DISPLAY_MISSING_IMAGES_DIALOG DisplayMissingImagesDialog,
    Rhino.RDK.Delegates.OPEN_NAMED_VIEW_ANIMATION_SETTINGS_DLG_PROC OpenNamedViewAnimationSettingsDlg,
    Rhino.RDK.Delegates.PEP_PICK_RECTANGLE_ON_IMAGE_PROC PepPickRectangleOnImage,
    Rhino.RDK.Delegates.SHOW_CONTENT_CTRL_PROP_DLG_PROC ShowContentCtrlPropDlg,
    Rhino.RDK.Delegates.CREATE_IN_PLACE_RENDER_VIEW_PROC CreateInPlaceRenderView,
    Rhino.RDK.Delegates.ADD_CONTENT_AUTOMATIC_UI_SECTION_PROC AddContentAutomaticUISection,
    Rhino.RDK.Delegates.SHOW_NAMED_VIEW_PROPERTIES_DLG ShowNamedItemPropertiesDlg,
    Rhino.RDK.Delegates.ON_PLUGIN_LOADED_PROC OnPlugInLoaded,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsFog,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionGlow,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsGlare,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsBloom,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsDOF,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsGamma,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsNoise,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsGaussianBlur,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsToneMappingNone,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsToneMappingBlackWhitePoint,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsToneMappingLogarithmic,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsToneMappingFilmic,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsToneMappingFilmicAdvanced,
    Rhino.RDK.Delegates.NEW_RENDER_SETTING_PAGE_PROC NewRenderSettingsPage,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsDithering,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsWatermark,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsHueSatLum,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsBriCon,
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC AddPostEffectUISectionsMultiplier,
    Rhino.RDK.Delegates.PEP_RENDER_SETTINGS_PAGE_PROC AttachRenderPostEffectsPage,
    Rhino.RDK.Delegates.NEW_RENDER_CHANNELS_SECTION_PROC NewRenderChannelsSection,
    Rhino.RDK.Delegates.CREATE_PBR_FROM_FILES_PROC CreatePBRFromFiles,
    Rhino.RDK.Delegates.ADD_TEXTURE_SUMMARY_SECTION_PROC AddTextureSummarySection,
    Rhino.RDK.Delegates.SHOW_UNPACK_EMBEDDED_FILES_DIALOG_PROC ShowUnpackEmbeddedFilesDialog,
    Rhino.RDK.Delegates.NEW_AUTOMATIC_SECTION_PROC NewAutomaticSection,
    Rhino.RDK.Delegates.NEW_PLAIN_AUTOMATIC_HOLDER_PROC NewPlainAutomaticHolder,
    Rhino.RDK.Delegates.ASK_USER_FOR_CHILD_SLOT_PROC AskUserForChildSlot
  );

  // PostEffect
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnPostEffect_SetCallbacks(
    Rhino.RDK.Delegates.CAN_EXECUTE_PROC canexecute,
    Rhino.RDK.Delegates.REQUIRED_CHANNELS_PROC requiredchannels,
    Rhino.RDK.Delegates.READ_FROM_DOCUMENT_DEFAULTS_PROC readfromdocumentdefaults,
    Rhino.RDK.Delegates.WRITE_TO_DOCUMENT_DEFAULTS_PROC writetodocumentdefaults,
    Rhino.RDK.Delegates.EXECUTE_PROC execute,
    Rhino.RDK.Delegates.GET_PARAM_PROC getparam,
    Rhino.RDK.Delegates.SET_PARAM_PROC setparam,
    Rhino.RDK.Delegates.READ_STATE_PROC readstate,
    Rhino.RDK.Delegates.WRITE_STATE_PROC writestate,
    Rhino.RDK.Delegates.RESET_TO_FACTORY_DEFAULTS_PROC resettofactorydefaults,
    Rhino.RDK.Delegates.ADD_UI_SECTIONS_PROC adduiseections,
    Rhino.RDK.Delegates.DISPLAY_HELP_PROC displayhelp,
    Rhino.RDK.Delegates.DELETE_THIS_PROC deleteThis
  );

  // CmnPostEffectFactory
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnPostEffectFactory_SetCallbacks(
    Rhino.RDK.Delegates.NEW_POST_EFFECT_PROC newposteffect,
    Rhino.RDK.Delegates.PEP_UUID_PROC pluginid
  );

  // CmnPostEffectJob
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnPostEffectJob_SetCallbacks(
    Rhino.RDK.Delegates.CLONE_POST_EFFECT_JOB_PROC clone,
    Rhino.RDK.Delegates.DELETE_THIS_POST_EFFECT_JOB delete,
    Rhino.RDK.Delegates.EXECUTE_POST_EFFECT_JOB execute
  );

  //CmnTask
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_Task_SetCallbacks(
  Rhino.RDK.Delegates.REQUIRED_DATA_SOURCES_UI_COMMAND_PROC RequiredDataSources,
  Rhino.RDK.Delegates.RENDER_ENGINE_ID_UI_COMMAND_PROC PluginId,
  Rhino.RDK.Delegates.RENDER_ENGINE_ID_UI_COMMAND_PROC Id,
  Rhino.RDK.Delegates.RENDER_ENGINE_ID_UI_COMMAND_PROC RenderEngineId,
  Rhino.RDK.Delegates.IS_ENABLED_UI_COMMAND_PROC IsEnabled,
  Rhino.RDK.Delegates.SUPPORTS_SANDBOX_UI_COMMAND_PROC SupportsSandbox,
  Rhino.RDK.Delegates.QUERY_UI_COMMAND_PROC Query,
  Rhino.RDK.Delegates.USER_UPDATE_UI_COMMAND_PROC UserUpdate,
  Rhino.RDK.Delegates.USER_EXECUTE_UI_COMMAND_PROC UserExecute,
  Rhino.RDK.Delegates.MENU_ORDER_UI_COMMAND_PROC MenuOrder,
  Rhino.RDK.Delegates.MENU_SEPARATORS_UI_COMMAND_PROC MenuSeparators,
  Rhino.RDK.Delegates.MENU_STRING_UI_COMMAND_PROC MenuString,
  Rhino.RDK.Delegates.ICON_OUT_UI_COMMAND_PROC IconOut,
  Rhino.RDK.Delegates.ICON_OUT_UI_COMMAND_PROC IconIn,
  Rhino.RDK.Delegates.SET_ERROR_UI_COMMAND_PROC SetError,
  Rhino.RDK.Delegates.SUB_MENUS_UI_COMMAND_PROC SubMenu,
  Rhino.RDK.Delegates.IS_FOR_TOP_LEVEL_CONTENT_UI_COMMAND_PROC IsForTopLevelContent,
  Rhino.RDK.Delegates.USER_EXECUTE_UI_COMMAND_PROC Execute,
  Rhino.RDK.Delegates.USER_UPDATE_UI_COMMAND_PROC Update,
  Rhino.RDK.Delegates.GET_SURE_MESSAGE_UI_COMMAND_PROC GetSureMessage,
  Rhino.RDK.Delegates.GET_UNDO_STRING_UI_COMMAND_PROC GetUndoString,
  Rhino.RDK.Delegates.SET_SELECTION_UI_COMMAND_PROC SetSelection,
  Rhino.RDK.Delegates.ERROR_UI_COMMAND_PROC Error,
  Rhino.RDK.Delegates.GET_UNDO_STRING_UI_COMMAND_PROC FullUndoString
  );

  // FloatingPreviewManager
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_FloatingPreviewManager_SetCallbacks(
    Rhino.RDK.Delegates.GET_GUID_INTPTR_INT_INT_PROC openfloatingpreview,
    Rhino.RDK.Delegates.GET_GUID_INTPTR_INT_INT_PROC openfloatingpreviewatmouse,
    Rhino.RDK.Delegates.SET_GUID_INT_INT_BOOL_PROC resizefloatingpreview,
    Rhino.RDK.Delegates.SET_GUID_BOOL_PROC closefloatingpreview
  );

  // ContentUIAgent
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_ContentUIAgent_SetCallbacks(
    Rhino.RDK.Delegates.SET_INTPTR_PROC adduisections_proc,
    Rhino.RDK.Delegates.GET_GUID_PROC contenttypeid_proc
  );

  // SnapShotsClient
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhinoSnapShotsClient_SetCallbacks(
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETSTRINGPROC category,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETSTRINGPROC name,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLPROC supportsdocument,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCBUFFERPROC savedocument,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCBUFFERPROC restoredocument,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLPROC supportsobjects,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLOBJECTPROC supportsobject,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCOBJECTTRANSFORMBUFFERPROC saveobject,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCOBJECTTRANSFORMBUFFERPROC restoreobject,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLPROC supportsanimation,
    Rhino.DocObjects.SnapShots.SnapShotsClient.SETINTINTPROC animationstart,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCBUFFERBUFFERPROC preparefordocumentanimation,
    Rhino.DocObjects.SnapShots.SnapShotsClient.SETDOCBUFFERBUFFERBBOXPROC extendboundingboxfordocumentanimation,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETDOCDOUBLEBUFFERBUFFERPROC animatedocument,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCOBJTRANSFORMBUFFERBUFFERPROC prepareforobjectanimation,
    Rhino.DocObjects.SnapShots.SnapShotsClient.SETBOOLDOCOBJTRANSFORMBUFFERBUFFERBBOXPROC extendboundingboxforobjectanimation,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCOBJTRANSFORMDOUBLEBUFFERBUFFERPROC animateobject,
    Rhino.DocObjects.SnapShots.SnapShotsClient.SETINTINT animationstop,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCOBJECTTRANSFORMBUFFERPROC objecttransformnotification,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETGUIDPROC pluginid,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETGUIDPROC clientid,
    Rhino.DocObjects.SnapShots.SnapShotsClient.SETINTINT snapshotrestored,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCBUFFERBUFFERARRAYTEXTLOGPROC iscurrentmodelstateinanysnapshot,
    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCOBJBUFFERBUFFERARRAYTEXTLOGPROC iscurrentobjmodelstateinanysnapshot
  );

  // ThumbnailList
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_UiHolder_SetCallbacks(
      Rhino.UI.Controls.Delegates.MOVEPROC m,
      Rhino.RDK.Delegates.SET_BOOL_PROC s,
      Rhino.RDK.Delegates.VOID_PROC dt,
      Rhino.RDK.Delegates.GET_BOOL_PROC ish,
      Rhino.RDK.Delegates.GET_BOOL_PROC ien,
      Rhino.RDK.Delegates.SET_BOOL_PROC e,
      Rhino.RDK.Delegates.FLAGS_PROC u,
      Rhino.UI.Controls.CollapsibleSectionHolderImpl.ATTACHSECTIONPROC a,
      Rhino.UI.Controls.CollapsibleSectionHolderImpl.ATTACHSECTIONPROC de,
      Rhino.RDK.Delegates.GET_INT_PROC sc,
      Rhino.RDK.Delegates.AT_INDEX_PROC sa,
      Rhino.RDK.Delegates.SET_INTPTR_PROC sp);


  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_Thumbnaillist_SetCallbacks(
      Rhino.UI.Controls.Delegates.MOVEPROC m,
      Rhino.RDK.Delegates.SET_BOOL_PROC s,
      Rhino.RDK.Delegates.VOID_PROC dt,
      Rhino.RDK.Delegates.GET_BOOL_PROC ish,
      Rhino.RDK.Delegates.GET_BOOL_PROC ien,
      Rhino.RDK.Delegates.SET_BOOL_PROC e,
      Rhino.RDK.Delegates.SET_INTPTR_PROC sp,
      Rhino.RDK.Delegates.GET_GUID_PROC id,
      Rhino.RDK.Delegates.SET_MODE_PROC a,
      Rhino.RDK.Delegates.SET_INTPTR_PROC sc,
      Rhino.RDK.Delegates.SET_GUID_PROC sid,
      Rhino.RDK.Delegates.VOID_SET_SELECTION_PROC ssp,
      Rhino.RDK.Delegates.GET_BOOL_PROC gm,
      Rhino.RDK.Delegates.GET_INT_PTR_PROC getWindowPtr,
      Rhino.RDK.Delegates.GET_BOOL_PROC ic,
      Rhino.RDK.Delegates.SET_BOOL_PROC su,
      Rhino.RDK.Delegates.GET_BOOL_PROC gu,
      Rhino.RDK.Delegates.SET_INTPTR_PROC addThumb,
      Rhino.RDK.Delegates.GET_GUID_PROC cid);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_BreadCrumb_SetCallbacks(
    Rhino.UI.Controls.Delegates.MOVEPROC m,
    Rhino.RDK.Delegates.SET_BOOL_PROC s,
    Rhino.RDK.Delegates.VOID_PROC dt,
    Rhino.RDK.Delegates.GET_BOOL_PROC ish,
    Rhino.RDK.Delegates.GET_BOOL_PROC ien,
    Rhino.RDK.Delegates.SET_BOOL_PROC e,
    Rhino.RDK.Delegates.SET_INTPTR_PROC sp,
    Rhino.RDK.Delegates.GET_GUID_PROC id,
    Rhino.RDK.Delegates.SET_INTPTR_PROC sc,
    Rhino.RDK.Delegates.SET_GUID_PROC sid,
    Rhino.RDK.Delegates.GET_INT_PTR_PROC getWindowPtr,
    Rhino.RDK.Delegates.GET_BOOL_PROC ic,
    Rhino.RDK.Delegates.GET_GUID_PROC cid);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_NewContentCtrl_SetCallbacks(
    Rhino.UI.Controls.Delegates.MOVEPROC m,
    Rhino.RDK.Delegates.SET_BOOL_PROC s,
    Rhino.RDK.Delegates.VOID_PROC dt,
    Rhino.RDK.Delegates.GET_BOOL_PROC ish,
    Rhino.RDK.Delegates.GET_BOOL_PROC ien,
    Rhino.RDK.Delegates.SET_BOOL_PROC e,
    Rhino.RDK.Delegates.SET_INTPTR_PROC sp,
    Rhino.RDK.Delegates.GET_GUID_PROC id,
    Rhino.RDK.Delegates.SET_INTPTR_PROC sc,
    Rhino.RDK.Delegates.SET_GUID_PROC sid,
    Rhino.RDK.Delegates.GET_INT_PTR_PROC getWindowPtr,
    Rhino.RDK.Delegates.GET_BOOL_PROC ic,
    Rhino.RDK.Delegates.GET_GUID_PROC cid,
    Rhino.RDK.Delegates.SET_SELECTION_PROC ssp);

  // LightManagerSupport
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_LightManagerSupport_SetCallbacks(
    Rhino.RDK.Delegates.GET_GUID_PROC RenderEngineId,
    Rhino.RDK.Delegates.GET_GUID_PROC PluginId,
    Rhino.Render.LightManagerSupport.MODIFYLIGHTPROC ModifyLight,
    Rhino.Render.LightManagerSupport.DELETELIGHTPROC DeleteLight,
    Rhino.Render.LightManagerSupport.GETLIGHTSPROC GetLights,
    Rhino.Render.LightManagerSupport.LIGHTFROMIDPROC LightFromId,
    Rhino.Render.LightManagerSupport.OBJECTSERIALNUMBERFROMLIGHTPROC ObjectSerialNumberFromLight,
    Rhino.Render.LightManagerSupport.ONEDITLIGHTPROC OnEditLight,
    Rhino.Render.LightManagerSupport.GROUPLIGHTSPROC GroupLights,
    Rhino.Render.LightManagerSupport.UNGROUPPROC UnGroup,
    Rhino.Render.LightManagerSupport.LIGHTDESCRIPTIONPROC LightDescription,
    Rhino.Render.LightManagerSupport.SETLIGHTSOLO SetLightSolo,
    Rhino.Render.LightManagerSupport.GETLIGHTSOLO GeTLightSolo,
    Rhino.Render.LightManagerSupport.LIGHTSINSOLOSTORAGE LightInSoloStorage
  );

  // ChangeQueue >>
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_ChangeQueue_SetCallbacks(
    Rhino.Render.ChangeQueue.ChangeQueue.ViewCallback viewcb,
    Rhino.Render.ChangeQueue.ChangeQueue.MeshChangesCallback meshcb,
    Rhino.Render.ChangeQueue.ChangeQueue.MaterialChangesCallback matcb,
    Rhino.Render.ChangeQueue.ChangeQueue.BeginUpdatesCallback beginnotifcb,
    Rhino.Render.ChangeQueue.ChangeQueue.EndUpdatesCallback endnotifcb,
    Rhino.Render.ChangeQueue.ChangeQueue.DynamicUpdatesAreAvailableCallback dynupdatesavailablecb,
    Rhino.Render.ChangeQueue.ChangeQueue.BakeForCallback bakeforcb,
    Rhino.Render.ChangeQueue.ChangeQueue.BakingSizeCallback bakingsizecb,
    Rhino.Render.ChangeQueue.ChangeQueue.ApplyDisplayPipelineAttributesChangesCallback displayattrcb,
    Rhino.Render.ChangeQueue.ChangeQueue.MeshInstanceChangesCallback meshinstancecb,
    Rhino.Render.ChangeQueue.ChangeQueue.GroundplaneChangesCallback gpcb,
    Rhino.Render.ChangeQueue.ChangeQueue.DynamicObjectChangesCallback dynobjchangescb,
    Rhino.Render.ChangeQueue.ChangeQueue.LightChangesCallback lightcb,
    Rhino.Render.ChangeQueue.ChangeQueue.DynamicLightChangesCallback dynlightscb,
    Rhino.Render.ChangeQueue.ChangeQueue.SunChangesCallback suncb,
    Rhino.Render.ChangeQueue.ChangeQueue.SkylightChangesCallback skylightcb,
    Rhino.Render.ChangeQueue.ChangeQueue.EnvironmentChangesCallback envcb,
    Rhino.Render.ChangeQueue.ChangeQueue.RenderSettingsChangesCallback bgcb,
    Rhino.Render.ChangeQueue.ChangeQueue.LinearWorkflowChangesCallback lwfcb,
    Rhino.Render.ChangeQueue.ChangeQueue.ProvideOriginalObjectCallback origcb,
    Rhino.Render.ChangeQueue.ChangeQueue.ClippingPlaneChangesCallback clippingcb,
    Rhino.Render.ChangeQueue.ChangeQueue.DynamicClippingPlaneChangesCallback dynclippingcb,
    Rhino.Render.ChangeQueue.ChangeQueue.ContentRenderHashCallback renderContentHash
    );
  // << ChangeQueue

  // >> RenderedDisplayMode
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RtVpImpl_SetCallbacks(Rhino.Render.RealtimeDisplayMode.CreateWorldCallback createWorld,
    Rhino.Render.RealtimeDisplayMode.StartRenderCallback startRender,
    Rhino.Render.RealtimeDisplayMode.RestartRenderCallback restartRender,
    Rhino.Render.RealtimeDisplayMode.ShutdownRenderCallback shutdownRender,
    Rhino.Render.RealtimeDisplayMode.LastRenderedPassCallback lastRenderedPass,
    Rhino.Render.RealtimeDisplayMode.IsRendererStartedCallback isRendererStarted,
    Rhino.Render.RealtimeDisplayMode.IsRenderframeAvailableCallback isRenderframeAvailable,
    Rhino.Render.RealtimeDisplayMode.IsFrameBufferAvailableCallback isFramebufferAvailable,
    Rhino.Render.RealtimeDisplayMode.ShowCaptureProgressCallback showCaptureProgress,
    Rhino.Render.RealtimeDisplayMode.CaptureProgressCallback captureProgress,

    Rhino.Render.RealtimeDisplayMode.OnDisplayPipelineSettingsChangedCallback onDisplayPipelineSettingsChanged,
    Rhino.Render.RealtimeDisplayMode.OnDrawMiddlegroundCallback onDrawMiddleGround,
    Rhino.Render.RealtimeDisplayMode.OnInitFramebufferCallback onInitFramebuffer,

    Rhino.Render.RealtimeDisplayMode.DrawOpenGlCallback drawOpenGl,
    Rhino.Render.RealtimeDisplayMode.UseFastDrawCallback useFastDraw,

    Rhino.Render.RealtimeDisplayMode.HudProductNameCallback hudProductName,
    Rhino.Render.RealtimeDisplayMode.HudCustomStatusTextCallback hudCustomStatusText,
    Rhino.Render.RealtimeDisplayMode.HudMaximumPassesCallback hudMaximumPasses,
    Rhino.Render.RealtimeDisplayMode.HudLastRenderedPassCallback hudLastRenderedPass,
    Rhino.Render.RealtimeDisplayMode.HudRendererPausedCallback hudRendererPaused,
    Rhino.Render.RealtimeDisplayMode.HudRendererLockedCallback hudRendererLocked,
    Rhino.Render.RealtimeDisplayMode.HudShowMaxPassesCallback hudShowMaxPasses,
    Rhino.Render.RealtimeDisplayMode.HudShowPassesCallback hudShowPasses,
    Rhino.Render.RealtimeDisplayMode.HudShowCustomStatusTextCallback hudShowCustomStatusText,
    Rhino.Render.RealtimeDisplayMode.HudShowControlsCallback hudShowControls,
    Rhino.Render.RealtimeDisplayMode.HudShowCallback hudShow,
    Rhino.Render.RealtimeDisplayMode.HudStartTimeCallback hudStartTime,
    Rhino.Render.RealtimeDisplayMode.HudStartTimeMSCallback hudStartTimeMS,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudPlayButtonPressed,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudPauseButtonPressed,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudLockButtonPressed,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudUnlockButtonPressed,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudProductNamePressed,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudStatusTextPressed,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudTimePressed,
    Rhino.Render.RealtimeDisplayMode.HudMaxPassesChanged hudMaxPassesChanged,
    Rhino.Render.RealtimeDisplayMode.HudAllowEditMaxPassesCallback hudAllowEditMaxPasses,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudPostEffectsOnButtonPressed,
    Rhino.Render.RealtimeDisplayMode.HudButtonPressed hudPostEffectsOffnButtonPressed
    );
  // << RenderedDisplayMode

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSunAndRenderSettingsCallbacks(Rhino.PlugIns.PlugIn.OnAddSectionsToSunPanelDelegate s,
                                                    Rhino.PlugIns.PlugIn.OnAddSectionsToRenderSettingsPanelDelegate rs);

  //Events
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetObjectMaterialAssignmentChangedEventCallback(Rhino.Render.RenderContentTableEventForwarder.MaterialAssigmentChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetLayerMaterialAssignmentChangedEventCallback(Rhino.Render.RenderContentTableEventForwarder.MaterialAssigmentChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentAddedEventCallback(Rhino.Render.RenderContent.ContentAddedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentRenamedEventCallback(Rhino.Render.RenderContent.ContentRenamedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentDeletingEventCallback(Rhino.Render.RenderContent.ContentDeletingCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentDeletedEventCallback(Rhino.Render.RenderContent.ContentDeletingCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentReplacingEventCallback(Rhino.Render.RenderContent.ContentReplacingCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentReplacedEventCallback(Rhino.Render.RenderContent.ContentReplacedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentChangedEventCallback(Rhino.Render.RenderContent.ContentChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentUpdatePreviewEventCallback(Rhino.Render.RenderContent.ContentUpdatePreviewCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetContentCurrencyChangedEventCallback(Rhino.Render.RenderContent.CurrentContentChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetNewRdkDocumentEventCallback(Rhino.RhinoApp.RhCmnOneUintCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetGlobalSettingsChangedEventCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetUpdateAllPreviewsEventCallback(Rhino.RhinoApp.RhCmnOneUintCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetCacheImageChangedEventCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetRendererChangedEventCallback(Rhino.RhinoApp.RhCmnEmptyCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetFactoryAddedEventCallback(Rhino.Render.RenderContent.ContentTypeAddedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetFactoryDeletingEventCallback(Rhino.Render.RenderContent.ContentTypeDeletingCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetFactoryDeletedEventCallback(Rhino.Render.RenderContent.ContentTypeDeletedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetClientPlugInUnloadingEventCallback(Rhino.RhinoApp.ClientPlugInUnloadingCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSdkRenderCallback(Rhino.Render.RenderPipeline.ReturnBoolGeneralCallback callbackFunc);

  // Docking Tabs in rh_utilities.cpp
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnRdkRenderPlugIn_RegisterRenderWindowExtraSidePane(
    RhRdkCustomUiType panelType,
    [MarshalAs(UnmanagedType.LPWStr)] string caption,
    Guid tabId,
    Guid pluginId,
    Guid renderEngineId,
    uint position,
    bool initialShow,
    bool alwaysShow,
    Rhino.Render.RenderPanels.ExtraSidePaneCallback_Create createProc,
    Rhino.Render.RenderPanels.ExtraSidePaneCallback_Visible visibleProc,
    Rhino.Render.RenderPanels.ExtraSidePaneCallback_Destroy destroyProc,
    Rhino.Render.RenderPanels.ExtraSidePaneCallback_SetController setcontrollerProc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetAsyncRenderContextCallbacks(
    Rhino.Render.AsyncRenderContext.DeleteThisCallback deleteThis,
    Rhino.Render.AsyncRenderContext.StopRenderingCallback stopRendering);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RenderFrame_SetCallbacks(
    Rhino.RDK.Delegates.VOID_PROC delete,
    Rhino.RDK.Delegates.BOOL_INT_INTPTR_INT_INT_PROC create,
    Rhino.RDK.Delegates.BOOL_INT_PROC destroy,
    Rhino.RDK.Delegates.VOID_INT_BOOL_PROC SetVisibility,
    Rhino.RDK.Delegates.BOOL_INT_PROC StartRendering,
    Rhino.RDK.Delegates.BOOL_INT_PROC IsRendering,
    Rhino.RDK.Delegates.VOID_PROC StopRendering,
    Rhino.RDK.Delegates.VOID_GET_INT_INT_INT_PROC Size,
    Rhino.RDK.Delegates.VOID_SET_INT_INT_INT_PROC SetImageSize,
    Rhino.RDK.Delegates.VOID_INT_BOOL_PROC Refresh,
    Rhino.RDK.Delegates.SAVE_RENDER_IMAGE_AS_PROC SaveAs,
    Rhino.RDK.Delegates.BOOL_INT_PROC CopyToClipboard,
    Rhino.RDK.Delegates.PICK_POINT_ON_IMAGE_PROC PickPoint,
    Rhino.RDK.Delegates.PICK_RECTANGLE_ON_IMAGE_PROC PickRectangle);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnRdkRenderPlugIn_RegisterRenderWindowMainSidePaneTab(
    RhRdkCustomUiType panelType,
    [MarshalAs(UnmanagedType.LPWStr)] string caption,
    Guid tabId,
    Guid pluginId,
    Guid renderEngineId,
    IntPtr icon,
    Rhino.Render.RenderTabs.MainSidePaneTabCallback_Create createProc,
    Rhino.Render.RenderTabs.MainSidePaneTabCallback_Visible visibleProc,
    Rhino.Render.RenderTabs.MainSidePaneTabCallback_Destroy destroyProc,
    Rhino.Render.RenderTabs.MainSidePaneTabCallback_DoHelp doHelpProc,
    Rhino.Render.RenderTabs.MainSidePaneTabCallback_DisplayData displaydataProc,
    Rhino.Render.RenderTabs.MainSidePaneTabCallback_TabActivated tabactivatedproc);

  #endregion

  // Docking Tabs in rh_utilities.cpp
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_RegisterTabbedDockBar(
    [MarshalAs(UnmanagedType.LPWStr)] string caption,
    Guid tabId,
    Guid plugInId,
    IntPtr image,
    bool hasDocContext,
    Rhino.UI.PanelSystem.CreatePanelCallback createProc,
    Rhino.UI.PanelSystem.StatePanelCallback visibleProc,
    Rhino.UI.PanelSystem.StatePanelCallback onShowPanelProc);
#endif

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnUserData_SetCallbacks(Rhino.DocObjects.Custom.UserData.TransformUserDataCallback xformFunc,
    Rhino.DocObjects.Custom.UserData.ArchiveUserDataCallback archiveFunc,
    Rhino.DocObjects.Custom.UserData.ReadWriteUserDataCallback readwriteFunc,
    Rhino.DocObjects.Custom.UserData.DuplicateUserDataCallback duplicateFunc,
    Rhino.DocObjects.Custom.UserData.CreateUserDataCallback createFunc,
    Rhino.DocObjects.Custom.UserData.DeleteUserDataCallback deleteFunc,
    Rhino.DocObjects.Custom.UserData.ChangeSerialNumberCallback changeserialnumberFunc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  internal static extern bool ON_RTree_Search(IntPtr pConstRtree, Point3d pt0, Point3d pt1, int serialNumber, RTree.SearchCallback searchCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  internal static extern bool ON_RTree_SearchSphere(IntPtr pConstRtree, Point3d center, double radius, int serialNumber, RTree.SearchCallback searchCallback);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  internal static extern bool ON_RTree_Search2(IntPtr pConstRtreeA, IntPtr pConstRtreeB, double tolerance, int serialNumber, RTree.SearchCallback searchCallback);

  //bool ON_Arc_Copy(ON_Arc* pRdnArc, ON_Arc* pRhCmnArc, bool rdnToRhc)
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  internal static extern bool ON_Arc_Copy(IntPtr pRdnArc, ref Arc pRhCmnArc, [MarshalAs(UnmanagedType.U1)]bool rdnToRhc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ON_ProgressReporter_SetReportCallback(Rhino.ProgressReporter.ProgressReportCallback progressReportCallback);

#if RHINO_SDK
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnPersistentSettingHooks_SetHooks(Rhino.PersistentSettingsHooks.CreateDelegate createDelegate,
                                                                    Rhino.PersistentSettingsHooks.SaveDelegate saveDelegate,
                                                                    Rhino.PersistentSettingsHooks.FlushSavedDelegate flushSavedDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetKeysDelegate getKeysDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetPlugInPersistentSettingsPointerProc getPlugInPersistentSettings,
                                                                    Rhino.PersistentSettingsHooks.GetPlugInPersistentSettingsPointerProc getManagedPlugInPersistentSettings,
                                                                    Rhino.PersistentSettingsHooks.GetCommandPersistentSettingsPointerProc getCommandPersistentSettings,
                                                                    Rhino.PersistentSettingsHooks.GetChildPersistentSettingsPointerProc addCommandPersistentSettings,
                                                                    Rhino.PersistentSettingsHooks.GetChildPersistentSettingsPointerProc deleteCommandPersistentSettings,
                                                                    Rhino.PersistentSettingsHooks.GetChildPersistentSettingsPointerProc getChildPersistentSettings,
                                                                    Rhino.PersistentSettingsHooks.GetChildPersistentSettingsPointerProc deleteItemPersistentSettings,
                                                                    Rhino.PersistentSettingsHooks.ReleasePlugInSettingsPointerProc releasePointerHook,
                                                                    Rhino.PersistentSettingsHooks.SetStringDelegate setStringDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetIntegerDelegate setIntDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetUnsignedIntegerDelegate setUnsignedIntDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetDoubleDelegate setDoubleDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetIntegerDelegate setBoolDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetIntegerDelegate setHideDelegate,
                                                                    Rhino.PersistentSettingsHooks.PersistentSettingsHiddenProc setPersistentSettingsHiddenDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetIntegerDelegate setColorDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetRectDelegate setRectDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetPointDelegate setPointDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetPointDelegate setSizeDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetStringListDelegate setStringListDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetStringDictionaryDelegate setStringDictionaryDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetGuidDelegate setGuidDelegate,
                                                                    Rhino.PersistentSettingsHooks.SetPoint3DDelegate setPoint3DDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetStringDelegate getStringDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetIntegerDelegate getIntDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetUnsignedIntegerDelegate getUnsignedIntDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetDoubleDelegate getDoubleDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetIntegerDelegate getBoolDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetIntegerDelegate getHideDelegate,
                                                                    Rhino.PersistentSettingsHooks.PersistentSettingsHiddenProc getPersistentSettingsHiddenDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetIntegerDelegate getColorDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetRectDelegate getRectDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetPointDelegate getPointDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetPointDelegate getSizeDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetStringListDelegate getStringListDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetStringDictionaryDelegate getStringDictionaryDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetGuidDelegate getGuidDelegate,
                                                                    Rhino.PersistentSettingsHooks.GetPoint3DDelegate getPoint3DDelegate,
                                                                    Rhino.PersistentSettingsHooks.DarkModeDelegate darkModeDelegate
                                                                   );

  #region rh_menu.cpp

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRuiOnUpdateMenuItems_SetHooks(Rhino.UI.RuiOnUpdateMenuItems.OnUpdateMenuItemCallback onUpdateCallback);

  #endregion rh_menu.cpp

  #region rh_pages.cpp
  // IRhino...Page classes in rh_pages.cpp
  // Z:\dev\github\mcneel\rhino\src4\DotNetSDK\rhinocommon\c\rh_pages.cpp line 108
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnPageBase_SetHooks(
    Rhino.UI.RhinoPageHooks.RhinoPageGetStringDelegate getString,
    Rhino.UI.RhinoPageHooks.RhinoPageGetIconDelegate getIcon,
    Rhino.UI.RhinoPageHooks.RhinoPageWindowDelegate getWindow,
    Rhino.UI.RhinoPageHooks.RhinoPageMinimumSizeDelegate getMinSize,
    Rhino.UI.RhinoPageHooks.RhinoPageReleaseDelegate release,
    Rhino.UI.RhinoPageHooks.RhinoPageRefreshDelegate refresh,
    Rhino.UI.RhinoPageHooks.RhinoPageIntPtrDelegate hostCreated,
    Rhino.UI.RhinoPageHooks.RhinoPageIntIntDelegate sizeChanged,
    Rhino.UI.RhinoPageHooks.RhinoPageIntDelegate show,
    Rhino.UI.RhinoPageHooks.RhinoPageIntReturnsIntDelegate showHelp,
    Rhino.UI.RhinoPageHooks.RhinoPageIntReturnsIntDelegate activated,
    Rhino.UI.RhinoPageHooks.RhinoPageIntReturnsHwndIntDelegate overrideSupressEnterEscape
  );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void IRhinoOptionsPage_SetHooks(
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageUintDelegate runScript,
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageProcDelegate updatePage,
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageButtonsToDisplayDelegate buttonToDisplay,
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageIsButtonEnabled isButonEnabled,
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageProcDelegate apply,
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageProcDelegate cancel,
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageProcDelegate restoreDefaults,
    Rhino.UI.RhinoPageHooks.RhinoOptionsPageProcDelegate attachedToUi
  );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void IRhinoPropertiesPanelPage_SetHooks(
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelDelegate includeInNavigation,
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelDelegate runScript,
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelDelegate updatePage,
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelDelegate onModifyPage,
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelPageTypeDelegate pageType,
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelDelegate index,
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelDelegate pageEvent,
    Rhino.UI.RhinoPageHooks.RhinoPropertiesPanelDelegate supportsSubObjectSelection
  );

  #endregion rh_pages.cpp

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool CRhinoPrintInfo_VectorCapture(IntPtr constPrintInfo,
    Rhino.Runtime.ViewCaptureWriter.SetClipRectProc clipRectProc,
    Rhino.Runtime.ViewCaptureWriter.FillProc fillProc,
    Rhino.Runtime.ViewCaptureWriter.VectorPolylineProc polylineCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorArcV8Proc arcCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorStringProc stringCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorPolylineProc bezCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorFillPolygonProc fillPolyCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorPathProc pathCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorPointProc pointCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorBitmapProc bitmapCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorRoundedRectProc roundedRectCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorClipPathProc pathProc,
    Rhino.Runtime.ViewCaptureWriter.VectorGradientProc gradientProc,
    uint docSerialNumber,
    Rhino.Runtime.ViewCaptureWriter.VectorPolylineV8Proc polylineV8Callback,
    Rhino.Runtime.ViewCaptureWriter.VectorCircleV8Proc circleV8Callback,
    Rhino.Runtime.ViewCaptureWriter.VectorBeziersV8Proc beziersV8Callback);


  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void SetRhCsInternetFunctionalityCallback(
    Rhino.Render.InternalUtilities.RhCsDownloadFileProc downloadFileCallback,
    Rhino.Render.InternalUtilities.RhCsUrlResponseProc urlResponseCallback,
    Rhino.Render.InternalUtilities.RhCsBitmapFromSvgProc bitmapFromSvgCallback);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetDisplayPageHook(Guid id, [MarshalAs(UnmanagedType.U1)] bool isStaticProc, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetRemovePageHook(Guid id, [MarshalAs(UnmanagedType.U1)] bool isStaticProc, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetLoadPagesHook (uint docSeraialNumber, Guid id, [MarshalAs(UnmanagedType.U1)] bool isStaticProc, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetLoadPlugInPagesHook(Guid id, [MarshalAs(UnmanagedType.U1)] bool isStaticProc, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SeIncludeInNavHook (Guid id, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetActivePageHook (Guid id, [MarshalAs(UnmanagedType.U1)] bool isStaticProc, IntPtr proc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_PostEffectUI_SetCallbacks(
    Rhino.RDK.Delegates.PEP_UI_ADD_SECTIONS_PROC addsection
  );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmnObjectManagerExtension_SetCallbacks(
    Rhino.ObjectManager.ObjectManagerExtension.PlugInIdDelegate delegatePlugInId,
    Rhino.ObjectManager.ObjectManagerExtension.ExtensionIdDelegate delegateId,
    Rhino.ObjectManager.ObjectManagerExtension.ExtensionEnglishNameDelegate delegateEnglishName,
    Rhino.ObjectManager.ObjectManagerExtension.ExtensionLocalizedNameDelegate delegateLocalizedName,
    Rhino.ObjectManager.ObjectManagerExtension.ExtensionNodeFromArchiveDelegate delegateNodeFromArchive,
    Rhino.ObjectManager.ObjectManagerExtension.ExtensionChildNodesDelegate delegateChildNodes
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmnObjectManagerNode_SetCallbacks(
    Rhino.ObjectManager.ObjectManagerNode.NodeTypeIdDelegate delegateNodeTypeId,
    Rhino.ObjectManager.ObjectManagerNode.NodeEnglishNameDelegate delegateEnglishName,
    Rhino.ObjectManager.ObjectManagerNode.NodeLocalizedNameDelegate delegateLoclaizedName,
    Rhino.ObjectManager.ObjectManagerNode.NodeIdDelegate delegateId,
    Rhino.ObjectManager.ObjectManagerNode.NodeParentDelegate delegateParent,
    Rhino.ObjectManager.ObjectManagerNode.NodeChildrenDelegate delegateChildren,
    Rhino.ObjectManager.ObjectManagerNode.NodeImageDelegate delegateImage,
    Rhino.ObjectManager.ObjectManagerNode.NodePropertiesDelegate delegateProperties,
    Rhino.ObjectManager.ObjectManagerNode.NodeBeginChangeDelegate delegateBeginChange,
    Rhino.ObjectManager.ObjectManagerNode.NodeEndChangeDelegate delegateEndChange,
    Rhino.ObjectManager.ObjectManagerNode.NodeCommandsDelegate delegateCommands,
    Rhino.ObjectManager.ObjectManagerNode.NodeCommandsForNodesDelegate delegateCommandsForNodes,
    Rhino.ObjectManager.ObjectManagerNode.NodePreviewDelegate delegatePreview,
    Rhino.ObjectManager.ObjectManagerNode.NodeWriteToBufferDelegate delegateWrite,
    Rhino.ObjectManager.ObjectManagerNode.NodeIsDragableDelegate delegateIsDragable,
    Rhino.ObjectManager.ObjectManagerNode.NodeSupportsDropTargetDelegate delegateSupportDropTarget,
    Rhino.ObjectManager.ObjectManagerNode.NodeDropDelegate delegateDrop,
    Rhino.ObjectManager.ObjectManagerNode.NodeAddUiSectionsDelegate delegateAddUiSections,
    Rhino.ObjectManager.ObjectManagerNode.NodeGetParameterDelegate delegateGetParameter,
    Rhino.ObjectManager.ObjectManagerNode.NodeSetParameterDelegate delegateSetParameter,
    Rhino.ObjectManager.ObjectManagerNode.NodeIsEqualDelegate delegateIsEqual,
    Rhino.ObjectManager.ObjectManagerNode.NodeHighlightInViewDelegate delegateHighlightInView,
    Rhino.ObjectManager.ObjectManagerNode.NodeIsSelectedDelegate delegateIsSelected,
    Rhino.ObjectManager.ObjectManagerNode.NodeToolTipDelegate delegateToolTip
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoEventWatcher_SetObjectManagerEventCallback(
    Rhino.ObjectManager.ObjectManager.ObjectManagerEventDelegate delegateChanged
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmnObjectManagerNodeProperty_SetCallbacks(
    Rhino.ObjectManager.ObjectManagerNodeQuickAccessProperty.NodePropertyIdDelegate delegateId,
    Rhino.ObjectManager.ObjectManagerNodeQuickAccessProperty.NodePropertyDisplayNameDelegate delegateDisplayName,
    Rhino.ObjectManager.ObjectManagerNodeQuickAccessProperty.NodePropertyParameterNameDelegate delegateParameterName,
    Rhino.ObjectManager.ObjectManagerNodeQuickAccessProperty.NodePropertyImageDelegate delegateImage,
    Rhino.ObjectManager.ObjectManagerNodeQuickAccessProperty.NodePropertyEditableDelegate delegateEditable
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmnObjectManagerNodeCommand_SetCallbacks(
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandIdDelegate delegateId,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandEnglishNameDelegate delegateEnglishName,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandLocalizedNameDelegate delegateLocalizedName,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandImageDelegate delegateImage,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandStateDelegate delegateState,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandIsCheckboxDelegate delegateIsCheckbox,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandIsSeparatorDelegate delegateIsSeparator,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandExecuteDelegate delegateExecute,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandIsDefaultDelegate delegateIsDefault,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandIsEnabledDelegate delegateIsEnabled,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandIsRadioButtonDelegate delegateIsRadioButton,
    Rhino.ObjectManager.ObjectManagerNodeCommand.NodeCommandSupportsMultipleSelectionDelegate delegateMultipleSupport
  );

  internal delegate void ConstraintAddRemProc(uint docSerial, IntPtr Constraints, IntPtr Constraint);
  internal delegate void ConstraintChangedProc(uint docSerial, IntPtr Constraints, IntPtr ConstraintOld, IntPtr ConstraintNew);
  internal delegate void ConstraintsAddRemProc(uint docSerial, IntPtr Constraints);
  internal delegate void ConstraintsChangedProc(uint docSerial, IntPtr ConstraintsOld, IntPtr ConstraintsNew);
  internal delegate void ConstraintWidgetAddRemProc(uint docSerial, Guid constraintId);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void SetConstraintEventWatcherHooks(
    ConstraintAddRemProc ConstraintAdded,
    ConstraintAddRemProc ConstraintRemoved,
    ConstraintChangedProc ConstraintChanged,
    ConstraintsAddRemProc ConstraintsAdded,
    ConstraintsAddRemProc ConstraintsRemoved,
    ConstraintsChangedProc ConstraintsChanged,
    ConstraintWidgetAddRemProc ConstraintWidgetAdded,
    ConstraintWidgetAddRemProc ConstraintWidgetRemoved
    );

#endif
}
