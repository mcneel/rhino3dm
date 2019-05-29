#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;

namespace Rhino.Render
{
  // Not public
  sealed class RdkPlugIn : IDisposable
  {
    #region statics
    static bool g_callbacks_set;
    internal static void SetRdkCallbackFunctions(bool on)
    {
      // All of the RDK callback functions - gets called every time a new RdkPlugIn is created
      if (on)
      {
        UnsafeNativeMethods.Rdk_SetNewTextureCallback(RenderTexture.m_NewTextureCallback);
        UnsafeNativeMethods.Rdk_SetNewMaterialCallback(RenderMaterial.m_NewMaterialCallback);
        UnsafeNativeMethods.Rdk_SetNewEnvironmentCallback(RenderEnvironment.m_NewEnvironmentCallback);

        UnsafeNativeMethods.Rdk_SetRenderContentOnCppDtorCallback(RenderContent.OnCppDtor);
        UnsafeNativeMethods.Rdk_SetRenderContentBitFlagsCallback(RenderContent.BitFlags);
        UnsafeNativeMethods.Rdk_SetContentStringCallback(RenderContent.GetRenderContentString);
        UnsafeNativeMethods.Rdk_SetNewTextureEvaluatorCallback(RenderTexture.m_NewTextureEvaluator);
        UnsafeNativeMethods.Rdk_SetTextureEvaluatorCallbacks(TextureEvaluator.m_GetColor, TextureEvaluator.m_OnDeleteThis, TextureEvaluator.m_Initialize);
        UnsafeNativeMethods.Rdk_SetSimulateTextureCallback(RenderTexture.m_SimulateTexture);
        UnsafeNativeMethods.RdkSetTextureSetVirtualIntCallback(RenderTexture.SetVirtualInt);
        UnsafeNativeMethods.RdkSetTextureGetVirtualIntCallback(RenderTexture.GetVirtualInt);
        UnsafeNativeMethods.RdkSetTextureSetVirtualVector3dCallback(RenderTexture.SetVirtual3DVector);
        UnsafeNativeMethods.RdkSetTextureGetVirtualVector3dCallback(RenderTexture.GetVirtual3DVector);
        UnsafeNativeMethods.Rdk_SetAddUISectionsCallback(RenderContent.m_AddUISections);
        UnsafeNativeMethods.Rdk_SetGetDefaultsFromUserCallback(RenderContent.GetDefaultsFromUser);
        UnsafeNativeMethods.Rdk_IsFactoryProductAcceptableAsChildCallback(RenderContent.IsFactoryProductAcceptableAsChildHook);
        UnsafeNativeMethods.Rdk_SetIsContentTypeAcceptableAsChildCallback(RenderContent.m_IsContentTypeAcceptableAsChild);
        UnsafeNativeMethods.Rdk_SetHarvestDataCallback(RenderContent.HarvestData);
        UnsafeNativeMethods.Rdk_SetGetShaderCallback(RenderContent.m_GetShader);
        UnsafeNativeMethods.Rdk_SetGetContentIconCallback(RenderContent.SetContentIcon);
        UnsafeNativeMethods.Rdk_SetRenderContentRenderCrcCallback(RenderContent.g_on_render_crc);

        UnsafeNativeMethods.Rdk_SetSetParameterCallback(RenderContent.m_SetParameter);
        UnsafeNativeMethods.Rdk_SetEmbeddedFilesCallback(RenderContent.g_embedded_files_callback);
        UnsafeNativeMethods.Rdk_SetGetParameterCallback(RenderContent.m_GetParameter);

        UnsafeNativeMethods.Rdk_SetSetExtraRequirementParameterCallback(RenderContent.m_SetExtraRequirementParameter);
        UnsafeNativeMethods.Rdk_SetGetExtraRequirementParameterCallback(RenderContent.m_GetExtraRequirementParameter);

        //Materials
        UnsafeNativeMethods.Rdk_SetTextureChildSlotNameCallback(RenderMaterial.m_TextureChildSlotName);
        UnsafeNativeMethods.Rdk_SetSimulateMaterialCallback(RenderMaterial.m_SimulateMaterial);

        //Environments
        UnsafeNativeMethods.Rdk_SetSimulateEnvironmentCallback(RenderEnvironment.m_SimulateEnvironment);

        //CustomRenderMeshes
#pragma warning disable 612
        UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_DeleteThis(CustomRenderMeshProvider.DeleteThis);
        UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_WillBuild(CustomRenderMeshProvider.WillBuild);
        UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_BBox(CustomRenderMeshProvider.BBox);
        UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_Build(CustomRenderMeshProvider.Build);
#pragma warning restore 612

        //IoPlugins
        UnsafeNativeMethods.Rdk_SetRenderContentIoDeleteThisCallback(RenderContentSerializer.m_DeleteThis);
        UnsafeNativeMethods.Rdk_SetRenderContentIoLoadCallback(RenderContentSerializer.m_Load);
        UnsafeNativeMethods.Rdk_SetRenderContentIoSaveCallback(RenderContentSerializer.m_Save);
        UnsafeNativeMethods.Rdk_SetRenderContentIoStringCallback(RenderContentSerializer.m_GetRenderContentIoString);

        //SdkRender
        UnsafeNativeMethods.Rdk_SetSdkRenderCallback(RenderPipeline.m_ReturnBoolGeneralCallback);

        //AsyncRenderContext
        UnsafeNativeMethods.Rdk_SetAsyncRenderContextCallbacks(
          AsyncRenderContext.m_deleteThis,
          AsyncRenderContext.m_stopRendering
          );

        //ChangeQueue
        UnsafeNativeMethods.Rdk_ChangeQueue_SetCallbacks(
          ChangeQueue.ChangeQueue.m_on_apply_viewchange,
          ChangeQueue.ChangeQueue.m_on_apply_meshchanges,
          ChangeQueue.ChangeQueue.m_on_apply_materialchanges,
          ChangeQueue.ChangeQueue.m_on_notify_beginupdates,
          ChangeQueue.ChangeQueue.m_on_notify_endupdates,
          ChangeQueue.ChangeQueue.m_on_notify_dynamicupdatesareavailable,
          ChangeQueue.ChangeQueue.m_on_bakefor,
          ChangeQueue.ChangeQueue.m_on_bakingsize,
          ChangeQueue.ChangeQueue.m_on_displaypipelineattributeschanges,
          ChangeQueue.ChangeQueue.m_on_apply_meshinstancechanges,
          ChangeQueue.ChangeQueue.m_on_apply_groundplanechanges,
          ChangeQueue.ChangeQueue.m_on_apply_dynamicobject_transforms,
          ChangeQueue.ChangeQueue.m_on_apply_lightchanges,
          ChangeQueue.ChangeQueue.m_on_apply_dynamic_lightchanges,
          ChangeQueue.ChangeQueue.m_on_apply_sunchanges,
          ChangeQueue.ChangeQueue.m_on_apply_skylightchanges,
          ChangeQueue.ChangeQueue.m_on_apply_environmentchanges,
          ChangeQueue.ChangeQueue.m_on_apply_backgroundchanges,
          ChangeQueue.ChangeQueue.m_on_apply_linearworkflowchanges,
          ChangeQueue.ChangeQueue.m_on_provideoriginalobject,
          ChangeQueue.ChangeQueue.m_on_apply_clippingplanechanges,
          ChangeQueue.ChangeQueue.m_on_apply_dynamicclippingplanechanges
        );

        //RealtimeDisplayMode
        UnsafeNativeMethods.Rdk_RtVpImpl_SetCallbacks(
          RealtimeDisplayMode.m_on_create_world,
          RealtimeDisplayMode.m_on_start_renderer,
          RealtimeDisplayMode.m_on_rendersize_changed,
          RealtimeDisplayMode.m_on_shutdown_renderer,
          RealtimeDisplayMode.m_on_lastrenderedpass,
          RealtimeDisplayMode.m_on_renderer_started,
          RealtimeDisplayMode.m_on_iscompleted,
          RealtimeDisplayMode.m_on_framebuffer_available,
          RealtimeDisplayMode.m_on_showcaptureprogress,
          RealtimeDisplayMode.m_on_captureprogress,

          RealtimeDisplayMode.m_on_displaypipeline_settings_changed,
          RealtimeDisplayMode.m_on_drawmiddleground,
          RealtimeDisplayMode.m_on_initframebuffer,

          RealtimeDisplayMode.m_drawopengl,
          RealtimeDisplayMode.m_usefastdraw,

          RealtimeDisplayMode.m_on_hud_productname,
          RealtimeDisplayMode.m_on_hud_statustext,
          RealtimeDisplayMode.m_on_hud_maximumpasses,
          RealtimeDisplayMode.m_on_hud_lastrenderedpass,
          RealtimeDisplayMode.m_on_hud_rendererpaused,
          RealtimeDisplayMode.m_on_hud_rendererlocked,
          RealtimeDisplayMode.m_on_hud_showmaxpasses,
          RealtimeDisplayMode.m_on_hud_showpasses,
          RealtimeDisplayMode.m_on_hud_showstatustext,
          RealtimeDisplayMode.m_on_hud_showcontrols,
          RealtimeDisplayMode.m_on_hud_show,
          RealtimeDisplayMode.m_on_hud_starttime,

          RealtimeDisplayMode.m_on_hud_playbutton_pressed,
          RealtimeDisplayMode.m_on_hud_pausebutton_pressed,
          RealtimeDisplayMode.m_on_hud_lockbutton_pressed,
          RealtimeDisplayMode.m_on_hud_unlockbutton_pressed,
          RealtimeDisplayMode.m_on_hud_productname_pressed,
          RealtimeDisplayMode.m_on_hud_statustext_pressed,
          RealtimeDisplayMode.m_on_hud_time_pressed,
          RealtimeDisplayMode.m_on_hud_max_passes_changed,
          RealtimeDisplayMode.m_on_hud_allow_edit_maxpasses
          );

        UnsafeNativeMethods.Rdk_SetSunAndRenderSettingsCallbacks(Rhino.PlugIns.PlugIn.m_OnAddPagesToSunPanel, Rhino.PlugIns.PlugIn.m_OnAddPagesToRenderSettingsPanel);

        UnsafeNativeMethods.Rdk_LightManagerSupport_SetCallbacks(
        LightManagerSupport.renderengineid,
        LightManagerSupport.pluginid,
        LightManagerSupport.modifylight,
        LightManagerSupport.deletelight,
        LightManagerSupport.getlights,
        LightManagerSupport.lightfromid,
        LightManagerSupport.objectserialnumberfromlight,
        LightManagerSupport.oneditlight,
        LightManagerSupport.grouplights,
        LightManagerSupport.ungroup,
        LightManagerSupport.lightdescription,
        LightManagerSupport.setlightsolo,
        LightManagerSupport.getlightsolo,
        LightManagerSupport.lightsinsolostorage
        );

        g_callbacks_set = true;
      }
      else
      {
        if (g_callbacks_set)
        {
          UnsafeNativeMethods.Rdk_SetNewTextureCallback(null);
          UnsafeNativeMethods.Rdk_SetNewMaterialCallback(null);
          UnsafeNativeMethods.Rdk_SetNewEnvironmentCallback(null);

          UnsafeNativeMethods.Rdk_SetRenderContentOnCppDtorCallback(null);
          UnsafeNativeMethods.Rdk_SetRenderContentBitFlagsCallback(null);
          UnsafeNativeMethods.Rdk_SetContentStringCallback(null);
          UnsafeNativeMethods.Rdk_SetNewTextureEvaluatorCallback(null);
          UnsafeNativeMethods.Rdk_SetTextureEvaluatorCallbacks(null, null, null);
          UnsafeNativeMethods.Rdk_SetSimulateTextureCallback(null);
          UnsafeNativeMethods.RdkSetTextureSetVirtualIntCallback(null);
          UnsafeNativeMethods.RdkSetTextureGetVirtualIntCallback(null);
          UnsafeNativeMethods.Rdk_SetAddUISectionsCallback(null);
          UnsafeNativeMethods.Rdk_SetGetDefaultsFromUserCallback(null);
          UnsafeNativeMethods.Rdk_SetIsContentTypeAcceptableAsChildCallback(null);
          UnsafeNativeMethods.Rdk_SetHarvestDataCallback(null);
          UnsafeNativeMethods.Rdk_SetGetShaderCallback(null);
          UnsafeNativeMethods.Rdk_SetGetContentIconCallback(null);
          UnsafeNativeMethods.Rdk_SetRenderContentRenderCrcCallback(null);

          //Materials
          UnsafeNativeMethods.Rdk_SetTextureChildSlotNameCallback(null);
          UnsafeNativeMethods.Rdk_SetSimulateMaterialCallback(null);

          //Environments
          UnsafeNativeMethods.Rdk_SetSimulateEnvironmentCallback(null);

          //CustomRenderMeshes
          UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_DeleteThis(null);
          UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_WillBuild(null);
          UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_BBox(null);
          UnsafeNativeMethods.Rdk_SetCallback_CRMProvider_Build(null);

          //IoPlugins
          UnsafeNativeMethods.Rdk_SetRenderContentIoDeleteThisCallback(null);
          UnsafeNativeMethods.Rdk_SetRenderContentIoLoadCallback(null);
          UnsafeNativeMethods.Rdk_SetRenderContentIoSaveCallback(null);
          UnsafeNativeMethods.Rdk_SetRenderContentIoStringCallback(null);
          //SdkRender
          UnsafeNativeMethods.Rdk_SetSdkRenderCallback(null);

          // AsyncRenderContext
          UnsafeNativeMethods.Rdk_SetAsyncRenderContextCallbacks(null, null);

          // ChangeQueue
          UnsafeNativeMethods.Rdk_ChangeQueue_SetCallbacks(
            null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null,
            null, null
          );

          //RealtimeDisplayMode
          UnsafeNativeMethods.Rdk_RtVpImpl_SetCallbacks(null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null);

          UnsafeNativeMethods.Rdk_SetSunAndRenderSettingsCallbacks(null, null);

          // LightManagerSupport
          UnsafeNativeMethods.Rdk_LightManagerSupport_SetCallbacks(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null);

          g_callbacks_set = false;
        }
      }
    }
    /// <summary>
    /// Dictionary of valid RdkPlugIn's
    /// </summary>
    static readonly Dictionary<Guid, RdkPlugIn> g_rdk_plugin_dictionary = new Dictionary<Guid, RdkPlugIn>();
    /// <summary>
    /// Find loaded RdkPlugIn in the Render Development Kit(RDK) plug-in
    /// dictionary.
    /// </summary>
    /// <param name="plugInId">Plug-in Id to search for</param>
    /// <returns>
    /// If a plug-in with the specified Id is found in the dictionary then the
    /// plug-in object is returned, if not found then null is returned.
    /// </returns>
    public static RdkPlugIn FromPlugInId(Guid plugInId)
    {
      RdkPlugIn found;
      g_rdk_plugin_dictionary.TryGetValue(plugInId, out found);
      return found;
    }
    /// <summary>
    /// Search the Render Development Kit(RDK) plug-in dictionary for a render
    /// plug-in with the matching Rhino plug-in Id.
    /// </summary>
    /// <param name="plugIn">Rhino plug-in to search for.</param>
    /// <returns>
    /// If the plug-in is in the RDK dictionary then the dictionary plug-in
    /// object is returned, if not then null is returned.
    /// </returns>
    public static RdkPlugIn FromRhinoPlugIn(PlugIns.PlugIn plugIn)
    {
      return ((null == plugIn) ? null : FromPlugInId(plugIn.Id));
    }
    /// <summary>
    /// Check each RdkPlugIn in the Render Development Kit(RDK) dictionary and
    /// return the first one found that contains the specified content contentType.
    /// </summary>
    /// <param name="type">Class contentType to search for</param>
    /// <returns>
    /// If there is a plug-in in the dictionary that registered the requested
    /// contentType then return the plug-in, if not then return null.
    /// </returns>
    public static RdkPlugIn FromRenderContentClassType(Type type)
    {
      foreach (var item in g_rdk_plugin_dictionary)
        if (item.Value.m_render_content_types.Contains(type))
          return item.Value;
      return null;
    }

