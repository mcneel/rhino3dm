#pragma warning disable 1591
using System;
using System.Collections.Generic;
using DE = Rhino.RDK.Delegates;

namespace Rhino.Render
{

  public sealed class LightManagerSupportClient : IDisposable
  {
    private IntPtr m_cpp;
    private uint m_doc_uuid;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    [CLSCompliant(false)]
    public LightManagerSupportClient(uint doc_uuid)
    {
      m_doc_uuid = doc_uuid;
      m_cpp = UnsafeNativeMethods.CRhRdkLightManagerSupport_New();
    }

    ~LightManagerSupportClient()
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

    public LightArray GetLights()
    {
      LightArray lights = new LightArray();

      UnsafeNativeMethods.CRhRdkLightManagerSupport_GetLights(m_cpp, m_doc_uuid, lights.CppPointer);

      return lights;
    }

    public Rhino.Geometry.Light GetLightFromId(Guid uuid)
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        IntPtr p_light = UnsafeNativeMethods.CRhRdkLightManagerSupport_LightFromId(m_cpp, m_doc_uuid, uuid);
        if (p_light != IntPtr.Zero)
        {
          var l = new Rhino.Geometry.Light(p_light, null);
          l.DoNotDestructOnDispose();
          return l;
        }
      }

      return null;
    }

    public void OnEditLight(LightArray lights)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkLightManagerSupport_OnEditLight(m_cpp, m_doc_uuid, lights.CppPointer);
      }
    }

    public void GroupLights(LightArray lights)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkLightManagerSupport_GroupLights(m_cpp, m_doc_uuid, lights.CppPointer);
      }
    }

    public void UnGroup(LightArray lights)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkLightManagerSupport_UnGroup(m_cpp, m_doc_uuid, lights.CppPointer);
      }
    }

    public void ModifyLight(Rhino.Geometry.Light light)
    {
      UnsafeNativeMethods.CRhRdkLightManagerSupport_ModifyLight(m_cpp, m_doc_uuid, light.ConstPointer());
    }

    public void DeleteLight(Rhino.Geometry.Light light)
    {
      UnsafeNativeMethods.CRhRdkLightManagerSupport_DeleteLight(m_cpp, m_doc_uuid, light.ConstPointer());
    }

    public string LightDescription(Rhino.Geometry.Light light)
    {
      Rhino.Runtime.InteropWrappers.StringWrapper sh = new Rhino.Runtime.InteropWrappers.StringWrapper();

      UnsafeNativeMethods.CRhRdkLightManagerSupport_LightDescription(m_cpp, m_doc_uuid, light.ConstPointer(), sh.ConstPointer);
      return sh.ToString();
    }

    public Rhino.DocObjects.RhinoObject ObjectFromLight(Rhino.Geometry.Light light)
    {
      uint sn = UnsafeNativeMethods.CRhRdkLightManagerSupport_ObjectSerialNumberFromLight(m_cpp, m_doc_uuid, light.ConstPointer());
      if (sn >= 1)
        return new Rhino.DocObjects.RhinoObject(sn);

      return null;
    }

    public bool SetLightSolo(Rhino.Geometry.Light light, bool bSolo)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_SetLightSolo(m_cpp, m_doc_uuid, light.Id, bSolo);
    }

    public bool GetLightSolo(Rhino.Geometry.Light light)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_GetLightSolo(m_cpp, m_doc_uuid, light.Id);
    }

    public int LightsInSoloStorage()
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_LightsInSoloStorage(m_cpp, m_doc_uuid);
    }
  }

  public sealed class LightArray : IDisposable
  {
    private IntPtr m_cpp;
    private bool m_owns_pointer;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public LightArray()
    {
      m_owns_pointer = true;
      m_cpp = UnsafeNativeMethods.LightArray_New();
    }

    public LightArray(IntPtr pLightArray)
    {
      m_owns_pointer = false;
      m_cpp = pLightArray;
    }

    ~LightArray()
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
        if (m_owns_pointer)
          UnsafeNativeMethods.LightArray_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public Rhino.Geometry.Light ElementAt(int index)
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        IntPtr p_light = UnsafeNativeMethods.LightArray_ElementAt(m_cpp, index);
        var l = new Rhino.Geometry.Light(p_light, null);
        l.DoNotDestructOnDispose();
        return l;
      }

      return null;
    }

    public void Append(Rhino.Geometry.Light light)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.LightArray_Append(m_cpp, light.ConstPointer());
      }
    }

    public int Count()
    {
      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.LightArray_Count(m_cpp);
      }
      return -1;
    }
  }

  /// <summary>
  /// LightMangerSupportCustomEvent
  /// </summary>
  public enum LightMangerSupportCustomEvent
  {
    light_added,
    light_deleted,
    light_undeleted,
    light_modified,
    light_sorted,
  };

  /// <summary>
  /// Base class for implementing custom light managers in .NET
  /// </summary>
  public abstract class LightManagerSupport
  {
    private IntPtr m_cpp_pointer;
    private int m_runtime_serial_number;
    private static int g_current_serial_number = 1;
    private static readonly Dictionary<int, LightManagerSupport> g_all_providers = new Dictionary<int, LightManagerSupport>();

    /// <summary>
    /// Default constructor
    /// </summary>
    protected LightManagerSupport()
    {

    }

    /// <summary>
    /// The C++ callbacks call FromSerialNumber method in order to call the
    /// correct C# LightManagerSupport instance.
    /// </summary>
    /// <returns></returns>
    private static LightManagerSupport FromSerialNumber(int serialNumber)
    {
      LightManagerSupport rc;
      g_all_providers.TryGetValue(serialNumber, out rc);
      return rc;
    }

    /// <summary>
    /// Find and register classes that derive from LightManagerSupport
    /// from the given plug-in.
    /// </summary>
    /// <param name="plugin"></param>
    /// <returns></returns>
    public static void RegisterLightManager(PlugIns.PlugIn plugin)
    {
      RegisterProviders(plugin.Assembly, plugin.Id);
    }

    /// <summary>
    /// RegisterProviders calls this method to create a runtime unmanaged
    /// pointer for each custom light manager registered.
    /// </summary>
    /// <returns></returns>
    private IntPtr CreateCppObject()
    {
      m_runtime_serial_number = g_current_serial_number++;

      m_cpp_pointer = UnsafeNativeMethods.CRdkCmnLightManagerSupport_New(m_runtime_serial_number);

      return m_cpp_pointer;
    }

    /// <summary>
    /// Find and register classes that derive from RealtimeDisplayMode
    /// from the given plug-in. The plug-in is found in the given assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="pluginId"></param>
    /// <returns></returns>
    public static void RegisterProviders(System.Reflection.Assembly assembly, Guid pluginId)
    {
      var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
      if (plugin == null)
        return;

      var exported_types = assembly.GetExportedTypes();
      var provider_types = new List<Type>();
      var custom_type = typeof(LightManagerSupport);
      var options = new Type[] { };

      foreach (var type in exported_types)
        if (!type.IsAbstract && type.IsSubclassOf(custom_type) && type.GetConstructor(options) != null)
          provider_types.Add(type);

      if (provider_types.Count == 0)
        return;

      var rdk_plugin = RdkPlugIn.GetRdkPlugIn(plugin);
      if (rdk_plugin == null)
        return;

      foreach (var type in provider_types)
      {
        var provider = Activator.CreateInstance(type) as LightManagerSupport;
        if (provider == null) continue;
        var cpp_object = provider.CreateCppObject();
        if (cpp_object == IntPtr.Zero) continue;
        g_all_providers.Add(provider.m_runtime_serial_number, provider);

        rdk_plugin.AddRegisteredLightManagerSupport(cpp_object);
      }
    }

    bool SaveToSoloStorage(RhinoDoc doc, Rhino.Geometry.Light light)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_SaveToSoloStorage(m_cpp_pointer, doc.RuntimeSerialNumber, light.Id);
    }

    bool IsInSoloStorage(RhinoDoc doc, Rhino.Geometry.Light light)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_IsInSoloStorage(m_cpp_pointer, doc.RuntimeSerialNumber, light.Id);
    }

    bool RestoreFromSolo(RhinoDoc doc, Rhino.Geometry.Light light)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_RestoreFromSolo(m_cpp_pointer, doc.RuntimeSerialNumber, light.Id);
    }

    #region Public API

    /// <summary>
    ///  The Guid of the plugin 
    /// </summary>
    /// <returns>Returns the Guid of the plugin</returns>
    public abstract Guid PluginId();

    /// <summary>
    ///  The Guid of the render engine 
    /// </summary>
    /// <returns>Returns the Guid of the render engine that is associated with this light manager</returns>
    public abstract Guid RenderEngineId();

    /// <summary>
    ///  Modify properties of the light 
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light"></param>
    /// <returns></returns>
    public abstract void ModifyLight(RhinoDoc doc, Rhino.Geometry.Light light);

    /// <summary>
    ///  Delete light
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light"></param>
    /// <param name="bUndelete"></param>
    /// <returns>If delete is successful, then return true, else return false</returns>
    public abstract bool DeleteLight(RhinoDoc doc, Rhino.Geometry.Light light, bool bUndelete);

    /// <summary>
    ///  Get all the lights that are associated to the light manager. The lights are added 
    ///  to the LightArray parameter passed to the GetLights method
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light_array"></param>
    /// <return></return>
    public abstract void GetLights(RhinoDoc doc, ref LightArray light_array);

    /// <summary>
    ///  Get Rhino.Geometry.Light object associated to Guig uuid
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="uuid"></param>
    /// <param name="light"></param>
    /// <return>If found, then return true, else return false</return>
    public abstract bool LightFromId(RhinoDoc doc, Guid uuid, ref Rhino.Geometry.Light light);

    /// <summary>
    ///  Get the object serial number of the light
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light"></param>
    /// <return>returns the object serial number of light</return>
    public abstract int ObjectSerialNumberFromLight(RhinoDoc doc, ref Rhino.Geometry.Light light);

    /// <summary>
    ///  The default implementation of OnEditLight selects the lights and opens
    ///  the Lights Properties page
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light_array"></param>
    /// <returns>Returns true if successful, else false</returns>
    public abstract bool OnEditLight(RhinoDoc doc, ref LightArray light_array);

    /// <summary>
    /// Creates a new group with the lights
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light_array"></param>
    public abstract void GroupLights(RhinoDoc doc, ref LightArray light_array);

    /// <summary>
    /// UnGroups the lights
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light_array"></param>
    public abstract void UnGroup(RhinoDoc doc, ref LightArray light_array);

    /// <summary>
    ///  Gets the string representation of the light description 
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="light"></param>
    /// <returns>Returns the string representation of the light description</returns>
    public abstract string LightDescription(RhinoDoc doc, ref Rhino.Geometry.Light light);

    /// <summary>
    /// First checks to see if we are in "solo mode" - which means that there are any lights that respond "true" to IsInSoloStorage.
    /// If in solo mode:
    ///  If bSolo = true
    ///   Sets this light on.
    ///  If bSolo = false
    ///   If this is the last light "on", forces all lights out of solo mode.
    ///   If there are other lights on, turns this light off.
    /// If not in solo mode:
    ///  If bSolo = true
    ///   Forces all lights into solo storage and sets this light on.
    ///  If bSolo = false
    ///   This shouldn't happen.  Will cause an ASSERT
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="uuid_light"></param>
    /// <param name="bSolo"></param>
    /// <returns>Returns true if action is successful</returns>
    public virtual bool SetLightSolo(RhinoDoc doc, Guid uuid_light, bool bSolo)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_SetLightSolo(m_cpp_pointer, doc.RuntimeSerialNumber, uuid_light, bSolo);
    }

    /// <summary>
    ///  Returns the value of "ON_LIght::m_bOn" if the light is in solo storage, or 
    ///  false if not in solo storage (ie - this is the checkbox state on the light manager dialog)
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="uuid_light"></param>
    /// <returns>Returns true if the light is in solo storage, or false if not in solo storage</returns>
    public virtual bool GetLightSolo(RhinoDoc doc, Guid uuid_light)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_GetLightSolo(m_cpp_pointer, doc.RuntimeSerialNumber, uuid_light);
    }

    /// <summary>
    ///  Returns the number of lights in solo storage - any number other than 0 means "in solo mode"
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>Returns the number of lights in solo storage - any number other than 0 means "in solo mode"</returns>
    public virtual int LightsInSoloStorage(RhinoDoc doc)
    {
      return UnsafeNativeMethods.CRhRdkLightManagerSupport_LightsInSoloStorage(m_cpp_pointer, doc.RuntimeSerialNumber);
    }

    #endregion  Public API

    /// <summary>
    ///  Generates LightMangerSupportCustomEvent: 
    ///    light_added,
    ///    light_deleted,
    ///    light_undeleted,
    ///    light_modified,
    ///    light_sorted,
    ///  The event triggers a Light table event that the rdk lightmanager listens too
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="le"></param>
    /// <param name="light"></param>
    /// <returns>Returns the string representation of the light description</returns>
    public void OnCustomLightEvent(RhinoDoc doc, LightMangerSupportCustomEvent le, ref Rhino.Geometry.Light light)
    {
      UnsafeNativeMethods.CRdkCmnLightManagerSupport_OnCustomLightEvent(m_cpp_pointer, doc.RuntimeSerialNumber, (int)le);
    }

    #region callbacks

    internal delegate IntPtr LIGHTDESCRIPTIONPROC(int sn, uint doc_sn, IntPtr pLight);
    internal static LIGHTDESCRIPTIONPROC lightdescription = LightDescription;
    static IntPtr LightDescription(int serialNumber, uint doc_sn, IntPtr pLight)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        var l = new Rhino.Geometry.Light(pLight, null);
        l.DoNotDestructOnDispose();

        string description = provider.LightDescription(doc, ref l);
        Rhino.Runtime.InteropWrappers.StringWrapper sh = new Rhino.Runtime.InteropWrappers.StringWrapper(description);
        return sh.ConstPointer;
      }
      return IntPtr.Zero;
    }

    internal delegate int ONEDITLIGHTPROC(int sn, uint doc_sn, IntPtr pLights_array);
    internal static ONEDITLIGHTPROC oneditlight = OnEditLight;
    static int OnEditLight(int serialNumber, uint doc_sn, IntPtr pLights_array)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        LightArray light_array = new LightArray(pLights_array);

        return provider.OnEditLight(doc, ref light_array) ? 1 : 0;
      }
      return 0;
    }

    internal delegate void GROUPLIGHTSPROC(int sn, uint doc_sn, IntPtr pLights_array);
    internal static GROUPLIGHTSPROC grouplights = GroupLights;
    static void GroupLights(int serialNumber, uint doc_sn, IntPtr pLights_array)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        LightArray light_array = new LightArray(pLights_array);

        provider.GroupLights(doc, ref light_array);
      }
    }

    internal delegate void UNGROUPPROC(int sn, uint doc_sn, IntPtr pLights_array);
    internal static UNGROUPPROC ungroup = UnGroup;
    static void UnGroup(int serialNumber, uint doc_sn, IntPtr pLights_array)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        LightArray light_array = new LightArray(pLights_array);

        provider.UnGroup(doc, ref light_array);
      }
    }

    internal delegate int OBJECTSERIALNUMBERFROMLIGHTPROC(int sn, uint doc_sn, IntPtr pLight);
    internal static OBJECTSERIALNUMBERFROMLIGHTPROC objectserialnumberfromlight = ObjectSerialNumberFromLight;
    static int ObjectSerialNumberFromLight(int serialNumber, uint doc_sn, IntPtr pLight)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        var l = new Rhino.Geometry.Light(pLight, null);
        l.DoNotDestructOnDispose();

        return provider.ObjectSerialNumberFromLight(doc, ref l);
      }
      return 0;
    }

    internal delegate IntPtr LIGHTFROMIDPROC(int sn, uint doc_sn, Guid uuid, IntPtr pLight);
    internal static LIGHTFROMIDPROC lightfromid = LightFromId;
    static IntPtr LightFromId(int serialNumber, uint doc_sn, Guid uuid, IntPtr pLight)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        var l = new Rhino.Geometry.Light(pLight, null);
        l.DoNotDestructOnDispose();

        provider.LightFromId(doc, uuid, ref l);
        return l.ConstPointer();
      }
      return IntPtr.Zero;
    }

    internal delegate void GETLIGHTSPROC(int sn, uint doc_sn, IntPtr pLights_array);
    internal static GETLIGHTSPROC getlights = GetLights;
    static void GetLights(int serialNumber, uint doc_sn, IntPtr pLights_array)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        LightArray light_array = new LightArray(pLights_array);

        provider.GetLights(doc, ref light_array);
      }
    }

    internal delegate int DELETELIGHTPROC(int sn, uint doc_sn, IntPtr pLight, int bUndelete);
    internal static DELETELIGHTPROC deletelight = DeleteLight;
    static int DeleteLight(int serialNumber, uint doc_sn, IntPtr pLight, int bUndelete)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        var l = new Rhino.Geometry.Light(pLight, null);
        l.DoNotDestructOnDispose();
        bool bool_value = bUndelete == 1 ? true : false;
        return provider.DeleteLight(doc, l, bool_value) ? 1 : 0;
      }
      return 0;
    }

    internal delegate void MODIFYLIGHTPROC(int sn, uint doc_sn, IntPtr pLight);
    internal static MODIFYLIGHTPROC modifylight = ModifyLight;
    static void ModifyLight(int serialNumber, uint doc_sn, IntPtr pLight)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);

        var l = new Rhino.Geometry.Light(pLight, null);
        l.DoNotDestructOnDispose();
        provider.ModifyLight(doc, l);
      }
    }

    internal static DE.GETGUIDPROC renderengineid = RenderEngineId;
    static Guid RenderEngineId(int serialNumber)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        return provider.RenderEngineId();
      }
      return Guid.Empty;
    }

    internal static DE.GETGUIDPROC pluginid = PluginId;
    static Guid PluginId(int serialNumber)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        return provider.PluginId();
      }
      return Guid.Empty;
    }

    internal delegate bool SETLIGHTSOLO(int sn, uint doc_sn, Guid uuid_light, bool bSolo);
    internal static SETLIGHTSOLO setlightsolo = SetLightSolo;
    static bool SetLightSolo(int serialNumber, uint doc_sn, Guid uuid_light, bool bSolo)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);
        return provider.SetLightSolo(doc, uuid_light, bSolo);
      }
      return false;
    }

    internal delegate bool GETLIGHTSOLO(int sn, uint doc_sn, Guid uuid_light);
    internal static GETLIGHTSOLO getlightsolo = GetLightSolo;
    static bool GetLightSolo(int serialNumber, uint doc_sn, Guid uuid_light)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);
        return provider.GetLightSolo(doc, uuid_light);
      }
      return false;
    }

    internal delegate int LIGHTSINSOLOSTORAGE(int sn, uint doc_sn);
    internal static LIGHTSINSOLOSTORAGE lightsinsolostorage = LightsInSoloStorage;
    static int LightsInSoloStorage(int serialNumber, uint doc_sn)
    {
      var provider = FromSerialNumber(serialNumber);
      if (provider != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);
        return provider.LightsInSoloStorage(doc);
      }
      return 0;
    }


    #endregion callbacks
  }
}