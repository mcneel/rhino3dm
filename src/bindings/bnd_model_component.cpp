#include "bindings.h"

BND_ModelComponent::BND_ModelComponent()
{
}

void BND_ModelComponent::SetTrackedPointer(ON_ModelComponent* modelComponent, const ON_ModelComponentReference* compref)
{
  m_model_component = modelComponent;
  BND_CommonObject::SetTrackedPointer(modelComponent, compref);
}

BND_UUID BND_ModelComponent::GetId() const
{
  return ON_UUID_to_Binding(m_model_component->Id());
}
void BND_ModelComponent::SetId(BND_UUID id)
{
  m_model_component->SetId(Binding_to_ON_UUID(id));
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)

void initModelComponentBindings(rh3dmpymodule& m)
{
  py::class_<BND_ModelComponent, BND_CommonObject>(m, "ModelComponent")
    .def("DataCRC", &BND_ModelComponent::DataCRC, py::arg("currentRemainder"))
    .def_property_readonly("IsSystemComponent", &BND_ModelComponent::IsSystemComponent)
    .def_property("Id", &BND_ModelComponent::GetId, &BND_ModelComponent::SetId)
    .def("ClearId", &BND_ModelComponent::ClearId)
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initModelComponentBindings(void*)
{
  class_<BND_ModelComponent, base<BND_CommonObject>>("ModelComponent")
    .function("dataCRC", &BND_ModelComponent::DataCRC)
    .property("isSystemComponent", &BND_ModelComponent::IsSystemComponent)
    .property("id", &BND_ModelComponent::GetId, &BND_ModelComponent::SetId)
    .function("clearId", &BND_ModelComponent::ClearId)
    ;
}
#endif
