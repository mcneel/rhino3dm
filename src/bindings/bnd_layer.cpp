#include "bindings.h"

BND_Layer::BND_Layer()
{
  SetTrackedPointer(new ON_Layer(), nullptr);
}

BND_Layer::BND_Layer(ON_Layer* layer, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(layer, compref);
}


void BND_Layer::SetTrackedPointer(ON_Layer* layer, const ON_ModelComponentReference* compref)
{
  m_layer = layer;
  BND_CommonObject::SetTrackedPointer(layer, compref);
}

std::wstring BND_Layer::GetName() const
{
  return std::wstring(m_layer->Name());
}

void BND_Layer::SetName(const std::wstring name)
{
  m_layer->SetName(name.c_str());
}

BND_UUID BND_Layer::GetId() const
{
  return ON_UUID_to_Binding(m_layer->Id());
}

BND_UUID BND_Layer::GetParentLayerId() const
{
  return ON_UUID_to_Binding(m_layer->ParentLayerId());
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLayerBindings(pybind11::module& m)
{
  py::class_<BND_Layer, BND_CommonObject>(m, "Layer")
    .def(py::init<>())
    .def_property("Name", &BND_Layer::GetName, &BND_Layer::SetName)
    .def_property_readonly("Id", &BND_Layer::GetId)
    .def_property_readonly("ParentLayerId", &BND_Layer::GetParentLayerId)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initLayerBindings(void*)
{
  class_<BND_Layer, base<BND_CommonObject>>("Layer")
    .constructor<>()
    .property("name", &BND_Layer::GetName, &BND_Layer::SetName)
    .property("id", &BND_Layer::GetId)
    .property("parentLayerId", &BND_Layer::GetParentLayerId)
    ;
}
#endif
