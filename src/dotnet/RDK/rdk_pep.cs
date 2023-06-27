
#pragma warning disable 1591

using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
using DE = Rhino.RDK.Delegates;
using static Rhino.Render.RenderContent;
using Rhino.UI.Controls;
#endif

using Rhino.FileIO;
using System.Collections;

namespace Rhino.Render.PostEffects
{
  /// <since>7.0</since>
  public enum PostEffectType : int
  {
    Early = 0,
    ToneMapping = 1,
    Late = 2
  }
}

#if RHINO_SDK
namespace Rhino.Render
{
  /// <summary>
  /// Pixel component order for channels in the RenderWindow and PostEffects.
  /// </summary>
  /// <since>7.0</since>
  public enum ComponentOrders : int
  {
    /// <summary>
    /// Used in single-value channels.
    /// </summary>
    Irrelevant = 0x00,

    /// <summary>
    /// This is the default (to match Rhino 5)
    /// </summary>
    RGBA = 0x01,

    /// <summary>
    /// ARGB component order
    /// </summary>
    ARGB = 0x02,

    /// <summary>
    /// This will only access 3 components, even in the case of the RGBA channel
    /// </summary>
    RGB = 0x03,

    /// <summary>
    /// This will only access 3 components, even in the case of the RGBA channel.
    /// </summary>
    BGR = 0x04,

    /// <summary>
    /// ABGR component order
    /// </summary>
    ABGR = 0x05,

    /// <summary>
    /// BGRA component order
    /// </summary>
    BGRA = 0x06,

    /// <summary>
    /// For readability when using the Normal XYZ channel.  Same as RGB
    /// </summary>
    XYZ = RGB,

    /// <summary>
    /// For readability when using the Normal XYZ channel.  Same as BGR
    /// </summary>
    ZYX = BGR,
  };
}

namespace Rhino.Render.PostEffects
{
  internal class PostEffectBaseList
  {
    public static int serial_number = 0;
    public static List<PostEffect> PostEffectList = new List<PostEffect>();
  }

  internal class PostEffectJobBaseList
  {
    public static int serial_number = 0;
    public static List<PostEffectJob> ClonedPostEffectJobList = new List<PostEffectJob>();
    public static List<WeakReference<PostEffectJob>> PostEffectJobList = new List<WeakReference<PostEffectJob>>();
    public static void Purge(PostEffectJob posteffect)
    {
      int serial = posteffect.SerialNumber;

      lock (PostEffectJobList)
      {
        PostEffectJobList.RemoveAll(reference => reference != null && !reference.TryGetTarget(out PostEffectJob dummy) && dummy == posteffect);
      }
    }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public sealed class CustomPostEffectAttribute : Attribute
  {
    /// <since>7.0</since>
    public CustomPostEffectAttribute(
      PostEffectType postEffectType, 
      string name,
      PostEffectStyles styles = PostEffectStyles.ExecuteForProductionRendering | PostEffectStyles.ExecuteForRealtimeRendering | PostEffectStyles.DefaultOn, 
      PostEffectExecuteWhileRenderingOptions executeWhileRenderingOption = PostEffectExecuteWhileRenderingOptions.Always,
      bool canDisplayHelp = false, 
      int executeWhileRenderingDelay = 0
      )
    {
      Styles = styles;
      ExecuteWhileRenderingOption = executeWhileRenderingOption;
      CanDisplayHelp = canDisplayHelp;
      Name = name;
      ExecuteWhileRenderingDelay = executeWhileRenderingDelay;
      PostEffectType = postEffectType;
    }

    /// <since>7.0</since>
    public PostEffectType PostEffectType { get; set; } = PostEffectType.Early;
    /// <since>7.0</since>
    public PostEffectStyles Styles { get; set; } = PostEffectStyles.ExecuteForProductionRendering;
    /// <since>7.0</since>
    public PostEffectExecuteWhileRenderingOptions ExecuteWhileRenderingOption { get; set; } = PostEffectExecuteWhileRenderingOptions.Always;
    /// <since>7.0</since>
    public bool CanDisplayHelp { get; set; } = false;
    /// <since>7.0</since>
    public string Name { get; set; } = "Unset";
    /// <since>7.0</since>
    public int ExecuteWhileRenderingDelay { get; set; } = 0;
  }

  /// <since>7.0</since>
  [Flags]
  public enum PostEffectStyles : int
  {
    ExecuteForProductionRendering = 0x0001,
    ExecuteForRealtimeRendering   = 0x0002,
    ExecuteForViewportDisplay     = 0x0004,

    Fixed        = 0x0100, // Post effect is always shown and cannot be removed by the user.
    DefaultShown = 0x0200, // Post effect is shown by default.
    DefaultOn    = 0x0400, // Post effect is turned on by default.
  }

  /// <since>7.0</since>
  public enum PostEffectExecuteWhileRenderingOptions : int
  {
    None = 0,     // The post effect does not support execution while rendering.
    Always = 1,   // The post effect supports execution while rendering and it should be run every time the dib is updated.
    UseDelay = 2, // The post effect supports execution while rendering but only after a delay the first time.
  };

  public abstract class PostEffect : IDisposable
  {
    private int m_sn;
    private IntPtr m_cmn_cpp;

    virtual internal IntPtr CppPointer { get { return m_cmn_cpp; } }

    /// <since>7.0</since>
    public int SerialNumber
    {
      get => m_sn;
      set  { m_sn = value; }
    }

    /// <since>7.0</since>
    public PostEffect()
    {
      var type = GetType();
      var type_id = type.GUID;

      var usageFlags = PostEffectStyles.ExecuteForProductionRendering;
      var options = PostEffectExecuteWhileRenderingOptions.Always;
      var name = "Unset";
      var delay = 0;
      var canDisplayHelp = false;
      var postEffectType = PostEffectType.Early;

      //TODO - ensure that there is a real custom GUID attribute

      var attrs = type.GetCustomAttributes(typeof(CustomPostEffectAttribute), false);
      if (attrs.Length > 0)
      {
        foreach (var attr in attrs)
        {
          if (attr is CustomPostEffectAttribute pep_attr)
          {
            usageFlags = pep_attr.Styles;
            options = pep_attr.ExecuteWhileRenderingOption;
            name = pep_attr.Name;
            delay = pep_attr.ExecuteWhileRenderingDelay;
            canDisplayHelp = pep_attr.CanDisplayHelp;
            postEffectType = pep_attr.PostEffectType;
            break;
          }
        }
      }
      else
      {
        throw new Exception("No CustomPostEffectAttribute supplied for custom post effect class.");
      }

      // Create new generic control, serial_number as parameter
      SerialNumber = PostEffectBaseList.serial_number;
      m_cmn_cpp = UnsafeNativeMethods.CRdkCmnPostEffect_New((uint)postEffectType, SerialNumber, type_id, (uint)usageFlags, (uint)options, name, canDisplayHelp, delay);
      PostEffectBaseList.serial_number++;
      PostEffectBaseList.PostEffectList.Add(this);
    }

    //For native post effects
    //The serial number parameter is a hack - it's not used.  It's just for getting this ctor called.
    internal PostEffect(int serial_number)
    {
      m_cmn_cpp = IntPtr.Zero;
      m_sn = serial_number;
    }

    /// <since>7.0</since>
    public Guid Id
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkPostEffect_UUID(CppPointer);
      }
    }

    /// <since>7.0</since>
    public PostEffectType PostEffectType
    {
      get
      {
        return (PostEffectType)UnsafeNativeMethods.IRhRdkPostEffect_Type(CppPointer);
      }
    }

    /// <since>7.0</since>
    public PostEffectStyles Styles
    {
      get
      {
        return (PostEffectStyles)UnsafeNativeMethods.IRhRdkPostEffect_UsageFlags(CppPointer);
      }
    }