    public static RdkPlugIn FromRealtimeDisplayModeClassType(RealtimeDisplayModeClassInfo type)
    {
      foreach (var item in g_rdk_plugin_dictionary)
        if (item.Value.m_rendered_displaymode_types.Contains(type))
          return item.Value;
      return null;
    }
    /// <summary>
    /// Search the plug-in's dictionary for the specified plug-in and if it is
    /// not found then add it to the dictionary.
    /// </summary>
    /// <param name="plugIn"></param>
    /// <returns></returns>
    internal static RdkPlugIn GetRdkPlugIn(PlugIns.PlugIn plugIn)
    {
      var found = FromRhinoPlugIn(plugIn);
      if (null != found) return found;
      var plugin_pointer = plugIn.NonConstPointer();
      return AddPlugInToDictionary(plugin_pointer, plugIn.Id, plugIn.m_runtime_serial_number);
    }
    /// <summary>
    /// If the specified plug-in is not currently in the plug-in dictionary do
    /// nothing otherwise; see AddPlugInToDictionary for a description of what
    /// happens.
    /// </summary>
    /// <param name="rhinoPlugInId">Id of the plug-in to search for or add.</param>
    /// <param name="serialNumber">Plug-in C++ pointer serial number.</param>
    /// <returns>
    /// A plug-in object from the plug-in dictionary or null if there was a
    /// problem adding a new dictionary plug-in.
    /// </returns>
    internal static RdkPlugIn GetRdkPlugIn(Guid rhinoPlugInId, int serialNumber)
    {
      var found = FromPlugInId(rhinoPlugInId);
      if (null != found) return found;

      var ptr_rhino_plugin = UnsafeNativeMethods.CRhinoPlugInManager_GetPlugInFromId(rhinoPlugInId, true);

      if (IntPtr.Zero == ptr_rhino_plugin)
      {
        var rhcmn_plugin = PlugIns.PlugIn.GetLoadedPlugIn(rhinoPlugInId);
        if (null != rhcmn_plugin)
          ptr_rhino_plugin = rhcmn_plugin.NonConstPointer();
      }

      return AddPlugInToDictionary(ptr_rhino_plugin, rhinoPlugInId, serialNumber);
    }
    /// <summary>
    /// Create a new C++ runtime RDK plug-in object then create a RhinoCommon
    /// RdkPlugIn, attach the C++ pointer to it and set the C++ callback
    /// function pointers to the RdkPlugIn.
    /// </summary>
    /// <param name="rhinoPlugIn">
    /// The C++ pointer to the native CRhinoPlugIn that is causing this RDK
    /// plug-in to get added.
    /// </param>
    /// <param name="rhinoPlugInId">The plug-in Id</param>
    /// <param name="serialNumber">Plug-in C++ pointer serial number</param>
    /// <returns></returns>
    static private RdkPlugIn AddPlugInToDictionary(IntPtr rhinoPlugIn, Guid rhinoPlugInId, int serialNumber)
    {
      if (rhinoPlugIn != IntPtr.Zero)
      {
        SetRdkCallbackFunctions(true);
        var ptr_rdk_plugin = UnsafeNativeMethods.CRhCmnRdkPlugIn_New(rhinoPlugIn, serialNumber);
        if (ptr_rdk_plugin != IntPtr.Zero)
        {
          var plugin = new RdkPlugIn(ptr_rdk_plugin, rhinoPlugInId);
          g_rdk_plugin_dictionary.Add(rhinoPlugInId, plugin);
          return plugin;
        }
      }
      return null;
    }
    /// <summary>
    /// If there the plug-in dictionary contains a plug-in that registered the
    /// specified content type then return true otherwise; return false;
    /// </summary>
    /// <param name="contentType">Class type to search for.</param>
    /// <returns>
    /// If there the plug-in dictionary contains a plug-in that registered the
    /// specified content contentType then return true otherwise; return false;
    /// </returns>
    public static bool RenderContentTypeIsRegistered(Type contentType)
    {
      return (null != FromRenderContentClassType(contentType));
    }
    /// <summary>
    /// Search the plug-in dictionary for a plug-in for that registered the
    /// specified class type Guid, if it is found then return the class
    /// type and plug-in ID otherwise; return null.
    /// </summary>
    /// <param name="id">Class GUUID attribute value to search for.</param>
    /// <param name="pluginId">
    /// Output parameter, will be set to the Id of the plug-in that registered
    /// the class type or Guid.Empty if the type is not found.
    /// </param>
    /// <returns>
    /// Returns the class type and Id of the plug-in that registered the class
    /// type if the Id is found otherwise; return null.
    /// </returns>
    public static Type GetRenderContentType(Guid id, out Guid pluginId)
    {
      pluginId = Guid.Empty;
      foreach (var item in g_rdk_plugin_dictionary)
      {
        var found_type = item.Value.m_render_content_types.Find(t => t.GUID == id);
        if (found_type == null) continue;
        pluginId = item.Key;
        return found_type;
      }
      return null;
    }
    public static Type GetRealtimeDisplayModeType(Guid id, out Guid pluginId)
    {
      pluginId = Guid.Empty;
      foreach (var item in g_rdk_plugin_dictionary)
      {
        var found_type = item.Value.m_rendered_displaymode_types.Find(t => t.GUID == id);
        if (found_type == null) continue;
        pluginId = item.Key;
        return found_type.RealtimeDisplayModeType;
      }
      return null;
    }
    public static bool RealtimeDisplayModeTypeIsRegistered(RealtimeDisplayModeClassInfo rtvpType)
    {
      return (null != FromRealtimeDisplayModeClassType(rtvpType));
    }
    #endregion statics

