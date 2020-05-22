using System;
using Rhino.Geometry;
using System.Runtime.InteropServices;
using Rhino.Display;

// 19 Dec. 2010 S. Baer
// Giulio saw a significant performance increase by marking this class with the
// SuppressUnmanagedCodeSecurity attribute. See MSDN for details
[System.Security.SuppressUnmanagedCodeSecurity]
internal partial class UnsafeNativeMethods
{
  [StructLayout(LayoutKind.Sequential)]
  public struct Point
  {
    public int X;
    public int Y;

    public Point(int x, int y)
    {
      X = x;
      Y = y;
    }
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
  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool GetCursorPos(out Point lpPoint);

  [DllImport("user32.dll", CharSet = CharSet.Auto)]
  internal static extern bool DestroyIcon(IntPtr handle);

  [DllImport("Gdi32.dll", CharSet = CharSet.Auto)]
  internal static extern bool DeleteObject(IntPtr handle);

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  internal static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

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
    Rhino.PlugIns.RenderPlugIn.RenderSettingsSectionsCallback renderSettingsSections
    );

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RHC_SetRdkInitializationCallbacks(Rhino.Runtime.HostUtils.InitializeRDKCallback init, Rhino.Runtime.HostUtils.ShutdownRDKCallback shut);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoDigitizerPlugIn_SetCallbacks(Rhino.PlugIns.DigitizerPlugIn.EnableDigitizerFunc enablefunc,
    Rhino.PlugIns.DigitizerPlugIn.UnitSystemFunc unitsystemfunc,
    Rhino.PlugIns.DigitizerPlugIn.PointToleranceFunc pointtolfunc);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern IntPtr CRhinoSkin_New(Rhino.Runtime.Skin.ShowSplashCallback cb, [MarshalAs(UnmanagedType.LPWStr)]string name, IntPtr hicon);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoCommand_SetCallbacks(int commandSerialNumber,
    Rhino.Commands.Command.RunCommandCallback cb,
    Rhino.Commands.Command.DoHelpCallback dohelpCb,
    Rhino.Commands.Command.ContextHelpCallback contexthelpCb,
    Rhino.Commands.Command.ReplayHistoryCallback replayhistoryCb,
    Rhino.Commands.SelCommand.SelFilterCallback selCb);

  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhinoDisplayConduit_SetCallback(int which, Rhino.Display.DisplayPipeline.ConduitCallback cb, Rhino.Runtime.HostUtils.ReportCallback reportcb);

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
  internal static extern void CRdkCmnEventWatcher_SetSunChangedEventCallback(Rhino.Render.Sun.RdkSunSettingsChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetUndoRedoEventCallback(Rhino.Render.UndoRedo.RdkUndoRedoCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetUndoRedoEndedEventCallback(Rhino.Render.UndoRedo.RdkUndoRedoEndedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetGroundPlaneChangedEventCallback(Rhino.Render.GroundPlane.RdkGroundPlaneSettingsChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetSafeFrameChangedEventCallback(Rhino.Render.SafeFrame.RdkSafeFrameChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetSkylightChangedEventCallback(Rhino.Render.Skylight.RdkSkylightSettingsChangedCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCb);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetOnRenderImageEventCallback(Rhino.Render.ImageFile.OnRenderImageCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCallback);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetOnRenderWindowClonedEventCallback(Rhino.Render.RenderWindow.ClonedEventCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCallback);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRdkCmnEventWatcher_SetOnCustomEventCallback(Rhino.Render.CustomEvent.RdkCustomEventCallback cb, Rhino.Runtime.HostUtils.RdkReportCallback reportCallback);

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
  internal static extern void Rdk_SetOnContentFieldChangeCallback(Rhino.Render.RenderContent.OnContentFieldChangedCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetParameterCallback(Rhino.Render.RenderContent.GetParameterCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetExtraRequirementParameterCallback(Rhino.Render.RenderContent.GetExtraRequirementParameterCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetSetExtraRequirementParameterCallback(Rhino.Render.RenderContent.SetExtraRequirementParameterCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetContentIconCallback(Rhino.Render.RenderContent.SetContentIconCallback callbackFunction);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetHarvestDataCallback(Rhino.Render.RenderContent.HarvestDataCallback callbackFunc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetGetShaderCallback(Rhino.Render.RenderContent.GetShaderCallback callbackFunc);

  //Rhino.Render.UI.UserInterfaceSection is obsolete
#pragma warning disable 0612
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_ContentUiSectionSetCallbacks(Rhino.Render.UI.UserInterfaceSection.SerialNumberCallback deleteThisCallback,
                                                               Rhino.Render.UI.UserInterfaceSection.SerialNumberCallback displayDataCallback,
                                                               Rhino.Render.UI.UserInterfaceSection.SerialNumberBoolCallback onExpandCallback,
                                                               Rhino.Render.UI.UserInterfaceSection.SerialNumberCallback isHiddenCallback
                                                              );
#pragma warning restore 0612

  // UiSection
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RhinoUiSectionImpl_SetCallbacks(Rhino.UI.Controls.Delegates.MOVEPROC move,
                                                              Rhino.RDK.Delegates.SETBOOLPROC show,
                                                              Rhino.RDK.Delegates.VOIDPROC deletethis,
                                                              Rhino.RDK.Delegates.GETBOOLPROC isShown,
                                                              Rhino.RDK.Delegates.GETBOOLPROC isEnabled,
                                                              Rhino.RDK.Delegates.SETBOOLPROC enable,
                                                              Rhino.RDK.Delegates.GETINTPROC getHeight,
                                                              Rhino.RDK.Delegates.GETINTPROC getInitialState,
                                                              Rhino.RDK.Delegates.GETBOOLPROC isHidden,
                                                              Rhino.RDK.Delegates.FACTORYPROC getSection,
                                                              Rhino.RDK.Delegates.SETINTPTRPROC setParent,
                                                              Rhino.RDK.Delegates.GETGUIDPROC id,
                                                              Rhino.RDK.Delegates.GETSTRINGPROC englishCaption,
                                                              Rhino.RDK.Delegates.GETSTRINGPROC localCaption,
                                                              Rhino.RDK.Delegates.GETBOOLPROC collapsible,
                                                              Rhino.RDK.Delegates.GETINTPROC backgroundColor,
                                                              Rhino.RDK.Delegates.SETINTPROC setBackgroundColor,
                                                              Rhino.RDK.Delegates.GETINTPTRPROC getWindowPtr,
                                                              Rhino.RDK.Delegates.SETGUIDPROC onEvent,
                                                              Rhino.RDK.Delegates.VOIDPROC onViewModelActivatedEvent,
                                                              Rhino.RDK.Delegates.GETGUIDPROC PlugInId,
                                                              Rhino.RDK.Delegates.GETSTRINGPROC CommandOptName,
                                                              Rhino.RDK.Delegates.GETINTPROC RunScript,
                                                              Rhino.RDK.Delegates.GETGUIDPROC cid);


  // UiSection
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RhinoUiViewModelImpl_SetCallbacks(Rhino.RDK.Delegates.FACTORYPROC getViewModel,
                                                                    Rhino.RDK.Delegates.SETGUIDEVENTINFOPROC onEvent,
                                                                    Rhino.RDK.Delegates.VOIDPROC deleteThis,
                                                                    Rhino.RDK.Delegates.SETINTPTRPROC requireddatasources);

  // GenericController
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RhinoGenericController_SetCallbacks(Rhino.RDK.Delegates.SETGUIDEVENTINFOPROC onEvent,
                                                                      Rhino.RDK.Delegates.SETINTPTRPROC requireddatasources);

  // UiWithController
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_UiWithController_SetCallbacks(Rhino.RDK.Delegates.VOIDPROC deletethis,
                                                                Rhino.RDK.Delegates.SETINTPTRPROC setcontroller,
                                                                Rhino.RDK.Delegates.SETGUIDPROC onevent,
                                                                Rhino.RDK.Delegates.GETGUIDPROC controllerid);
  // RdkMenu
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_Menu_SetCallbacks(Rhino.RDK.Delegates.ADDSUBMENUPROC addsubmenu,
                                                    Rhino.RDK.Delegates.ADDITEMPROC additem,
                                                    Rhino.RDK.Delegates.ADDSEPARATORPROC addseparator);

  // RdkDataSource
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_DataSource_SetCallbacks(Rhino.RDK.Delegates.SUPPORTEDUUIDDATAPROC supporteduuiddata);

  // RdkColorDataSource
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_ColorDataSource_SetCallbacks(Rhino.RDK.Delegates.GETCOLORPROC getcolor,
                                                               Rhino.RDK.Delegates.SETCOLORPROC setcolor,
                                                               Rhino.RDK.Delegates.USESALPHAPROC usesalpha);

  // UiDynamicAcces
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_UiDynamicAccess_SetCallbacks(Rhino.RDK.Delegates.SETINTPTRPROC addsectionspre,
                                                               Rhino.RDK.Delegates.SETINTPTRPROC addsectionspost,
                                                               Rhino.RDK.Delegates.SETINTPTRREFINTPTRPROC newcontentcreatorusingtypebrowser,
                                                               Rhino.RDK.Delegates.GETGUIDINTPROC contenteditortabid,
                                                               Rhino.RDK.Delegates.SETINTPTRPROC addtextureadjustmentsection,
                                                               Rhino.RDK.Delegates.SETINTPTRPROC addtwocolorsection,
                                                               Rhino.RDK.Delegates.SETINTPTRPROC addlocalmapping2dsection,
                                                               Rhino.RDK.Delegates.SETINTPTRPROC addlocalmapping3dsection,
                                                               Rhino.RDK.Delegates.SETINTPTRPROC addgraphsection,
                                                               Rhino.RDK.Delegates.GETINTPTRPROC newfloatingpreviewmanager,
                                                               Rhino.RDK.Delegates.SETINTPTRGETBOOLPROC openfloatingcontentedtirodlg,
                                                               Rhino.RDK.Delegates.SETINTPTRINTPTRBOOLPROC gettagswindow,
                                                               Rhino.RDK.Delegates.SETREFINTREFINTREFBOOLBOOLPROC showdecalsmappingstyledialog,
                                                               Rhino.RDK.Delegates.SETINTPTRINTPTRUINTUINTBOOLBOOLPROC showmodalsundialog,
                                                               Rhino.RDK.Delegates.SETINTPTRGETBOOLPROC showmodalcontenteditordialog,
                                                               Rhino.RDK.Delegates.SETUINTGUIDBOOLPROC showchooselayersdlg,
                                                               Rhino.RDK.Delegates.NEWRENDERFRAMEPROC newrenderframe,
                                                               Rhino.RDK.Delegates.SETBOOLUINT showttmappingmesheditordockbar,
                                                               Rhino.RDK.Delegates.SHOWRENDERINGOPENFILEDLGPROC ShowRenderOpenFileDlg,
                                                               Rhino.RDK.Delegates.SHOWRENDERINGSAVEFILEDLGPROC ShowRenderSaveFileDlg,
                                                               Rhino.RDK.Delegates.SHOWCONTENTTYPEBROWSERPROC ShowContentTypeBrowser,
                                                               Rhino.RDK.Delegates.PROMPTFORIMAGEFILEPARAMSPROC PromptForImageFileParams,
                                                               Rhino.RDK.Delegates.SHOWNAMEDITEMEDITDLGPROC ShowNamedItemEditDlg,
                                                               Rhino.RDK.Delegates.SHOWSMARTMERGENAMECOLLISIONDLGPROC ShowSmartMergeNameCollisionDialog,
                                                               Rhino.RDK.Delegates.SHOWPREVIEWPROPERTIESDLGPROC ShowPreviewPropertiesDlg,
                                                               Rhino.RDK.Delegates.CHOOSECONTENTPROC ChooseContent,
                                                               Rhino.RDK.Delegates.PEPPICKPOINTONIMAGEPROC PepPickPointOnImage,
                                                               Rhino.RDK.Delegates.SHOWLAYERMATERIALDIALOGPROC ShowLayerMaterialDialog,
                                                               Rhino.RDK.Delegates.PROMPTFORIMAGEDRAGOPTIONSDLGPROC PromptForImageDragOptionsDlg,
                                                               Rhino.RDK.Delegates.PROMPTFOROPENACTIONSDLGPROC PromptForOpenActionsDlg,
                                                               Rhino.RDK.Delegates.DISPLAYMISSINGTEXTURESDIALOG DisplayMissingTexturesDialog,
                                                               Rhino.RDK.Delegates.OPENNAMEDVIEWANIMATIONSETTINGSDLG OpenNamedViewAnimationSettingsDlg,
                                                               Rhino.RDK.Delegates.PEPPICKRECTANGLEONIMAGEPROC PepPickRectangleOnImage,
                                                               Rhino.RDK.Delegates.SHOWCONTENTCTRLPROPDLGPROC ShowContentCtrlPropDlg,
                                                               Rhino.RDK.Delegates.CREATEINPLACERENDERVIEWPROC CreateInPlaceRenderView,
                                                               Rhino.RDK.Delegates.ADDCONTENTAUTOMATICUISECTIONPROC AddContentAutomaticUISection,
                                                               Rhino.RDK.Delegates.SHOWNAMEDVIEWPROPERTIESDLG ShowNamedItemPropertiesDlg,
                                                               Rhino.RDK.Delegates.ONPLUGINLOADEDPROC OnPlugInLoaded,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsFog,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionGlow,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsGlare,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsDOF,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsGamma,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsToneMappingNone,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsToneMappingBlackWhitePoint,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsToneMappingLogarithmic,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsToneMappingFilmic,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsToneMappingFilmicAdvanced,
                                                               Rhino.RDK.Delegates.NEWRENDERSETTINGPAGEPROC NewRenderSettingsPage,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsDithering,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsWatermark,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsHueSatLum,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsBriCon,
                                                               Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC AddPostEffectUISectionsMultiplier,
                                                               Rhino.RDK.Delegates.PEPRENDERSETTINGSPAGEPROC AttachRenderPostEffectsPage);

  // EarlyPostEffectPlugIn
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnEarlyPostEffectPlugIn_SetCallbacks(Rhino.RDK.Delegates.SHOWNPROC shown,
                                                                            Rhino.RDK.Delegates.SETSHOWNPROC setshown,
                                                                            Rhino.RDK.Delegates.ONPROC on,
                                                                            Rhino.RDK.Delegates.SETONPROC seton,
                                                                            Rhino.RDK.Delegates.FIXEDPROC fixedd,
                                                                            Rhino.RDK.Delegates.CANEXECUTEPROC canexecute,
                                                                            Rhino.RDK.Delegates.REQUIREDCHANNELSPROC requiredchannels,
                                                                            Rhino.RDK.Delegates.EXECUTEWHILERENDERINGDELAYMSPROC executewhilerenderingdelaysms,
                                                                            Rhino.RDK.Delegates.SUPPORTSHDRDATAPROC supportshdrdata,
                                                                            Rhino.RDK.Delegates.READFROMDOCUMENTDEFAULTSPROC readfromdocumentdefaults,
                                                                            Rhino.RDK.Delegates.WRITETODOCUMENTDEFAULTSPROC writetodocumentdefaults,
                                                                            Rhino.RDK.Delegates.CRCPROC crc,
                                                                            Rhino.RDK.Delegates.UUIDPROC uuid,
                                                                            Rhino.RDK.Delegates.LOCALNAMEPROC localname,
                                                                            Rhino.RDK.Delegates.USAGEFLAGSPROC usageflags,
                                                                            Rhino.RDK.Delegates.EXECUTEWHILERENDERINGOPTIONSPROC executewhilerenderingoptions,
                                                                            Rhino.RDK.Delegates.EXECUTEPROC execute,
                                                                            Rhino.RDK.Delegates.GETPARAMPROC getparam,
                                                                            Rhino.RDK.Delegates.SETPARAMPROC setparam,
                                                                            Rhino.RDK.Delegates.READSTATEPROC readstate,
                                                                            Rhino.RDK.Delegates.WRITESTATEPROC writestate,
                                                                            Rhino.RDK.Delegates.RESETTOFACTORYDEFAULTSPROC resettofactorydefaults,
                                                                            Rhino.RDK.Delegates.ADDUISECTIONSPROC adduiseections,
                                                                            Rhino.RDK.Delegates.DISPLAYHELPPROC displayhelp,
                                                                            Rhino.RDK.Delegates.CANDISPLAYHELPPROC candisplayhelp);

  // LatePostEffectPlugIn
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnLatePostEffectPlugIn_SetCallbacks(Rhino.RDK.Delegates.SHOWNPROC shown,
                                                                            Rhino.RDK.Delegates.SETSHOWNPROC setshown,
                                                                            Rhino.RDK.Delegates.ONPROC on,
                                                                            Rhino.RDK.Delegates.SETONPROC seton,
                                                                            Rhino.RDK.Delegates.FIXEDPROC fixedd,
                                                                            Rhino.RDK.Delegates.CANEXECUTEPROC canexecute,
                                                                            Rhino.RDK.Delegates.REQUIREDCHANNELSPROC requiredchannels,
                                                                            Rhino.RDK.Delegates.EXECUTEWHILERENDERINGDELAYMSPROC executewhilerenderingdelaysms,
                                                                            Rhino.RDK.Delegates.SUPPORTSHDRDATAPROC supportshdrdata,
                                                                            Rhino.RDK.Delegates.READFROMDOCUMENTDEFAULTSPROC readfromdocumentdefaults,
                                                                            Rhino.RDK.Delegates.WRITETODOCUMENTDEFAULTSPROC writetodocumentdefaults,
                                                                            Rhino.RDK.Delegates.CRCPROC crc,
                                                                            Rhino.RDK.Delegates.UUIDPROC uuid,
                                                                            Rhino.RDK.Delegates.LOCALNAMEPROC localname,
                                                                            Rhino.RDK.Delegates.USAGEFLAGSPROC usageflags,
                                                                            Rhino.RDK.Delegates.EXECUTEWHILERENDERINGOPTIONSPROC executewhilerenderingoptions,
                                                                            Rhino.RDK.Delegates.EXECUTEPROC execute,
                                                                            Rhino.RDK.Delegates.GETPARAMPROC getparam,
                                                                            Rhino.RDK.Delegates.SETPARAMPROC setparam,
                                                                            Rhino.RDK.Delegates.READSTATEPROC readstate,
                                                                            Rhino.RDK.Delegates.WRITESTATEPROC writestate,
                                                                            Rhino.RDK.Delegates.RESETTOFACTORYDEFAULTSPROC resettofactorydefaults,
                                                                            Rhino.RDK.Delegates.ADDUISECTIONSPROC adduiseections,
                                                                            Rhino.RDK.Delegates.DISPLAYHELPPROC displayhelp,
                                                                            Rhino.RDK.Delegates.CANDISPLAYHELPPROC candisplayhelp);

  // ToneMappingPostEffectPlugIn
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnToneMappingPostEffectPlugIn_SetCallbacks(
                                                                            Rhino.RDK.Delegates.CANEXECUTEPROC canexecute,
                                                                            Rhino.RDK.Delegates.REQUIREDCHANNELSPROC requiredchannels,
                                                                            Rhino.RDK.Delegates.EXECUTEWHILERENDERINGDELAYMSPROC executewhilerenderingdelaysms,
                                                                            Rhino.RDK.Delegates.SUPPORTSHDRDATAPROC supportshdrdata,
                                                                            Rhino.RDK.Delegates.READFROMDOCUMENTDEFAULTSPROC readfromdocumentdefaults,
                                                                            Rhino.RDK.Delegates.WRITETODOCUMENTDEFAULTSPROC writetodocumentdefaults,
                                                                            Rhino.RDK.Delegates.CRCPROC crc,
                                                                            Rhino.RDK.Delegates.UUIDPROC uuid,
                                                                            Rhino.RDK.Delegates.LOCALNAMEPROC localname,
                                                                            Rhino.RDK.Delegates.USAGEFLAGSPROC usageflags,
                                                                            Rhino.RDK.Delegates.EXECUTEWHILERENDERINGOPTIONSPROC executewhilerenderingoptions,
                                                                            Rhino.RDK.Delegates.EXECUTEPROC execute,
                                                                            Rhino.RDK.Delegates.GETPARAMPROC getparam,
                                                                            Rhino.RDK.Delegates.SETPARAMPROC setparam,
                                                                            Rhino.RDK.Delegates.READSTATEPROC readstate,
                                                                            Rhino.RDK.Delegates.WRITESTATEPROC writestate,
                                                                            Rhino.RDK.Delegates.RESETTOFACTORYDEFAULTSPROC resettofactorydefaults,
                                                                            Rhino.RDK.Delegates.ADDUISECTIONSPROC adduiseections,
                                                                            Rhino.RDK.Delegates.DISPLAYHELPPROC displayhelp,
                                                                            Rhino.RDK.Delegates.CANDISPLAYHELPPROC candisplayhelp,
                                                                            Rhino.RDK.Delegates.SETMANAGERPROC setmanager);
  // CmnPostEffectFactory
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnPostEffectFactory_SetCallbacks(Rhino.RDK.Delegates.NEWPOSTEFFECTPROC newposteffect,
                                                                        Rhino.RDK.Delegates.PEFUUIDPROC pluginid);

  // CmnPostEffectJob
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_CRdkCmnPostEffectJob_SetCallbacks(Rhino.RDK.Delegates.CLONEPOSTEFFECTJOBPROC clone,
                                                                    Rhino.RDK.Delegates.DELETETHISPOSTEFFECTJOB delete,
                                                                    Rhino.RDK.Delegates.EXECUTEPOSTEFFECTJOB execute);

  // FloatingPreviewManager
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_FloatingPreviewManager_SetCallbacks(Rhino.RDK.Delegates.GETGUIDINTPTRINTINTPROC openfloatingpreview,
                                                                      Rhino.RDK.Delegates.GETGUIDINTPTRINTINTPROC openfloatingpreviewatmouse,
                                                                      Rhino.RDK.Delegates.SETGUIDINTINTBOOLPROC resizefloatingpreview,
                                                                      Rhino.RDK.Delegates.SETGUIDBOOLPROC closefloatingpreview);

  // ContentUIAgent
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_ContentUIAgent_SetCallbacks(Rhino.RDK.Delegates.SETINTPTRPROC adduisections_proc,
                                                              Rhino.RDK.Delegates.GETGUIDPROC contenttypeid_proc);
  
  // SnapShotsClient
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhinoSnapShotsClient_SetCallbacks(Rhino.DocObjects.SnapShots.SnapShotsClient.GETSTRINGPROC category,
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
                                                                    Rhino.DocObjects.SnapShots.SnapShotsClient.GETBOOLDOCOBJBUFFERBUFFERARRAYTEXTLOGPROC iscurrentobjmodelstateinanysnapshot);

  // ThumbnailList
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_UiHolder_SetCallbacks(
      Rhino.UI.Controls.Delegates.MOVEPROC m,
      Rhino.RDK.Delegates.SETBOOLPROC s,
      Rhino.RDK.Delegates.VOIDPROC dt,
      Rhino.RDK.Delegates.GETBOOLPROC ish,
      Rhino.RDK.Delegates.GETBOOLPROC ien,
      Rhino.RDK.Delegates.SETBOOLPROC e,
      Rhino.RDK.Delegates.VOIDPROC u,
      Rhino.UI.Controls.CollapsibleSectionHolderImpl.ATTACHSECTIONPROC a,
      Rhino.UI.Controls.CollapsibleSectionHolderImpl.ATTACHSECTIONPROC de,
      Rhino.RDK.Delegates.GETINTPROC sc,
      Rhino.RDK.Delegates.ATINDEXPROC sa,
      Rhino.RDK.Delegates.SETINTPTRPROC sp);


  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_Thumbnaillist_SetCallbacks(
      Rhino.UI.Controls.Delegates.MOVEPROC m,
      Rhino.RDK.Delegates.SETBOOLPROC s,
      Rhino.RDK.Delegates.VOIDPROC dt,
      Rhino.RDK.Delegates.GETBOOLPROC ish,
      Rhino.RDK.Delegates.GETBOOLPROC ien,
      Rhino.RDK.Delegates.SETBOOLPROC e,
      Rhino.RDK.Delegates.SETINTPTRPROC sp,
      Rhino.RDK.Delegates.GETGUIDPROC id,
      Rhino.RDK.Delegates.SETMODEPROC a,
      Rhino.RDK.Delegates.SETINTPTRPROC sc,
      Rhino.RDK.Delegates.SETGUIDPROC sid,
      Rhino.RDK.Delegates.VOIDSETSELECTIONPROC ssp,
      Rhino.RDK.Delegates.GETBOOLPROC gm,
      Rhino.RDK.Delegates.GETINTPTRPROC getWindowPtr,
      Rhino.RDK.Delegates.GETBOOLPROC ic,
      Rhino.RDK.Delegates.SETBOOLPROC su,
      Rhino.RDK.Delegates.GETBOOLPROC gu,
      Rhino.RDK.Delegates.SETINTPTRPROC addThumb,
      Rhino.RDK.Delegates.GETGUIDPROC cid);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_BreadCrumb_SetCallbacks(
    Rhino.UI.Controls.Delegates.MOVEPROC m,
    Rhino.RDK.Delegates.SETBOOLPROC s,
    Rhino.RDK.Delegates.VOIDPROC dt,
    Rhino.RDK.Delegates.GETBOOLPROC ish,
    Rhino.RDK.Delegates.GETBOOLPROC ien,
    Rhino.RDK.Delegates.SETBOOLPROC e,
    Rhino.RDK.Delegates.SETINTPTRPROC sp,
    Rhino.RDK.Delegates.GETGUIDPROC id,
    Rhino.RDK.Delegates.SETINTPTRPROC sc,
    Rhino.RDK.Delegates.SETGUIDPROC sid,
    Rhino.RDK.Delegates.GETINTPTRPROC getWindowPtr,
    Rhino.RDK.Delegates.GETBOOLPROC ic,
    Rhino.RDK.Delegates.SETINTPTRPROC scec,
    Rhino.RDK.Delegates.GETGUIDPROC cid);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_NewContentCtrl_SetCallbacks(
    Rhino.UI.Controls.Delegates.MOVEPROC m,
    Rhino.RDK.Delegates.SETBOOLPROC s,
    Rhino.RDK.Delegates.VOIDPROC dt,
    Rhino.RDK.Delegates.GETBOOLPROC ish,
    Rhino.RDK.Delegates.GETBOOLPROC ien,
    Rhino.RDK.Delegates.SETBOOLPROC e,
    Rhino.RDK.Delegates.SETINTPTRPROC sp,
    Rhino.RDK.Delegates.GETGUIDPROC id,
    Rhino.RDK.Delegates.SETINTPTRPROC sc,
    Rhino.RDK.Delegates.SETGUIDPROC sid,
    Rhino.RDK.Delegates.GETINTPTRPROC getWindowPtr,
    Rhino.RDK.Delegates.GETBOOLPROC ic,
    Rhino.RDK.Delegates.SETINTPTRPROC scec,
    Rhino.RDK.Delegates.GETGUIDPROC cid,
    Rhino.RDK.Delegates.SETSELECTIONPROC ssp);

  // LightManagerSupport
  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_LightManagerSupport_SetCallbacks(
    Rhino.RDK.Delegates.GETGUIDPROC RenderEngineId,
    Rhino.RDK.Delegates.GETGUIDPROC PluginId,
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
    Rhino.Render.ChangeQueue.ChangeQueue.DynamicClippingPlaneChangesCallback dynclippingcb
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
    Rhino.Render.RealtimeDisplayMode.HudAllowEditMaxPassesCallback hudAllowEditMaxPasses
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
  internal static extern void CRhCmnRdkRenderPlugIn_RegisterCustomPlugInUi(
    RhRdkCustomUiType panelType,
    [MarshalAs(UnmanagedType.LPWStr)] string caption,
    Guid tabId,
    Guid pluginId,
    Guid renderEngineId,
    bool initialShow,
    bool alwaysShow,
    Rhino.Render.RenderPanels.CreatePanelCallback createProc,
    Rhino.Render.RenderPanels.VisiblePanelCallback visibleProc,
    Rhino.Render.RenderPanels.DestroyPanelCallback destroyProc,
    Rhino.Render.RenderPanels.SetControllerPanelCallback setcontrollerProc);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_SetAsyncRenderContextCallbacks(
    Rhino.Render.AsyncRenderContext.DeleteThisCallback deleteThis,
    Rhino.Render.AsyncRenderContext.StopRenderingCallback stopRendering);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_RenderFrame_SetCallbacks(
    Rhino.RDK.Delegates.VOIDPROC delete,
    Rhino.RDK.Delegates.BOOL_INTINPTRINTINT_PROC create,
    Rhino.RDK.Delegates.BOOL_INT_PROC destroy,
    Rhino.RDK.Delegates.VOID_INTBOOL_PROC SetVisibility,
    Rhino.RDK.Delegates.BOOL_INT_PROC StartRendering,
    Rhino.RDK.Delegates.BOOL_INT_PROC IsRendering,
    Rhino.RDK.Delegates.VOIDPROC StopRendering,
    Rhino.RDK.Delegates.VOID_GETINTINTINT_PROC Size,
    Rhino.RDK.Delegates.VOID_SETINTINTINT_PROC SetImageSize,
    Rhino.RDK.Delegates.VOID_INTBOOL_PROC Refresh,
    Rhino.RDK.Delegates.SAVERENDERIMAGEASPROC SaveAs,
    Rhino.RDK.Delegates.BOOL_INT_PROC CopyToClipboard,
    Rhino.RDK.Delegates.PICKPOINTONRENDERIMAGEPROC PickPoint,
    Rhino.RDK.Delegates.PICKRECTANGLEONRENDERIMAGEPROC PickRectangle);

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnRdkRenderPlugIn_RegisterCustomDockBarTab(
    RhRdkCustomUiType panelType,
    [MarshalAs(UnmanagedType.LPWStr)] string caption,
    Guid tabId,
    Guid pluginId,
    Guid renderEngineId,
    IntPtr icon,
    Rhino.Render.RenderPanels.CreatePanelCallback createProc,
    Rhino.Render.RenderPanels.VisiblePanelCallback visibleProc,
    Rhino.Render.RenderPanels.DestroyPanelCallback destroyProc,
    Rhino.Render.RenderPanels.VisiblePanelCallback doHelpProc,
    Rhino.Render.RenderPanels.SetControllerPanelCallback setcontrollerProc);

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
    Rhino.DocObjects.Custom.UserData.DeleteUserDataCallback deleteFunc);

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
                                                                    Rhino.PersistentSettingsHooks.GetPoint3DDelegate getPoint3DDelegate
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
    Rhino.Runtime.ViewCaptureWriter.VectorArcProc arcCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorStringProc stringCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorPolylineProc bezCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorFillPolygonProc fillPolyCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorPathProc pathCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorPointProc pointCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorBitmapProc bitmapCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorRoundedRectProc roundedRectCallback,
    Rhino.Runtime.ViewCaptureWriter.VectorClipPathProc pathProc,
    Rhino.Runtime.ViewCaptureWriter.VectorGradientProc gradientProc,
    uint docSerialNumber);


  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void SetRhCsInternetFunctionalityCallback(Rhino.Render.InternalUtilities.RhCsDownloadFileProc downloadFileCallback, Rhino.Render.InternalUtilities.RhCsUrlResponseProc urlResponseCallback);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetDisplayPageHook(Guid id, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetRemovePageHook(Guid id, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetLoadPagesHook (Guid id, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetLoadPlugInPagesHook(Guid id, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SeIncludeInNavHook (Guid id, IntPtr proc);

  [DllImport (Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void RhCmn_PropertiesEditor_SetActivePageHook (Guid id, IntPtr proc);

  #region rh_fileeventwatcher.cpp
  //void CRhCmnFileEventWatcherInterop_SetHooks(CREATEFILEWATCHERPROC createProc)
  [DllImport(Import.lib, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void CRhCmnFileEventWatcherInterop_SetHooks(
    Rhino.RhinoFileEventWatcherHooks.AttachFileWatcherDelegate attach,
    Rhino.RhinoFileEventWatcherHooks.AttachFileWatcherDelegate detach,
    Rhino.RhinoFileEventWatcherHooks.FileWatcherWatchDelegate watch,
    Rhino.RhinoFileEventWatcherHooks.FileWatcherEnableDelegate enable
  );
  #endregion

  [DllImport(Import.librdk, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void Rdk_PostEffectUI_SetCallbacks(
    Rhino.RDK.Delegates.PEP_UI_ADDSECTIONS_PROC addsection
  );
#endif
}
