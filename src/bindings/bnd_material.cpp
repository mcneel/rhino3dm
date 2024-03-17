#include "bindings.h"

BND_Material::BND_Material()
{
  SetTrackedPointer(new ON_Material(), nullptr);
}

BND_Material::BND_Material(const BND_Material& m)
{
  ON_Material* material = new ON_Material(*m.m_material);
  SetTrackedPointer(material, nullptr);
}

BND_Material::BND_Material(ON_Material* material, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(material, compref);
}

void BND_Material::SetTrackedPointer(ON_Material* material, const ON_ModelComponentReference* compref)
{
  m_material = material;
  BND_ModelComponent::SetTrackedPointer(material, compref);
}

int BND_Material::CompareAppearance(const BND_Material& mat1, const BND_Material& mat2)
{
  return ON_Material::CompareAppearance(*(mat1.m_material), *(mat2.m_material));
}


BND_PhysicallyBasedMaterial* BND_Material::PhysicallyBased()
{
  BND_PhysicallyBasedMaterial* pbr = new BND_PhysicallyBasedMaterial();
  pbr->m_material = m_material;
  pbr->m_component_ref = this->m_component_ref;
  return pbr;
}

void BND_Material::ToPhysicallyBased()
{

  if(m_material)
  {
    m_material->ToPhysicallyBased();
  }

}

static BND_Texture* GetTextureHelper(const ON_Material* mat, ON_Texture::TYPE t)
{
  int index = mat->FindTexture(nullptr, t);
  const ON_Texture* texture = mat->m_textures.At(index);
  if (nullptr == texture)
    return nullptr;
  return new BND_Texture(new ON_Texture(*texture), nullptr);
}

BND_Texture* BND_Material::GetTexture(ON_Texture::TYPE t) const
{
  return GetTextureHelper(m_material, t);
}


BND_Texture* BND_Material::GetBitmapTexture() const
{
  return GetTextureHelper(m_material, ON_Texture::TYPE::bitmap_texture);
}
BND_Texture* BND_Material::GetBumpTexture() const
{
  return GetTextureHelper(m_material, ON_Texture::TYPE::bump_texture);
}
BND_Texture* BND_Material::GetEnvironmentTexture() const
{
  return GetTextureHelper(m_material, ON_Texture::TYPE::emap_texture);
}
BND_Texture* BND_Material::GetTransparencyTexture() const
{
  return GetTextureHelper(m_material, ON_Texture::TYPE::transparency_texture);
}

bool BND_Material::SetBitmapTexture(std::wstring filename)
{
  m_material->DeleteTexture(nullptr, ON_Texture::TYPE::bitmap_texture);
  return m_material->AddTexture(filename.c_str(), ON_Texture::TYPE::bitmap_texture);
}
bool BND_Material::SetBumpTexture(std::wstring filename)
{
  m_material->DeleteTexture(nullptr, ON_Texture::TYPE::bump_texture);
  return m_material->AddTexture(filename.c_str(), ON_Texture::TYPE::bump_texture);
}
bool BND_Material::SetEnvironmentTexture(std::wstring filename)
{
  m_material->DeleteTexture(nullptr, ON_Texture::TYPE::emap_texture);
  return m_material->AddTexture(filename.c_str(), ON_Texture::TYPE::emap_texture);
}
bool BND_Material::SetTransparencyTexture(std::wstring filename)
{
  m_material->DeleteTexture(nullptr, ON_Texture::TYPE::transparency_texture);
  return m_material->AddTexture(filename.c_str(), ON_Texture::TYPE::transparency_texture);
}

static bool SetTextureHelper(ON_Material* material, const ON_Texture* texture, ON_Texture::TYPE t)
{
  material->DeleteTexture(nullptr, t);
  ON_Texture tx(*texture);
  tx.m_type = ON_Texture::TYPE::bitmap_texture;
  return material->AddTexture(tx);
}

