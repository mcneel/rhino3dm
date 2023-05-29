
#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSkylightBindings(pybind11::module& m)
{
  py::class_<BND_File3dmSkylight>(m, "Skylight")
    .def(py::init<>())
    .def(py::init<const BND_File3dmSkylight&>(), py::arg("other"))
    .def_property("On", &BND_File3dmSkylight::GetOn, &BND_File3dmSkylight::SetOn)
    .def_property("ShadowIntensity", &BND_File3dmSkylight::GetShadowIntensity, &BND_File3dmSkylight::SetShadowIntensity)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSkylightBindings(void*)
{
  class_<BND_File3dmSkylight>("Skylight")
    .constructor<>()
    /*
    .constructor<const BND_File3dmSkylight&>()
    .property("on", &BND_File3dmSkylight::GetOn, &BND_File3dmSkylight::SetOn)
    .property("shadowIntensity", &BND_File3dmSkylight::GetShadowIntensity, &BND_File3dmSkylight::SetShadowIntensity)
    */
    ;
}
#endif
