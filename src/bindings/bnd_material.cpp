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
  BND_CommonObject::SetTrackedPointer(material, compref);
}

static BND_Texture* GetTextureHelper(const ON_Material* mat, ON_Texture::TYPE t)
{
  int index = mat->FindTexture(nullptr, t);
  const ON_Texture* texture = mat->m_textures.At(index);
  if (nullptr == texture)
    return nullptr;
  return new BND_Texture(new ON_Texture(*texture), nullptr);
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

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initMaterialBindings(pybind11::module& m)
{
  py::class_<BND_Material, BND_CommonObject>(m, "Material")
    .def(py::init<>())
    .def(py::init<const BND_Material&>(), py::arg("other"))
    .def_property("RenderPlugInId", &BND_Material::GetRenderPlugInId, &BND_Material::SetRenderPlugInId)
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
    .def("GetBitmapTexture", &BND_Material::GetBitmapTexture)
    .def("SetBitmapTexture", &BND_Material::SetBitmapTexture, py::arg("filename"))
    .def("SetBitmapTexture", &BND_Material::SetBitmapTexture2, py::arg("texture"))
    .def("GetBumpTexture", &BND_Material::GetBumpTexture)
    .def("SetBumpTexture", &BND_Material::SetBumpTexture, py::arg("filename"))
    .def("SetBumpTexture", &BND_Material::SetBumpTexture2, py::arg("texture"))
    .def("GetEnvironmentTexture", &BND_Material::GetEnvironmentTexture)
    .def("SetEnvironmentTexture", &BND_Material::SetEnvironmentTexture, py::arg("filename"))
    .def("SetEnvironmentTexture", &BND_Material::SetEnvironmentTexture2, py::arg("texture"))
    .def("GetTransparencyTexture", &BND_Material::GetTransparencyTexture)
    .def("SetTransparencyTexture", &BND_Material::SetTransparencyTexture, py::arg("filename"))
    .def("SetTransparencyTexture", &BND_Material::SetTransparencyTexture2, py::arg("texture"))
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initMaterialBindings(void*)
{
  class_<BND_Material, base<BND_CommonObject>>("Material")
    .constructor<>()
    .constructor<const BND_Material&>()
    .property("renderPlugInId", &BND_Material::GetRenderPlugInId, &BND_Material::SetRenderPlugInId)
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
    .function("getBitmapTexture", &BND_Material::GetBitmapTexture, allow_raw_pointers())
    .function("setBitmapTexture", &BND_Material::SetBitmapTexture)
    //.function("setBitmapTexture", &BND_Material::SetBitmapTexture2)
    .function("getBumpTexture", &BND_Material::GetBumpTexture, allow_raw_pointers())
    .function("setBumpTexture", &BND_Material::SetBumpTexture)
    //.function("SetBumpTexture", &BND_Material::SetBumpTexture2)
    .function("getEnvironmentTexture", &BND_Material::GetEnvironmentTexture, allow_raw_pointers())
    .function("setEnvironmentTexture", &BND_Material::SetEnvironmentTexture)
    //.function("SetEnvironmentTexture", &BND_Material::SetEnvironmentTexture2)
    .function("getTransparencyTexture", &BND_Material::GetTransparencyTexture, allow_raw_pointers())
    .function("setTransparencyTexture", &BND_Material::SetTransparencyTexture)
    //.function("SetTransparencyTexture", &BND_Material::SetTransparencyTexture2)
    ;
}
#endif
