#include "bindings.h"


#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initEllipseBindings(py::module_& m){}
#else
namespace py = pybind11;
void initEllipseBindings(py::module& m)
{
  py::class_<BND_Ellipse>(m, "Ellipse")
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initEllipseBindings(void*)
{
  class_<BND_Ellipse>("Ellipse")
    ;
}
#endif
