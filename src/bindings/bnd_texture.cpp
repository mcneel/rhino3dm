
#include "bindings.h"

BND_Texture::BND_Texture()
{
  SetTrackedPointer(new ON_Texture, nullptr);
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


BND_Environment::BND_Environment()
{
  SetTrackedPointer(new ON_Environment, nullptr);
}

BND_Environment::BND_Environment(ON_Environment* env, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(env, compref);
}

BND_Color BND_Environment::BackgroundColor() const
{
  return ON_Color_to_Binding(m_env->BackgroundColor());
}
 
void BND_Environment::SetBackgroundColor(BND_Color col)
{
  m_env->SetBackgroundColor(Binding_to_ON_Color(col));
}

BND_Texture* BND_Environment::BackgroundImage() const
{
  const auto& tex = m_env->BackgroundImage();

  return new BND_Texture(new ON_Texture(tex), nullptr);
}

void BND_Environment::SetBackgroundImage(const BND_Texture& tex)
{
  if (nullptr != tex.m_texture)
  {
    m_env->SetBackgroundImage(*tex.m_texture);
  }
}

ON_Environment::BackgroundProjections BND_Environment::BackgroundProjection() const
{
  return m_env->BackgroundProjection();
}

void BND_Environment::SetBackgroundProjection(int p)
{
  const auto proj = ON_Environment::BackgroundProjections(p);
  m_env->SetBackgroundProjection(proj);
}

void BND_Environment::SetTrackedPointer(ON_Environment* env, const ON_ModelComponentReference* compref)
{
  m_env = env;
  BND_CommonObject::SetTrackedPointer(env, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initEnvironmentBindings(py::module_& m){}
#else
namespace py = pybind11;
void initEnvironmentBindings(py::module& m)
{
  py::class_<BND_Environment>(m, "Environment")
    .def(py::init<>())
    .def_property("BackgroundColor", &BND_Environment::BackgroundColor, &BND_Environment::SetBackgroundColor)
    .def_property("BackgroundImage", &BND_Environment::BackgroundImage, &BND_Environment::SetBackgroundImage)
    .def_property("BackgroundProjection", &BND_Environment::BackgroundProjection, &BND_Environment::SetBackgroundProjection)
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initEnvironmentBindings(void*)
{
  class_<BND_Environment>("Environment")
    .constructor<>()
    .property("backgroundColor", &BND_Environment::BackgroundColor, &BND_Environment::SetBackgroundColor)
    .function("getBackgroundImage", &BND_Environment::BackgroundImage, allow_raw_pointers())
    .function("setBackgroundImage", &BND_Environment::SetBackgroundImage)
    .property("backgroundProjection", &BND_Environment::BackgroundProjection, &BND_Environment::SetBackgroundProjection)
    ;
}
#endif

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
void initTextureBindings(py::module_& m)
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
  
  py::enum_<ON_Texture::WRAP>(m, "TextureUvwWrapping")
    .value("Repeat", ON_Texture::WRAP::repeat_wrap)
    .value("Clamp", ON_Texture::WRAP::clamp_wrap)
    ;

  py::enum_<ON_Environment::BackgroundProjections>(m, "EnvironmentBackgroundProjections")
    .value("Planar",ON_Environment::BackgroundProjections::Planar)//Planar = 0,
    .value("Spherical",ON_Environment::BackgroundProjections::Spherical)//Spherical = 1, // Equirectangular projection.
    .value("Emap",ON_Environment::BackgroundProjections::Emap)//Emap = 2,      // Mirror ball.
    .value("Box",ON_Environment::BackgroundProjections::Box)//Box = 3,
    .value("Automatic",ON_Environment::BackgroundProjections::Automatic)//Automatic = 4,
    .value("LightProbe",ON_Environment::BackgroundProjections::LightProbe)//LightProbe = 5,
    .value("CubeMap",ON_Environment::BackgroundProjections::CubeMap)//CubeMap = 6,
    .value("VerticalCrossCubeMap",ON_Environment::BackgroundProjections::VerticalCrossCubeMap)//VerticalCrossCubeMap = 7,
    .value("HorizontalCrossCubeMap",ON_Environment::BackgroundProjections::HorizontalCrossCubeMap)//HorizontalCrossCubeMap = 8,
    .value("Hemispherical",ON_Environment::BackgroundProjections::Hemispherical)//Hemispherical = 9,
    ;

  py::class_<BND_Texture>(m, "Texture")
    .def(py::init<>())
    .def_property("FileName", &BND_Texture::GetFileName, &BND_Texture::SetFileName)
    .def_property_readonly("WrapU", &BND_Texture::WrapU)
    .def_property_readonly("WrapV", &BND_Texture::WrapV)
    .def_property_readonly("WrapW", &BND_Texture::WrapW)
    .def_property_readonly("UvwTransform", &BND_Texture::UvwTransform)
    .def("FileReference", &BND_Texture::GetFileReference)
    .def_property_readonly("Id", &BND_Texture::Id)
    .def_property("Enabled", &BND_Texture::Enabled,  &BND_Texture::SetEnabled)
    .def_property("TextureType", &BND_Texture::TextureType,  &BND_Texture::SetTextureType)
    .def_property("Repeat", &BND_Texture::Repeat, &BND_Texture::SetRepeat)
    .def_property("Offset", &BND_Texture::Offset, &BND_Texture::SetOffset)
    .def_property("Rotation", &BND_Texture::Rotation, &BND_Texture::SetRotation)
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

  enum_<ON_Texture::WRAP>("TextureUvwWrapping")
    .value("Repeat", ON_Texture::WRAP::repeat_wrap)
    .value("Clamp", ON_Texture::WRAP::clamp_wrap)
    ;

  enum_<ON_Environment::BackgroundProjections>("EnvironmentBackgroundProjections")
    .value("Planar",ON_Environment::BackgroundProjections::Planar)//Planar = 0,
    .value("Spherical",ON_Environment::BackgroundProjections::Spherical)//Spherical = 1, // Equirectangular projection.
    .value("Emap",ON_Environment::BackgroundProjections::Emap)//Emap = 2,      // Mirror ball.
    .value("Box",ON_Environment::BackgroundProjections::Box)//Box = 3,
    .value("Automatic",ON_Environment::BackgroundProjections::Automatic)//Automatic = 4,
    .value("LightProbe",ON_Environment::BackgroundProjections::LightProbe)//LightProbe = 5,
    .value("CubeMap",ON_Environment::BackgroundProjections::CubeMap)//CubeMap = 6,
    .value("VerticalCrossCubeMap",ON_Environment::BackgroundProjections::VerticalCrossCubeMap)//VerticalCrossCubeMap = 7,
    .value("HorizontalCrossCubeMap",ON_Environment::BackgroundProjections::HorizontalCrossCubeMap)//HorizontalCrossCubeMap = 8,
    .value("Hemispherical",ON_Environment::BackgroundProjections::Hemispherical)//Hemispherical = 9,
    ;

  class_<BND_Texture>("Texture")
    .constructor<>()
    .property("fileName", &BND_Texture::GetFileName, &BND_Texture::SetFileName)
    .property("wrapU", &BND_Texture::WrapU)
    .property("wrapV", &BND_Texture::WrapV)
    .property("wrapW", &BND_Texture::WrapW)
    .property("uvwTransform", &BND_Texture::UvwTransform)
    .property("id", &BND_Texture::Id)
    .property("enabled", &BND_Texture::Enabled,  &BND_Texture::SetEnabled)
    .property("textureType", &BND_Texture::TextureType,  &BND_Texture::SetTextureType)
    .property("repeat", &BND_Texture::Repeat, &BND_Texture::SetRepeat)
    .property("offset", &BND_Texture::Offset, &BND_Texture::SetOffset)
    .property("rotation", &BND_Texture::Rotation, &BND_Texture::SetRotation)
    .function("fileReference", &BND_Texture::GetFileReference, allow_raw_pointers())
    ;
}
#endif
