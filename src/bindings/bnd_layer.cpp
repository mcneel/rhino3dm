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

BND_Color BND_Layer::PerViewportColor(BND_UUID viewportId) const
{
  ON_UUID id = Binding_to_ON_UUID(viewportId);
  ON_Color c = m_layer->PerViewportColor(id);
  return ON_Color_to_Binding(c);
}
void BND_Layer::SetPerViewportColor(BND_UUID viewportId, BND_Color color)
{
  ON_UUID id = Binding_to_ON_UUID(viewportId);
  ON_Color c = Binding_to_ON_Color(color);
  m_layer->SetPerViewportColor(id, c);
}
void BND_Layer::DeletePerViewportColor(BND_UUID viewportId)
{
  ON_UUID id = Binding_to_ON_UUID(viewportId);
  m_layer->DeletePerViewportColor(id);
}

BND_Color BND_Layer::GetPlotColor() const
{
  ON_Color c = m_layer->PlotColor();
  return ON_Color_to_Binding(c);
}
void BND_Layer::SetPlotColor(const BND_Color& color)
{
  ON_Color c = Binding_to_ON_Color(color);
  m_layer->SetPlotColor(c);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLayerBindings(pybind11::module& m)
{
  py::class_<BND_Layer, BND_CommonObject>(m, "Layer")
    .def(py::init<>())
    .def_property("Name", &BND_Layer::GetName, &BND_Layer::SetName)
    .def_property("Id", &BND_Layer::GetId, &BND_Layer::SetId)
    .def_property_readonly("Index", &BND_Layer::GetIndex)
    .def_property_readonly("ParentLayerId", &BND_Layer::GetParentLayerId)
    .def_property("IgesLevel", &BND_Layer::GetIgesLevel, &BND_Layer::SetIgesLevel)
    .def("HasPerViewportSettings", &BND_Layer::HasPerViewportSettings, py::arg("viewportId"))
    .def("DeletePerViewportSettings", &BND_Layer::DeletePerViewportSettings, py::arg("viewportId"))
    .def_property("Color", &BND_Layer::GetColor, &BND_Layer::SetColor)
    .def("PerViewportColor", &BND_Layer::PerViewportColor, py::arg("viewportId"))
    .def("SetPerViewportColor", &BND_Layer::SetPerViewportColor, py::arg("viewportId"), py::arg("color"))
    .def("DeletePerViewportColor", &BND_Layer::DeletePerViewportColor, py::arg("viewportId"))
    .def_property("PlotColor", &BND_Layer::GetPlotColor, &BND_Layer::SetPlotColor)
    .def_property("PlotWeight", &BND_Layer::GetPlotWeight, &BND_Layer::SetPlotWeight)
    .def_property("LinetypeIndex", &BND_Layer::GetLinetypeIndex, &BND_Layer::SetLinetypeIndex)
    .def_property("RenderMaterialIndex", &BND_Layer::GetRenderMaterialIndex, &BND_Layer::SetRenderMaterialIndex)
    .def_property("Visible", &BND_Layer::IsVisible, &BND_Layer::SetVisible)
    .def_property("Locked", &BND_Layer::IsLocked, &BND_Layer::SetLocked)
    .def("GetPersistentVisibility", &BND_Layer::GetPersistentVisibility)
    .def("SetPersistentVisibility", &BND_Layer::SetPersistentVisibility, py::arg("persistentVisibility"))
    .def("UnsetPersistentVisibility", &BND_Layer::UnsetPersistentVisibility)
    .def("GetPersistentLocking", &BND_Layer::GetPersistentLocking)
    .def("SetPersistentLocking", &BND_Layer::SetPersistentLocking, py::arg("persistentLocking"))
    .def("UnsetPersistentLocking", &BND_Layer::UnsetPersistentLocking)
    .def_property("Expanded", &BND_Layer::IsExpanded, &BND_Layer::SetExpanded)
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
    .property("id", &BND_Layer::GetId, &BND_Layer::SetId)
    .property("parentLayerId", &BND_Layer::GetParentLayerId)
    .property("igesLevel", &BND_Layer::GetIgesLevel, &BND_Layer::SetIgesLevel)
    .function("hasPerViewportSettings", &BND_Layer::HasPerViewportSettings)
    .function("deletePerViewportSettings", &BND_Layer::DeletePerViewportSettings)
    .property("color", &BND_Layer::GetColor, &BND_Layer::SetColor)
    .function("perViewportColor", &BND_Layer::PerViewportColor)
    .function("setPerViewportColor", &BND_Layer::SetPerViewportColor)
    .function("deletePerViewportColor", &BND_Layer::DeletePerViewportColor)
    .property("plotColor", &BND_Layer::GetPlotColor, &BND_Layer::SetPlotColor)
    .property("plotWeight", &BND_Layer::GetPlotWeight, &BND_Layer::SetPlotWeight)
    .property("linetypeIndex", &BND_Layer::GetLinetypeIndex, &BND_Layer::SetLinetypeIndex)
    .property("renderMaterialIndex", &BND_Layer::GetRenderMaterialIndex, &BND_Layer::SetRenderMaterialIndex)
    .property("visible", &BND_Layer::IsVisible, &BND_Layer::SetVisible)
    .property("locked", &BND_Layer::IsLocked, &BND_Layer::SetLocked)
    .function("getPersistentVisibility", &BND_Layer::GetPersistentVisibility)
    .function("setPersistentVisibility", &BND_Layer::SetPersistentVisibility)
    .function("unsetPersistentVisibility", &BND_Layer::UnsetPersistentVisibility)
    .function("getPersistentLocking", &BND_Layer::GetPersistentLocking)
    .function("setPersistentLocking", &BND_Layer::SetPersistentLocking)
    .function("unsetPersistentLocking", &BND_Layer::UnsetPersistentLocking)
    .property("expanded", &BND_Layer::IsExpanded, &BND_Layer::SetExpanded)
    ;
}
#endif
