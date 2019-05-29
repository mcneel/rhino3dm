#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Runtime;

namespace Rhino.Render
{
  public enum TextureProjectionMode : int
  {
    MappingChannel = 0,
    View = 1,
    Wcs = 2,
    EnvironmentMap = 3,  // Now means "environment mapped" - call "EnvironmentMappingMode" to get specific projection for this texture.
    WcsBox = 4,
    Screen = 5,
  }

  // WORK IN PROGRESS, I will be using the rest of these shortly, just wanted to have
  // proof of concept checked so I am checking this in now.
  // VirtualIntValue properties
  public enum TextureMode
  {
    PROJECTION_MODE = 0,
    MAPPING_CHANNEL_MODE,
    WRAP_TYPE_MODE,
    // bool values as int
    REPEAT_LOCKED_MODE,
    OFFSET_LOCKED_MODE,
    PREVIEW_IN_3D_MODE,
    // VirtualVector3d properties
    REPEAT_MODE,
    OFFSET_MODE,
    ROTATION_MODE,
    // Non virtual int properties
    ENVIRONMENT_MAPPING_MODE,
    INTERNAL_ENVIRONMENT_MAPPING_MODE,
    // Non virtual bool properties
    PREVIEW_LOCAL_MAPPING_MODE,
    DISPLAY_IN_VIEWPORT_MODE,
    IS_HDR_CAPABLE_MODE, // (get only)
    IS_LINEAR_MODE, // (get only)
    IS_IMAGE_BASED, // (get only)
  }

  public enum TextureWrapType : int
  {
    Clamped = 0,
    Repeating = 1,
  }

  public enum TextureEnvironmentMappingMode : int
  {
    Automatic   = 0,
    Spherical   = 1,  // Equirectangular projection.
    EnvironmentMap    = 2,  // Mirrorball.
    Box         = 3,
    LightProbe = 5,
    Cube  = 6,
    VerticalCrossCube = 7,
    HorizontalCrossCube = 8,
    Hemispherical = 9,
  }

  public enum TextureGeneration : int
  {
    Allow = UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow,
    Disallow = UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow
  }

  [CLSCompliant(false)]
  public enum TextureRenderHashFlags : ulong
  {
    ExcludeLocalMapping = (1 << 32),
  }

  public abstract class RenderTexture : RenderContent
  {
    /// <summary>
    /// Constructs a new basic texture from a SimulatedTexture.
    /// </summary>
    /// <param name="texture">The texture to create the basic texture from.</param>
    /// <returns>A new render texture.</returns>
    [Obsolete]
    public static RenderTexture NewBitmapTexture(SimulatedTexture texture)
    {
      return NewBitmapTexture(texture, null);
    }

    public static RenderTexture NewBitmapTexture(SimulatedTexture texture, RhinoDoc doc)
    {
      var p_const_texture = texture == null ? IntPtr.Zero : texture.ConstPointer();
      
      var new_texture = FromPointer(UnsafeNativeMethods.Rdk_Globals_NewBasicTexture(p_const_texture, (null == doc) ? 0 : doc.RuntimeSerialNumber)) as NativeRenderTexture;

      if (new_texture != null)
      {
        new_texture.AutoDelete = true;
      }

      return new_texture;
    }

    /// <summary>
    /// Save texture as image
    /// </summary>
    /// <param name="FullPath">The full path of the file</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="depth">Image depth</param>
    /// <returns>returns true if file was saved, otherwise false</returns>
    public bool SaveAsImage(string FullPath, int width, int height, int depth)
    {
      using (var sw = new Rhino.Runtime.InteropWrappers.StringWrapper(FullPath))
      {
        var p_string = sw.NonConstPointer;
        var p_const_this = ConstPointer();
        return UnsafeNativeMethods.Rdk_RenderTexture_SaveAsImage(p_const_this, p_string, width, height, depth);
      } 
    }

    /// <summary>
    /// Render hash for texture excluding local mapping.
    /// </summary>
    [CLSCompliant(false)]
    public uint RenderHashWithoutLocalMapping
    {
      get
      {
        ulong rcrcFlags = (ulong)TextureRenderHashFlags.ExcludeLocalMapping;
        var const_pointer = ConstPointer();
        return UnsafeNativeMethods.Rdk_RenderContent_RenderCRC(const_pointer, rcrcFlags);
      }
    }

