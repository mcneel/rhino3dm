#include "bindings.h"

BND_3dmAttributes::BND_3dmAttributes()
{
  SetTrackedPointer(new ON_3dmObjectAttributes(), nullptr);
}

BND_3dmAttributes::BND_3dmAttributes(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(attrs, compref);
}

void BND_3dmAttributes::SetTrackedPointer(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref)
{
  m_attributes = attrs;
  BND_CommonObject::SetTrackedPointer(attrs, compref);
}

BND_UUID BND_3dmAttributes::GetObjectId() const
{
  return ON_UUID_to_Binding(m_attributes->m_uuid);
}

std::wstring BND_3dmAttributes::GetName() const
{
  return std::wstring(m_attributes->m_name);
}

void BND_3dmAttributes::SetName(const std::wstring name)
{
  m_attributes->m_name = name.c_str();
}

//////////////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void init3dmAttributesBindings(pybind11::module& m)
{
  py::class_<BND_3dmAttributes, BND_CommonObject>(m, "ObjectAttributes")
    .def(py::init<>())
    .def_property("Visible", &BND_3dmAttributes::IsVisible, &BND_3dmAttributes::SetVisible)
    .def_property("Name", &BND_3dmAttributes::GetName, &BND_3dmAttributes::SetName)
    .def_property_readonly("Id", &BND_3dmAttributes::GetObjectId)
    .def_property("LayerIndex", &BND_3dmAttributes::GetLayerIndex, &BND_3dmAttributes::SetLayerIndex)
    .def_property("MaterialIndex", &BND_3dmAttributes::MaterialIndex, &BND_3dmAttributes::SetMaterialIndex)
    .def_property("ObjectColor", &BND_3dmAttributes::GetObjectColor, &BND_3dmAttributes::SetObjectColor)
    .def_property("PlotColor", &BND_3dmAttributes::GetPlotColor, &BND_3dmAttributes::SetPlotColor)
    .def_property("DisplayOrder", &BND_3dmAttributes::GetDisplayOrder, &BND_3dmAttributes::SetDisplayOrder)
    .def_property("PlotWeight", &BND_3dmAttributes::PlotWeight, &BND_3dmAttributes::SetPlotWeight)
    .def_property("WireDensity", &BND_3dmAttributes::WireDensity, &BND_3dmAttributes::SetWireDensity)
    .def_property("ViewportId", &BND_3dmAttributes::GetViewportId, &BND_3dmAttributes::SetViewportId)
    .def_property_readonly("GroupCount", &BND_3dmAttributes::GroupCount)
    .def("AddToGroup", &BND_3dmAttributes::AddToGroup)
    .def("RemoveFromGroup", &BND_3dmAttributes::RemoveFromGroup)
    .def("RemoveFromAllGroups", &BND_3dmAttributes::RemoveFromAllGroups)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void init3dmAttributesBindings(void*)
{
  class_<BND_3dmAttributes, base<BND_CommonObject>>("ObjectAttributes")
    .constructor<>()
    .property("visible", &BND_3dmAttributes::IsVisible, &BND_3dmAttributes::SetVisible)
    .property("name", &BND_3dmAttributes::GetName, &BND_3dmAttributes::SetName)
    .property("id", &BND_3dmAttributes::GetObjectId)
    .property("layerIndex", &BND_3dmAttributes::GetLayerIndex, &BND_3dmAttributes::SetLayerIndex)
  ;

}
#endif
