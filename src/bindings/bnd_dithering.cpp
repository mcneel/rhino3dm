
#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDitheringBindings(pybind11::module& m)
{
  py::class_<BND_File3dmDithering>(m, "Dithering")
    .def(py::init<>())
    .def(py::init<const BND_File3dmDithering&>(), py::arg("other"))
    .def_property("Enabled", &BND_File3dmDithering::GetEnabled, &BND_File3dmDithering::SetEnabled)
    .def_property("Method", &BND_File3dmDithering::GetMethod, &BND_File3dmDithering::SetMethod)
   ;
}
#endif

#if defined(ON_WASM_COMPILE____TEMP)
using namespace emscripten;

void initDitheringBindings(void*)
{
  class_<BND_File3dmDithering>("Dithering")
    .constructor<>()
    .constructor<const BND_File3dmDithering&>()
    .property("enabled", &BND_File3dmDithering::GetEnabled, &BND_File3dmDithering::SetEnabled)
    .property("method", &BND_File3dmDithering::GetMethod, &BND_File3dmDithering::SetMethod)
    ;
}
#endif
