#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Diagnostics;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
  public interface ICurrentEnvironment_Get
  {
      RenderEnvironment ForBackground               { get; }
      RenderEnvironment ForBackground_CheckMode     { get; }
      RenderEnvironment ForLighting                 { get; }
      RenderEnvironment ForReflectionAndRefraction  { get; }
  }

  public interface ICurrentEnvironment : ICurrentEnvironment_Get
  {
      new RenderEnvironment ForBackground               { get; set; }
      new RenderEnvironment ForBackground_CheckMode     { get; set; }
      new RenderEnvironment ForLighting                 { get; set; }
      new RenderEnvironment ForReflectionAndRefraction  { get; set; }
      RenderEnvironment     ForAnyUsage                 { set; }
  }
  
  internal class CurrentEnvironmentImpl : ICurrentEnvironment
  {
    IntPtr m_ptr = IntPtr.Zero;
    uint m_doc_serial = 0;

    internal CurrentEnvironmentImpl(IntPtr ptr_IRhRdkCurrentEnvironment)
    {
        m_ptr = ptr_IRhRdkCurrentEnvironment;
    }

    internal CurrentEnvironmentImpl(uint serial)
    {
        m_doc_serial = serial;
    }

    IntPtr IRhRdkCurrentEnvironmentPtr
    { 
        get
        {
            if (m_doc_serial == 0)
                return m_ptr;
            return UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_FromDocSerial(m_doc_serial);
        }
    }

    [Obsolete("Use plain ForBackground")]
    public RenderEnvironment ForBackground_CheckMode
    {
        get
        {
            return RenderEnvironment.FromPointer(UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_GetBackground(IRhRdkCurrentEnvironmentPtr, true)) as RenderEnvironment;
        }
        set
        {
            ForBackground = value;
        }
    }

    /// <summary>
    /// Get or set the RenderEnvironment used as 360deg background.
    ///
    /// Null will be returned if the background environment isn't on.
    /// 
    /// To unset current background environment pass in null. Setting to a valid environment will also
    /// turn the environment for this specific usage on.
    /// </summary>
    public RenderEnvironment ForBackground
    {
        get
        {
            return RenderEnvironment.FromPointer(UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_GetBackground(IRhRdkCurrentEnvironmentPtr, false)) as RenderEnvironment;
        }
        set
        {
            if(value==null)
              UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetBackground(IRhRdkCurrentEnvironmentPtr, Guid.Empty);
            else
              UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetBackground(IRhRdkCurrentEnvironmentPtr, value.Id);
        }
    }

    /// <summary>
    /// Get or set the RenderEnvironment used as custom skylight environment.
    /// 
    /// Null will be returned if the custom environment isn't on.
    /// 
    /// To unset current custom environment pass in null. Setting to a valid environment will also
    /// turn the environment for this specific usage on.
    /// </summary>
    public RenderEnvironment ForLighting
    {
        get
        {
            return RenderEnvironment.FromPointer(UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_GetLighting(IRhRdkCurrentEnvironmentPtr)) as RenderEnvironment;
        }
        set
        {
            if(value==null)
              UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetLighting(IRhRdkCurrentEnvironmentPtr, Guid.Empty);
            else
              UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetLighting(IRhRdkCurrentEnvironmentPtr, value.Id);
        }
    }

    /// <summary>
    /// Get or set the RenderEnvironment used as custom reflection and refraction environment.
    /// 
    /// Null will be returned if the custom environment isn't on.
    /// 
    /// To unset current custom environment pass in null. Setting to a valid environment will also
    /// turn the environment for this specific usage on.
    /// </summary>
    public RenderEnvironment ForReflectionAndRefraction
    {
        get
        {
            return RenderEnvironment.FromPointer(UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_GetReflectionAndRefraction(IRhRdkCurrentEnvironmentPtr)) as RenderEnvironment;
        }
        set
        {
            if(value==null)
              UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetReflectionAndRefraction(IRhRdkCurrentEnvironmentPtr, Guid.Empty);
            else
              UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetReflectionAndRefraction(IRhRdkCurrentEnvironmentPtr, value.Id);
        }
    }

    /// <summary>
    /// Set given RenderEnvironment for all usage forms (background/360deg, custom reflection and refraction,
    /// custom skylight).
    /// Give null to disable all environments
    /// </summary>
    public RenderEnvironment ForAnyUsage
    {
        set
        {
        if (value == null)
        {
          UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetBackground(IRhRdkCurrentEnvironmentPtr, Guid.Empty);
          UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetLighting(IRhRdkCurrentEnvironmentPtr, Guid.Empty);
          UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetReflectionAndRefraction(IRhRdkCurrentEnvironmentPtr, Guid.Empty);
        }
        else
        {
          UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetBackground(IRhRdkCurrentEnvironmentPtr, value.Id);
          UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetLighting(IRhRdkCurrentEnvironmentPtr, value.Id);
          UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetReflectionAndRefraction(IRhRdkCurrentEnvironmentPtr, value.Id);
        }
        }
    }

    /// <summary>
    /// Check if given environment is enabled
    /// </summary>
    public bool On(RenderEnvironment.Usage usage)
    {
      return UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_On(IRhRdkCurrentEnvironmentPtr, (uint)usage);
    }

    /// <summary>
    /// Set given environment on/off
    /// </summary>
    public void SetOn(RenderEnvironment.Usage usage, bool bOn)
    {
      UnsafeNativeMethods.Rdk_IRhRdkCurrentEnvironment_SetOn(IRhRdkCurrentEnvironmentPtr, (uint)usage, bOn);
    }
  }

  public abstract class RenderEnvironment : RenderContent
  {
    [Flags]
    public enum Usage : int
    {
      None                      = 0x00,
      Background                = 0x01,
      ReflectionAndRefraction   = 0x02,
      Skylighting               = 0x04,
      AnyUsage                  = Background | ReflectionAndRefraction | Skylighting
    }

    [Obsolete("Obsolete, use RhinoDoc.CurrentEnvironment")]
    public static RenderEnvironment CurrentEnvironment
    {
      get 
      {
          var doc = RhinoDoc.ActiveDoc;
          if (null == doc)
              return null;
          return doc.CurrentEnvironment.ForBackground_CheckMode;
      }
      set 
      {
          var doc = RhinoDoc.ActiveDoc;
          if (null == doc)
              return;

          doc.CurrentEnvironment.ForBackground = value;
      }
    }

    /// <summary>
    /// Constructs a new <see cref="RenderEnvironment"/> from a <see cref="SimulatedEnvironment"/>.
    /// </summary>
    /// <param name="environment">The environment to create the basic environment from.</param>
    /// <returns>A new basic environment.</returns>
    /// 
    public static RenderEnvironment NewBasicEnvironment(SimulatedEnvironment environment)
    {
      return NewBasicEnvironment(environment, null);
    }

    public static RenderEnvironment NewBasicEnvironment(SimulatedEnvironment environment, RhinoDoc doc)
    {
      var pEnvironment = environment == null ? IntPtr.Zero : environment.ConstPointer();

      var doc_id = doc==null ? 0 : doc.RuntimeSerialNumber;

      var newEnvironment = FromPointer(UnsafeNativeMethods.Rdk_Globals_NewBasicEnvironment(pEnvironment, doc_id)) as NativeRenderEnvironment;

      if (newEnvironment != null)
      {
        newEnvironment.AutoDelete = true;
      }

      return newEnvironment;
    }

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

    public virtual SimulatedEnvironment SimulateEnvironment(bool isForDataOnly)
    {
      var env = new SimulatedEnvironment();
      SimulateEnvironment(ref env, isForDataOnly);
      return env;
    }

    public String TextureChildSlotName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.Rdk_RenderEnvironment_GetTextureChildSlotName(ConstPointer(), p_string, (int)Usage.Background);
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

    public NativeRenderEnvironment(IntPtr pRenderContent)
    {
      m_native_instance_id = UnsafeNativeMethods.Rdk_RenderContent_InstanceId(pRenderContent);
      m_document_id = UnsafeNativeMethods.Rdk_RenderContent_RdkDocumentRegisteredId(pRenderContent);

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
