#include "bindings.h"

BND_TUPLE BND_Intersection::LineLine(const ON_Line& lineA, const ON_Line& lineB)
{
  double a = 0;
  double b = 0;
  bool success = ON_Intersect(lineA, lineB, &a, &b);
#if defined(ON_PYTHON_COMPILE)
  pybind11::tuple rc(3);
  rc[0] = success;
  rc[1] = a;
  rc[2] = b;
#endif
#if defined(ON_WASM_COMPILE)
  emscripten::val rc(emscripten::val::array());
  rc.set(0, success);
  rc.set(1, a);
  rc.set(2, b);
#endif
  return rc;
}

BND_TUPLE BND_Intersection::LinePlane(const ON_Line& line, const BND_Plane& plane)
{
  double a = 0;
  bool success = ON_Intersect(line, plane.ToOnPlane(), &a);
#if defined(ON_PYTHON_COMPILE)
  pybind11::tuple rc(2);
  rc[0] = success;
  rc[1] = a;
#endif
#if defined(ON_WASM_COMPILE)
  emscripten::val rc(emscripten::val::array());
  rc.set(0, success);
  rc.set(1, a);
#endif
  return rc;
}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initIntersectBindings(pybind11::module& m)
{
  py::class_<BND_Intersection>(m, "Intersection")
    .def_static("LineLine", &BND_Intersection::LineLine, py::arg("lineA"), py::arg("lineB"))
    .def_static("LinePlane", &BND_Intersection::LinePlane, py::arg("line"), py::arg("plane"))
    ;

}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initIntersectBindings(void*)
{
  class_<BND_Intersection>("Intersection")
    .class_function("lineLine", &BND_Intersection::LineLine)
    .class_function("linePlane", &BND_Intersection::LinePlane)
    ;
}
#endif
