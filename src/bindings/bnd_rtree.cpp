#include "bindings.h"



#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initRTreeBindings(py::module_& m){}
#else
namespace py = pybind11;
void initRTreeBindings(py::module& m)
{
  //py::class_<BND_RTree>(m, "RTree")
  //  .def(py::init<>())
  //  ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initRTreeBindings(void*)
{
  //class_<BND_RTree>("RTree")
  //  .constructor<>()
  //  ;
}
#endif