    /// <since>7.0</since>
    public string LocalName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.IRhRdkPostEffect_LocalName(CppPointer, p_string);
          return sh.ToString();
        }
      }
    }

    /// <since>7.0</since>
    public bool CanDisplayHelp
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkPostEffect_CanDisplayHelp(CppPointer);
      }
    }

    /// <since>7.0</since>
    public PostEffectExecuteWhileRenderingOptions ExecuteWhileRenderingOption
    {
      get
      {
        return (PostEffectExecuteWhileRenderingOptions)UnsafeNativeMethods.IRhRdkPostEffect_GetExecuteWhileRenderingOption(CppPointer);
      }
    }

//    /// <since>7.0</since>
//    public bool Fixed // [ANDYLOOK] C# SDK change... it is now a usage flag.
//    {
//      get
//      {
//        return UnsafeNativeMethods.IRhRdkPostEffect_Listable_Fixed(CppPointer);
//      }
//      set
//      {
//        UnsafeNativeMethods.IRhRdkPostEffect_Listable_SetFixed(CppPointer, value);
//      }
//    }

    /// <since>7.0</since>
    public bool On
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkPostEffect_Listable_On(CppPointer);
      }
      set
      {
        UnsafeNativeMethods.IRhRdkPostEffect_Listable_SetOn(CppPointer, value);
      }
    }

    /// <since>7.0</since>
    public bool Shown
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkPostEffect_Listable_Shown(CppPointer);
      }
      set
      {
        UnsafeNativeMethods.IRhRdkPostEffect_Listable_SetShown(CppPointer, value);
      }
    }

    private bool disposed = false;

    ~PostEffect()
    {
      Dispose(false);
    }

    /// <since>7.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <since>7.0</since>
    public virtual void Dispose(bool bDisposing)
    {
      if (!disposed)
      {
        disposed = true;
        if (m_cmn_cpp != IntPtr.Zero)
        {
          // Delete m_cpp
          UnsafeNativeMethods.CRdkCmnPostEffect_Delete(m_cmn_cpp);
          m_cmn_cpp = IntPtr.Zero;
        }
      }

      PostEffectBaseList.PostEffectList.Remove(this);
    }

    private static PostEffect FromSerialNumber(int serial)
    {
      foreach (var pep in PostEffectBaseList.PostEffectList)
      {
        if (pep.SerialNumber == serial)
          return pep;
      }

      return null;
    }

    internal static DE.CAN_EXECUTE_PROC can_execute_proc = CanExecute;
    private static int CanExecute(int serial, IntPtr pIRhRdkPostEffectPipeLine)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var pipeline = new PostEffectPipeline(pIRhRdkPostEffectPipeLine);
        return client.CanExecute(pipeline) ? 1 : 0;
      }
      return 0;
    }

    internal static DE.REQUIRED_CHANNELS_PROC required_channels_proc = GetRequiredChannels;
    private static void GetRequiredChannels(int serial, IntPtr pOnSimpleArrayUuid)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var channels = new SimpleArrayGuid(pOnSimpleArrayUuid);
        var ids = client.RequiredChannels;
        foreach (var id in ids)
        {
          channels.Append(id);
        }
      }
    }

    internal static DE.DELETE_THIS_PROC delete_this_proc = DeleteThis;
    private static void DeleteThis(int serial)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        //Ensure that this post effect doesn't try to re-enter the C++ destructor.
        client.disposed = true;
        client.Dispose();
      }
    }

    internal static DE.READ_FROM_DOCUMENT_DEFAULTS_PROC read_from_document_defaults_proc = ReadFromDocumentDefaults;
    private static int ReadFromDocumentDefaults(int serial, uint doc_serial)
    {
      //var client = FromSerialNumber(serial);
      //if (client != null)
      //{
      //  RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
      //  return client.ReadFromDocumentDefaults(doc) ? 1 : 0;
      //}
      return 0;
    }

    internal static DE.WRITE_TO_DOCUMENT_DEFAULTS_PROC write_to_document_defaults_proc = WriteToDocumentDefaults;
    private static int WriteToDocumentDefaults(int serial, uint doc_serial)
    {
      //var client = FromSerialNumber(serial);
      //if (client != null)
      //{
      //  RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
      //  return client.WriteToDocumentDefaults(doc) ? 1 : 0;
      //}
      return 0;
    }

    //private static uint CRC(int serial)
    //{
    //  var client = FromSerialNumber(serial);
    //  if (client != null)
    //  {
    //    return (uint)client.GetHashCode();
    //  }
    //  return 0;
    //}

    // Remember to change IntPtr to real instance
    internal static DE.EXECUTE_PROC execute_proc = Execute;
    private static int Execute(int serial, IntPtr pIRhRdkPostEffectPipeline, int left, int top, int width, int height)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var pipeline = new PostEffectPipeline(pIRhRdkPostEffectPipeline);
        var rect = new Rectangle(left, top, width, height);
        return client.Execute(pipeline, rect) ? 1 : 0;
      }
      return 0;
    }

    internal static DE.GET_PARAM_PROC get_param_proc = GetParam;
    private static int GetParam(int serial, IntPtr pString, IntPtr pVariant)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var input_var = new Variant(pVariant);
        var sParam = StringWrapper.GetStringFromPointer(pString);

        object input_var_obj = input_var.AsObject();
        bool ret = client.GetParam(sParam, ref input_var_obj);

        Variant output_variant = new Variant(input_var_obj);
        output_variant.CopyToPointer(pVariant);

        return ret ? 1 : 0;
      }
      return 0;
    }

    internal static DE.SET_PARAM_PROC set_param_proc = SetParam;
    private static int SetParam(int serial, IntPtr pString, IntPtr pVariant)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var var = new Variant(pVariant);
        var sParam = StringWrapper.GetStringFromPointer(pString);
        return client.SetParam(sParam, var.AsObject()) ? 1 : 0;
      }
      return 0;
    }

    internal static DE.READ_STATE_PROC read_state_proc = ReadState;
    private static int ReadState(int serial, IntPtr pState)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var state = new PostEffectState(pState);
        return client.ReadState(state) ? 1 : 0;
      }
      return 0;
    }

    internal static DE.WRITE_STATE_PROC write_state_proc = WriteState;
    private static int WriteState(int serial, IntPtr pState)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var state = new PostEffectState(pState);
        return client.WriteState(ref state) ? 1 : 0;
      }
      return 0;
    }

    internal static DE.RESET_TO_FACTORY_DEFAULTS_PROC reset_to_factory_defaults_proc = ResetToFactoryDefaults;
    private static void ResetToFactoryDefaults(int serial)
    {
      var client = FromSerialNumber(serial);
      client?.ResetToFactoryDefaults();
    }

    internal static DE.ADD_UI_SECTIONS_PROC add_ui_sections_proc = AddUISections;
    private static void AddUISections(int serial, IntPtr pIRhRdkPostEffecsUI)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        var ui = new PostEffectUI(pIRhRdkPostEffecsUI);
        client.AddUISections(ui);
      }
    }

    internal static DE.DISPLAY_HELP_PROC display_help_proc = DisplayHelp;
    private static int DisplayHelp(int serial)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        return client.DisplayHelp() ? 1 : 0;
      }
      return 0;
    }

    static internal void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.Rdk_CRdkCmnPostEffect_SetCallbacks
                (
                can_execute_proc, required_channels_proc, read_from_document_defaults_proc,
                write_to_document_defaults_proc, execute_proc,
                get_param_proc, set_param_proc, read_state_proc, write_state_proc,
                reset_to_factory_defaults_proc, add_ui_sections_proc, display_help_proc, delete_this_proc
                );
      }
      else
      {
        UnsafeNativeMethods.Rdk_CRdkCmnPostEffect_SetCallbacks(null, null, null, null, null,
                                             null, null, null, null, null, null, null, null);
      }
    }

     /// <since>7.0</since>
     public static Type[] RegisterPostEffect(PlugIns.PlugIn plugin)
    {
      return RegisterPostEffect(plugin.Assembly, plugin.Id);
    }

    /// <since>7.0</since>
    public static Type[] RegisterPostEffect(Assembly assembly, Guid pluginId)
    {
      // Check the Rhino plug-in for a RhinoPlugIn with the specified Id
      var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);

      // RhinoPlugIn not found so bail, all content gets associated with a plug-in!
      if (plugin == null)
        return null;

      // Get a list of the publicly exported class types from the requested assembly
      var exported_types = assembly.GetExportedTypes();

      // Scan the exported class types for PostEffect derived classes
      var posteffect_types = new List<Type>();
      var post_effect_type = typeof(PostEffect);
      for (var i = 0; i<exported_types.Length; i++)
      {
        var exported_type = exported_types[i];

        // If abstract class or not derived from RenderContent or does not contain a public constructor then skip it
        if (exported_type.IsAbstract || !exported_type.IsSubclassOf(post_effect_type) || exported_type.GetConstructor(new Type[] { }) == null)
          continue;

        // Check the class type for a GUID custom attribute
        var attr = exported_type.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);

        // If the class does not have a GUID custom attribute then throw an exception
        if (attr.Length< 1)
          throw new InvalidDataException("Class \"" + exported_type + "\" must include a GUID attribute");

        // Add the class type to the posteffect list
        posteffect_types.Add(exported_type);
      }

      // If this plug-in does not contain any valid PostEffect derived objects then bail
      if (posteffect_types.Count == 0)  
        return null;

      // Process the valid class type list and register each class with the
      // appropriate C++ RDK class factory
      foreach (var posteffect_type in posteffect_types)
      {
        var factory = new PostEffectFactory(posteffect_type);
      }

      // Return an array of the valid posteffect types
      return posteffect_types.ToArray();
    }

    /// <summary>
    /// Return true if the post effect is selected.
    /// </summary>
    /// <since>7.0</since>
    public bool IsSelected
    {
      get 
       {
        return UnsafeNativeMethods.IRhRdkPostEffect_IsSelected(CppPointer);
      }
    }

    /// <summary>
    /// Execute the post effect.
    /// </summary>
    /// <param name="pipeline">pipeline provides access to the post-effect pipeline.</param>
    /// <param name="rect">rect is the pixel area to process.</param>
    /// <returns>Return true if successful, else false. </returns>
    /// <since>7.0</since>
    public abstract bool Execute(PostEffectPipeline pipeline, Rectangle rect);

    /// <summary>
    /// Get a parameter.
    /// </summary>
    /// <param name="param">is the name of the parameter to get.</param>
    /// <param name="v">accepts the parameter value.</param>
    /// <returns> Returns true if successful or false if the parameter was not found.</returns>
    /// <since>7.0</since>
    public abstract bool GetParam(string param, ref object v);

    /// <summary>
    /// Set a parameter.
    /// </summary>
    /// <param name="param">is the name of the parameter to set.</param>
    /// <param name="v">specifies the type and value to set.</param>
    /// <returns>Return true if successful or false if the parameter could not be set.</returns>
    /// <since>7.0</since>
    public abstract bool SetParam(string param, object v);

    /// <summary>
    /// Read the state. If your post effect has no state, you must still return true for success.
    /// </summary>
    /// <param name="state">PostEffectState</param>
    /// <returns> Return true if successful, else false.</returns>
    /// <since>7.0</since>
    public abstract bool ReadState(PostEffectState state);

    /// <summary>
    /// Write the state. If your post effect has no state, you must still return true for success.
    /// </summary>
    /// <param name="state">PostEffectState</param>
    /// <returns>Return true if successful, else false.</returns>
    /// <since>7.0</since>
    public abstract bool WriteState(ref PostEffectState state);

    /// <summary>
    /// Reset the state to factory defaults.
    /// </summary>
    /// <since>7.0</since>
    public abstract void ResetToFactoryDefaults();

    /// <summary>
    /// Create each of your UI sections using 'new' and then call ui.AddSection() on them.
    /// RDK takes ownership of the sections.If your post effect does not need a UI, then
    /// your implementation of this method can be a no-op.
    /// </summary>
    /// <param name="ui">PostEffectUI</param>
    /// <since>7.0</since>
    public abstract void AddUISections(PostEffectUI ui);

    /// <summary>
    /// Displays the post effect's help page, if any.
    /// </summary>
    /// <returns> Return true if successful, else false. </returns>
    /// <since>7.0</since>
    public abstract bool DisplayHelp();

    /// <summary>
    /// Return true if the post effect can execute, else false.
    /// The base implementation checks if the post effect is 'On' and 'Shown'.
    /// Post effect authors can override this to include other criteria but cannot disable the base criteria.
    /// </summary>
    /// <param name="pipeline">PostEffectPipeline</param>
    /// <returns>Return true if the post effect can execute, else false</returns>
    /// <since>7.0</since>
    public virtual bool CanExecute(PostEffectPipeline pipeline)
    {
      bool rc = false;

      if(pipeline != null)
      {
        rc = UnsafeNativeMethods.CRdkCmnPostEffect_Base_CanExecute(CppPointer, pipeline.CppPointer);
      }

      return rc;
    }

    /// <summary>
    /// The RDK calls this method to determine which channels a post effect requires. If a required channel is not
    /// available, the RDK will hide the post effect's UI and display explanatory text instead.
    /// Note: As a convenience, the default implementation adds IRhRdkRenderWindow::chanRGBA to the output array.
    /// Most post effects should be able to use this default with no need to override the method.
    /// </summary>
    /// <returns>The channel Ids.The post effect should return all channels used by its implementation to this array.</returns>
    /// <since>7.0</since>
    public virtual Guid[] RequiredChannels
    {
      get
      {
        var aChannels = new SimpleArrayGuid();

        UnsafeNativeMethods.CRdkCmnPostEffect_Base_RequiredChannels(CppPointer, aChannels.NonConstPointer());

        return aChannels.ToArray();
      }
    }

    /// <summary>
    /// Because post effects are now in the render settings, this function can no longer be called.
    /// </summary>
    /// <since>7.0</since>
    [Obsolete ("This function is no longer called")]
    public virtual bool ReadFromDocumentDefaults(RhinoDoc doc)
    {
      return false;
    }

    /// <summary>
    /// Because post effects are now in the render settings, this function can no longer be called.
    /// </summary>
    /// <since>7.0</since>
    [Obsolete ("This function is no longer called")]
    public virtual bool WriteToDocumentDefaults(RhinoDoc doc)
    {
      return false;
    }

    /// <summary>
    /// A CRC of the state of this post effect.
    /// </summary>
    /// <returns>returns a crc of post effect state</returns>
    /// <since>7.0</since>
    public override int GetHashCode() // [ANDYLOOK] This does not need to be virtual but I can't understand why the overrides even exist.
    {
      return (int)UnsafeNativeMethods.IRhRdkPostEffect_CRC(CppPointer);
    }

    /// <since>7.0</since>
    public void BeginChange(ChangeContexts changeContext)
    {
      UnsafeNativeMethods.IRhRdkPostEffect_BeginChange(CppPointer, (int)changeContext);
      
    }

    /// <since>7.0</since>
    public bool EndChange()
    {
      return UnsafeNativeMethods.IRhRdkPostEffect_EndChange(CppPointer);
    }

    /// <since>7.0</since>
    public void Changed()
    {
      UnsafeNativeMethods.IRhRdkPostEffect_Changed(CppPointer);
    }
  }

  public class PostEffectState : IDisposable
  {
    private IntPtr m_cpp;

    /// <since>7.0</since>
    internal IntPtr CppPointer
    { 
      get { return m_cpp; }
    }

    /// <since>7.0</since>
    internal PostEffectState(IntPtr p)
    {
      m_cpp = p;
    }

    ~PostEffectState()
    {
      Dispose(false);
    }

    /// <since>7.0</since>
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

    /// <since>7.0</since>
    public bool TryGetValue<T>(string name, out T vValue)// where T : struct, Display.Color4f, Geometry.Point4d, Geometry.Vector3d, Geometry.Vector2d, String, Geometry.Transform, DateTime
    {
      using (var sf = new StringWrapper(name))
      {
        // https://mcneel.myjetbrains.com/youtrack/issue/RH-60356
        var v = new Variant();
        object obj = v.ToType(typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        v = new Variant(obj);

        // do the call
        bool ret = UnsafeNativeMethods.IRhRdkPostEffect_IState_GetParam(m_cpp, sf.ConstPointer, v.NonConstPointer());

        vValue = (T)v.AsObject();

        return ret;
      }
    }

    /// <since>7.0</since>
    public bool SetValue<T>(string name, T vValue)
    {
      using (var sf = new StringWrapper(name))
      {
        var v = new Variant(vValue);
        return UnsafeNativeMethods.IRhRdkPostEffect_IState_SetParam(m_cpp, sf.ConstPointer, v.NonConstPointer());
      }
    }
  }

  /// <summary>
  /// PostEffectUI class provides a way for post effect plug-ins to add ui sections.
  /// </summary>
  public class PostEffectUI : IDisposable
  {
    private IntPtr m_cpp;

    /// <since>7.0</since>
    internal IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <since>7.0</since>
    internal PostEffectUI(IntPtr p)
    {
      m_cpp = p;
    }

    ~PostEffectUI()
    {
      Dispose(false);
    }

    /// <since>7.0</since>
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
    /// Add a section to the UI.
    /// </summary>
    /// <param name="section"></param>
    /// <since>7.0</since>
    public void AddSection(ICollapsibleSection section)
    {
      UnsafeNativeMethods.IRhRdkPostEffectUI_AddSection(m_cpp, section.CppPointer);
    }
  }

  internal class PostEffectFactory : PostEffectFactoryBase
  {
    readonly Type m_type;

    public PostEffectFactory(Type t) // Constructor needed to be registered as exported type
    {
      m_type = t;
    }

    public override PostEffect NewPostEffect()
    {
      try
      {
        return (PostEffect)Activator.CreateInstance(m_type);
      }
      catch
      {
        return null;
      }
    }
  }

  /// <summary>
  /// To register a pep plugin create factory from PostEffectFactory.
  /// 
  /// The factory should be public and have a public constructor in order 
  /// for it to be able to register itself.
  /// 
  /// Use Rhino.UI.Controls.FactoryBase.Register() to register it. This will register
  /// all rdk factories that are expoerted and have a public constructor.
  /// </summary>
  internal abstract class PostEffectFactoryBase : FactoryBase
  {
    private IntPtr m_cpp;

    internal override IntPtr CreateCpp(int serial_number)
    {
      m_cpp = UnsafeNativeMethods.CRdkCmnPostEffectFactory_New(serial_number);
      UnsafeNativeMethods.RdkAddExtension(m_cpp);
      return m_cpp;
    }

    /// <summary>
    /// This class allows you to provide a factory for generating a custom post-effect plug-in.
    /// </summary>
    /// <since>7.0</since>
    public PostEffectFactoryBase()
    {
    }

    /// <summary>
    /// Create the new post effect instance.
    /// </summary>
    /// <returns>Return an instance to the new post effect object. Do not return null.</returns>
    /// <since>7.0</since>
    public abstract PostEffect NewPostEffect();

    /// <summary>
    /// Plugin id
    /// </summary>
    /// <returns>Plugin id</returns>
    /// <since>7.0</since>
    public Guid PlugInId()
    {
      Type type = GetType();
      Assembly asm = type.Assembly;
      string uuid = asm.GetCustomAttribute<GuidAttribute>().Value.ToUpper();
      return new Guid(uuid);
    }

    internal static DE.NEW_POST_EFFECT_PROC new_posteffect_proc = NewPostEffect;
    private static IntPtr NewPostEffect(int serial)
    {
      if (InternalRdkViewModelFactory.m_factories.ContainsKey(serial))
      {
        if (InternalRdkViewModelFactory.m_factories[serial] is PostEffectFactoryBase factory)
        {
          var pep = factory.NewPostEffect();
          if (pep == null)
            return IntPtr.Zero;

          return pep.CppPointer;
        }
      }

      return IntPtr.Zero;
    }

    internal static DE.PEP_UUID_PROC plugin_id_proc = PlugInId;
    private static Guid PlugInId(int serial)
    {
      if (InternalRdkViewModelFactory.m_factories.ContainsKey(serial))
      {
        if (InternalRdkViewModelFactory.m_factories[serial] is PostEffectFactoryBase factory)
        {
          return factory.PlugInId();
        }
      }

      return Guid.Empty;
    }

    static internal void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.Rdk_CRdkCmnPostEffectFactory_SetCallbacks(new_posteffect_proc, plugin_id_proc);
      }
      else
      {
        UnsafeNativeMethods.Rdk_CRdkCmnPostEffectFactory_SetCallbacks(null, null);
      }
    }
  }

  /// <since>7.0</since>
  [Flags]
  public enum PostEffectHistograms : int
  {
    None = 0,
    BeforeEarlyEffects = 1,
    BeforeToneMapping = 2,
    AfterToneMapping = 4,
    AfterLateEffects = 8,

    All = BeforeEarlyEffects | BeforeToneMapping | AfterToneMapping | AfterLateEffects,

    ToneMappingDisplay = BeforeToneMapping | AfterToneMapping,

    AfterEarlyEffects = BeforeToneMapping,
    BeforeLateEffects = AfterToneMapping,
  }

  /// <since>7.0</since>
  public enum PostEffectExecuteContexts : int
  {
    ProductionRendering = 0,
    RealtimeRendering = 1,
    ViewportDisplay = 2,
    ThumbnailCreation = 3,
    ConvertingToHDR = 4
  }

  /// <summary>
  /// This object provides a way for post effects to access the frame buffer channels from a rendering and create
  /// new channels containing post-processed information which can be passed to the next post effect in the chain.
  /// Consider a simple post effect that just modifies the red component of a rendering.It will call GetChannel()
  /// to get the red channel as its input, and it will call NewChannel() to get a new red channel for its output.
  /// It will then read the input channel, do calculations and write to the output channel.When finished, it will
  /// call Commit() passing the new channel.Because both channels have the same identifier, this will replace the
  /// old channel with the new one so that subsequent post effects in the chain will use the new channel instead of the
  /// original.Note that this will only replace the channel used by the pipeline.The original channel will still
  /// exist in the frame buffer.This system allows any post effect to access any number of channels for reading and
  /// create any number of new channels which may or may not replace existing channels depending on the channel
  /// id.The final stage (convert to 8-bit) operates on the channels left in the pipeline by the post effect chain to
  /// produce the final 32-bit RGBA image in a dib.
  /// 
  /// It is also possible for a post effect to create and use any number of 'scratch' channels.If a post effect needs a
  /// temporary pixel buffer for some intermediate results, it can call NewChannel() with a custom (random) id.
  /// Once it is finished with this scratch channel, it can call Discard() on it.
  /// </summary>
  public class PostEffectPipeline : IDisposable, IProgress<int>, IPostEffects
  {
    private IntPtr m_cpp;

    /// <since>7.0</since>
    internal IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <since>7.0</since>
    internal PostEffectPipeline(IntPtr p)
    {
      m_cpp = p;
    }

    ~PostEffectPipeline()
    {
      Dispose(false);
    }

    /// <since>7.0</since>
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
    /// Return a UUID that uniquely identifies the rendering being processed.
    /// </summary>
    /// <returns>Return a UUID that uniquely identifies the rendering being processed.</returns>
    /// <since>7.0</since>
    public Guid RenderingId
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkPostEffectPipeline_RenderingId(m_cpp);
      }
    }

    /// <summary>
    /// Post effect authors should check that GPU use is allowed before using the GPU in a post effect.
    /// </summary>
    /// <returns>Return true if the pipeline is allowed to use the GPU, else false</returns>
    /// <since>7.0</since>
    public bool GPUAllowed
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkPostEffectPipeline_AllowGPU(m_cpp);
      }
    }

    /// <summary>
    /// Get the dimensions of the frame buffer. All channels in the frame buffer have the same dimensions.
    /// </summary>
    /// <returns>Dimension as Size</returns>
    /// <since>7.0</since>
    public Size Dimensions()
    {
      int width = 0;
      int height = 0;
      UnsafeNativeMethods.IRhRdkPostEffectPipeline_Dimensions(m_cpp, ref width, ref height);

      return new Size(width, height);
    }

    /// <summary>
    /// Return a pointer to the specified post effect or null if not found.
    /// </summary>
    /// <param name="uuid">uuid of the poest effect plugin</param>
    /// <returns>Return a pointer to the specified post effect or null if not found.</returns>
    /// <since>7.0</since>
    PostEffect IPostEffects.PostEffectFromId(Guid uuid)
    {
      var pep = UnsafeNativeMethods.IRhRdkPostEffectPipeline_FindPostEffect(m_cpp, uuid);
      if (pep == IntPtr.Zero)
        return null;

      return new PostEffectNative(pep);
    }

    PostEffect[] IPostEffects.GetPostEffects(PostEffectType type)
    {
      var pep_list = new List<PostEffect>();

      if (this is IPostEffects peps)
      {
        var guids = ExecutionOrder();
        foreach (var id in guids)
        {
          var pep = peps.PostEffectFromId(id);
          if (pep != null)
          {
            if (pep.PostEffectType == type)
            {
              pep_list.Add(pep);
            }
          }
        }
      }

      return pep_list.ToArray();
    }

    ///<summary>
    /// Returns a list of the post effects to be executed by this pipeline in order.
    /// </summary>
    /// <returns>A list of the post effects to be executed by this pipeline in order</returns>
    /// <since>7.0</since>
    public Guid[] ExecutionOrder()
    {
      SimpleArrayGuid array = new SimpleArrayGuid();
      UnsafeNativeMethods.IRhRdkPostEffectPipeline_PostEffects(m_cpp, array.NonConstPointer());
      return array.ToArray();
    }

    /// <summary>
    /// Execute the pipeline. This executes all the post effects in order.
    /// Only this rectangle need be modified by the post effects.
    /// </summary>
    /// <param name="p">p is a rectangle within the frame buffer.</param>
    /// <param name="renderingInProgress">rendering is true if rendering is in progress.</param>
    /// <param name="usageContexts">Context this pipeline is being executed in.</param>
    /// <param name="histogramsToUpdate">Bitwise list of histograms to update during the execution of the pipeline</param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool Execute(Rectangle p, bool renderingInProgress, PostEffectExecuteContexts usageContexts, PostEffectHistograms histogramsToUpdate)
    {
      return UnsafeNativeMethods.IRhRdkPostEffectPipeline_Execute(m_cpp, p.X, p.Y, p.Width, p.Height,
                                 renderingInProgress, (int)usageContexts, (int)histogramsToUpdate);
    }

    ///<summary>
    /// Post effects should call this during execution to report progress.
    /// A good strategy is to call this once per pixel row (or several rows).
    /// If the function returns \e false, your post effect should exit its pixel loop
    /// as the user has requested that the operation be canceled.
    ///</summary>
    /// <returns>Return true if the process should continue, else false.</returns>
    /// <since>7.0</since>
    void IProgress<int>.Report(int rowsCompleted)
    {
      UnsafeNativeMethods.IRhRdkPostEffectPipeline_ReportProgress(m_cpp, rowsCompleted);
    }

    /// <summary>
    /// Get the max luminance in the rendering.
    /// </summary>
    /// <returns>max luminance</returns>
    /// <since>7.0</since>
    public float GetMaxLuminance()
    {
      return UnsafeNativeMethods.IRhRdkPostEffectPipeline_GetMaxLuminance(m_cpp);
    }

    /// <summary>
    /// Set the start time of the rendering in milliseconds since some unspecified epoch.
    /// </summary>
    /// <param name="ms">milliseconds</param>
    [CLSCompliant(false)]
    public void SetStartTimeInMilliseconds(ulong ms)
    {
      UnsafeNativeMethods.IRhRdkPostEffectPipeline_SetStartTimeInMilliseconds(m_cpp, ms);
    }

    /// <summary>
    /// Get the start time of the rendering expressed in milliseconds since some unspecified epoch.
    /// Do not make assumptions about what the epoch is; it might be different on different platforms.
    /// </summary>
    /// <returns>milliseconds</returns>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public ulong GetStartTimeInMilliseconds()
    {
      return UnsafeNativeMethods.IRhRdkPostEffectPipeline_GetStartTimeInMilliseconds(m_cpp);
    }

    /// <summary>
    /// Get the end time of the rendering expressed in milliseconds since some unspecified epoch.
    /// Do not make assumptions about what the epoch is; it might be different on different platforms.
    /// </summary>
    /// <returns>milliseconds</returns>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public ulong GetEndTimeInMilliseconds()
    {
      return UnsafeNativeMethods.IRhRdkPostEffectPipeline_GetEndTimeInMilliseconds(m_cpp);
    }

    /// <summary>
    /// IsRendering
    /// </summary>
    /// <returns>Return true if rendering is active, else false.</returns>
    /// <since>7.0</since>
    public bool IsRendering
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkPostEffectPipeline_IsRendering(m_cpp);
      }
    }

    /// <summary>
    /// Get a channel for reading. A post effect will use this to get channel data as input to
    /// its process. Output will be written to new channel(s). \see GetChannelForWrite()
    /// This method returns the current state of the channel at this stage in the pipeline.
    /// If the first post effect calls this, it will get the actual frame buffer channel.
    /// Subsequent post effects will get the data left behind by the previous post effect.
    /// A post effect calls GetChannelForRead() to get its input and GetChannelForWrite()
    /// to get the object to which it will write its output. Even when the same channel id
    /// is specified, these are separate, unconnected objects.
    /// </summary>
    /// <param name="id">The channel identifier.</param>
    /// <returns>A pointer to the channel or null if the channel is not available.</returns>
    /// <since>7.0</since>
    public PostEffectChannel GetChannelForRead(Guid id)
    {
      IntPtr pChannel = UnsafeNativeMethods.IRhRdkPostEffectPipeline_GetChannelForRead(m_cpp, id);
      if (pChannel != IntPtr.Zero)
      {
        PostEffectChannel channel = new PostEffectChannel(pChannel);
        return channel;
      }
      return null;
    }

    /// <summary>
    /// Get a channel for writing. A post effect will use this to get channel(s) to write the output of its
    /// processing to. Input will usually come from existing channels, although a post effect is free to read
    /// its own output channels if needed. See GetChannelForRead()
    /// You are allowed to create one new channel with the same identifier as an existing channel,
    /// in which case IChannel::Commit() will replace the existing channel with the new one in the pipeline.
    /// </summary>
    /// <param name="id">The channel identifier.</param>
    /// <returns>A pointer to the new channel or null if the channel could not be created.</returns>
    /// <since>7.0</since>
    public PostEffectChannel GetChannelForWrite(Guid id)
    {
      IntPtr pChannel = UnsafeNativeMethods.IRhRdkPostEffectPipeline_GetChannelForWrite(m_cpp, id);
      if (pChannel != IntPtr.Zero)
      {
        PostEffectChannel channel = new PostEffectChannel(pChannel);
        return channel;
      }
      return null;
    }

    /// <summary>
    /// Get the post effect thread engine.
    /// </summary>
    /// <since>7.0</since>
    public PostEffectThreadEngine ThreadEngine()
    {
      IntPtr pThreadEngine = UnsafeNativeMethods.IRhRdkPostEffectPipeline_ThreadEngine(m_cpp);
      if (pThreadEngine != IntPtr.Zero)
        return new PostEffectThreadEngine(pThreadEngine);

      return null;
    }
  }

  public class PostEffectChannel : IDisposable
  {
    protected IntPtr m_cpp;

    /// <since>7.0</since>
    internal PostEffectChannel(IntPtr p)
    {
      m_cpp = p;
    }

    ~PostEffectChannel()
    {
      Dispose(false);
    }

    /// <since>7.0</since>
    internal IntPtr CppPointer
    {
      get
      {
        return m_cpp;
      }
    }

    /// <since>7.0</since>
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
    /// Return the channel id which indicates what type of data is used in this channel.
    /// </summary>
    /// <since>7.0</since>
    virtual public Guid Id
    {
      get => UnsafeNativeMethods.IRhRdkPostEffectPipeline_IChannel_Id(m_cpp);
    }

    /// <summary>
    /// Return the pixel size in bytes for this channel.
    /// </summary>
    /// <since>7.0</since>
    public int PixelSize
    {
      get => (int)UnsafeNativeMethods.IRhRdkPostEffectPipeline_IChannel_PixelSize(m_cpp);
    }

    /// <summary>
    /// Return an interface to this channel for doing channel operations on the CPU.
    /// </summary>
    /// <since>7.0</since>
    public RenderWindow.Channel CPU()
    {
      var channel = UnsafeNativeMethods.IRhRdkPostEffectPipeline_IChannel_CPU(m_cpp);
      if (channel == IntPtr.Zero)
        return null;

      return new RenderWindow.Channel(channel);
    }

    /// <summary>
    /// Return an interface to this channel for doing channel operations on the GPU.
    /// </summary>
    /// <since>7.0</since>
    public RenderWindow.ChannelGPU GPU()
    {
      var channel = UnsafeNativeMethods.IRhRdkPostEffectPipeline_IChannel_GPU(m_cpp);
      if (channel == IntPtr.Zero)
        return null;

      return new RenderWindow.ChannelGPU(channel);
    }

    /// <summary>
    /// Return a clone of this channel.
    /// </summary>
    /// <since>7.0</since>
    public PostEffectChannel Clone()
    {
      var channel = UnsafeNativeMethods.IRhRdkPostEffectPipeline_IChannel_Clone(m_cpp);
      if (channel == IntPtr.Zero)
        return null;

      return new PostEffectChannel(channel);
    }

    /// <summary>
    /// Commit changes to the channel so that those changes can be used by subsequent post effects in the chain.
    /// Only valid for channels that were obtained by calling GetChannelForWrite().
    /// If the channel has the same id as an existing channel, the existing channel will be replaced by
    /// the new one. If the existing channel was created by a previous post effect in the chain, it will be deleted.
    /// Changes to channels that are not commited simply get ignored.
    /// Note: This call merely sets a flag. The process is deferred until after the post effect has finished executing.
    /// </summary>
    /// <since>7.0</since>
    public void Commit()
    {
      UnsafeNativeMethods.IRhRdkPostEffectPipeline_IChannel_Commit(m_cpp);
    }
  }

  public abstract class PostEffectJob : IDisposable
  {
    /// <since>7.0</since>
    internal int SerialNumber { get; set; }

    /// <since>7.0</since>
    internal IntPtr CppPointer { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    /// <since>7.0</since>
    public PostEffectJob()
    {
      lock (PostEffectJobBaseList.PostEffectJobList)
      {
        // create new generic controll, serial_number as parameter
        SerialNumber = PostEffectJobBaseList.serial_number;
        CppPointer = UnsafeNativeMethods.CRdkCmnPostEffectJob_New(SerialNumber);
        PostEffectJobBaseList.serial_number++;
        PostEffectJobBaseList.PostEffectJobList.Add(new WeakReference<PostEffectJob>(this));
      }
    }

    ~PostEffectJob()
    {
      Dispose(false);
    }

    bool disposed = false;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <since>7.0</since>
    public virtual void Dispose(bool bDisposing)
    {
      if (!disposed)
      {
        disposed = true;
        if (CppPointer != IntPtr.Zero)
        {
          // Delete m_cpp
          UnsafeNativeMethods.CRdkCmnPostEffectJob_Delete(CppPointer);
          CppPointer = IntPtr.Zero;
        }

        PostEffectJobBaseList.Purge(this);
        PostEffectJobBaseList.ClonedPostEffectJobList.Remove(this);
      }
    }

    private static PostEffectJob FromSerialNumber(int serial)
    {
      lock(PostEffectJobBaseList.PostEffectJobList)
      {
        foreach (var refUI in PostEffectJobBaseList.PostEffectJobList)
        {
          if (refUI.TryGetTarget(out var ui))
          {
            if (ui.SerialNumber == serial)
              return ui;
          }
        }
      }

      return null;
    }

    /// <since>7.0</since>
    public abstract PostEffectJob Clone();

    /// <since>7.0</since>
    public abstract bool Execute(Rectangle rect, PostEffectJobChannels access);

    internal static DE.CLONE_POST_EFFECT_JOB_PROC clone_proc = Clone;
    private static IntPtr Clone(int serial)
    {
      var client = FromSerialNumber(serial);
      if (client != null)
      {
        PostEffectJob clone = client.Clone();
        if (clone != null)
        {
          // All PostEffects that are cloned via Cpp are added to a static list so that C# cannot
          // garbage collect them. The C++ side will take care of deleting and DeleteThis will remove
          // the object from the list.
          PostEffectJobBaseList.ClonedPostEffectJobList.Add(clone);
          return clone.CppPointer;
        }
      }
      return IntPtr.Zero;
    }

    internal static DE.DELETE_THIS_POST_EFFECT_JOB delete_this_proc = DeleteThis;
    private static void DeleteThis(int serial)
    {
      var client = FromSerialNumber(serial);
      client?.Dispose();
    }

    // Kom ihåg att byta pPixels....
    internal static DE.EXECUTE_POST_EFFECT_JOB execute_proc = Execute;
    private static int Execute(int serial, int left, int top, int width, int height, IntPtr pAccess)
    {
      var client = FromSerialNumber(serial);
      if (client == null)
        return 0;

      var rect = new Rectangle(left, top, width, height);
      var access = new PostEffectJobChannels(pAccess);
      return client.Execute(rect, access) ? 1 : 0;
    }

    static internal void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.Rdk_CRdkCmnPostEffectJob_SetCallbacks(clone_proc, delete_this_proc, execute_proc);
      }
      else
      {
        UnsafeNativeMethods.Rdk_CRdkCmnPostEffectJob_SetCallbacks(null, null, null);
      }
    }
  }

  public class PostEffectJobChannels : IDisposable
  {
    /// <since>7.0</since>
    internal IntPtr CppPointer { get; private set; }

    /// <since>7.0</since>
    internal PostEffectJobChannels(IntPtr p) { CppPointer = p; }

    ~PostEffectJobChannels() { Dispose(false); }

    /// <since>7.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      CppPointer = IntPtr.Zero;
    }

    /// <since>7.0</since>
    public PostEffectChannel GetChannel(Guid channelId)
    {
      var p = UnsafeNativeMethods.IRhRdkPostEffectThreadEngine_IJob_IChannels_GetChannel(CppPointer, channelId);
      if (IntPtr.Zero == p)
        return null;

      return new PostEffectChannel(p);
    }
  }

  public class PostEffectThreadEngine : IDisposable
  {
    /// <since>7.0</since>
    internal IntPtr CppPointer { get; private set; }

    /// <since>7.0</since>
    internal PostEffectThreadEngine(IntPtr p)
    {
      CppPointer = p;
    }

    ~PostEffectThreadEngine()
    {
      Dispose(false);
    }

    /// <since>7.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      CppPointer = IntPtr.Zero;
    }

    /// <since>7.0</since>
    public bool RunPostEffect(PostEffectJob job, PostEffectPipeline pipeline, PostEffect plugin, Rectangle rect, Guid[] channels)
    {
      int x = rect.X;
      int y = rect.Y;
      int w = rect.Width;
      int h = rect.Height;

      var guidArray = new SimpleArrayGuid(channels);

      bool rc = UnsafeNativeMethods.IRhRdkPostEffectThreadEngine_RunPostEffect(CppPointer, job.CppPointer, pipeline.CppPointer, plugin.CppPointer, x, y, w, h, guidArray.ConstPointer());

      // 2021-08-23 David E.
      // "job" needs to not be garbage collected until IRhRdkPostEffectThreadEngine_RunPostEffect has executed.
      // Fixes RH-65311.
      GC.KeepAlive(job);

      return rc;
    }
  }

  public interface IPostEffects
  {
    /// <since>7.0</since>
    PostEffect PostEffectFromId(Guid uuid);

    /// <since>7.0</since>
    PostEffect[] GetPostEffects(PostEffectType type);
  }

  internal sealed class PostEffectsImpl : DocumentOrFreeFloatingBase, IPostEffects
  {
    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      return UnsafeNativeMethods.IRhRdkPostEffects_FromDocSerial(doc_sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f) { return IntPtr.Zero; }

    internal override IntPtr DefaultCppConstructor()
    {
      throw new NotImplementedException();
    }

    /// <since>7.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      throw new NotImplementedException();
    }

    internal override void DeleteCpp()
    {
      throw new NotImplementedException();
    }

    internal PostEffectsImpl(uint ds) : base(ds) { }

    /// <since>7.0</since>
    public PostEffect PostEffectFromId(Guid uuid)
    {
      IntPtr pPEP = UnsafeNativeMethods.IRhRdkPostEffects_PostEffectFromId(CppPointer, uuid);
      if (pPEP != IntPtr.Zero)
      {
        return new PostEffectNative(pPEP);
      }
      return null;
    }

    /// <since>7.0</since>
    public PostEffect[] GetPostEffects(PostEffectType type)
    {
      var array = new PostEffectArray();
      UnsafeNativeMethods.IRhRdkPostEffects_GetPostEffects(CppPointer, (uint)type, array.CppPointer);
      return array.ToArray();
    }
  }

  internal sealed class PostEffectArray : IDisposable
  {
    private IntPtr m_cpp;
    private readonly bool m_owner;

    /// <since>7.0</since>
    internal IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    internal PostEffectArray()
    {
      m_owner = true;
      m_cpp = UnsafeNativeMethods.PostEffectArray_New();
    }

    internal PostEffectArray(IntPtr pArray)
    {
      m_owner = false;
      m_cpp = pArray;
    }

    ~PostEffectArray()
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
        if (m_owner)
        {
          UnsafeNativeMethods.PostEffectArray_Delete(m_cpp);
        }
        m_cpp = IntPtr.Zero;
      }
    }

    public int Count
    {
      get
      {
        if (m_cpp != IntPtr.Zero)
        {
          return UnsafeNativeMethods.PostEffectArray_Count(m_cpp);
        }

        return 0;
      }
    }

    public void Append(PostEffect plugin)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.PostEffectArray_Add(m_cpp, plugin.CppPointer);
      }
    }

    public PostEffect At(int index)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pPlugin = UnsafeNativeMethods.PostEffectArray_At(m_cpp, index);
        return new PostEffectNative(pPlugin);
      }

      return null;
    }

    public PostEffect[] ToArray()
    {
      var list = new List<PostEffect>();
      for (int i = 0; i < Count; i++)
      {
        list.Add(At(i));
      }

      return list.ToArray();
    }
  }

  internal class PostEffectNative : PostEffect, IDisposable
  {
    private IntPtr m_native_cpp;

    public PostEffectNative(IntPtr p)
      : base(-1)  //Force the do-nothing ctor to be called.
    {
      m_native_cpp = p;
    }

    internal override IntPtr CppPointer => m_native_cpp;

    public override void Dispose(bool bDisposing)
    {
      //base.Dispose(bDisposing);
      m_native_cpp = IntPtr.Zero;
    }

    public override bool GetParam(string param, ref object vValue)
    {
      using (var sf = new StringWrapper(param))
      {
        var v = new Variant(vValue);
        bool ret = UnsafeNativeMethods.IRhRdkPostEffect_GetParameter(CppPointer, sf.ConstPointer, v.NonConstPointer());
        vValue = v.AsObject();
        return ret;
      }
    }

    public override bool SetParam(string param, object vValue)
    {
      using (var sf = new StringWrapper(param))
      {
        var v = new Variant(vValue);
        return UnsafeNativeMethods.IRhRdkPostEffect_SetParameter(CppPointer, sf.ConstPointer, v.NonConstPointer());
      }
    }

    public override void AddUISections(PostEffectUI ui)
    {
      UnsafeNativeMethods.IRhRdkPostEffect_AddUISections(CppPointer, ui.CppPointer);
    }

    public override bool Execute(PostEffectPipeline pipeline, Rectangle rect)
    {
      return UnsafeNativeMethods.IRhRdkPostEffect_Execute(CppPointer, pipeline.CppPointer, rect.Top, rect.Left, rect.Width, rect.Height);
    }

    public override bool DisplayHelp()
    {
      return UnsafeNativeMethods.IRhRdkPostEffect_DisplayHelp(CppPointer);
    }

    public override bool ReadState(PostEffectState state)
    {
      return UnsafeNativeMethods.IRhRdkPostEffect_ReadState(CppPointer, state.CppPointer);
    }

    public override void ResetToFactoryDefaults()
    {
      UnsafeNativeMethods.IRhRdkPostEffect_ResetToFactoryDefaults(CppPointer);
    }

    public override bool WriteState(ref PostEffectState state)
    {
      return UnsafeNativeMethods.IRhRdkPostEffect_WriteState(CppPointer, state.CppPointer);
    }

    new public void BeginChange(ChangeContexts changeContext)
    {
      UnsafeNativeMethods.IRhRdkPostEffect_BeginChange(CppPointer, (int)changeContext);
    }

    new public bool EndChange()
    {
      return UnsafeNativeMethods.IRhRdkPostEffect_EndChange(CppPointer);
    }

    new public void Changed()
    {
      UnsafeNativeMethods.IRhRdkPostEffect_Changed(CppPointer);
    }
  }
}
#endif

