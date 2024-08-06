#include "bindings.h"



#if defined(ON_PYTHON_COMPILE)

void initRTreeBindings(rh3dmpymodule& m)
{
  //py::class_<BND_RTree>(m, "RTree")
  //  .def(py::init<>())
  //  ;
}

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
