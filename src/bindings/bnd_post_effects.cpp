
#include "bindings.h"

BND_File3dmPostEffect::BND_File3dmPostEffect()
{
  SetTrackedPointer(new ON_PostEffect, nullptr);
}

BND_File3dmPostEffect::BND_File3dmPostEffect(const BND_File3dmPostEffect& pep)
{
  SetTrackedPointer(new ON_PostEffect(*pep.m_post_effect), nullptr);
}

BND_File3dmPostEffect::BND_File3dmPostEffect(ON_PostEffect* pep, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(pep, compref);
}

void BND_File3dmPostEffect::SetTrackedPointer(ON_PostEffect* pep, const ON_ModelComponentReference* compref)
{
  m_post_effect = pep;

  BND_ModelComponent::SetTrackedPointer(pep, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPostEffectBindings(pybind11::module& m)
{
  py::class_<BND_File3dmPostEffect>(m, "PostEffect")
    .def(py::init<>())
    .def(py::init<const BND_File3dmPostEffect&>(), py::arg("other"))
    .def_property_readonly("Type", &BND_File3dmPostEffect::Type)
    .def_property_readonly("LocalName", &BND_File3dmPostEffect::LocalName)
    .def_property_readonly("IsVisible", &BND_File3dmPostEffect::IsVisible)
    .def_property_readonly("IsActive", &BND_File3dmPostEffect::IsActive)
    .def("GetParameter", &BND_File3dmPostEffect::GetParameter)
    .def("SetParameter", &BND_File3dmPostEffect::SetParameter)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPostEffectBindings(void*)
{
  class_<BND_File3dmPostEffect>("PostEffect")
    .constructor<>()
    .constructor<const BND_File3dmPostEffect&>()
    .property("LocalName", &BND_File3dmPostEffect::LocalName)
    .property("IsVisible", &BND_File3dmPostEffect::IsVisible)
    .property("IsActive", &BND_File3dmPostEffect::IsActive)
    .function("GetParameter", &BND_File3dmPostEffect::GetParameter)
    .function("SetParameter", &BND_File3dmPostEffect::SetParameter)
    ;
}
#endif
