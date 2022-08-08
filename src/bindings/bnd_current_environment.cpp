
#include "bindings.h"

BND_File3dmCurrentEnvironment::BND_File3dmCurrentEnvironment()
{
}

BND_File3dmCurrentEnvironment::BND_File3dmCurrentEnvironment(ON_CurrentEnvironment* ce)
{
  SetTrackedPointer(ce);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initCurrentEnvironmentBindings(pybind11::module& m)
{
  py::class_<BND_File3dmCurrentEnvironment>(m, "CurrentEnvironment")
    .def(py::init<>())
    .def(py::init<const BND_File3dmCurrentEnvironment&>(), py::arg("other"))
    .def_property("BackgroundOn", &BND_File3dmCurrentEnvironment::GetBackgroundOn, &BND_File3dmCurrentEnvironment::SetBackgroundOn)
    .def_property("ReflectionOn", &BND_File3dmCurrentEnvironment::GetReflectionOn, &BND_File3dmCurrentEnvironment::SetReflectionOn)
    .def_property("SkylightingOn", &BND_File3dmCurrentEnvironment::GetSkylightingOn, &BND_File3dmCurrentEnvironment::SetSkylightingOn)
    .def_property("Background", &BND_File3dmCurrentEnvironment::GetBackground, &BND_File3dmCurrentEnvironment::SetBackground)
    .def_property("Reflection", &BND_File3dmCurrentEnvironment::GetReflection, &BND_File3dmCurrentEnvironment::SetReflection)
    .def_property("Skylighting", &BND_File3dmCurrentEnvironment::GetSkylighting, &BND_File3dmCurrentEnvironment::SetSkylighting)
   ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCurrentEnvironmentBindings(void*)
{
  class_<BND_File3dmCurrentEnvironment>("CurrentEnvironment")
    .constructor<>()
    .constructor<const BND_File3dmCurrentEnvironment&>()
    .property("backgroundOn", &BND_File3dmCurrentEnvironment::GetBackgroundOn, &BND_File3dmCurrentEnvironment::SetBackgroundOn)
    .property("reflectionOn", &BND_File3dmCurrentEnvironment::GetReflectionOn, &BND_File3dmCurrentEnvironment::SetReflectionOn)
    .property("skylightingOn", &BND_File3dmCurrentEnvironment::GetSkylightingOn, &BND_File3dmCurrentEnvironment::SetSkylightingOn)
    .property("background", &BND_File3dmCurrentEnvironment::GetBackground, &BND_File3dmCurrentEnvironment::SetBackground)
    .property("reflection", &BND_File3dmCurrentEnvironment::GetReflection, &BND_File3dmCurrentEnvironment::SetReflection)
    .property("skylighting", &BND_File3dmCurrentEnvironment::GetSkylighting, &BND_File3dmCurrentEnvironment::SetSkylighting)
    ;
}
#endif
