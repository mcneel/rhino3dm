#include "bindings.h"


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initConeBindings(pybind11::module& m)
{
  py::class_<BND_Cone>(m, "Cone")
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initConeBindings(void*)
{
  class_<BND_Cone>("Cone")
    ;
}
#endif
