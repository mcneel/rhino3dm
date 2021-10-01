#pragma warning disable 1591

// moved from rdk_material.cs so we can export this to rhino3dm
namespace Rhino.DocObjects
{
  public class PhysicallyBasedMaterial
  {
    /// <since>6.12</since>
    internal PhysicallyBasedMaterial()
    {
      m_material = new DocObjects.Material();
    }

    internal PhysicallyBasedMaterial(DocObjects.Material m)
    {
      m_material = m;
    }

    internal DocObjects.Material m_material;

    /// <since>7.0</since>
    public DocObjects.Material Material
    {
      get { return m_material; }
    }

    /// <summary>
    /// Set the texture that corresponds with the specified texture type for this material.
    /// </summary>
    /// <param name="texture">An instance of Rhino.DocObjects.Texture</param>
    /// <param name="which">Use Rhino.DocObjects.TextureType</param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool SetTexture(DocObjects.Texture texture, DocObjects.TextureType which)
    {
      return Material.SetTexture(texture, which);
    }

    /// <summary>
    /// Get the texture that corresponds with the specified texture type for this material.
    /// </summary>
    /// <param name="which"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public DocObjects.Texture GetTexture(DocObjects.TextureType which)
    {
      return Material.GetTexture(which);
    }

    /// <summary>
    /// Get array of textures that this material uses
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public DocObjects.Texture[] GetTextures()
    {
      return Material.GetTextures();
    }

    /// <since>7.0</since>
    public void SynchronizeLegacyMaterial()
    {
      UnsafeNativeMethods.ON_Material_PBR_SynchronizeLegacyMaterial(m_material.NonConstPointer());
    }

    /// <since>7.0</since>
    public Rhino.Display.Color4f BaseColor
    {
      get
      {
        var color = new Rhino.Display.Color4f();
        UnsafeNativeMethods.ON_Material_PBR_BaseColor(m_material.ConstPointer(), ref color);
        return color;
      }
      set
      {
        UnsafeNativeMethods.ON_Material_PBR_SetBaseColor(m_material.NonConstPointer(), value);
      }
    }

    /// <since>7.0</since>
    public enum BRDFs : int
    {
      GGX = 0,
      Ward = 1,
    }

    /// <since>7.0</since>
    public BRDFs BRDF
    {
      get { return (BRDFs)UnsafeNativeMethods.ON_Material_PBR_BRDF(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetBRDF(m_material.NonConstPointer(), (int)value); }
    }

    /// <since>7.0</since>
    public Rhino.Display.Color4f SubsurfaceScatteringColor
    {
      get
      {
        var color = new Rhino.Display.Color4f();
        UnsafeNativeMethods.ON_Material_PBR_SubsurfaceScatteringColor(m_material.ConstPointer(), ref color);
        return color;
      }
      set
      {
        UnsafeNativeMethods.ON_Material_PBR_SetSubsurfaceScatteringColor(m_material.NonConstPointer(), value);
      }
    }

    /// <since>7.0</since>
    public double Subsurface
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Subsurface(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetSubsurface(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double SubsurfaceScatteringRadius
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_SubsurfaceScatteringRadius(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetSubsurfaceScatteringRadius(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double Metallic
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Metallic(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetMetallic(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double Specular
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Specular(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetSpecular(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double ReflectiveIOR
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_ReflectiveIOR(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetReflectiveIOR(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double SpecularTint
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_SpecularTint(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetSpecularTint(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double Roughness
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Roughness(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetRoughness(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double Anisotropic
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Anisotropic(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetAnisotropic(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double AnisotropicRotation
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_AnisotropicRotation(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetAnisotropicRotation(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double Sheen
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Sheen(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetSheen(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double SheenTint
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_SheenTint(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetSheenTint(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double Clearcoat
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Clearcoat(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetClearcoat(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double ClearcoatRoughness
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_ClearcoatRoughness(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetClearcoatRoughness(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double OpacityIOR
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_OpacityIOR(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetOpacityIOR(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double Opacity
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Opacity(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetOpacity(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public double OpacityRoughness
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_OpacityRoughness(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetOpacityRoughness(m_material.NonConstPointer(), value); }
    }

    /// <since>7.1</since>
    public double Alpha
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_Alpha(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetAlpha(m_material.NonConstPointer(), value); }
    }

    /// <since>7.1</since>
    public bool UseBaseColorTextureAlphaForObjectAlphaTransparencyTexture
    {
      get { return UnsafeNativeMethods.ON_Material_PBR_BaseColorTextureAlphaForObjectAlphaTransparencyTexture(m_material.ConstPointer()); }
      set { UnsafeNativeMethods.ON_Material_PBR_SetBaseColorTextureAlphaForObjectAlphaTransparencyTexture(m_material.NonConstPointer(), value); }
    }

    /// <since>7.0</since>
    public Rhino.Display.Color4f Emission
    {
      get
      {
        var color = new Rhino.Display.Color4f();
        UnsafeNativeMethods.ON_Material_PBR_Emission(m_material.ConstPointer(), ref color);
        return color;
      }
      set
      {
        UnsafeNativeMethods.ON_Material_PBR_SetEmission(m_material.NonConstPointer(), value);
      }
    }
  }
}

