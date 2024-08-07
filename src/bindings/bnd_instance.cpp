#include "bindings.h"

BND_InstanceDefinitionGeometry::BND_InstanceDefinitionGeometry(ON_InstanceDefinition* idef, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(idef, compref);
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

InstanceDefinitionUpdateType BND_InstanceDefinitionGeometry::UpdateType() const
{
  ON_InstanceDefinition::IDEF_UPDATE_TYPE updateType = m_idef->InstanceDefinitionType();

  switch (updateType)
  {
  case ON_InstanceDefinition::IDEF_UPDATE_TYPE::Static:
    return InstanceDefinitionUpdateType::Static;
  case ON_InstanceDefinition::IDEF_UPDATE_TYPE::LinkedAndEmbedded:
    return InstanceDefinitionUpdateType::LinkedAndEmbedded;
  case ON_InstanceDefinition::IDEF_UPDATE_TYPE::Linked:
    return InstanceDefinitionUpdateType::Linked;
  default:
    break;
  }
  return InstanceDefinitionUpdateType::Static;
}

BND_TUPLE BND_InstanceDefinitionGeometry::GetObjectIds() const
{
  const ON_SimpleArray<ON_UUID>& list = m_idef->InstanceGeometryIdList();
  int count = list.Count();
  BND_TUPLE rc = CreateTuple(count);
  for (int i = 0; i < count; i++)
    SetTuple(rc, i, ON_UUID_to_Binding(list[i]));
  return rc;
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

void initInstanceBindings(rh3dmpymodule& m)
{
  py::enum_<InstanceDefinitionUpdateType>(m, "InstanceDefinitionUpdateType")
    .value("Static", InstanceDefinitionUpdateType::Static)
    .value("Embedded", InstanceDefinitionUpdateType::Embedded)
    .value("LinkedAndEmbedded", InstanceDefinitionUpdateType::LinkedAndEmbedded)
    .value("Linked", InstanceDefinitionUpdateType::Linked)
    ;
  
  py::class_<BND_InstanceDefinitionGeometry, BND_CommonObject>(m, "InstanceDefinition")
    .def(py::init<>())
    .def_property_readonly("Description", &BND_InstanceDefinitionGeometry::Description)
    .def_property_readonly("Name", &BND_InstanceDefinitionGeometry::Name)
    .def_property_readonly("Id", &BND_InstanceDefinitionGeometry::Id)
    .def_property_readonly("SourceArchive", &BND_InstanceDefinitionGeometry::SourceArchive)
    .def_property_readonly("UpdateType", &BND_InstanceDefinitionGeometry::UpdateType)
    .def("GetObjectIds", &BND_InstanceDefinitionGeometry::GetObjectIds)
    .def("IsInstanceGeometryId", &BND_InstanceDefinitionGeometry::IsInstanceGeometryId, py::arg("id"))
    ;

  py::class_<BND_InstanceReferenceGeometry, BND_GeometryBase>(m, "InstanceReference")
    .def(py::init<BND_UUID, const BND_Transform&>())
    .def_property_readonly("ParentIdefId", &BND_InstanceReferenceGeometry::ParentIdefId)
    .def_property_readonly("Xform", &BND_InstanceReferenceGeometry::Xform)
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initInstanceBindings(void*)
{
  enum_<InstanceDefinitionUpdateType>("InstanceDefinitionUpdateType")
    .value("Static", InstanceDefinitionUpdateType::Static)
    .value("Embedded", InstanceDefinitionUpdateType::Embedded)
    .value("LinkedAndEmbedded", InstanceDefinitionUpdateType::LinkedAndEmbedded)
    .value("Linked", InstanceDefinitionUpdateType::Linked)
    ;

  class_<BND_InstanceDefinitionGeometry, base<BND_CommonObject>>("InstanceDefinition")
    .constructor<>()
    .property("description", &BND_InstanceDefinitionGeometry::Description)
    .property("name", &BND_InstanceDefinitionGeometry::Name)
    .property("id", &BND_InstanceDefinitionGeometry::Id)
    .property("sourceArchive", &BND_InstanceDefinitionGeometry::SourceArchive)
    .property("updateType", &BND_InstanceDefinitionGeometry::UpdateType)
    .function("getObjectIds", &BND_InstanceDefinitionGeometry::GetObjectIds)
    .function("isInstanceGeometryId", &BND_InstanceDefinitionGeometry::IsInstanceGeometryId)
    ;

  class_<BND_InstanceReferenceGeometry, base<BND_GeometryBase>>("InstanceReference")
    .constructor<BND_UUID, const BND_Transform&>()
    .property("parentIdefId", &BND_InstanceReferenceGeometry::ParentIdefId)
    .property("xform", &BND_InstanceReferenceGeometry::Xform)
    ;
}
#endif
