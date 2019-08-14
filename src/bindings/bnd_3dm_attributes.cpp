#include "bindings.h"

BND_3dmObjectAttributes::BND_3dmObjectAttributes()
{
  SetTrackedPointer(new ON_3dmObjectAttributes(), nullptr);
}

BND_3dmObjectAttributes::BND_3dmObjectAttributes(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(attrs, compref);
}

void BND_3dmObjectAttributes::SetTrackedPointer(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref)
{
  m_attributes = attrs;
  BND_CommonObject::SetTrackedPointer(attrs, compref);
}

bool BND_3dmObjectAttributes::Transform(const class BND_Transform& transform)
{
  return m_attributes->Transform(transform.m_xform);
}

bool BND_3dmObjectAttributes::HasDisplayModeOverride(BND_UUID viewportId) const
{
  ON_UUID dmr_id;
  ON_UUID _viewportId = Binding_to_ON_UUID(viewportId);
  if (m_attributes->FindDisplayMaterialId(_viewportId, &dmr_id))
  {
    //make sure dmr is not the "invisible in detail" id
    if (dmr_id != ON_DisplayMaterialRef::m_invisible_in_detail_id)
      return true;
  }
  return false;
}

BND_UUID BND_3dmObjectAttributes::GetObjectId() const
{
  return ON_UUID_to_Binding(m_attributes->m_uuid);
}

void BND_3dmObjectAttributes::SetObjectId(BND_UUID id)
{
  m_attributes->m_uuid = Binding_to_ON_UUID(id);
}

std::wstring BND_3dmObjectAttributes::GetName() const
{
  return std::wstring(m_attributes->m_name);
}

void BND_3dmObjectAttributes::SetName(const std::wstring name)
{
  m_attributes->m_name = name.c_str();
}

#if defined(ON_PYTHON_COMPILE)
pybind11::tuple BND_3dmObjectAttributes::GetGroupList() const
{
  int count = m_attributes->GroupCount();
  pybind11::tuple rc(count);
  const int* groups = m_attributes->GroupList();
  for (int i = 0; i < count; i++)
    rc[i] = groups[i];
  return rc;
}
#endif