    /// <summary>
    /// Gets the transformation that can be applied to the UVW vector to convert it
    /// from normalized texture space into locally mapped space (ie - with repeat,
    /// offset and rotation applied.)
    /// </summary>
    public Transform LocalMappingTransform
    {
      get
      {
        var xform = new Transform();
        var p_const_this = ConstPointer();
        UnsafeNativeMethods.Rdk_RenderTexture_LocalMappingTransform(p_const_this, ref xform);
        return xform;
      }
    }

    /// <summary>
    /// Get the texture dimensions for the RenderTexture.
    /// </summary>
    /// <param name="u">width</param>
    /// <param name="v">height</param>
    /// <param name="w">depth, used for 3D textures</param>
    public void PixelSize(out int u, out int v, out int w)
    {
      int _u = 0;
      int _v = 0;
      int _w = 0;
      UnsafeNativeMethods.Rdk_RenderTexture_PixelSize(ConstPointer(), ref _u, ref _v, ref _w);

      u = _u;
      v = _v;
      w = _w;
    }

    [Flags]
    public enum TextureEvaluatorFlags : int
  {
    Normal                  = 0x0000,
    DisableFiltering        = 0x0001, // Force the texture to be evaluated without filtering (the implementation gets to decide what that means).
    DisableLocalMapping     = 0x0002, // Force the texture to be evaluated without local mapping - ie, Repeat(1.0, 1.0, 1.0), Offset(0.0, 0.0 0.0), Rotation(0.0, 0.0, 0.0).
    DisableAdjustment       = 0x0004, // Force the texture to be evaluated without post-adjustment (greyscale, invert, clamp etc)
    DisableProjectionChange = 0x0008, // Force the texture to be evaluated without any projection modification (ProjectionIn == ProjectionOut)
  };

    [Flags]
    [CLSCompliant(false)]
    public enum eLocalMappingType : uint
    {
      lmt_none,
      lmt_2D,
      lmt_3D,
      lmt_force32bit = 0xFFFFFFFF,
    };

    /// <summary>
    /// Constructs a texture evaluator. This is an independent lightweight object
    /// capable of evaluating texture color throughout uvw space. May be called
    /// from within a rendering shade pipeline.
    /// </summary>
    /// <returns>A texture evaluator instance.</returns>
    public virtual TextureEvaluator CreateEvaluator(TextureEvaluatorFlags evaluatorFlags)
    {
      if (IsNativeWrapper())
      {
        var p_const_this = ConstPointer();
        var p_te = UnsafeNativeMethods.Rdk_RenderTexture_NewTextureEvaluator(p_const_this, (uint)evaluatorFlags);
        if (p_te != IntPtr.Zero)
        {
          var te = TextureEvaluator.FromPointer(p_te, false);
          return te;
        }
      }
      return null;
    }

    /// <summary>
    /// Constructs a texture evaluator. This is an independent lightweight object
    /// capable of evaluating texture color throughout uvw space. May be called
    /// from within a rendering shade pipeline.
    /// </summary>
    /// <returns>A texture evaluator instance.</returns>
    [Obsolete("Use version that takes TextureEvaluatorFlags enum")]
    public virtual TextureEvaluator CreateEvaluator()
    {
        return CreateEvaluator(TextureEvaluatorFlags.Normal);
    }

    public enum TextureGeneration
    {
      Allow = UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow,
      Disallow = UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow
    }

    [Obsolete("Use SimulateTexture with size, TextureGeneration and object instead")]
    public virtual void SimulateTexture(ref SimulatedTexture simulation, bool isForDataOnly)
    {
      var gen = isForDataOnly
        ? UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow
        : UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow;

      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderTexture_SimulateTexture(NonConstPointer(), simulation.ConstPointer(), gen, -1, IntPtr.Zero);
      }
      else
      {
        UnsafeNativeMethods.Rdk_RenderTexture_CallSimulateTextureBase(NonConstPointer(), simulation.ConstPointer(), gen, -1, IntPtr.Zero);
      }
    }

