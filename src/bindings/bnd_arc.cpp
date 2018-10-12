#include "bindings.h"

BND_Arc::BND_Arc(ON_3dPoint a, double b, double c) : ON_Arc(a,b,c)
{
}

BND_NurbsCurve* BND_Arc::ToNurbsCurve()
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if(0==GetNurbForm(*nc))
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initArcBindings(pybind11::module& m)
{
  py::class_<BND_Arc>(m, "Arc")
    .def(py::init<ON_3dPoint, double, double>())
    .def("ToNurbsCurve", &BND_Arc::ToNurbsCurve);
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initArcBindings(void*)
{
  class_<BND_Arc>("Arc")
    .constructor<ON_3dPoint, double, double>()
    .function("toNurbsCurve", &BND_Arc::ToNurbsCurve, allow_raw_pointers());
}
#endif