//////////////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void init3dmAttributesBindings(pybind11::module& m)
{
  py::class_<BND_3dmObjectAttributes, BND_CommonObject>(m, "ObjectAttributes")
    .def(py::init<>())
    .def_property("Mode", &BND_3dmObjectAttributes::GetMode, &BND_3dmObjectAttributes::SetMode)
    .def("Transform", &BND_3dmObjectAttributes::Transform, py::arg("transform"))
    .def_property_readonly("IsInstanceDefinitionObject", &BND_3dmObjectAttributes::IsInstanceDefinitionObject)
    .def_property("Visible", &BND_3dmObjectAttributes::IsVisible, &BND_3dmObjectAttributes::SetVisible)
    .def_property("CastsShadows", &BND_3dmObjectAttributes::CastsShadows, &BND_3dmObjectAttributes::SetCastsShadows)
    .def_property("ReceivesShadows", &BND_3dmObjectAttributes::ReceivesShadows, &BND_3dmObjectAttributes::SetReceivesShadows)
    .def_property("LinetypeSource", &BND_3dmObjectAttributes::GetLinetypeSource, &BND_3dmObjectAttributes::SetLinetypeSource)
    .def_property("ColorSource", &BND_3dmObjectAttributes::GetColorSource, &BND_3dmObjectAttributes::SetColorSource)
    .def_property("PlotColorSource", &BND_3dmObjectAttributes::GetPlotColorSource, &BND_3dmObjectAttributes::SetPlotColorSource)
    .def_property("PlotWeightSource", &BND_3dmObjectAttributes::GetPlotWeightSource, &BND_3dmObjectAttributes::SetPlotWeightSource)
    .def("HasDisplayModeOverride", &BND_3dmObjectAttributes::HasDisplayModeOverride, py::arg("viewportId"))
    .def_property("Id", &BND_3dmObjectAttributes::GetObjectId, &BND_3dmObjectAttributes::SetObjectId)
    .def_property("Name", &BND_3dmObjectAttributes::GetName, &BND_3dmObjectAttributes::SetName)
    .def_property("Url", &BND_3dmObjectAttributes::GetUrl, &BND_3dmObjectAttributes::SetUrl)
    .def_property("LayerIndex", &BND_3dmObjectAttributes::GetLayerIndex, &BND_3dmObjectAttributes::SetLayerIndex)
    .def_property("LinetypeIndex", &BND_3dmObjectAttributes::GetLinetypeIndex, &BND_3dmObjectAttributes::SetLinetypeIndex)
    .def_property("MaterialIndex", &BND_3dmObjectAttributes::MaterialIndex, &BND_3dmObjectAttributes::SetMaterialIndex)
    .def_property("MaterialSource", &BND_3dmObjectAttributes::GetMaterialSource, &BND_3dmObjectAttributes::SetMaterialSource)
    .def_property("ObjectColor", &BND_3dmObjectAttributes::GetObjectColor, &BND_3dmObjectAttributes::SetObjectColor)
    .def_property("PlotColor", &BND_3dmObjectAttributes::GetPlotColor, &BND_3dmObjectAttributes::SetPlotColor)
    .def_property("DisplayOrder", &BND_3dmObjectAttributes::GetDisplayOrder, &BND_3dmObjectAttributes::SetDisplayOrder)
    .def_property("PlotWeight", &BND_3dmObjectAttributes::PlotWeight, &BND_3dmObjectAttributes::SetPlotWeight)
    .def_property("ObjectDecoration", &BND_3dmObjectAttributes::GetObjectDecoration, &BND_3dmObjectAttributes::SetObjectDecoration)
    .def_property("WireDensity", &BND_3dmObjectAttributes::WireDensity, &BND_3dmObjectAttributes::SetWireDensity)
    .def_property("ViewportId", &BND_3dmObjectAttributes::GetViewportId, &BND_3dmObjectAttributes::SetViewportId)
    .def_property("ActiveSpace", &BND_3dmObjectAttributes::GetSpace, &BND_3dmObjectAttributes::SetSpace)
    .def_property_readonly("GroupCount", &BND_3dmObjectAttributes::GroupCount)
    .def("GetGroupList", &BND_3dmObjectAttributes::GetGroupList)
    .def("AddToGroup", &BND_3dmObjectAttributes::AddToGroup)
    .def("RemoveFromGroup", &BND_3dmObjectAttributes::RemoveFromGroup)
    .def("RemoveFromAllGroups", &BND_3dmObjectAttributes::RemoveFromAllGroups)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void init3dmAttributesBindings(void*)
{
  class_<BND_3dmObjectAttributes, base<BND_CommonObject>>("ObjectAttributes")
    .constructor<>()
    .property("mode", &BND_3dmObjectAttributes::GetMode, &BND_3dmObjectAttributes::SetMode)
    .function("transform", &BND_3dmObjectAttributes::Transform)
    .property("isInstanceDefinitionObject", &BND_3dmObjectAttributes::IsInstanceDefinitionObject)
    .property("visible", &BND_3dmObjectAttributes::IsVisible, &BND_3dmObjectAttributes::SetVisible)
    .property("name", &BND_3dmObjectAttributes::GetName, &BND_3dmObjectAttributes::SetName)
    .property("castsShadows", &BND_3dmObjectAttributes::CastsShadows, &BND_3dmObjectAttributes::SetCastsShadows)
    .property("receivesShadows", &BND_3dmObjectAttributes::ReceivesShadows, &BND_3dmObjectAttributes::SetReceivesShadows)
    .property("linetypeSource", &BND_3dmObjectAttributes::GetLinetypeSource, &BND_3dmObjectAttributes::SetLinetypeSource)
    .property("colorSource", &BND_3dmObjectAttributes::GetColorSource, &BND_3dmObjectAttributes::SetColorSource)
    .property("plotColorSource", &BND_3dmObjectAttributes::GetPlotColorSource, &BND_3dmObjectAttributes::SetPlotColorSource)
    .property("plotWeightSource", &BND_3dmObjectAttributes::GetPlotWeightSource, &BND_3dmObjectAttributes::SetPlotWeightSource)
    .function("hasDisplayModeOverride", &BND_3dmObjectAttributes::HasDisplayModeOverride)
    .property("id", &BND_3dmObjectAttributes::GetObjectId)
    .property("url", &BND_3dmObjectAttributes::GetUrl, &BND_3dmObjectAttributes::SetUrl)
    .property("layerIndex", &BND_3dmObjectAttributes::GetLayerIndex, &BND_3dmObjectAttributes::SetLayerIndex)
    .property("materialIndex", &BND_3dmObjectAttributes::MaterialIndex, &BND_3dmObjectAttributes::SetMaterialIndex)
    .property("materialSource", &BND_3dmObjectAttributes::GetMaterialSource, &BND_3dmObjectAttributes::SetMaterialSource)
    .property("objectColor", &BND_3dmObjectAttributes::GetObjectColor, &BND_3dmObjectAttributes::SetObjectColor)
    .property("plotColor", &BND_3dmObjectAttributes::GetPlotColor, &BND_3dmObjectAttributes::SetPlotColor)
    .property("displayOrder", &BND_3dmObjectAttributes::GetDisplayOrder, &BND_3dmObjectAttributes::SetDisplayOrder)
    .property("plotWeight", &BND_3dmObjectAttributes::PlotWeight, &BND_3dmObjectAttributes::SetPlotWeight)
    .property("objectDecoration", &BND_3dmObjectAttributes::GetObjectDecoration, &BND_3dmObjectAttributes::SetObjectDecoration)
    .property("wireDensity", &BND_3dmObjectAttributes::WireDensity, &BND_3dmObjectAttributes::SetWireDensity)
    .property("viewportId", &BND_3dmObjectAttributes::GetViewportId, &BND_3dmObjectAttributes::SetViewportId)
    .property("activeSpace", &BND_3dmObjectAttributes::GetSpace, &BND_3dmObjectAttributes::SetSpace)
    .property("groupCount", &BND_3dmObjectAttributes::GroupCount)
    //.function("getGroupList", &BND_3dmObjectAttributes::GetGroupList)
    .function("addToGroup", &BND_3dmObjectAttributes::AddToGroup)
    .function("removeFromGroup", &BND_3dmObjectAttributes::RemoveFromGroup)
    .function("removeFromAllGroups", &BND_3dmObjectAttributes::RemoveFromAllGroups)
  ;

}
#endif
