#pragma warning disable 1591
#if RHINO_SDK
using System;
using Rhino.Runtime.InteropWrappers;

using SmellsLike = UnsafeNativeMethods.RhRdkSmellsLike;

namespace Rhino.Render
{
  /// <summary>
  /// Generic class to help holding on to related values. This can be
  /// used to get data from textured content fields with the
  /// <c>HandleTexturedValue</c> function.
  /// </summary>
  /// <typeparam name="T">Color4f or float</typeparam>
  /// <since>6.12</since>
  public class TexturedValue<T>
  {
    public TexturedValue(string name, T value, bool on, float amount)
    {
      Name = name;
      Value = value;
      On = on;
      Amount = amount;
      Texture = null;
    }

    public readonly string Name;
    public T Value;
    public bool On;
    public float Amount;
    public RenderTexture Texture;
  }

  /// <summary>
  /// Color4f specialization of TexturedValue.
  /// </summary>
  /// <since>6.12</since>
  public class TexturedColor : TexturedValue<Display.Color4f>
  {
    public TexturedColor(string name, Display.Color4f value, bool on, float amount) : base(name, value, on, amount) { }
  }
  /// <summary>
  /// float specialization of TexturedValue.
  /// </summary>
  /// <since>6.12</since>
  public class TexturedFloat : TexturedValue<float>
  {
    public TexturedFloat(string name, float value, bool on, float amount) : base(name, value, on, amount) { }
  }

  public abstract class RenderMaterial : RenderContent
  {
    /// <summary>
    /// Constructs a new basic material from a <see cref="Rhino.DocObjects.Material">Material</see>.
    /// </summary>
    /// <param name="material">(optional)The material to create the basic material from.</param>
    /// <returns>A new basic material.</returns>
    public static RenderMaterial CreateBasicMaterial(DocObjects.Material material)
    {
      return CreateBasicMaterial(material, null);
    }

    public static RenderMaterial CreateBasicMaterial(DocObjects.Material material, RhinoDoc doc)
    {
      var const_ptr_source_material = (material == null ? IntPtr.Zero : material.ConstPointer());

      var ptr_new_material = UnsafeNativeMethods.Rdk_Globals_NewBasicMaterial(const_ptr_source_material, (doc == null) ? 0 : doc.RuntimeSerialNumber);

      var new_material = FromPointer(ptr_new_material) as NativeRenderMaterial;

      if (new_material != null)
      {
        new_material.AutoDelete = true;
      }

      return new_material;
    }

    public static Guid PlasterMaterialGuid => UnsafeNativeMethods.Rdk_RenderMaterial_Plaster();
    public static Guid PlasticMaterialGuid => UnsafeNativeMethods.Rdk_RenderMaterial_Plastic();
    public static Guid PaintMaterialGuid => UnsafeNativeMethods.Rdk_RenderMaterial_Paint();
    public static Guid GlassMaterialGuid => UnsafeNativeMethods.Rdk_RenderMaterial_Glass();
    public static Guid GemMaterialGuid => UnsafeNativeMethods.Rdk_RenderMaterial_Plaster();
    public static Guid MetalMaterialGuid => UnsafeNativeMethods.Rdk_RenderMaterial_Metal();
    public static Guid PictureMaterialGuid => UnsafeNativeMethods.Rdk_RenderMaterial_Picture();

    /// <summary>
    /// Defines enumerated constant values for use in <see cref="TextureChildSlotName"/> method.
    /// </summary>
    public enum StandardChildSlots : int
    {
      /// <summary>
      /// Corresponds to ON_Texture::TYPE::bitmap_texture.
      /// </summary>
      Diffuse = UnsafeNativeMethods.RenderContent_StringIds.DiffuseChildSlotName,
      /// <summary>
      /// Corresponds to ON_Texture::transparancy_texture.
      /// </summary>
      Transparency = UnsafeNativeMethods.RenderContent_StringIds.TransparencyChildSlotName,
      /// <summary>
      /// Corresponds to ON_Texture::TYPE::bump_texture.
      /// </summary>
      Bump = UnsafeNativeMethods.RenderContent_StringIds.BumpChildSlotName,
      /// <summary>
      /// Corresponds to ON_Texture::TYPE::emap_texture.
      /// </summary>
      Environment = UnsafeNativeMethods.RenderContent_StringIds.EnvironmentChildSlotName,
    }