    public virtual void SimulateTexture(ref SimulatedTexture simulation, TextureGeneration tg, int size = -1, Rhino.DocObjects.RhinoObject obj = null)
    {
      var pObject = (obj == null) ? IntPtr.Zero : obj.ConstPointer();
      var tex_gen = (UnsafeNativeMethods.CRhRdkTextureGenConsts)(int)tg;

      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderTexture_SimulateTexture(NonConstPointer(), simulation.ConstPointer(), tex_gen, size, pObject);
      }
      else
      {
        UnsafeNativeMethods.Rdk_RenderTexture_CallSimulateTextureBase(NonConstPointer(), simulation.ConstPointer(), tex_gen, size, pObject);
      }
    }

    public virtual SimulatedTexture SimulatedTexture(TextureGeneration tg, int size = -1, Rhino.DocObjects.RhinoObject obj = null)
    {
      var simtex = new SimulatedTexture();

      SimulateTexture(ref simtex, tg, size, obj);

      return simtex;
    }

    public virtual TextureProjectionMode GetProjectionMode()
    {
      var const_pointer = ConstPointer();
      var result = UnsafeNativeMethods.Rdk_RenderTexture_GetVirtualIntValue(const_pointer, (int)TextureMode.PROJECTION_MODE, true);
      return (TextureProjectionMode) result;
    }

    public virtual void SetProjectionMode(TextureProjectionMode value, ChangeContexts changeContext)
    {
      BeginChange(changeContext);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtualIntValue(pointer, (int)TextureMode.PROJECTION_MODE, true, (int) value);
      EndChange();
    }

    public virtual TextureWrapType GetWrapType()
    {
      var const_pointer = ConstPointer();
      var result = UnsafeNativeMethods.Rdk_RenderTexture_GetVirtualIntValue(const_pointer, (int)TextureMode.WRAP_TYPE_MODE, true);
      return (TextureWrapType) result;
    }

    public virtual void SetWrapType(TextureWrapType value, ChangeContexts changeContext)
    {
      BeginChange(changeContext);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtualIntValue(pointer, (int)TextureMode.WRAP_TYPE_MODE, true, (int) value);
      EndChange();
    }

    public virtual int GetMappingChannel()
    {
      var const_pointer = ConstPointer();
      var result = UnsafeNativeMethods.Rdk_RenderTexture_GetVirtualIntValue(const_pointer, (int)TextureMode.MAPPING_CHANNEL_MODE, true);
      return result;
    }

    public virtual void SetMappingChannel(int value, ChangeContexts changeContext)
    {
      BeginChange(changeContext);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtualIntValue(pointer, (int)TextureMode.MAPPING_CHANNEL_MODE, true, value);
      EndChange();
    }

    public virtual bool GetRepeatLocked()
    {
      var const_pointer = ConstPointer();
      var result = UnsafeNativeMethods.Rdk_RenderTexture_GetVirtualIntValue(const_pointer, (int)TextureMode.REPEAT_LOCKED_MODE, true);
      return (result != 0);
    }

    public virtual void SetRepeatLocked(bool value, ChangeContexts changeContext)
    {
      BeginChange(changeContext);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtualIntValue(pointer, (int)TextureMode.REPEAT_LOCKED_MODE, true, value ? 1 : 0);
      EndChange();
    }

    public virtual bool GetOffsetLocked()
    {
      var const_pointer = ConstPointer();
      var result = UnsafeNativeMethods.Rdk_RenderTexture_GetVirtualIntValue(const_pointer, (int)TextureMode.OFFSET_LOCKED_MODE, true);
      return (result != 0);
    }

    public virtual void SetOffsetLocked(bool value, ChangeContexts changeContext)
    {
      BeginChange(changeContext);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtualIntValue(pointer, (int)TextureMode.OFFSET_LOCKED_MODE, true, value ? 1 : 0);
      EndChange();
    }

    public virtual bool GetPreviewIn3D()
    {
      var const_pointer = ConstPointer();
      var result = UnsafeNativeMethods.Rdk_RenderTexture_GetVirtualIntValue(const_pointer, (int)TextureMode.PREVIEW_IN_3D_MODE, true);
      return (result != 0);
    }

    public virtual void SetPreviewIn3D(bool value, ChangeContexts changeContext)
    {
      BeginChange(changeContext);
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtualIntValue(pointer, (int)TextureMode.PREVIEW_IN_3D_MODE, true, value ? 1 : 0);
      EndChange();
    }

    /// <summary>
    /// Get repeat value across UVW space. If the projection type is WCS or
    /// other type specified in model units, then this is the repeat across 1
    /// meter of the model.
    /// </summary>
    /// <returns></returns>
    public virtual Vector3d GetRepeat()
    {
      var const_pointer = ConstPointer();
      var vector = Vector3d.Unset;
      UnsafeNativeMethods.Rdk_RenderTexture_GetVirtual3dVector(const_pointer, (int)TextureMode.REPEAT_MODE, true, ref vector);
      return vector;
    }

    /// <summary>
    /// Set repeat value across UVW space. If the projection type is WCS or
    /// other type specified in model units, then this is the repeat across 1
    /// meter of the model.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="changeContext"></param>
    public virtual void SetRepeat(Vector3d value, ChangeContexts changeContext)
    {
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtual3dVector(pointer, (int)TextureMode.REPEAT_MODE, true, value, (int) changeContext);
    }

    /// <summary>
    /// Get offset value across UVW space. If the projection type is WCS or
    /// other type specified in model units, then this is the offset in meters.
    /// </summary>
    /// <returns></returns>
    public virtual Vector3d GetOffset()
    {
      var const_pointer = ConstPointer();
      var vector = Vector3d.Unset;
      UnsafeNativeMethods.Rdk_RenderTexture_GetVirtual3dVector(const_pointer, (int)TextureMode.OFFSET_MODE, true, ref vector);
      return vector;
    }

    /// <summary>
    /// Set offset value across UVW space. If the projection type is WCS or
    /// other type specified in model units, then this is the offset in meters.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="changeContext"></param>
    public virtual void SetOffset(Vector3d value, ChangeContexts changeContext)
    {
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtual3dVector(pointer, (int)TextureMode.OFFSET_MODE, true, value, (int) changeContext);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public virtual Vector3d GetRotation()
    {
      var const_pointer = ConstPointer();
      var vector = Vector3d.Unset;
      UnsafeNativeMethods.Rdk_RenderTexture_GetVirtual3dVector(const_pointer, (int)TextureMode.ROTATION_MODE, true, ref vector);
      return vector;
    }

    public virtual void SetRotation(Vector3d value, ChangeContexts changeContext)
    {
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetVirtual3dVector(pointer, (int)TextureMode.ROTATION_MODE, true, value, (int) changeContext);
    }

    public TextureEnvironmentMappingMode GetInternalEnvironmentMappingMode()
    {
      var const_pointer = ConstPointer();
      return
        (TextureEnvironmentMappingMode)
          UnsafeNativeMethods.Rdk_RenderTexture_GetIntValue(const_pointer, (int)TextureMode.INTERNAL_ENVIRONMENT_MAPPING_MODE,
            (int) TextureEnvironmentMappingMode.Automatic);
    }

    public TextureEnvironmentMappingMode GetEnvironmentMappingMode()
    {
      var const_pointer = ConstPointer();
      return
        (TextureEnvironmentMappingMode)
          UnsafeNativeMethods.Rdk_RenderTexture_GetIntValue(const_pointer, (int)TextureMode.ENVIRONMENT_MAPPING_MODE,
            (int) TextureEnvironmentMappingMode.Automatic);
    }

    public void SetEnvironmentMappingMode(TextureEnvironmentMappingMode value, ChangeContexts changeContext)
    {
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetIntValue(pointer, (int)TextureMode.ENVIRONMENT_MAPPING_MODE, (int) value, (int) changeContext);
    }

    public void SetEnvironmentMappingMode(TextureEnvironmentMappingMode value)
    {
      SetEnvironmentMappingMode(value, ChangeContexts.Program);
    }

    public bool GetPreviewLocalMapping()
    {
      var const_pointer = ConstPointer();
      return (0 != UnsafeNativeMethods.Rdk_RenderTexture_GetIntValue(const_pointer, (int)TextureMode.PREVIEW_LOCAL_MAPPING_MODE, 1));
    }

    public void SetPreviewLocalMapping(bool value, ChangeContexts changeContext)
    {
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetIntValue(pointer, (int)TextureMode.PREVIEW_LOCAL_MAPPING_MODE, value ? 1 : 0,
        (int) changeContext);
    }

    [CLSCompliant(false)]
    public eLocalMappingType GetLocalMappingType()
    {
      var const_pointer = ConstPointer();
      eLocalMappingType type = eLocalMappingType.lmt_none;

      uint type_value = UnsafeNativeMethods.Rdk_RenderTexture_LocalMappingType(const_pointer);

      if (type_value == 0)
        type = eLocalMappingType.lmt_none;

      if (type_value == 1)
        type = eLocalMappingType.lmt_2D;

      if (type_value == 2)
        type = eLocalMappingType.lmt_3D;

      return type;
    }

    public void SetPreviewLocalMapping(bool value)
    {
      SetPreviewLocalMapping(value, ChangeContexts.Program);
    }

    public bool GetDisplayInViewport()
    {
      var const_pointer = ConstPointer();
      return (0 != UnsafeNativeMethods.Rdk_RenderTexture_GetIntValue(const_pointer, (int)TextureMode.DISPLAY_IN_VIEWPORT_MODE, 1));
    }

    public void SetDisplayInViewport(bool value, ChangeContexts changeContext)
    {
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetIntValue(pointer, (int)TextureMode.DISPLAY_IN_VIEWPORT_MODE, value ? 1 : 0,
        (int) changeContext);
    }

    public void SetDisplayInViewport(bool value)
    {
      SetDisplayInViewport(value, ChangeContexts.Program);
    }

    public bool IsHdrCapable()
    {
      var const_pointer = ConstPointer();
      return (0 != UnsafeNativeMethods.Rdk_RenderTexture_GetIntValue(const_pointer, (int)TextureMode.IS_HDR_CAPABLE_MODE, 0));
    }

    public virtual bool IsLinear()
    {
      var const_pointer = ConstPointer();
      return (0 != UnsafeNativeMethods.Rdk_RenderTexture_GetIntValue(const_pointer, (int) TextureMode.IS_LINEAR_MODE, 0));
    }

    public void GraphInfo(ref TextureGraphInfo tgi)
    {
      var const_pointer = ConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_GraphInfo(const_pointer, tgi.CppPointer);
    }

    public void SetGraphInfo(TextureGraphInfo tgi)
    {
      var const_pointer = ConstPointer();
      UnsafeNativeMethods.Rdk_RenderTexture_SetGraphInfo(const_pointer, tgi.CppPointer);
    }

    /// <summary>
    /// Query if the texture is image based.
    /// </summary>
    /// <returns>true if the texture is image-based.</returns>
    public virtual bool IsImageBased()
    {
      var const_pointer = ConstPointer();
      return (0 != UnsafeNativeMethods.Rdk_RenderTexture_GetIntValue(const_pointer, (int) TextureMode.IS_IMAGE_BASED, 0));

    }

    public static bool GetEnvironmentMappingProjection(TextureEnvironmentMappingMode mode, Vector3d reflectionVector,
      out float u, out float v)
    {
      u = v = 0;
      return UnsafeNativeMethods.Rdk_RenderTexture_EnvironmentMappingProjection((int) mode, reflectionVector, ref u, ref v);
    }

    public static Point3d GetWcsBoxMapping(Point3d worldXyz, Vector3d normal)
    {
      var value = Point3d.Unset;
      UnsafeNativeMethods.Rdk_RenderTexture_WcsBoxMapping(worldXyz, normal, ref value);
      return value;
    }

    #region callbacks from c++

    internal static NewRenderContentCallbackEvent m_NewTextureCallback = OnNewTexture;

    private static IntPtr OnNewTexture(Guid typeId)
    {
      var render_content = NewRenderContent(typeId, typeof (RenderTexture));
      return (null == render_content ? IntPtr.Zero : render_content.NonConstPointer());
    }

    internal delegate void SimulateTextureCallback(int serialNumber, IntPtr p, UnsafeNativeMethods.CRhRdkTextureGenConsts textureGen, int size, uint object_sn);
    internal static SimulateTextureCallback m_SimulateTexture = OnSimulateTexture;
    private static void OnSimulateTexture(int serialNumber, IntPtr pSim, UnsafeNativeMethods.CRhRdkTextureGenConsts textureGen, int size, uint object_sn)
    {
      try
      {
        var content = FromSerialNumber(serialNumber) as RenderTexture;
        if (content != null && pSim != IntPtr.Zero)
        {
          var sim = new SimulatedTexture(pSim);

          //Can be - and probably will be - null
          var obj = Rhino.DocObjects.RhinoObject.FromRuntimeSerialNumber(object_sn);

          content.SimulateTexture(ref sim, (TextureGeneration)(int)textureGen, size, obj);
        }
      }
      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
      }
    }


  internal delegate int GetVirtualIntCallback(int serialNumber, int propertyId, bool fromBaseClass);
    internal static GetVirtualIntCallback GetVirtualInt = OnGetVirtualInt;
    static int OnGetVirtualInt(int serialNumber, int propertyId, bool fromBaseClass)
    {
      try
      {
        var texture = FromSerialNumber(serialNumber) as RenderTexture;
        if (texture == null) return -1;
        if (fromBaseClass) return UnsafeNativeMethods.Rdk_RenderTexture_GetVirtualIntValue(texture.ConstPointer(), propertyId, true);
        switch ((TextureMode)propertyId)
        {
          case TextureMode.PROJECTION_MODE:
            return (int)texture.GetProjectionMode();
          case TextureMode.WRAP_TYPE_MODE:
            return (int)texture.GetWrapType();
          case TextureMode.REPEAT_LOCKED_MODE:
            return texture.GetRepeatLocked() ? 1 : 0;
          case TextureMode.OFFSET_LOCKED_MODE:
            return texture.GetOffsetLocked() ? 1 : 0;
          case TextureMode.PREVIEW_IN_3D_MODE:
            return texture.GetPreviewIn3D() ? 1 : 0;
        }
        return -1;
      }
      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
      }
      return -1;
    }

    internal delegate void SetVirtualIntCallback(int serialNumber, int propertyId, bool callBaseClass, int value);
    internal static SetVirtualIntCallback SetVirtualInt = OnSetVirtualInt;
    static void OnSetVirtualInt(int serialNumber, int propertyId, bool callBaseClass, int value)
    {
      try
      {
        const ChangeContexts change_context = ChangeContexts.Program;
        var texture = FromSerialNumber(serialNumber) as RenderTexture;
        if (texture == null) return;
        if (callBaseClass)
        {
          var pointer = texture.NonConstPointer();
          UnsafeNativeMethods.Rdk_RenderTexture_SetVirtualIntValue(pointer, propertyId, true, value);
          return;
        }
        switch (propertyId)
        {
          case (int)TextureMode.PROJECTION_MODE:
            texture.SetProjectionMode((TextureProjectionMode)value, change_context);
            break;
          case (int)TextureMode.WRAP_TYPE_MODE:
            texture.SetWrapType((TextureWrapType)value, change_context);
            break;
          case (int)TextureMode.REPEAT_LOCKED_MODE:
            texture.SetRepeatLocked(value != 0, change_context);
            break;
          case (int)TextureMode.OFFSET_LOCKED_MODE:
            texture.SetOffsetLocked(value != 0, change_context);
            break;
          case (int)TextureMode.PREVIEW_IN_3D_MODE:
            texture.SetPreviewIn3D(value != 0, change_context);
            break;
        }
      }
      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
      }
    }

    internal delegate void GetVirtual3DVectorCallback(int serialNumber, int propertyId, bool fromBaseClass, ref Vector3d value);
    internal static GetVirtual3DVectorCallback GetVirtual3DVector = OnGetVirtual3DVector;
    private static void OnGetVirtual3DVector(int serialNumber, int propertyId, bool fromBaseClass, ref Vector3d value)
    {
      try
      {
        var texture = FromSerialNumber(serialNumber) as RenderTexture;
        if (texture == null) return;
        var const_pointer = texture.ConstPointer();
        if (fromBaseClass)
        {
          UnsafeNativeMethods.Rdk_RenderTexture_GetVirtual3dVector(const_pointer, propertyId, true, ref value);
          return;
        }
        switch (propertyId)
        {
          case (int)TextureMode.REPEAT_MODE:
            value = texture.GetRepeat();
            return;
          case (int)TextureMode.OFFSET_MODE:
            value = texture.GetOffset();
            return;
          case (int)TextureMode.ROTATION_MODE:
            value = texture.GetRotation();
            return;
        }
      }
      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
      }
    }


    internal delegate void SetVirtual3DVectorCallback(int serialNumber, int propertyId, bool callBaseClass, Vector3d value, int changeContext);
    internal static SetVirtual3DVectorCallback SetVirtual3DVector = OnSetVirtual3DVector;
    private static void OnSetVirtual3DVector(int serialNumber, int propertyId, bool callBaseClass, Vector3d value, int changeContext)
    {
      try
      {
        var texture = FromSerialNumber(serialNumber) as RenderTexture;
        if (texture == null) return;
        if (callBaseClass)
        {
          var pointer = texture.NonConstPointer();
          UnsafeNativeMethods.Rdk_RenderTexture_SetVirtual3dVector(pointer, propertyId, true, value, changeContext);
          return;
        }
        switch (propertyId)
        {
          case (int)TextureMode.REPEAT_MODE:
            texture.SetRepeat(value, (ChangeContexts)changeContext);
            return;
          case (int)TextureMode.OFFSET_MODE:
            texture.SetOffset(value, (ChangeContexts)changeContext);
            return;
          case (int)TextureMode.ROTATION_MODE:
            texture.SetRotation(value, (ChangeContexts)changeContext);
            return;
        }
      }
      catch (Exception exception)
      {
        HostUtils.ExceptionReport(exception);
      }
    }

    internal delegate IntPtr GetNewTextureEvaluatorCallback(int serialNumber, uint evaluatorFlags);
    internal static GetNewTextureEvaluatorCallback m_NewTextureEvaluator = OnNewTextureEvaluator;
    static IntPtr OnNewTextureEvaluator(int serialNumber, uint evaluatorFlags)
    {
      var rc = IntPtr.Zero;
      try
      {
        var texture = FromSerialNumber(serialNumber) as RenderTexture;
        if (texture != null)
        {
          var eval = texture.CreateEvaluator((TextureEvaluatorFlags)evaluatorFlags);
          if (eval != null)
          {
            rc = eval.CppPointer;
          }
        }
      }
      catch
      {
        rc = IntPtr.Zero;
      }
      return rc;
    }
    #endregion

    internal static TextureGeneration ConvertTextureGeneration(UnsafeNativeMethods.CRhRdkTextureGenConsts textureGen)
    {
      switch (textureGen)
      {
        case UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow:
          return TextureGeneration.Allow;
        case UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow:
          return TextureGeneration.Disallow;
      }
      throw new Exception("Unsupported RenderMaterial.TextureGeneration value encountered.");
    }
    internal static UnsafeNativeMethods.CRhRdkTextureGenConsts ConvertTextureGeneration(TextureGeneration textureGen)
    {
      switch (textureGen)
      {
        case TextureGeneration.Allow:
          return UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow;
        case TextureGeneration.Disallow:
          return UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow;
      }
      throw new Exception("Unsupported UnsafeNativeMethods.CRhRdkTextureGenConsts value encountered.");
    }
  
  }

  public class TextureGraphInfo : IDisposable
  {
    public enum Axis
    {
      kU = 0,
      kV = 1,
      kW = 2,
    };

    public enum Channel
    {
      kRed = 0,
      kGrn = 1,
      kBlu = 2,
      kAlp = 3,
      kLum = 4,
    };

    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public TextureGraphInfo()
    {
      m_cpp = UnsafeNativeMethods.Rdk_TextureGraphInfo_New();
    }

    ~TextureGraphInfo()
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
        UnsafeNativeMethods.Rdk_TextureGraphInfo_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public double AmountU()
    {
      return UnsafeNativeMethods.Rdk_TextureGraphInfo_AmountU(m_cpp);
    }

    public void SetAmountU(double d)
    {
      UnsafeNativeMethods.Rdk_TextureGraphInfo_SetAmountU(m_cpp, d);
    }

    public double AmountV()
    {
      return UnsafeNativeMethods.Rdk_TextureGraphInfo_AmountV(m_cpp);
    }

    public void SetAmountV(double d)
    {
          UnsafeNativeMethods.Rdk_TextureGraphInfo_SetAmountV(m_cpp, d);
    }

    public double AmountW()
    {
      return UnsafeNativeMethods.Rdk_TextureGraphInfo_AmountW(m_cpp);
    }

    public void SetAmountW(double d)
    {
      UnsafeNativeMethods.Rdk_TextureGraphInfo_SetAmountW(m_cpp, d);
    }

    public Axis ActiveAxis()
    {
      int axis_value = UnsafeNativeMethods.Rdk_TextureGraphInfo_ActiveAxis(m_cpp);

      Axis axis = Axis.kU;
      if (axis_value == 0)
        axis = Axis.kU;
      if (axis_value == 1)
        axis = Axis.kV;
      if (axis_value == 2)
        axis = Axis.kW;

      return axis;
    }

    public void SetActiveAxis(Axis axis)
    {
      UnsafeNativeMethods.Rdk_TextureGraphInfo_SetActiveAxis(m_cpp, (int)axis);
    }

    public Channel ActiveChannel()
    {
      int channel_value = UnsafeNativeMethods.Rdk_TextureGraphInfo_ActiveChannel(m_cpp);

      Channel channel = Channel.kRed;
      if (channel_value == 0)
        channel = Channel.kRed;
      if (channel_value == 1)
        channel = Channel.kGrn;
      if (channel_value == 2)
        channel = Channel.kBlu;
      if (channel_value == 3)
        channel = Channel.kAlp;
      if (channel_value == 4)
        channel = Channel.kLum;

      return channel;
    }

    public void SetActiveChannel(Channel channel)
    {
      UnsafeNativeMethods.Rdk_TextureGraphInfo_SetActiveChannel(m_cpp, (int)channel);
    }
  }


  public abstract class TwoColorRenderTexture : RenderTexture
  {
    protected override sealed void OnAddUserInterfaceSections()
    {
      UnsafeNativeMethods.Rdk_RenderTexture_AddTwoColorSection(NonConstPointer(), OnAddUiSectionsUIId);
      AddAdditionalUISections();
      base.OnAddUserInterfaceSections();
    }

    protected abstract void AddAdditionalUISections();

    protected TwoColorRenderTexture()
    {
      m_color1 = Fields.Add("color-one", Display.Color4f.Black, Rhino.UI.Localization.LocalizeString("Color 1", 22));
      m_color2 = Fields.Add("color-two", Display.Color4f.White, Rhino.UI.Localization.LocalizeString("Color 2", 23));

      m_texture1_on = Fields.Add("texture-on-one", true, Rhino.UI.Localization.LocalizeString("Texture1 On", 24));
      m_texture2_on = Fields.Add("texture-on-two", true, Rhino.UI.Localization.LocalizeString("Texture2 On", 25));

      m_texture1_amount = Fields.Add("texture-amount-one", 1.0, Rhino.UI.Localization.LocalizeString("Texture1 Amt", 26));
      m_texture2_amount = Fields.Add("texture-amount-two", 1.0, Rhino.UI.Localization.LocalizeString("Texture2 Amt", 27));

      m_swap_colors = Fields.Add("swap-colors", false, Rhino.UI.Localization.LocalizeString("Swap Colors", 28));
      m_super_sample = Fields.Add("super-sample", false, Rhino.UI.Localization.LocalizeString("Super sample", 29));
    }

    private readonly Fields.Color4fField m_color1;
    private readonly Fields.Color4fField m_color2;

    private readonly Fields.BoolField m_texture1_on;
    private readonly Fields.BoolField m_texture2_on;

    private readonly Fields.DoubleField m_texture1_amount;
    private readonly Fields.DoubleField m_texture2_amount;

    private readonly Fields.BoolField m_swap_colors;
    private readonly Fields.BoolField m_super_sample;

    public Display.Color4f Color1
    {
      get { return m_color1.Value; }
      set { m_color1.Value = value; }
    }
    public Display.Color4f Color2
    {
      get { return m_color2.Value; }
      set { m_color2.Value = value; }
    }
    public bool Texture1On
    {
      get { return m_texture1_on.Value; }
      set { m_texture1_on.Value = value; }
    }
    public bool Texture2On
    {
      get { return m_texture2_on.Value; }
      set { m_texture2_on.Value = value; }
    }
    public double Texture1Amount
    {
      get { return m_texture1_amount.Value; }
      set { m_texture1_amount.Value = value; }
    }
    public double Texture2Amount
    {
      get { return m_texture2_amount.Value; }
      set { m_texture2_amount.Value = value; }
    }
    public bool SwapColors
    {
      get { return m_swap_colors.Value; }
      set { m_swap_colors.Value = value; }
    }
    public bool SuperSample
    {
      get { return m_super_sample.Value; }
      set { m_super_sample.Value = value; }
    }
  }


  #region native wrapper
  // DO NOT make public
  internal class NativeRenderTexture : RenderTexture, INativeContent
  {
    readonly Guid m_native_instance_id = Guid.Empty;
    Guid m_document_id;
    IntPtr m_transient_pointer = IntPtr.Zero;

    public NativeRenderTexture(IntPtr pRenderContent)
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

    Guid INativeContent.Document
    {
      get { return m_document_id;}
      set { m_document_id = value;  }
    }

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
  }
  #endregion
}

#endif
