using System;
using System.Drawing;
using System.Runtime.Serialization;
using Rhino.Geometry;
using Rhino.FileIO;

namespace Rhino.DocObjects
{
  /// <summary>
  /// The TextureType controls how the pixels in the bitmap
  /// are interpreted.
  /// </summary>
  /// <since>5.6</since>
  public enum TextureType : int
  {
    /// <summary> 
    /// </summary>
    None = 0,
    /// <summary>
    /// Deprecated - this should be diffuse
    /// </summary>
    Bitmap = 1,
    /// <summary>
    /// The diffuse color of the material, ideally the albedo.
    /// </summary>
    Diffuse = 1,
    /// <summary>
    /// bump map
    /// </summary>
    Bump = 2,
    /// <summary>
    /// Deprecated - see Opacity.  This has always actually meant opacity in Rhino, so there is nothing to change.
    /// </summary>
    Transparency = 3,
    /// <summary>
    /// value = alpha
    /// </summary>
    Opacity = 3,
    /// <summary>
    /// Emap/Environment texture
    /// </summary>
    Emap = 86,
    /// <summary>
    /// Physically based materials only - base color.  Re-uses diffuse texture slot.
    /// </summary>
    PBR_BaseColor = 1,
    /// <summary>
    /// Physically based materials only - subsurface (greyscale)
    /// </summary>
    PBR_Subsurface = 10,
    /// <summary>
    /// Physically based materials only - subsurface scattering
    /// </summary>
    PBR_SubsurfaceScattering = 11,
    /// <summary>
    /// Physically based materials only - subsurface scattering radius (greyscale)
    /// </summary>
    PBR_SubsurfaceScatteringRadius = 12,
    /// <summary>
    /// Physically based materials only - metallic (greyscale)
    /// </summary>
    PBR_Metallic = 13,
    /// <summary>
    /// Physically based materials only - specular (greyscale)
    /// </summary>
    PBR_Specular = 14,
    /// <summary>
    /// Physically based materials only - specular tint (greyscale)
    /// </summary>
    PBR_SpecularTint = 15,
    /// <summary>
    /// Physically based materials only - roughness (greyscale)
    /// </summary>
    PBR_Roughness = 16,
    /// <summary>
    /// Physically based materials only - anisotropic (greyscale)
    /// </summary>
    PBR_Anisotropic = 17,
    /// <summary>
    /// Physically based materials only - anisotropic rotation 0 = 0, 255 = 360
    /// </summary>
    PBR_Anisotropic_Rotation = 18,
    /// <summary>
    /// Physically based materials only - sheen (greyscale)
    /// </summary>
    PBR_Sheen = 19,
    /// <summary>
    /// Physically based materials only - sheen tint (greyscale)
    /// </summary>
    PBR_SheenTint = 20,
    /// <summary>
    /// Physically based materials only - clearcoat (greyscale)
    /// </summary>
    PBR_Clearcoat = 21,
    /// <summary>
    /// Physically based materials only - clearcoat roughness (greyscale)
    /// </summary>
    PBR_ClearcoatRoughness = 22,
    /// <summary>
    /// Physically based materials only - ior (greyscale - remaps from 1.0 to 2.0)
    /// </summary>
    PBR_OpacityIor = 23,
    /// <summary>
    /// Physically based materials only - transparency roughness (greyscale)
    /// </summary>
    PBR_OpacityRoughness = 24,
    /// <summary>
    /// Physically based materials only - emission (greyscale)
    /// </summary>
    PBR_Emission = 25,
    /// <summary>
    /// Physically based materials only - occlusion (greyscale)
    /// </summary>
    PBR_AmbientOcclusion = 26,
    //// <summary>
    //// Physically based materials only - smudge (greyscale)
    //// </summary>
    //PBR_Smudge = 27,
    /// <summary>
    /// Physically based materials only - normal 8-bit RGB, alpha is ignored
    /// </summary>
    PBR_Displacement = 28,
    /// <summary>
    /// Physically based materials only - clearcoat normal  or bump (normal map, RGB)
    /// </summary>
    PBR_ClearcoatBump = 29,
    /// <summary>
    /// Physically based materials only - clearcoat normal  or bump (normal map, RGB)
    /// </summary>
    PBR_Alpha = 30

  }

