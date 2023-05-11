
#if RHINO_SDK

#pragma warning disable 1591

using System;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
  public interface ICurrentEnvironment_Get
  {
      /// <summary>
      /// Get the RenderEnvironment used as the 360 degree backdrop environment. Null will be returned
      /// if the background mode is set to 'Solid Color' or 'Gradient' (i.e., not 'Environment').
      /// </summary>
      /// <since>6.0</since>
      RenderEnvironment ForBackground { get; }
      /// <since>6.0</since>
      [Obsolete("Same as ForBackground. Please use ForBackground")]
      RenderEnvironment ForBackground_CheckMode { get; }

      /// <summary>
      /// Get the RenderEnvironment used as custom skylighting environment.
      /// Null will be returned if the custom skylighting environment override is disabled.
      /// </summary>
      /// <since>6.0</since>
      RenderEnvironment ForLighting { get; }

      /// <summary>
      /// Get the RenderEnvironment used as custom reflection (and refraction) environment.
      /// Null will be returned if the custom reflection override is disabled.
      /// </summary>
      /// <since>6.0</since>
      RenderEnvironment ForReflectionAndRefraction { get; }
  }

  public interface ICurrentEnvironment : ICurrentEnvironment_Get
  {
      /// <summary>
      /// Get or set the RenderEnvironment used as the 360 degree backdrop environment.
      /// If getting, null will be returned if the background mode is set to 'Solid Color'
      /// or 'Gradient' (i.e., not 'Environment').
      /// If setting, passing null will not work because this environment usage can't be turned off.
      /// </summary>
      /// <since>6.0</since>
      new RenderEnvironment ForBackground { get; set; }

      /// <since>6.0</since>
      [Obsolete("Same as ForBackground. Please use ForBackground")]
      new RenderEnvironment ForBackground_CheckMode { get; set; }

      /// <summary>
      /// Get or set the RenderEnvironment used as custom skylighting environment.
      /// If getting, null will be returned if the custom skylighting environment override is disabled.
      /// If setting, passing null will turn the custom skylighting environment override off but it will
      /// not actually set its id to null. Setting to a valid environment will also turn the custom
      /// skylighting environment override on.
      /// </summary>
      /// <since>6.0</since>
      new RenderEnvironment ForLighting { get; set; }

      /// <summary>
      /// Get or set the RenderEnvironment used as custom reflection (and refraction) environment.
      /// If getting, null will be returned if the custom reflection override is disabled.
      /// If setting, passing null will turn the custom reflection environment override off but it will
      /// not actually set its id to null. Setting to a valid environment will also turn the custom
      /// reflection environment override on.
      /// </summary>
      /// <since>6.0</since>
      new RenderEnvironment ForReflectionAndRefraction { get; set; }

      /// <since>6.0</since>
      [Obsolete("To set all, just set each one individually")]
      RenderEnvironment ForAnyUsage { set; }
  }

  internal class CurrentEnvironmentImpl : ICurrentEnvironment
  {
    readonly IntPtr m_ptr = IntPtr.Zero;
    readonly uint m_doc_serial = 0;

    internal CurrentEnvironmentImpl(IntPtr p) { m_ptr = p; }

    internal CurrentEnvironmentImpl(uint serial) { m_doc_serial = serial; }

    internal IntPtr CppPointer
    { 
      get
      {
        if (m_doc_serial != 0)
        {
          return UnsafeNativeMethods.IRhRdkCurrentEnvironment_FromDocSerial(m_doc_serial);
        }
        return m_ptr;
      }
    }

    [Obsolete("Same as ForBackground. Please use ForBackground")]
    public RenderEnvironment ForBackground_CheckMode
    {
        get
        {
          return RenderEnvironment.FromPointer(UnsafeNativeMethods.
            IRhRdkCurrentEnvironment_Get360BackdropEnv(CppPointer), null) as RenderEnvironment;
        }
        set
        {
          ForBackground = value;
        }
    }

    public RenderEnvironment ForBackground
    {
      get
      {
        return RenderEnvironment.FromPointer(UnsafeNativeMethods.
               IRhRdkCurrentEnvironment_Get360BackdropEnv(CppPointer), null) as RenderEnvironment;
      }
      set
      {
        var guid = value != null ? value.Id : Guid.Empty;
        UnsafeNativeMethods.IRhRdkCurrentEnvironment_Set360BackdropEnv(CppPointer, guid);
      }
    }

    public RenderEnvironment ForLighting
    {
      get
      {
        return RenderEnvironment.FromPointer(
          UnsafeNativeMethods.IRhRdkCurrentEnvironment_GetSkylightingEnv(CppPointer), null) as RenderEnvironment;
      }
      set
      {
        var guid = value != null ? value.Id : Guid.Empty;
        UnsafeNativeMethods.IRhRdkCurrentEnvironment_SetSkylightingEnv(CppPointer, guid);
      }
    }

    public RenderEnvironment ForReflectionAndRefraction
    {
      get
      {
        return RenderEnvironment.FromPointer(
          UnsafeNativeMethods.IRhRdkCurrentEnvironment_GetReflectionEnv(CppPointer), null) as RenderEnvironment;
      }
      set
      {
        var guid = value != null ? value.Id : Guid.Empty;
        UnsafeNativeMethods.IRhRdkCurrentEnvironment_SetReflectionEnv(CppPointer, guid);
      }
    }

    [Obsolete("To set all, just set each one individually")]
    public RenderEnvironment ForAnyUsage
    {
      set
      {
        var guid = value != null ? value.Id : Guid.Empty;
        UnsafeNativeMethods.IRhRdkCurrentEnvironment_Set360BackdropEnv(CppPointer, guid);
        UnsafeNativeMethods.IRhRdkCurrentEnvironment_SetSkylightingEnv(CppPointer, guid);
        UnsafeNativeMethods.IRhRdkCurrentEnvironment_SetReflectionEnv (CppPointer, guid);
      }
    }

    /// <summary>
    /// For usage background, this checks if the background style is set to 'Environment'.
    /// For reflection and skylighting, it checks if the relevant custom override is enabled. 
    /// </summary>
    public bool On(RenderSettings.EnvironmentUsage usage)
    {
      return UnsafeNativeMethods.IRhRdkCurrentEnvironment_On(CppPointer, (int)usage);
    }

    /// <summary>
    /// Set the given environment override on/off. Only works for reflection and skylighting usages.
    /// </summary>
    public void SetOn(RenderSettings.EnvironmentUsage usage, bool bOn)
    {
      UnsafeNativeMethods.IRhRdkCurrentEnvironment_SetOn(CppPointer, (int)usage, bOn);
    }
  }

  public abstract class RenderEnvironment : RenderContent
  {
    /// <since>6.0</since>
    [Flags]
    [Obsolete("Use Rhino.Render.RenderSettings.EnvironmentUsage")]
    public enum Usage : int
    {
      None                      = 0x00,
      Background                = 0x01,
      ReflectionAndRefraction   = 0x02,
      Skylighting               = 0x04,
      AnyUsage                  = Background | ReflectionAndRefraction | Skylighting
    }

    /// <since>5.1</since>
    [Obsolete("Obsolete, use RhinoDoc.CurrentEnvironment")]
    public static RenderEnvironment CurrentEnvironment
    {
      get 
      {
          var doc = RhinoDoc.ActiveDoc;
          if (null == doc) return null;
          return doc.CurrentEnvironment.ForBackground;
      }
      set 
      {
          var doc = RhinoDoc.ActiveDoc;
          if (null != doc)
            doc.CurrentEnvironment.ForBackground = value;
      }
    }

    /// <summary>
    /// Constructs a new <see cref="RenderEnvironment"/> from a <see cref="SimulatedEnvironment"/>.
    /// </summary>
    /// <param name="environment">The environment to create the basic environment from.</param>
    /// <returns>A new basic environment.</returns>
    /// 
    /// <since>5.3</since>
    public static RenderEnvironment NewBasicEnvironment(SimulatedEnvironment environment)
    {
      return NewBasicEnvironment(environment, null);
    }

    /// <since>6.4</since>
    public static RenderEnvironment NewBasicEnvironment(SimulatedEnvironment environment, RhinoDoc doc)
    {
      var pEnvironment = environment == null ? IntPtr.Zero : environment.ConstPointer();

      var doc_id = doc==null ? 0 : doc.RuntimeSerialNumber;

      var newEnvironment = FromPointer(UnsafeNativeMethods.Rdk_Globals_NewBasicEnvironment(pEnvironment, doc_id), null) as NativeRenderEnvironment;

      if (newEnvironment != null)
      {
        newEnvironment.AutoDelete = true;
      }

      return newEnvironment;
    }

    /// <since>5.1</since>
    public virtual void SimulateEnvironment(ref SimulatedEnvironment simulation, bool isForDataOnly)
    {
      var gen = isForDataOnly ? UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow : UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow;
      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderEnvironment_SimulateEnvironment(NonConstPointer(), simulation.ConstPointer(), gen);
      }
      else
      {
        UnsafeNativeMethods.Rdk_RenderEnvironment_CallSimulateEnvironmentBase(NonConstPointer(), simulation.ConstPointer(), gen);
      }
    }

    /// <since>6.0</since>
    public virtual SimulatedEnvironment SimulateEnvironment(bool isForDataOnly)
    {
      var env = new SimulatedEnvironment();
      SimulateEnvironment(ref env, isForDataOnly);
      return env;
    }

    /// <since>6.0</since>
    public String TextureChildSlotName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          var p_string = sh.NonConstPointer();
