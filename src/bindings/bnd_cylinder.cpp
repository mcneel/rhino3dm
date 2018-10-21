#include "bindings.h"


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initCylinderBindings(pybind11::module& m)
{
  py::class_<BND_Cylinder>(m, "Cylinder")
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCylinderBindings(void*)
{
  class_<BND_Cylinder>("Cylinder")
    ;
}
#endif