  /// <summary>
  /// Determines how this texture is combined with others in a material's
  /// texture list.
  /// </summary>
  /// <since>5.6</since>
  public enum TextureCombineMode : int
  {
    /// <summary>
    /// </summary>
    None = 0,
    /// <summary>
    /// Modulate with material diffuse color
    /// </summary>
    Modulate = 1,
    /// <summary>
    /// Decal
    /// </summary>
    Decal = 2,
    /// <summary>
    /// Blend texture with others in the material
    ///   To "add" a texture, set BlendAmount = +1
    ///   To "subtract" a texture, set BlendAmount = -1
    /// </summary>
    Blend = 3,
  }

  /// <summary>
  /// Defines Texture UVW wrapping modes
  /// </summary>
  /// <since>5.6</since>
  public enum TextureUvwWrapping : int
  {
    /// <summary>
    /// Repeat the texture
    /// </summary>
    Repeat = 0,
    /// <summary>
    /// Clamp the texture
    /// </summary>
    Clamp = 1
  }

  /// <summary>
  /// Determines how the color of the image pixel is calculated when the image
  /// pixel corresponds to multiple texture bitmap pixels.
  /// </summary>
  /// <since>8.3</since>
  public enum TextureFilter : int
  {
    /// <summary>
    /// Nearest texture pixel is used.
    /// </summary>
    Nearest = 0,
    /// <summary>
    /// Weighted average of corresponding texture pixels.
    /// </summary>
    Linear = 1
  }

  /// <summary>
  /// Represents a texture that is mapped on objects.
  /// </summary>
  [Serializable]
  public class Texture : Runtime.CommonObject
  {
    private readonly int m_index;

    /// <summary>
    /// Initializes a new texture.
    /// </summary>
    /// <since>5.0</since>
    public Texture()
    {
      IntPtr ptr_this = UnsafeNativeMethods.ON_Texture_New();
      ConstructNonConstObject(ptr_this);
    }

    internal Texture(int index, Material parent)
    {
      m_index = index;
      m__parent = parent;
    }

#if RHINO_SDK
    private readonly bool m_front = true;

    internal Texture(int index, Display.DisplayMaterial parent, bool front)
    {
      m_index = index;
      m__parent = parent;
      m_front = front;
    }

    internal Texture(Render.SimulatedTexture parent)
    {
      m__parent = parent;
    }
#endif

