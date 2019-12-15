#include "bindings.h"

BND_Group::BND_Group()
{
  SetTrackedPointer(new ON_Group(), nullptr);
}

BND_Group::BND_Group(ON_Group* group, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(group, compref);
}


void BND_Group::SetTrackedPointer(ON_Group* group, const ON_ModelComponentReference* compref)
{
  m_group = group;
  BND_CommonObject::SetTrackedPointer(group, compref);
}



#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initGroupBindings(pybind11::module& m)
{
  py::class_<BND_Group, BND_CommonObject>(m, "Group")
    .def(py::init<>())
    .def_property("Name", &BND_Group::GetName, &BND_Group::SetName)
    .def_property("Id", &BND_Group::GetId, &BND_Group::SetId)
    .def_property_readonly("Index", &BND_Group::GetIndex)
    ;
}
#endif