    #region class members
    /// <summary>
    /// CRhinoPlugIn Id that owns this RdkPlugIn
    /// </summary>
    readonly Guid m_rhino_plug_in_id;
    /// <summary>
    /// The RDK C++ CRdkPlugIn pointer associated with this object
    /// </summary>
    readonly IntPtr m_rdk_plug_in_pointer = IntPtr.Zero;
    /// <summary>
    /// List of valid RenderContent class types associated with this plug-in.
    /// </summary>
    readonly List<Type> m_render_content_types = new List<Type>();

    /// <summary>
    /// List of valid RealtimeDisplayMode class types associated with this plug-in.
    /// </summary>
    readonly List<RealtimeDisplayModeClassInfo> m_rendered_displaymode_types = new List<RealtimeDisplayModeClassInfo>();
    #endregion class members

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rdkPlugInPointer">
    /// C++ Pointer to the CRhinoPlugIn that this plug-in is associated with.
    /// </param>
    /// <param name="rhinoPlugInId">C++ CRhinoPlugIn Id</param>
    private RdkPlugIn(IntPtr rdkPlugInPointer, Guid rhinoPlugInId)
    {
      m_rdk_plug_in_pointer = rdkPlugInPointer;
      m_rhino_plug_in_id = rhinoPlugInId;
    }
    /// <summary>
    /// Add list of class types, the type list has been sanitized and should
    /// only contain valid RenderContent class types, this should only be
    /// called from RenderContent.RegisterContent.
    /// </summary>
    /// <param name="types">Types to add to the plug-ins contenet type list.</param>
    internal void AddRegisteredContentTypes(IEnumerable<Type> types)
    {
      m_render_content_types.AddRange(types);
    }

    internal void AddRegisteredRealtimeDisplayModeTypes(IEnumerable<RealtimeDisplayModeClassInfo> types)
    {
      m_rendered_displaymode_types.AddRange(types);
    }

    internal void AddRegisteredLightManagerSupport(IntPtr pLightManager)
    {
      UnsafeNativeMethods.Rdk_AddLightManager(pLightManager);
    }

    #region IDisposable Members
    /// <summary>
    /// Required IDisposable method
    /// </summary>
    public void Dispose()
    {
      // We need to find the reference to this thing in the list, un-initialize the C++
      // object, delete it and then remove it to actually make sure thing gets garbage
      // collected.
      RdkPlugIn plugin;
      if (g_rdk_plugin_dictionary.TryGetValue(m_rhino_plug_in_id, out plugin))
      {
        g_rdk_plugin_dictionary.Remove(m_rhino_plug_in_id);
        // This is now handled by the rhcommonrdk_c.dll
        //UnsafeNativeMethods.CRhCmnRdkPlugIn_Delete(plugin.m_rdk_plug_in_pointer);
      }
    }
    #endregion
  }
}
#endif