    /// <summary>
    /// Parameter names for use in GetNamedParameter and SetNamedParameter with basic materials.
    /// </summary>
    public class BasicMaterialParameterNames
    {
      public const string Ambient = "ambient";
      public const string Emission = "emission";
      public const string FlamingoLibrary = "flamingo-library";
      public const string DisableLighting = "disable-lighting";
      public const string Diffuse = "diffuse";
      public const string Specular = "specular";
      public const string TransparencyColor = "transparency-color";
      public const string ReflectivityColor = "reflectivity-color";
      public const string Shine = "shine";
      public const string Transparency = "transparency";
      public const string Reflectivity = "reflectivity";
      public const string Ior = "ior";
    }

    /// <summary>
    /// Override this function to provide information about which texture is used for
    /// the standard (ie - defined in ON_Texture) texture channels.
    /// </summary>
    /// <param name="slot">An valid slot.</param>
    /// <returns>The texture used for the channel.</returns>
    public virtual string TextureChildSlotName(StandardChildSlots slot)
    {
      if (IsNativeWrapper())
      {
        var string_id = UnsafeNativeMethods.RenderContent_StringIds.DiffuseChildSlotName;
        switch (slot)
        {
          case StandardChildSlots.Diffuse:
            string_id = UnsafeNativeMethods.RenderContent_StringIds.DiffuseChildSlotName;
            break;
          case StandardChildSlots.Transparency:
            string_id = UnsafeNativeMethods.RenderContent_StringIds.TransparencyChildSlotName;
            break;
          case StandardChildSlots.Bump:
            string_id = UnsafeNativeMethods.RenderContent_StringIds.BumpChildSlotName;
            break;
          case StandardChildSlots.Environment:
            string_id = UnsafeNativeMethods.RenderContent_StringIds.EnvironmentChildSlotName;
            break;
        }
        return GetString(string_id);
      }

      using (var sh = new StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.Rdk_RenderMaterial_CallTextureChildSlotNameBase(ConstPointer(), p_string, (int)slot);
        return sh.ToString();
      }
    }

    public RenderTexture GetTextureFromUsage(RenderMaterial.StandardChildSlots which)
    {
      var p_const_this = ConstPointer();

      var usage = RenderContentStringIdFromStandardChildSlot(which);
      var p_child = UnsafeNativeMethods.Rdk_RenderContent_GetTextureForUsage(p_const_this, usage);
      return FromPointer(p_child) as RenderTexture;
    }

    private static UnsafeNativeMethods.RenderContent_StringIds RenderContentStringIdFromStandardChildSlot(StandardChildSlots which)
    {
      UnsafeNativeMethods.RenderContent_StringIds usage = UnsafeNativeMethods.RenderContent_StringIds.DiffuseChildSlotName;
      switch (which)
      {
        case StandardChildSlots.Bump:
          usage = UnsafeNativeMethods.RenderContent_StringIds.BumpChildSlotName;
          break;
        case StandardChildSlots.Diffuse:
          usage = UnsafeNativeMethods.RenderContent_StringIds.DiffuseChildSlotName;
          break;
        case StandardChildSlots.Transparency:
          usage = UnsafeNativeMethods.RenderContent_StringIds.TransparencyChildSlotName;
          break;
        case StandardChildSlots.Environment:
          usage = UnsafeNativeMethods.RenderContent_StringIds.EnvironmentChildSlotName;
          break;
      }
      return usage;
    }

    public bool GetTextureOnFromUsage(RenderMaterial.StandardChildSlots which)
    {
      var p_const_this = ConstPointer();
      var usage = RenderContentStringIdFromStandardChildSlot(which);
      var rc = UnsafeNativeMethods.Rdk_RenderContent_GetTextureOnForUsage(p_const_this, usage);
      return rc;
    }

    public double GetTextureAmountFromUsage(RenderMaterial.StandardChildSlots which)
    {
      var p_const_this = ConstPointer();
      var usage = RenderContentStringIdFromStandardChildSlot(which);
      var rc = UnsafeNativeMethods.Rdk_RenderContent_GetTextureAmountForUsage(p_const_this, usage);
      return rc;
    }

