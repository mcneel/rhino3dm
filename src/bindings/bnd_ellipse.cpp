#include "bindings.h"


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initEllipseBindings(pybind11::module& m)
{
  py::class_<BND_Ellipse>(m, "Ellipse")
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initEllipseBindings(void*)
{
  class_<BND_Ellipse>("Ellipse")
    ;
}
#endif