namespace Rhino.Render.PostEffects
{
  /// <summary>
  /// This is a wrapper around the data ('on', 'shown', 'state' parameters, etc.) of a post effect.
  /// </summary>
  /// <since>8.0</since>
  public class PostEffectData : IDisposable
  {
    private readonly Guid _pep_id;

    internal IntPtr CppPointer // ON_PostEffect
    {
      get
      {
        var native_pep = Collection.FindPostEffect(_pep_id);
        if (native_pep == IntPtr.Zero)
          throw new Exception("PostEffectData.CppPointer failed");

        return native_pep;
      }
    }

    public PostEffectCollection Collection { get; private set; }

    internal PostEffectData(PostEffectCollection c, Guid pep_id)
    {
      Collection = c;
      _pep_id = pep_id;
    }

    ~PostEffectData()
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
      // If we ever add support for free-floating objects.
      //if (IntPtr.Zero != _native_ptr)
      //{
      //  UnsafeNativeMethods.ON_PostEffect_Delete(_native_ptr);
      //  _native_ptr = IntPtr.Zero;
      //}
    }

    /// <summary>
    /// Returns the unique id of this post effect.
    /// </summary>
    /// <since>8.0</since>
    public Guid Id
    {
      get
      {
        var id = new Guid();
        UnsafeNativeMethods.ON_PostEffect_GetId(CppPointer, ref id);
        return id;
      }
    }