bool BND_Material::SetBitmapTexture2(const BND_Texture& texture)
{
  return SetTextureHelper(m_material, texture.m_texture, ON_Texture::TYPE::bitmap_texture);
}
bool BND_Material::SetBumpTexture2(const BND_Texture& texture)
{
  return SetTextureHelper(m_material, texture.m_texture, ON_Texture::TYPE::bump_texture);
}
bool BND_Material::SetEnvironmentTexture2(const BND_Texture& texture)
{
  return SetTextureHelper(m_material, texture.m_texture, ON_Texture::TYPE::emap_texture);
}
bool BND_Material::SetTransparencyTexture2(const BND_Texture& texture)
{
  return SetTextureHelper(m_material, texture.m_texture, ON_Texture::TYPE::transparency_texture);
}


bool BND_PhysicallyBasedMaterial::Supported() const
{
  return m_material->IsPhysicallyBased();
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initMaterialBindings(pybind11::module& m)
{
  py::class_<BND_PhysicallyBasedMaterial>(m, "PhysicallyBasedMaterial")
    .def_property_readonly("Supported", &BND_PhysicallyBasedMaterial::Supported)
    .def_property("Subsurface", &BND_PhysicallyBasedMaterial::Subsurface, &BND_PhysicallyBasedMaterial::SetSubsurface)
    .def_property("SubsurfaceScatteringRadius", &BND_PhysicallyBasedMaterial::SubsurfaceScatteringRadius, &BND_PhysicallyBasedMaterial::SetSubsurfaceScatteringRadius)
    .def_property("Metallic", &BND_PhysicallyBasedMaterial::Metallic, &BND_PhysicallyBasedMaterial::SetMetallic)
    .def_property("Specular", &BND_PhysicallyBasedMaterial::Specular, &BND_PhysicallyBasedMaterial::SetSpecular)
    .def_property("ReflectiveIOR", &BND_PhysicallyBasedMaterial::ReflectiveIOR, &BND_PhysicallyBasedMaterial::SetReflectiveIOR)
    .def_property("SpecularTint", &BND_PhysicallyBasedMaterial::SpecularTint, &BND_PhysicallyBasedMaterial::SetSpecularTint)
    .def_property("Roughness", &BND_PhysicallyBasedMaterial::Roughness, &BND_PhysicallyBasedMaterial::SetRoughness)
    .def_property("Anisotropic", &BND_PhysicallyBasedMaterial::Anisotropic, &BND_PhysicallyBasedMaterial::SetAnisotropic)
    .def_property("AnisotropicRotation", &BND_PhysicallyBasedMaterial::AnisotropicRotation, &BND_PhysicallyBasedMaterial::SetAnisotropicRotation)
    .def_property("Sheen", &BND_PhysicallyBasedMaterial::Sheen, &BND_PhysicallyBasedMaterial::SetSheen)
    .def_property("SheenTint", &BND_PhysicallyBasedMaterial::SheenTint, &BND_PhysicallyBasedMaterial::SetSheenTint)
    .def_property("Clearcoat", &BND_PhysicallyBasedMaterial::Clearcoat, &BND_PhysicallyBasedMaterial::SetClearcoat)
    .def_property("ClearcoatRoughness", &BND_PhysicallyBasedMaterial::ClearcoatRoughness, &BND_PhysicallyBasedMaterial::SetClearcoatRoughness)
    .def_property("OpacityIOR", &BND_PhysicallyBasedMaterial::OpacityIOR, &BND_PhysicallyBasedMaterial::SetOpacityIOR)
    .def_property("Opacity", &BND_PhysicallyBasedMaterial::Opacity, &BND_PhysicallyBasedMaterial::SetOpacity)
    .def_property("OpacityRoughness", &BND_PhysicallyBasedMaterial::OpacityRoughness, &BND_PhysicallyBasedMaterial::SetOpacityRoughness)
    .def_property("BaseColor", &BND_PhysicallyBasedMaterial::GetBaseColor, &BND_PhysicallyBasedMaterial::SetBaseColor)
    .def_property("EmissionColor", &BND_PhysicallyBasedMaterial::GetEmissionColor, &BND_PhysicallyBasedMaterial::SetEmissionColor)
    .def_property("SubsurfaceScatteringColor", &BND_PhysicallyBasedMaterial::GetSubsurfaceScatteringColor, &BND_PhysicallyBasedMaterial::SetSubsurfaceScatteringColor)
    ;

  py::class_<BND_Material, BND_ModelComponent>(m, "Material")
    .def(py::init<>())
    .def(py::init<const BND_Material&>(), py::arg("other"))
    .def_static("CompareAppearance", &BND_Material::CompareAppearance, py::arg("material1"), py::arg("material2"))
    .def_property("RenderPlugInId", &BND_Material::GetRenderPlugInId, &BND_Material::SetRenderPlugInId)
    .def_property_readonly("RenderMaterialInstanceId", &BND_Material::GetRdkMaterialInstanceId)
    .def_property("Name", &BND_Material::GetName, &BND_Material::SetName)
    .def_property("Shine", &BND_Material::GetShine, &BND_Material::SetShine)
    .def_property("Transparency", &BND_Material::GetTransparency, &BND_Material::SetTransparency)
    .def_property("IndexOfRefraction", &BND_Material::GetIndexOfRefraction, &BND_Material::SetIndexOfRefraction)
    .def_property("FresnelIndexOfRefraction", &BND_Material::GetFresnelIndexOfRefraction, &BND_Material::SetFresnelIndexOfRefraction)
    .def_property("RefractionGlossiness", &BND_Material::GetRefractionGlossiness, &BND_Material::SetRefractionGlossiness)
    .def_property("ReflectionGlossiness", &BND_Material::GetReflectionGlossiness, &BND_Material::SetReflectionGlossiness)
    .def_property("FresnelReflections", &BND_Material::GetFresnelReflections, &BND_Material::SetFresnelReflections)
    .def_property("DisableLighting", &BND_Material::GetDisableLighting, &BND_Material::SetDisableLighting)
    .def_property("Reflectivity", &BND_Material::GetReflectivity, &BND_Material::SetReflectivity)
    .def_property_readonly("PreviewColor", &BND_Material::GetPreviewColor)
    .def_property("DiffuseColor", &BND_Material::GetDiffuseColor, &BND_Material::SetDiffuseColor)
    .def_property("AmbientColor", &BND_Material::GetAmbientColor, &BND_Material::SetAmbientColor)
    .def_property("EmissionColor", &BND_Material::GetEmissionColor, &BND_Material::SetEmissionColor)
    .def_property("SpecularColor", &BND_Material::GetSpecularColor, &BND_Material::SetSpecularColor)
    .def_property("ReflectionColor", &BND_Material::GetReflectionColor, &BND_Material::SetReflectionColor)
    .def_property("TransparentColor", &BND_Material::GetTransparentColor, &BND_Material::SetTransparentColor)
    .def("Default", &BND_Material::Default)
    .def("GetTexture", &BND_Material::GetTexture, py::arg("which"))
    .def("GetBitmapTexture", &BND_Material::GetBitmapTexture)
    .def("SetBitmapTexture", &BND_Material::SetBitmapTexture, py::arg("fileName"))
    .def("SetBitmapTexture", &BND_Material::SetBitmapTexture2, py::arg("texture"))
    .def("GetBumpTexture", &BND_Material::GetBumpTexture)
    .def("SetBumpTexture", &BND_Material::SetBumpTexture, py::arg("fileName"))
    .def("SetBumpTexture", &BND_Material::SetBumpTexture2, py::arg("texture"))
    .def("GetEnvironmentTexture", &BND_Material::GetEnvironmentTexture)
    .def("SetEnvironmentTexture", &BND_Material::SetEnvironmentTexture, py::arg("fileName"))
    .def("SetEnvironmentTexture", &BND_Material::SetEnvironmentTexture2, py::arg("texture"))
    .def("GetTransparencyTexture", &BND_Material::GetTransparencyTexture)
    .def("SetTransparencyTexture", &BND_Material::SetTransparencyTexture, py::arg("fileName"))
    .def("SetTransparencyTexture", &BND_Material::SetTransparencyTexture2, py::arg("texture"))
    .def_property_readonly("PhysicallyBased", &BND_Material::PhysicallyBased)
    .def("ToPhysicallyBased", &BND_Material::ToPhysicallyBased)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initMaterialBindings(void*)
{
  class_<BND_PhysicallyBasedMaterial>("PhysicallyBasedMaterial")
    .property("supported", &BND_PhysicallyBasedMaterial::Supported)
    .property("subsurface", &BND_PhysicallyBasedMaterial::Subsurface, &BND_PhysicallyBasedMaterial::SetSubsurface)
    .property("subsurfaceScatteringRadius", &BND_PhysicallyBasedMaterial::SubsurfaceScatteringRadius, &BND_PhysicallyBasedMaterial::SetSubsurfaceScatteringRadius)
    .property("metallic", &BND_PhysicallyBasedMaterial::Metallic, &BND_PhysicallyBasedMaterial::SetMetallic)
    .property("specular", &BND_PhysicallyBasedMaterial::Specular, &BND_PhysicallyBasedMaterial::SetSpecular)
    .property("reflectiveIOR", &BND_PhysicallyBasedMaterial::ReflectiveIOR, &BND_PhysicallyBasedMaterial::SetReflectiveIOR)
    .property("specularTint", &BND_PhysicallyBasedMaterial::SpecularTint, &BND_PhysicallyBasedMaterial::SetSpecularTint)
    .property("roughness", &BND_PhysicallyBasedMaterial::Roughness, &BND_PhysicallyBasedMaterial::SetRoughness)
    .property("anisotropic", &BND_PhysicallyBasedMaterial::Anisotropic, &BND_PhysicallyBasedMaterial::SetAnisotropic)
    .property("anisotropicRotation", &BND_PhysicallyBasedMaterial::AnisotropicRotation, &BND_PhysicallyBasedMaterial::SetAnisotropicRotation)
    .property("sheen", &BND_PhysicallyBasedMaterial::Sheen, &BND_PhysicallyBasedMaterial::SetSheen)
    .property("sheenTint", &BND_PhysicallyBasedMaterial::SheenTint, &BND_PhysicallyBasedMaterial::SetSheenTint)
    .property("clearcoat", &BND_PhysicallyBasedMaterial::Clearcoat, &BND_PhysicallyBasedMaterial::SetClearcoat)
    .property("clearcoatRoughness", &BND_PhysicallyBasedMaterial::ClearcoatRoughness, &BND_PhysicallyBasedMaterial::SetClearcoatRoughness)
    .property("opacityIOR", &BND_PhysicallyBasedMaterial::OpacityIOR, &BND_PhysicallyBasedMaterial::SetOpacityIOR)
    .property("opacity", &BND_PhysicallyBasedMaterial::Opacity, &BND_PhysicallyBasedMaterial::SetOpacity)
    .property("opacityRoughness", &BND_PhysicallyBasedMaterial::OpacityRoughness, &BND_PhysicallyBasedMaterial::SetOpacityRoughness)
    .property("baseColor", &BND_PhysicallyBasedMaterial::GetBaseColor, &BND_PhysicallyBasedMaterial::SetBaseColor)
    .property("emissionColor", &BND_PhysicallyBasedMaterial::GetEmissionColor, &BND_PhysicallyBasedMaterial::SetEmissionColor)
    .property("subsurfaceScatteringColor", &BND_PhysicallyBasedMaterial::GetSubsurfaceScatteringColor, &BND_PhysicallyBasedMaterial::SetSubsurfaceScatteringColor)
    ;

  class_<BND_Material, base<BND_ModelComponent>>("Material")
    .constructor<>()
    .constructor<const BND_Material&>()
    .class_function("compareAppearance", &BND_Material::CompareAppearance)
    .property("renderPlugInId", &BND_Material::GetRenderPlugInId, &BND_Material::SetRenderPlugInId)
     property("renderMaterialInstanceId", &BND_Material::GetRdkMaterialInstanceId)
    .property("name", &BND_Material::GetName, &BND_Material::SetName)
    .property("shine", &BND_Material::GetShine, &BND_Material::SetShine)
    .property("transparency", &BND_Material::GetTransparency, &BND_Material::SetTransparency)
    .property("indexOfRefraction", &BND_Material::GetIndexOfRefraction, &BND_Material::SetIndexOfRefraction)
    .property("fresnelIndexOfRefraction", &BND_Material::GetFresnelIndexOfRefraction, &BND_Material::SetFresnelIndexOfRefraction)
    .property("refractionGlossiness", &BND_Material::GetRefractionGlossiness, &BND_Material::SetRefractionGlossiness)
    .property("reflectionGlossiness", &BND_Material::GetReflectionGlossiness, &BND_Material::SetReflectionGlossiness)
    .property("fresnelReflections", &BND_Material::GetFresnelReflections, &BND_Material::SetFresnelReflections)
    .property("disableLighting", &BND_Material::GetDisableLighting, &BND_Material::SetDisableLighting)
    .property("reflectivity", &BND_Material::GetReflectivity, &BND_Material::SetReflectivity)
    .property("previewColor", &BND_Material::GetPreviewColor)
    .property("diffuseColor", &BND_Material::GetDiffuseColor, &BND_Material::SetDiffuseColor)
    .property("ambientColor", &BND_Material::GetAmbientColor, &BND_Material::SetAmbientColor)
    .property("emissionColor", &BND_Material::GetEmissionColor, &BND_Material::SetEmissionColor)
    .property("specularColor", &BND_Material::GetSpecularColor, &BND_Material::SetSpecularColor)
    .property("reflectionColor", &BND_Material::GetReflectionColor, &BND_Material::SetReflectionColor)
    .property("transparentColor", &BND_Material::GetTransparentColor, &BND_Material::SetTransparentColor)
    .function("default", &BND_Material::Default)
    .function("getTexture", &BND_Material::GetTexture, allow_raw_pointers())
    .function("getBitmapTexture", &BND_Material::GetBitmapTexture, allow_raw_pointers())
    .function("setBitmapTextureFilename", &BND_Material::SetBitmapTexture)
    .function("setBitmapTexture", &BND_Material::SetBitmapTexture2)
    .function("getBumpTexture", &BND_Material::GetBumpTexture, allow_raw_pointers())
    .function("setBumpTextureFilename", &BND_Material::SetBumpTexture)
    .function("setBumpTexture", &BND_Material::SetBumpTexture2)
    .function("getEnvironmentTexture", &BND_Material::GetEnvironmentTexture, allow_raw_pointers())
    .function("setEnvironmentTextureFilename", &BND_Material::SetEnvironmentTexture)
    .function("setEnvironmentTexture", &BND_Material::SetEnvironmentTexture2)
    .function("getTransparencyTexture", &BND_Material::GetTransparencyTexture, allow_raw_pointers())
    .function("setTransparencyTextureFilename", &BND_Material::SetTransparencyTexture)
    .function("setTransparencyTexture", &BND_Material::SetTransparencyTexture2)
    .function("physicallyBased", &BND_Material::PhysicallyBased, allow_raw_pointers())
    .function("toPhysicallyBased", &BND_Material::ToPhysicallyBased)
    ;
}
#endif
