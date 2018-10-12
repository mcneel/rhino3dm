#include "bindings.h"

BND_Sphere::BND_Sphere(ON_3dPoint center, double radius)
: ON_Sphere(center, radius)
{
}

BND_Brep* BND_Sphere::ToBrep()
{
  ON_Brep* brep = ON_BrepSphere(*this);
  if(brep)
    return new BND_Brep(brep, nullptr);
  return nullptr;
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSphereBindings(pybind11::module& m)
{
  py::class_<BND_Sphere>(m, "Sphere")
    .def(py::init<ON_3dPoint, double>())
    .def_property_readonly("Center", &BND_Sphere::Center)
    .def_property_readonly("Radius", &BND_Sphere::Radius)
    .def("ToBrep", &BND_Sphere::ToBrep);
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSphereBindings(void*)
{
  class_<BND_Sphere>("Sphere")
    .constructor<ON_3dPoint,double>()
    .property("center", &BND_Sphere::Center)
    .property("radius", &BND_Sphere::Radius)
    .function("toBrep", &BND_Sphere::ToBrep, allow_raw_pointers());
}
#endif
