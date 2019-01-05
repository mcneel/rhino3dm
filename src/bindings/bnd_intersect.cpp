#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
pybind11::tuple BND_Intersection::LineLine(const ON_Line& lineA, const ON_Line& lineB)
{
  double a = 0;
  double b = 0;
  bool success = ON_Intersect(lineA, lineB, &a, &b);
  pybind11::tuple rc(3);
  rc[0] = success;
  rc[1] = a;
  rc[2] = b;
  return rc;
}

pybind11::tuple BND_Intersection::LinePlane(const ON_Line& line, const BND_Plane& plane)
{
  double a = 0;
  bool success = ON_Intersect(line, plane.ToOnPlane(), &a);
  pybind11::tuple rc(2);
  rc[0] = success;
  rc[1] = a;
  return rc;
}

#endif


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initIntersectBindings(pybind11::module& m)
{
  py::class_<BND_Intersection>(m, "Intersection")
    .def_static("LineLine", &BND_Intersection::LineLine)
    .def_static("LinePlane", &BND_Intersection::LinePlane)
    ;

}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initIntersectBindings(void*)
{
}
#endif