    internal override IntPtr _InternalGetConstPointer()
    {
      DocObjects.Material parent_material = m__parent as DocObjects.Material;
      if (parent_material != null)
      {
        IntPtr pRhinoMaterial = parent_material.ConstPointer();
        return UnsafeNativeMethods.ON_Material_GetTexturePointer(pRhinoMaterial, m_index);
      }

#if RHINO_SDK
      Display.DisplayMaterial parent_display_material = m__parent as Display.DisplayMaterial;
      if (parent_display_material != null)
      {
        IntPtr pDisplayPipelineMaterial = parent_display_material.ConstPointer();
        IntPtr pMaterial = UnsafeNativeMethods.CDisplayPipelineMaterial_MaterialPointer(pDisplayPipelineMaterial,
                                                                                        m_front);
        return UnsafeNativeMethods.ON_Material_GetTexturePointer(pMaterial, m_index);
      }

      Rhino.Render.SimulatedTexture parent_simulated_texture = m__parent as Rhino.Render.SimulatedTexture;
      if (parent_simulated_texture != null)
      {
        IntPtr pSimulatedTexture = parent_simulated_texture.ConstPointer();
        return UnsafeNativeMethods.Rdk_SimulatedTexture_OnTexturePointer(pSimulatedTexture);
      }
#endif
      return IntPtr.Zero;
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Texture(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr pConstPointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(pConstPointer);
    }

    /// <summary>
    /// Gets or sets a file name that is used by this texture.
    /// <para>NOTE: We are moving away from string-based FileName, and suggest
    /// the usage of the new FileReference class.</para>
    /// <para>Also, this filename may well not be a path that makes sense
    /// on a user's computer because it was a path initially set on
    /// a different user's computer. If you want to get a workable path
    /// for this user, use the BitmapTable.Find function using this
    /// property.</para>
    /// </summary>
    /// <since>5.0</since>
    public string FileName
    {
      get
      {
        IntPtr pConstTexture = ConstPointer();
        if (IntPtr.Zero == pConstTexture)
          return String.Empty;
        using (var sh = new Runtime.InteropWrappers.StringHolder())
        {
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.ON_Texture_GetFileName(pConstTexture, pString);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr pTexture = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetFileName(pTexture, value);
      }
    }

    /// <summary>
    /// Gets or sets a copy of the file reference that is used by this texture.
    /// <remarks>After the get or the set method complete,
    /// you own the copy you received or passed in, and can Dispose() of it.</remarks>
    /// </summary>
    /// <since>6.0</since>
    public FileReference FileReference
    {
      get
      {
        IntPtr pConstTexture = ConstPointer();
        IntPtr pFileRef = UnsafeNativeMethods.ON_Texture_GetFileReference(pConstTexture);
        return FileReference.ConstructAndOwnFromConstPtr(pFileRef);
      }
      set
      {
        IntPtr pTexture = NonConstPointer();
        IntPtr pFileRef = value == null ? IntPtr.Zero : value.ConstPtr();

        if (!UnsafeNativeMethods.ON_Texture_SetFileReference(pTexture, pFileRef))
        {
          throw new NotSupportedException("Setting the texture failed.");
        }
      }
    }

    /// <summary>
    /// Gets the globally unique identifier of this texture.
    /// </summary>
    /// <since>5.0</since>
    public Guid Id
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_Texture_GetId(pConstThis);
      }
    }

    /// <summary>
    /// If the texture is enabled then it will be visible in the rendered
    /// display otherwise it will not.
    /// </summary>
    /// <since>5.0</since>
    public bool Enabled
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Texture_GetEnabled(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetEnabled(ptr_this, value);
      }
    }