    /// <summary>
    /// Returns the type of this post effect.
    /// </summary>
    /// <since>8.0</since>
    public PostEffectType Type
    {
      get
      {
        switch (UnsafeNativeMethods.ON_PostEffect_GetType(CppPointer))
        {
        default:
        case 1: return PostEffectType.Early;
        case 2: return PostEffectType.ToneMapping;
        case 3: return PostEffectType.Late;
        }
      }
    }

    /// <summary>
    /// Returns the localized name of this post effect.
    /// </summary>
    /// <since>8.0</since>
    public string LocalName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          UnsafeNativeMethods.ON_PostEffect_GetLocalName(CppPointer, sh.NonConstPointer());
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// The 'on' state of this post effect.
    /// </summary>
    /// <since>8.0</since>
    public bool On
    {
      get => UnsafeNativeMethods.ON_PostEffect_GetOn(CppPointer);
      set {  UnsafeNativeMethods.ON_PostEffect_SetOn(CppPointer, value); }
    }

    /// <summary>
    /// The 'shown' state of this post effect.
    /// </summary>
    /// <since>8.0</since>
    public bool Shown
    {
      get => UnsafeNativeMethods.ON_PostEffect_GetShown(CppPointer);
      set {  UnsafeNativeMethods.ON_PostEffect_SetShown(CppPointer, value); }
    }

