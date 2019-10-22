#include "bindings.h"

BND_InstanceDefinitionGeometry::BND_InstanceDefinitionGeometry(ON_InstanceDefinition* idef, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(idef, compref);
  m_guids = ON_SimpleArrayUUID_to_Binding(idef->InstanceGeometryIdList());
}

void BND_InstanceDefinitionGeometry::SetTrackedPointer(ON_InstanceDefinition* idef, const ON_ModelComponentReference* compref)
{
  m_idef = idef;
  BND_CommonObject::SetTrackedPointer(idef, compref);
}

BND_InstanceDefinitionGeometry::BND_InstanceDefinitionGeometry()
{
  SetTrackedPointer(new ON_InstanceDefinition(), nullptr);
}


BND_InstanceReferenceGeometry::BND_InstanceReferenceGeometry(ON_InstanceRef* iref, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(iref, compref);
}

void BND_InstanceReferenceGeometry::SetTrackedPointer(ON_InstanceRef* iref, const ON_ModelComponentReference* compref)
{
  m_iref = iref;
  BND_GeometryBase::SetTrackedPointer(iref, compref);
}

BND_InstanceReferenceGeometry::BND_InstanceReferenceGeometry(BND_UUID instanceDefinitionId, const BND_Transform& transform)
{
  ON_InstanceRef* iref = new ON_InstanceRef();
  iref->m_instance_definition_uuid = Binding_to_ON_UUID(instanceDefinitionId);
  iref->m_xform = transform.m_xform;
  SetTrackedPointer(iref, nullptr);
}

BND_UUID BND_InstanceReferenceGeometry::ParentIdefId() const
{
  return ON_UUID_to_Binding(m_iref->m_instance_definition_uuid);
}

BND_Transform BND_InstanceReferenceGeometry::Xform() const
{
  return BND_Transform(m_iref->m_xform);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initInstanceBindings(pybind11::module& m)
{
  py::class_<BND_InstanceDefinitionGeometry, BND_CommonObject>(m, "InstanceDefinition")
    .def(py::init<>())
    .def_property_readonly("Description", &BND_InstanceDefinitionGeometry::Description)
	.def_property_readonly("Name", &BND_InstanceDefinitionGeometry::Name)
	.def_property_readonly("Id", &BND_InstanceDefinitionGeometry::Id)
	.def("GetObjectIds", &BND_InstanceDefinitionGeometry::GetObjectIds)
    ;

  py::class_<BND_InstanceReferenceGeometry, BND_GeometryBase>(m, "InstanceReference")
    .def(py::init<BND_UUID, const BND_Transform&>())
    .def_property_readonly("ParentIdefId", &BND_InstanceReferenceGeometry::ParentIdefId)
    .def_property_readonly("Xform", &BND_InstanceReferenceGeometry::Xform)
    ;
  //bind opaque std::vector<BND_UUID> and provide functionality (acts ~like a python list)
  py::class_<std::vector<BND_UUID>>(m, "GuidList")
	.def(py::init<>())
	.def("__len__", [](const std::vector<BND_UUID>& self) { return self.size(); })
	.def("__getitem__", [](const std::vector<BND_UUID>& self, const unsigned int i) { return self[i]; })
	.def("__iter__", [](std::vector<BND_UUID>& self) {
			return py::make_iterator(self.begin(), self.end());
			}, py::keep_alive<0, 1>());
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initInstanceBindings(void*)
{
  class_<BND_InstanceDefinitionGeometry, base<BND_CommonObject>>("InstanceDefinition")
    .constructor<>()
    .property("description", &BND_InstanceDefinitionGeometry::Description)
    ;

  class_<BND_InstanceReferenceGeometry, base<BND_GeometryBase>>("InstanceReference")
    .constructor<BND_UUID, const BND_Transform&>()
    .property("parentIdefId", &BND_InstanceReferenceGeometry::ParentIdefId)
    .property("xform", &BND_InstanceReferenceGeometry::Xform)
    ;
}
#endif