    /// <summary>
    /// Override this function to provide a Rhino.DocObjects.Material definition for this material
    /// to be used by other rendering engines including the display.
    /// </summary>
    /// <param name="simulation">Set the properties of the input basic material to provide the simulation for this material.</param>
    /// <param name="isForDataOnly">Called when only asking for a hash - don't write any textures to the disk - just provide the filenames they will get.</param>
    public virtual void SimulateMaterial(ref DocObjects.Material simulation, bool isForDataOnly)
    {
      var gen = isForDataOnly ? UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow : UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow;
      if (IsNativeWrapper())
      {
        UnsafeNativeMethods.Rdk_RenderMaterial_SimulateMaterial(NonConstPointer(), simulation.ConstPointer(), gen);
      }
      else
      {
        UnsafeNativeMethods.Rdk_RenderMaterial_CallSimulateMaterialBase(NonConstPointer(), simulation.ConstPointer(), gen);
      }
    }

    /// <summary>
    /// Override this function to provide a Rhino.DocObjects.Material definition for this material
    /// to be used by other rendering engines including the display.
    /// </summary>
    /// <param name="isForDataOnly">Called when only asking for a hash - don't write any textures to the disk - just provide the filenames they will get.</param>
    /// <returns>The simulation of the render material</returns>
    public virtual DocObjects.Material SimulateMaterial(bool isForDataOnly)
    {
      var gen = isForDataOnly ? UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow : UnsafeNativeMethods.CRhRdkTextureGenConsts.Allow;
      var simulation = new DocObjects.Material();

      SimulateMaterial(ref simulation, isForDataOnly);

      return simulation;
    }

    #region callbacks from c++

    internal static NewRenderContentCallbackEvent m_NewMaterialCallback = OnNewMaterial;
    static IntPtr OnNewMaterial(Guid typeId)
    {
      var render_content = NewRenderContent(typeId, typeof(RenderMaterial));
      return (null == render_content ? IntPtr.Zero : render_content.NonConstPointer());
    }

    internal delegate void TextureChildSlotNameCallback(int serialNumber, int which, IntPtr pOnWString);
    internal static TextureChildSlotNameCallback m_TextureChildSlotName = OnTextureChildSlotName;
    static void OnTextureChildSlotName(int serialNumber, int which, IntPtr pOnWString)
    {
      try
      {
        var material = FromSerialNumber(serialNumber) as RenderMaterial;
        if (material != null)
        {
          var str = material.TextureChildSlotName((StandardChildSlots)which);
          if (!String.IsNullOrEmpty(str))
          {
            UnsafeNativeMethods.ON_wString_Set(pOnWString, str);
          }
        }
      }
      catch (Exception exception)
      {
        Runtime.HostUtils.ExceptionReport(exception);
      }
    }

    internal delegate void SimulateMaterialCallback(int serialNumber, IntPtr p, UnsafeNativeMethods.CRhRdkTextureGenConsts textureGen);
    internal static SimulateMaterialCallback m_SimulateMaterial = OnSimulateMaterial;
    static void OnSimulateMaterial(int serialNumber, IntPtr pSim, UnsafeNativeMethods.CRhRdkTextureGenConsts textureGen)
    {
      try
      {
        var content = FromSerialNumber(serialNumber) as RenderMaterial;
        if (content != null && pSim != IntPtr.Zero)
        {
          var temp_material = DocObjects.Material.NewTemporaryMaterial(pSim, null);
          if (temp_material != null)
          {
            var data_only = (textureGen == UnsafeNativeMethods.CRhRdkTextureGenConsts.Disallow);
            content.SimulateMaterial(ref temp_material, data_only);
            temp_material.ReleaseNonConstPointer();
          }
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }
    #endregion


    #region Public properties

    /// <summary>
    /// Geometry that appears in preview panes
    /// </summary>
    public enum PreviewGeometryType
    {
      Cone = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Cone,
      Cube = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Cuboid,
      //Mesh = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Mesh,
      Plane = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Plane,
      Pyramid = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Pyramid,
      Sphere = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Sphere,
      Torus = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Torus,
      //SelectedObjects = UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.SelectedObjects
    }

    /// <summary>
    /// Set or get the default geometry that appears in preview panes
    /// </summary>
    public PreviewGeometryType DefaultPreviewGeometryType
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_GetDefaultPreviewGeometry(pointer);
        switch (value)
        {
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Cone:
            return PreviewGeometryType.Cone;
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Cuboid:
            return PreviewGeometryType.Cube;
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Pyramid:
            return PreviewGeometryType.Pyramid;
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Plane:
            return PreviewGeometryType.Plane;
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Torus:
            return PreviewGeometryType.Torus;
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Mesh:
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.Sphere:
          case UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry.SelectedObjects:
            return PreviewGeometryType.Sphere;
        }
        throw new Exception("Unknown RhRdkPreviewSceneServerGeometry value");
      }
      set
      {
        switch (value)
        {
          case PreviewGeometryType.Cone:
          case PreviewGeometryType.Cube:
          case PreviewGeometryType.Pyramid:
          case PreviewGeometryType.Plane:
          case PreviewGeometryType.Sphere:
          case PreviewGeometryType.Torus:
            break;
          default:
            throw new Exception("Unhandled PreviewGeometryType");
        }
        var pointer = NonConstPointer();
        UnsafeNativeMethods.Rdk_RenderMaterial_SetDefaultPreviewGeometry(pointer, (UnsafeNativeMethods.RhRdkPreviewSceneServerGeometry)value);
      }
    }