    /// <summary>
    /// Controls how the pixels in the bitmap are interpreted
    /// </summary>
    /// <since>5.6</since>
    public TextureType TextureType
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Texture_TextureType(const_ptr_this);
        return (TextureType)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetTextureType(ptr_this, (int)value);
      }
    }

    /// <summary>
    /// The MinFilter setting controls how the color
    /// of the image pixel is calculated when the image pixel
    /// corresponds to multiple texture bitmap pixels.
    /// </summary>
    /// <since>8.3</since>
    public TextureFilter MinFilter
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Texture_MinFilter(const_ptr_this);
        return (TextureFilter)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetMinFilter(ptr_this, (int)value);
      }
    }

    /// <summary>
    /// The MagFilter setting controls how the color
    /// of the image pixel is calculated when the image pixel
    /// corresponds to a fraction of a texture bitmap pixel.
    /// </summary>
    /// <since>8.3</since>
    public TextureFilter MagFilter
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Texture_MagFilter(const_ptr_this);
        return (TextureFilter)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetMagFilter(ptr_this, (int)value);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <since>5.10</since>
    public int MappingChannelId
    {
      get { return UnsafeNativeMethods.ON_Texture_GetMappingChannelId(ConstPointer()); }
    }

    /// <summary>
    /// How texture is projected onto geometry
    /// </summary>
    public TextureProjectionModes ProjectionMode
    {
      get
      {
        return UnsafeNativeMethods.ON_Texture_GetProjectionMode(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.ON_Texture_SetProjectionMode(NonConstPointer(), value);
      }
    }

    /// <summary>
    /// Is true if this texture uses world coordinate system (WCS) projection for texture mapping.
    ///  Notice: If this texture is used by an object that has an object coordinate system (OCS) frame
    ///  defined on a mapping channel then that OCS frame is used instead of the WCS.
    /// </summary>
    public bool WcsProjected
    {
      get
      {
        return UnsafeNativeMethods.ON_Texture_IsWcsProjected(ConstPointer());
      }
    }

    /// <summary>
    /// If false, the texture color values should be correctly by the linear workflow pre-process gamma value (in the document)
    /// if linear workflow is on.  Otherwise, if the values is true, the values should be used raw from the texture.
    /// </summary>
    public bool TreatAsLinear
    {
      get
      {
        return UnsafeNativeMethods.ON_Texture_TreatAsLinear(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.ON_Texture_SetTreatAsLinear(ConstPointer(), value);
      }
    }

    /// <summary>
    /// Is true if this texture uses world coordinate system (WCS) box projection for texture mapping.
    ///  Notice: If this texture is used by an object that has an object coordinate system (OCS) frame
    ///  defined on a mapping channel then that OCS frame is used instead of the WCS.
    /// </summary>
    public bool WcsBoxProjected
    {
      get
      {
        return UnsafeNativeMethods.ON_Texture_IsWcsBoxProjected(ConstPointer());
      }
    }

    /// <summary>
    /// Determines how this texture is combined with others in a material's
    /// texture list.
    /// </summary>
    /// <since>5.6</since>
    public TextureCombineMode TextureCombineMode
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Texture_Mode(const_ptr_this);
        return (TextureCombineMode)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetMode(ptr_this, (int)value);
      }
    }

    //skipping for now
    //  FILTER m_minfilter;
    //  FILTER m_magfilter;

    const int IDX_WRAPMODE_U = 0;
    const int IDX_WRAPMODE_V = 1;
    const int IDX_WRAPMODE_W = 2;

    /// <summary>
    /// Helper function for getting the ON_Texture::WRAP mode and converting
    /// it to a TextureUvwWrapping value.
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    TextureUvwWrapping WrapUvwHelper(int mode)
    {
      IntPtr const_ptr_texture = ConstPointer();
      int value = UnsafeNativeMethods.ON_Texture_wrapuvw(const_ptr_texture, mode);
      return (value == (int)TextureUvwWrapping.Clamp ? TextureUvwWrapping.Clamp : TextureUvwWrapping.Repeat);
    }

    /// <summary>
    /// Texture wrapping mode in the U direction
    /// </summary>
    /// <since>5.6</since>
    public TextureUvwWrapping WrapU
    {
      get { return WrapUvwHelper(IDX_WRAPMODE_U); }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_Set_wrapuvw(ptr_this, IDX_WRAPMODE_U, (int)value);
      }
    }

    /// <summary>
    /// Texture wrapping mode in the V direction
    /// </summary>
    /// <since>5.6</since>
    public TextureUvwWrapping WrapV
    {
      get { return WrapUvwHelper(IDX_WRAPMODE_V); }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_Set_wrapuvw(ptr_this, IDX_WRAPMODE_V, (int)value);
      }
    }

    /// <summary>
    /// Texture wrapping mode in the W direction
    /// </summary>
    /// <since>5.6</since>
    public TextureUvwWrapping WrapW
    {
      get { return WrapUvwHelper(IDX_WRAPMODE_W); }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_Set_wrapuvw(ptr_this, IDX_WRAPMODE_W, (int)value);
      }
    }

    /// <summary>
    /// If true then the UVW transform is applied to the texture
    /// otherwise the UVW transform is ignored.
    /// </summary>
    /// <since>5.6</since>
    public bool ApplyUvwTransform
    {
      get
      {
        // OBSOLETE - always true
        bool value = true;
        return value;
      }
      set
      {
        // OBSOLETE - always true
      }
    }

    /// <summary>
    /// Transform to be applied to each instance of this texture
    /// if ApplyUvw is true
    /// </summary>
    /// <since>5.6</since>
    public Transform UvwTransform
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        Transform value = new Transform(1);
        UnsafeNativeMethods.ON_Texture_uvw(const_ptr_this, ref value);
        return value;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_Setuvw(ptr_this, ref value);
      }
    }

    /// <summary>
    /// Helper for access to the repeat value encoded in UvwTransform
    /// </summary>
    /// <since>8.0</since>
    public Vector2d Repeat
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        Vector2d value = new Vector2d();
        UnsafeNativeMethods.ON_Texture_Repeat(const_ptr_this, ref value);
        return value;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetRepeat(ptr_this, ref value);
      }
    }

    /// <summary>
    /// Helper for access to the offset value encoded in UvwTransform
    /// </summary>
    /// <since>8.0</since>
    public Vector2d Offset
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        Vector2d value = new Vector2d();
        UnsafeNativeMethods.ON_Texture_Offset(const_ptr_this, ref value);
        return value;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetOffset(ptr_this, ref value);
      }
    }

    /// <summary>
    /// Helper for access to the rotation value encoded in UvwTransform
    /// </summary>
    /// <since>8.0</since>
    public double Rotation
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Texture_Rotation(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Texture_SetRotation(ptr_this, value);
      }
    }

    // skipping for now
    //  ON_Color m_border_color;
    //  ON_Color m_transparent_color;
    //  ON_UUID m_transparency_texture_id;
    //  ON_Interval m_bump_scale;

    /// <summary>
    /// If the TextureCombineMode is Blend, then the blending function
    /// for alpha is determined by
    /// <para>
    /// new alpha = constant
    ///             + a0*(current alpha)
    ///             + a1*(texture alpha)
    ///             + a2*min(current alpha,texture alpha)
    ///             + a3*max(current alpha,texture alpha)
    /// </para>
    /// </summary>
    /// <param name="constant"></param>
    /// <param name="a0"></param>
    /// <param name="a1"></param>
    /// <param name="a2"></param>
    /// <param name="a3"></param>
    /// <since>5.6</since>
    public void GetAlphaBlendValues(out double constant, out double a0, out double a1, out double a2, out double a3)
    {
      constant = 0;
      a0 = 0;
      a1 = 0;
      a2 = 0;
      a3 = 0;
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_Texture_GetAlphaBlendValues(const_ptr_this, ref constant, ref a0, ref a1, ref a2, ref a3);
    }

    /// <summary>
    /// If the TextureCombineMode is Blend, then the blending function
    /// for alpha is determined by
    /// <para>
    /// new alpha = constant
    ///             + a0*(current alpha)
    ///             + a1*(texture alpha)
    ///             + a2*min(current alpha,texture alpha)
    ///             + a3*max(current alpha,texture alpha)
    /// </para>
    /// </summary>
    /// <param name="constant"></param>
    /// <param name="a0"></param>
    /// <param name="a1"></param>
    /// <param name="a2"></param>
    /// <param name="a3"></param>
    /// <since>5.6</since>
    public void SetAlphaBlendValues(double constant, double a0, double a1, double a2, double a3)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Texture_SetAlphaBlendValues(ptr_this, constant, a0, a1, a2, a3);
    }

    /// <summary>
    /// If the TextureCombineMode is Blend, then the blending function
    /// for RGB is determined by
    /// <para>
    /// new rgb = colorcolor
    ///         + a0[0]*(current RGB)
    ///         + a1[1]*(texture RGB)
    ///         + a2[2]*min(current RGB,texture RGB)
    ///         + a3[3]*max(current RGB,texture RGB)
    ///</para>
    /// </summary>
    /// <param name="color"></param>
    /// <param name="a0"></param>
    /// <param name="a1"></param>
    /// <param name="a2"></param>
    /// <param name="a3"></param>
    /// <since>6.0</since>
    public void SetRGBBlendValues(Color color, double a0, double a1, double a2, double a3)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Texture_SetRGBBlendValues(ptr_this, (uint)color.ToArgb(), a0, a1, a2, a3);
    }


    // skipping for now
    //  int m_blend_order;
  }
}