#pragma warning disable 618 // RenderEnvironment.Usage is deprecated but we still need it to work.
          UnsafeNativeMethods.Rdk_RenderEnvironment_GetTextureChildSlotName(ConstPointer(), p_string,
                                                                           (int)Usage.Background);
#pragma warning restore 618
          return sh.ToString();
        }
      }
    }

    #region callbacks from c++

    internal static NewRenderContentCallbackEvent m_NewEnvironmentCallback = OnNewEnvironment;
    static IntPtr OnNewEnvironment(Guid typeId)
    {
      var renderContent = NewRenderContent(typeId, typeof(RenderEnvironment));
      return (null == renderContent ? IntPtr.Zero : renderContent.NonConstPointer());
    }

    internal delegate void SimulateEnvironmentCallback(int serial_number, IntPtr p, UnsafeNativeMethods.CRhRdkTextureGenConsts textureGen);
    internal static SimulateEnvironmentCallback m_SimulateEnvironment = OnSimulateEnvironment;
    static void OnSimulateEnvironment(int serial_number, IntPtr pSim, UnsafeNativeMethods.CRhRdkTextureGenConsts textureGen)
    {
      try
      {
        var content = FromSerialNumber(serial_number) as RenderEnvironment;
        if (content != null && pSim != IntPtr.Zero)
        {
          var sim = new SimulatedEnvironment(pSim);
          var data_only = (textureGen == UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow);
          content.SimulateEnvironment(ref sim, data_only);
        }
      }
      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
      }
    }

    #endregion
  }






  #region Native wrapper
  // DO NOT make public
  internal class NativeRenderEnvironment : RenderEnvironment, INativeContent
  {
    readonly Guid m_native_instance_id = Guid.Empty;
    Guid m_document_id;
    IntPtr m_transient_pointer = IntPtr.Zero;

    //This is critical in tracking content that was the original owner (ie - is AutoDelete) and may go out of scope
    //because the "ownership" has been transferred to this non-owning wrapper.  That means that
    //if m_related_content is non-null, it should (initially) be pointing to an AutoDelete content, and this must be non-Autodelete
    //Its only purpose is to stop stuff getting collected.
    RenderContent m_related_content = null;

    public NativeRenderEnvironment(IntPtr pRenderContent, RenderContent related)
    {
      m_native_instance_id = UnsafeNativeMethods.Rdk_RenderContent_InstanceId(pRenderContent);
      m_document_id = UnsafeNativeMethods.Rdk_RenderContent_RdkDocumentAssocId(pRenderContent);
      m_related_content = related;

      if (IntPtr.Zero == ConstPointer())
      {
        //The content is not registered.  Set the actual pointer just for these objects (at the moment, modally edited returned contents)
        m_transient_pointer = pRenderContent;
      }
    }
    public override string TypeName { get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.TypeName); } }
    public override string TypeDescription { get { return GetString(UnsafeNativeMethods.RenderContent_StringIds.TypeDescription); } }
    internal override IntPtr ConstPointer()
    {
      var ptr_this = UnsafeNativeMethods.Rdk_FindContentInstanceInRdkDoc(m_document_id, m_native_instance_id);
      if (IntPtr.Zero != ptr_this)
        return ptr_this;
      return m_transient_pointer;
    }
    internal override IntPtr NonConstPointer()
    {
      var ptr_this = UnsafeNativeMethods.Rdk_FindContentInstanceInRdkDoc(m_document_id, m_native_instance_id);
      if (IntPtr.Zero != ptr_this)
        return ptr_this;
      return m_transient_pointer;
    }

    Guid INativeContent.Document
    {
      get { return m_document_id; }
      set { m_document_id = value; }
    }
  }
  #endregion
}
#endif