    /// <summary>
    /// The default scene background for the image that appears in
    /// preview panes
    /// </summary>
    public enum PreviewBackgroundType
    {
      None = UnsafeNativeMethods.RhCmnMaterialPreviewBackground.None,
      Checkered = UnsafeNativeMethods.RhCmnMaterialPreviewBackground.Checkered,
      //Unused = UnsafeNativeMethods.RhCmnMaterialPreviewBackground.Unused,
      //Custom = UnsafeNativeMethods.RhCmnMaterialPreviewBackground.Custom,
    };

    /// <summary>
    /// Set or get the default scene background for the image that appears in
    /// preview panes
    /// </summary>
    public PreviewBackgroundType DefaultPreviewBackgroundType
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_GetDefaultPreviewBackground(pointer);
        switch (value)
        {
          case UnsafeNativeMethods.RhCmnMaterialPreviewBackground.Checkered:
            return PreviewBackgroundType.Checkered;
          case UnsafeNativeMethods.RhCmnMaterialPreviewBackground.None:
          case UnsafeNativeMethods.RhCmnMaterialPreviewBackground.Unused:
          case UnsafeNativeMethods.RhCmnMaterialPreviewBackground.Custom:
            return PreviewBackgroundType.None;
        }
        throw new Exception("Unknown RhCmnMaterialPreviewBackground value");
      }
      set
      {
        switch (value)
        {
          case PreviewBackgroundType.Checkered:
          case PreviewBackgroundType.None:
            break;
          default:
            throw new Exception("Unhandled PreviewBackgroundType");
        }
        var pointer = NonConstPointer();
        UnsafeNativeMethods.Rdk_RenderMaterial_SetDefaultPreviewBackground(pointer, (UnsafeNativeMethods.RhCmnMaterialPreviewBackground)value);
      }
    }

    /// <summary>
    /// The default preview geometry size
    /// </summary>
    public double DefaultPreviewSize
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_GetDefaultPreviewSize(pointer);
        return value;
      }
      set
      {
        if (value < 0.0) throw new Exception("DefaultPreviewSize must be greater than 0.0");
        var pointer = NonConstPointer();
        UnsafeNativeMethods.Rdk_RenderMaterial_SetDefaultPreviewSize(pointer, value);
      }
    }

    public bool SmellsLikePlaster
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLike(pointer, (int)SmellsLike.plaster);
        return value;
      }
    }

    public bool SmellsLikePaint
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLike(pointer, (int)SmellsLike.paint);
        return value;
      }
    }

    public bool SmellsLikeMetal
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLike(pointer, (int)SmellsLike.metal);
        return value;
      }
    }

    public bool SmellsLikePlastic
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLike(pointer, (int)SmellsLike.plastic);
        return value;
      }
    }

    public bool SmellsLikeGem
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLike(pointer, (int)SmellsLike.gem);
        return value;
      }
    }

    public bool SmellsLikeGlass
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLike(pointer, (int)SmellsLike.glass);
        return value;
      }
    }
    public bool SmellsLikeTexturedPlaster
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLikeTextured(pointer, (int)SmellsLike.plaster);
        return value;
      }
    }

    public bool SmellsLikeTexturedPaint
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLikeTextured(pointer, (int)SmellsLike.paint);
        return value;
      }
    }

    public bool SmellsLikeTexturedMetal
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLikeTextured(pointer, (int)SmellsLike.metal);
        return value;
      }
    }

    public bool SmellsLikeTexturedPlastic
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLikeTextured(pointer, (int)SmellsLike.plastic);
        return value;
      }
    }

    public bool SmellsLikeTexturedGem
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLikeTextured(pointer, (int)SmellsLike.gem);
        return value;
      }
    }

    public bool SmellsLikeTexturedGlass
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.Rdk_RenderMaterial_SmellsLikeTextured(pointer, (int)SmellsLike.glass);
        return value;
      }
    }

    #endregion Public properties

    #region PBR support

    /// <summary>
    /// Handle a textured content field. Values will be read into an
    /// instance of TexturedColor
    /// </summary>
    /// <param name="slotname"></param>
    /// <param name="tc"></param>
    /// <since>6.12</since>
    /// <returns>true if reading the base value succeeded</returns>
    public bool HandleTexturedValue<T>(string slotname, TexturedValue<T> tc)
    {
      bool success = false;

      if (Fields.TryGetValue(slotname, out T c))
      {
        tc.Value = c;
        success = true;
      }
      if (success)
      {
        var texAmountConv = GetChildSlotParameter(slotname, "texture-amount") as IConvertible;
        if (texAmountConv != null)
        {
          float texamount = Convert.ToSingle(texAmountConv);
          tc.Amount = texamount / 100.0f;
        }
        var texOnnessConv = GetChildSlotParameter(slotname, "texture-on") as IConvertible;
        if (texOnnessConv != null)
        {
          bool texon = Convert.ToBoolean(texOnnessConv);
          tc.On = texon;
        }
        if (FindChild(slotname) is RenderTexture rt)
        {
          tc.Texture = rt;
        }
      }
      return success;
    }
    #endregion
  }

  #region Native wrapper
  // DO NOT make public
  internal class NativeRenderMaterial : RenderMaterial, INativeContent
  {
    readonly Guid m_native_instance_id = Guid.Empty;
    Guid m_document_id;
    IntPtr m_transient_pointer = IntPtr.Zero;
    public NativeRenderMaterial(IntPtr pRenderContent)
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

  #region PBR
  /// <summary>
  /// Placeholder class for future PBR implementation
  /// </summary>
  /// <since>6.12</since>
  public class PhysicallyBasedMaterial
  {
    /// <summary>
    /// Helper class with fields containing the names of fields available in our PBR implementation.
    /// </summary>
    public class ParametersNames
    {
      public static string BaseColor { get { return "pbr-base-color"; } }
      public static string BRDF { get { return "pbr-brdf"; } }
      public static string Subsurface { get { return "pbr-subsurface"; } }
      public static string SubsurfaceScatteringColor { get { return "pbr-subsurface-scattering-color"; } }
      public static string SubsurfaceScatteringRadius { get { return "pbr-subsurface-scattering-radius"; } }
      public static string Specular { get { return "pbr-specular"; } }
      public static string SpecularTint { get { return "pbr-specular-tint"; } }
      public static string Metallic { get { return "pbr-metallic"; } }
      public static string Roughness { get { return "pbr-roughness"; } }
      public static string Anisotropic { get { return "pbr-anisotropic"; } }
      public static string AnisotropicRotation { get { return "pbr-anisotropic-rotation"; } }
      public static string Sheen { get { return "pbr-sheen"; } }
      public static string SheenTint { get { return "pbr-sheen-tint"; } }
      public static string Clearcoat { get { return "pbr-clearcoat"; } }
      public static string ClearcoatRoughness { get { return "pbr-clearcoat-roughness"; } }
      public static string OpacityIor { get { return "pbr-opacity-ior"; } }
      public static string Opacity { get { return "pbr-opacity"; } }
      public static string OpacityRoughness { get { return "pbr-opacity-roughness"; } }
      public static string Emission { get { return "pbr-emission"; } }
      public static string AmbientOcclusion { get { return "pbr-ambient-occlusion"; } }
      public static string Smudge { get { return "pbr-smudge"; } }
      public static string Displacement { get { return "pbr-displacement"; } }
      public static string Normal { get { return "pbr-normal"; } }
      public static string Bump { get { return "pbr-bump"; } }
    }
  }
  #endregion
}
#endif
