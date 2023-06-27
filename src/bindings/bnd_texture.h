#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initTextureBindings(pybind11::module& m);
#else
void initTextureBindings(void* m);
#endif

enum class TextureType : unsigned int
{
  no_texture_type = 0,

  bitmap_texture       = 1, // "standard" image texture.  // Deprecated.  Use Diffuse.
  diffuse_texture      = 1, // ideally albedo.
  bump_texture         = 2, // bump map - see m_bump_scale comment
  transparency_texture = 3, // value = alpha (see m_tranparancy_id)  Deprecated.  Use Opacity.  No change needed to functionality - transparency in Rhino has always meant opacity.
  opacity_texture      = 3, // value = alpha.

  // The following textures are only for PBR materials
  // They are not supported by the basic ON_Material definition, and should only be used when
  // rendering physically based (PBR) materials.
  pbr_base_color_texture            = 1,   //Reuse diffuse texture.
  pbr_subsurface_texture            = 10,
  pbr_subsurface_scattering_texture = 11,
  pbr_subsurface_scattering_radius_texture  = 12,
  pbr_metallic_texture              = 13,
  pbr_specular_texture              = 14,
  pbr_specular_tint_texture         = 15,
  pbr_roughness_texture             = 16,
  pbr_anisotropic_texture           = 17,
  pbr_anisotropic_rotation_texture  = 18,
  pbr_sheen_texture                 = 19,
  pbr_sheen_tint_texture            = 20,
  pbr_clearcoat_texture             = 21,
  pbr_clearcoat_roughness_texture   = 22,
  pbr_opacity_ior_texture           = 23,
  pbr_opacity_roughness_texture     = 24,
  pbr_emission_texture              = 25,
  pbr_ambient_occlusion_texture     = 26,
  //pbr_smudge_texture                 = 27U,
  pbr_displacement_texture          = 28,
  pbr_clearcoat_bump_texture        = 29,
  pbr_alpha_texture                 = 30,
  pbr_opacity_texture               = 3,
  pbr_bump_texture                  = 2,

  // emap_texture is OBSOLETE - set m_mapping_channel_id = ON_MappingChannel::emap_mapping
  emap_texture = 86 // spherical environment mapping.
};

class BND_Texture : public BND_CommonObject
{
public:
  ON_Texture* m_texture = nullptr;
public:
  BND_Texture();
  BND_Texture(ON_Texture* texture, const ON_ModelComponentReference* compref);

  std::wstring GetFileName() const { return std::wstring(m_texture->m_image_file_reference.FullPathAsPointer()); }
  void SetFileName(std::wstring path) { m_texture->m_image_file_reference.SetFullPath(path.c_str(), true); }
  class BND_FileReference* GetFileReference() const;
  //public Guid Id
  BND_UUID Id() const { return ON_UUID_to_Binding(m_texture->m_texture_id); }
  //public bool Enabled
  bool Enabled() const { return m_texture->m_bOn; }
  void SetEnabled(bool enabled) { m_texture->m_bOn = enabled; }
  //public TextureType TextureType
  ON_Texture::TYPE TextureType() const;
  void SetTextureType(int textureType) { m_texture->m_type = ON_Texture::TypeFromUnsigned(textureType);  }
  //public int MappingChannelId
  
  //public TextureCombineMode TextureCombineMode
  int WrapU() const { return (unsigned int) m_texture->m_wrapu; }
  int WrapV() const { return (unsigned int) m_texture->m_wrapv; }
  int WrapW() const { return (unsigned int) m_texture->m_wrapw; }
  BND_Transform UvwTransform() const { return m_texture->m_uvw; }
  //public void GetAlphaBlendValues(out double constant, out double a0, out double a1, out double a2, out double a3)
  //public void SetAlphaBlendValues(double constant, double a0, double a1, double a2, double a3)
  //public void SetRGBBlendValues(Color color, double a0, double a1, double a2, double a3)

protected:
  void SetTrackedPointer(ON_Texture* texture, const ON_ModelComponentReference* compref);
};