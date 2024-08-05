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
#if defined(NANOBIND)
namespace py = nanobind;
void initGroupBindings(py::module_& m){}
#else
namespace py = pybind11;
void initGroupBindings(py::module& m)
{
  py::class_<BND_Group, BND_CommonObject>(m, "Group")
    .def(py::init<>())
    .def_property("Name", &BND_Group::GetName, &BND_Group::SetName)
    .def_property("Id", &BND_Group::GetId, &BND_Group::SetId)
    .def_property_readonly("Index", &BND_Group::GetIndex)
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initGroupBindings(void*)
{
  class_<BND_Group, base<BND_CommonObject>>("Group")
    .constructor<>()
    .property("name", &BND_Group::GetName, &BND_Group::SetName)
    .property("id", &BND_Group::GetId, &BND_Group::SetId)
    .property("index", &BND_Group::GetIndex)
    ;
}
#endif