    /// <summary>
    /// Get an arbitrary parameter from this post effect by its name.
    /// If the parameter is not known to the post effect, the method will fail.
    /// Returns a variant object if successful or null on failure.
    /// </summary>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public IConvertible GetParameter(string param_name)
    {
      var v = new Variant();

      if (!UnsafeNativeMethods.ON_PostEffect_GetParameter(CppPointer, param_name, v.NonConstPointer()))
        return null;

      return v;
    }

    /// <summary>
    /// Set an arbitrary parameter to the post effect by its name.
    /// If the parameter is not known to the post effect, the method will fail.
    /// Returns true if successful or false on failure.
    /// </summary>
    /// <since>8.0</since>
    public bool SetParameter(string param_name, object param_value)
    {
      var v = new Variant(param_value);
      return UnsafeNativeMethods.ON_PostEffect_SetParameter(CppPointer, param_name, v.ConstPointer());
    }

    /// <summary>
    /// Get a CRC representing the state of the entire post effect.
    /// </summary>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public uint DataCRC(uint current_remainder)
    {
      return UnsafeNativeMethods.ON_PostEffect_GetDataCRC(CppPointer, current_remainder);
    }
  }

  internal class PostEffectDataEnumerator : IEnumerator<PostEffectData>
  {
    private int _index = 0;
    private readonly PostEffectCollection _collection;

    public PostEffectDataEnumerator(PostEffectCollection c) { _collection = c; }

    public void Reset() { _index = 0; }
    public PostEffectData Current { get; private set; }
    object IEnumerator.Current { get => Current; }

    public bool MoveNext()
    {
      var native_pep = UnsafeNativeMethods.ON_PostEffects_GetAt(_collection.CppPointer, _index);
      if (native_pep == IntPtr.Zero)
        return false;

      var id = new Guid();
      UnsafeNativeMethods.ON_PostEffect_GetId(native_pep, ref id);
      Current = new PostEffectData(_collection, id);
      _index++;

      return true;
    }

    public void Dispose() { Dispose(true); }
    protected void Dispose(bool b) { }
  }

  /// <summary>
  /// Represents the collection of post effects in render settings.
  /// </summary>
  public sealed class PostEffectCollection : DocumentOrFreeFloatingBase,
                                             IEnumerable<PostEffectData>, IDisposable
  {
    internal PostEffectCollection(IntPtr native)    : base(native) { } // ON_PostEffects
    internal PostEffectCollection(FileIO.File3dm f) : base(f) { }

#if RHINO_SDK
    internal PostEffectCollection(uint doc_sn)      : base(doc_sn) { }
    internal PostEffectCollection(RhinoDoc doc)     : base(doc.RuntimeSerialNumber) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetPostEffects(rs.ConstPointer());
    }
