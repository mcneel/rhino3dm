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

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initMaterialBindings(pybind11::module& m)
{
  py::class_<BND_Material, BND_CommonObject>(m, "Material")
    .def(py::init<>())
    .def(py::init<const BND_Material&>())
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
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initMaterialBindings(void*)
{
  class_<BND_Material, base<BND_CommonObject>>("Material")
    .constructor<>()
    ;
}
#endif
