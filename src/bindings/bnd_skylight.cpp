
#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSkylightBindings(pybind11::module& m)
{
  py::class_<BND_File3dmSkylight>(m, "Skylight")
    .def(py::init<>())
    .def(py::init<const BND_File3dmSkylight&>(), py::arg("other"))
    .def_property("Enabled", &BND_File3dmSkylight::GetEnabled, &BND_File3dmSkylight::SetEnabled)
    .def_property("ShadowIntensity", &BND_File3dmSkylight::GetShadowIntensity, &BND_File3dmSkylight::SetShadowIntensity)
    ;
}
#endif

#if defined(ON_WASM_COMPILE____TEMP)
using namespace emscripten;

void initSkylightBindings(void*)
{
  class_<BND_File3dmSkylight>("Skylight")
    .constructor<>()
    .constructor<const BND_File3dmSkylight&>()
    .property("enabled", &BND_File3dmSkylight::GetEnabled, &BND_File3dmSkylight::SetEnabled)
    .property("shadowIntensity", &BND_File3dmSkylight::GetShadowIntensity, &BND_File3dmSkylight::SetShadowIntensity)
    ;
}
#endif