#endif

    /// <summary>
    /// Create a utility object not associated with any document
    /// </summary>
    /// <since>8.0</since>
    public PostEffectCollection() : base() { }

    /// <summary>
    /// Create a utility object not associated with any document from another object
    /// </summary>
    /// <param name="c">The other collection.</param>
    /// <since>8.0</since>
    public PostEffectCollection(PostEffectCollection c) : base(c) { }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_PostEffects_New();
    }

    internal override IntPtr CppFromFile3dm(File3dm f)
    {
      return UnsafeNativeMethods.ON_PostEffects_FromONX_Model(f.ConstPointer());
    }

    ~PostEffectCollection()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Get a post effect data for an id.
    /// </summary>
    /// <since>8.0</since>
    public PostEffectData PostEffectDataFromId(Guid id)
    {
      return new PostEffectData(this, id);
    }

    /// <summary>
    /// Move a post effect before another post effect in the list.
    /// Param 'id_move' is the id of the post effect to move.
    /// Param 'id_before' is the id of a post effect before which the post effect should be moved.
    /// If this is Guid.Empty, the post effect is moved to the end of the list.
    /// If the post effect identified by 'id_before' is not found, the method will fail.
    /// Returns true if successful, else false.
    /// </summary>
    /// <since>8.0</since>
    public bool MovePostEffectBefore(Guid id_move, Guid id_before)
    {
      return UnsafeNativeMethods.ON_PostEffects_MovePostEffectBefore(CppPointer, ref id_move, ref id_before);
    }

    /// <summary>
    /// Gets the selected post effect for a certain type into 'id'.
    /// Returns true if successful or false if the selection information could not be found.
    /// </summary>
    /// <since>8.0</since>
    public bool GetSelectedPostEffect(PostEffectType type, out Guid id)
    {
      id = Guid.Empty;
      return UnsafeNativeMethods.ON_PostEffects_GetSelectedPostEffect(CppPointer, (int)type, ref id);
    }

    /// <summary>
    /// Sets the selected post effect for a certain type.
    /// </summary>
    /// <since>8.0</since>
    public void SetSelectedPostEffect(PostEffectType type, Guid id)
    {
      UnsafeNativeMethods.ON_PostEffects_SetSelectedPostEffect(CppPointer, (int)type, ref id);
    }

    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_PostEffects_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_PostEffects_Delete(CppPointer);
    }

    /// <summary></summary>
    /// Get an enumerator for accessing PostEffectData.
    /// <since>8.0</since>
    public IEnumerator<PostEffectData> GetEnumerator()
    {
      return new PostEffectDataEnumerator(this);
    }

    /// <summary>
    /// Get an enumerator for accessing PostEffectData.
    /// </summary>
    /// <since>8.0</since>
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

    internal IntPtr FindPostEffect(Guid id)
    {
      return UnsafeNativeMethods.ON_PostEffects_PostEffectFromId(CppPointer, id);
    }
  }
}
