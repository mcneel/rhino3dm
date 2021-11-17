#include "bindings.h"

BND_Texture::BND_Texture()
{
  SetTrackedPointer(new ON_Texture(), nullptr);
}

BND_Texture::BND_Texture(ON_Texture* texture, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(texture, compref);
}

void BND_Texture::SetTrackedPointer(ON_Texture* texture, const ON_ModelComponentReference* compref)
{
  m_texture = texture;
  BND_CommonObject::SetTrackedPointer(texture, compref);
}

BND_FileReference* BND_Texture::GetFileReference() const
{
  return new BND_FileReference(m_texture->m_image_file_reference);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initTextureBindings(pybind11::module& m)
{
  py::enum_<ON_Texture::TYPE>(m, "TextureType")
    .value("None", ON_Texture::TYPE::no_texture_type)
    .value("Bitmap", ON_Texture::TYPE::bitmap_texture)
    .value("Diffuse", ON_Texture::TYPE::diffuse_texture)
    .value("Bump", ON_Texture::TYPE::bump_texture)
    .value("Transparency", ON_Texture::TYPE::transparency_texture)
    .value("Opacity", ON_Texture::TYPE::opacity_texture)
    .value("Emap", ON_Texture::TYPE::emap_texture)
    .value("PBR_BaseColor", ON_Texture::TYPE::pbr_base_color_texture)
    .value("PBR_Subsurface", ON_Texture::TYPE::pbr_subsurface_texture)
    .value("PBR_SubsurfaceScattering", ON_Texture::TYPE::pbr_subsurface_scattering_texture)
    .value("PBR_SubsurfaceScatteringRadius", ON_Texture::TYPE::pbr_subsurface_scattering_radius_texture)
    .value("PBR_Metallic", ON_Texture::TYPE::pbr_metallic_texture)
    .value("PBR_Specular", ON_Texture::TYPE::pbr_specular_texture)
    .value("PBR_SpecularTint", ON_Texture::TYPE::pbr_specular_tint_texture)
    .value("PBR_Roughness", ON_Texture::TYPE::pbr_roughness_texture)
    .value("PBR_Anisotropic", ON_Texture::TYPE::pbr_anisotropic_texture)
    .value("PBR_Anisotropic_Rotation", ON_Texture::TYPE::pbr_anisotropic_rotation_texture)
    .value("PBR_Sheen", ON_Texture::TYPE::pbr_sheen_texture)
    .value("PBR_SheenTint", ON_Texture::TYPE::pbr_sheen_tint_texture)
    .value("PBR_Clearcoat", ON_Texture::TYPE::pbr_clearcoat_texture)
    .value("PBR_ClearcoatRoughness", ON_Texture::TYPE::pbr_clearcoat_roughness_texture)
    .value("PBR_OpacityIor", ON_Texture::TYPE::pbr_opacity_ior_texture)
    .value("PBR_OpacityRoughness", ON_Texture::TYPE::pbr_opacity_roughness_texture)
    .value("PBR_Emission", ON_Texture::TYPE::pbr_emission_texture)
    .value("PBR_AmbientOcclusion", ON_Texture::TYPE::pbr_ambient_occlusion_texture)
    .value("PBR_Displacement", ON_Texture::TYPE::pbr_displacement_texture)
    .value("PBR_ClearcoatBump", ON_Texture::TYPE::pbr_clearcoat_bump_texture)
    ;
  
  py::enum_<TextureWrap>(m, "TextureWrap")
    .value("Repeat", TextureWrap::Repeat)
    .value("Clamp", TextureWrap::Clamp)
    ;

  py::class_<BND_Texture>(m, "Texture")
    .def(py::init<>())
    .def_property("FileName", &BND_Texture::GetFileName, &BND_Texture::SetFileName)
    .def_property("WrapU", &BND_Texture::WrapU)
    .def_property("WrapV", &BND_Texture::WrapV)
    .def_property("WrapW", &BND_Texture::WrapW)
    .def_property("UvwTransform", &BND_Texture::UvwTransform)
    .def("FileReference", &BND_Texture::GetFileReference)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initTextureBindings(void*)
{
  enum_<ON_Texture::TYPE>("TextureType")
    .value("None", ON_Texture::TYPE::no_texture_type)
    .value("Bitmap", ON_Texture::TYPE::bitmap_texture)
    .value("Diffuse", ON_Texture::TYPE::diffuse_texture)
    .value("Bump", ON_Texture::TYPE::bump_texture)
    .value("Transparency", ON_Texture::TYPE::transparency_texture)
    .value("Opacity", ON_Texture::TYPE::opacity_texture)
    .value("Emap", ON_Texture::TYPE::emap_texture)
    .value("PBR_BaseColor", ON_Texture::TYPE::pbr_base_color_texture)
    .value("PBR_Subsurface", ON_Texture::TYPE::pbr_subsurface_texture)
    .value("PBR_SubsurfaceScattering", ON_Texture::TYPE::pbr_subsurface_scattering_texture)
    .value("PBR_SubsurfaceScatteringRadius", ON_Texture::TYPE::pbr_subsurface_scattering_radius_texture)
    .value("PBR_Metallic", ON_Texture::TYPE::pbr_metallic_texture)
    .value("PBR_Specular", ON_Texture::TYPE::pbr_specular_texture)
    .value("PBR_SpecularTint", ON_Texture::TYPE::pbr_specular_tint_texture)
    .value("PBR_Roughness", ON_Texture::TYPE::pbr_roughness_texture)
    .value("PBR_Anisotropic", ON_Texture::TYPE::pbr_anisotropic_texture)
    .value("PBR_Anisotropic_Rotation", ON_Texture::TYPE::pbr_anisotropic_rotation_texture)
    .value("PBR_Sheen", ON_Texture::TYPE::pbr_sheen_texture)
    .value("PBR_SheenTint", ON_Texture::TYPE::pbr_sheen_tint_texture)
    .value("PBR_Clearcoat", ON_Texture::TYPE::pbr_clearcoat_texture)
    .value("PBR_ClearcoatRoughness", ON_Texture::TYPE::pbr_clearcoat_roughness_texture)
    .value("PBR_OpacityIor", ON_Texture::TYPE::pbr_opacity_ior_texture)
    .value("PBR_OpacityRoughness", ON_Texture::TYPE::pbr_opacity_roughness_texture)
    .value("PBR_Emission", ON_Texture::TYPE::pbr_emission_texture)
    .value("PBR_AmbientOcclusion", ON_Texture::TYPE::pbr_ambient_occlusion_texture)
    .value("PBR_Displacement", ON_Texture::TYPE::pbr_displacement_texture)
    .value("PBR_ClearcoatBump", ON_Texture::TYPE::pbr_clearcoat_bump_texture)
    ;

  enum_<TextureWrap>("TextureWrap")
    .value("Repeat", TextureWrap::Repeat)
    .value("Clamp", TextureWrap::Clamp)
    ;

  class_<BND_Texture>("Texture")
    .constructor<>()
    .property("fileName", &BND_Texture::GetFileName, &BND_Texture::SetFileName)
    .property("wrapU", &BND_Texture::WrapU)
    .property("wrapV", &BND_Texture::WrapV)
    .property("wrapW", &BND_Texture::WrapW)
    .property("uvwTransform", &BND_Texture::UvwTransform)
    .function("fileReference", &BND_Texture::GetFileReference, allow_raw_pointers())
    ;
}
#endif
