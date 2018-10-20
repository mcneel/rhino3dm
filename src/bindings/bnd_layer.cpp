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

bool BND_Layer::HasPerViewportSettings(BND_UUID viewportId) const
{
  ON_UUID id = Binding_to_ON_UUID(viewportId);
  return m_layer->HasPerViewportSettings(id);
}

void BND_Layer::DeletePerViewportSettings(BND_UUID viewportId)
{
  ON_UUID id = Binding_to_ON_UUID(viewportId);
  m_layer->DeletePerViewportSettings(id);
}

BND_Color BND_Layer::GetColor() const
{
  ON_Color c = m_layer->Color();
  return ON_Color_to_Binding(c);
}
void BND_Layer::SetColor(const BND_Color& color)
{
  ON_Color c = Binding_to_ON_Color(color);
  m_layer->SetColor(c);
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
    .def_property("IgesLevel", &BND_Layer::GetIgesLevel, &BND_Layer::SetIgesLevel)
    .def("HasPerViewportSettings", &BND_Layer::HasPerViewportSettings)
    .def("DeletePerViewportSettings", &BND_Layer::DeletePerViewportSettings)
    .def_property("Color", &BND_Layer::GetColor, &BND_Layer::SetColor)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initLayerBindings(void*)
{
  class_<BND_Layer, base<BND_CommonObject>>("Layer")
    .constructor<>()
    ;
}
#endif
