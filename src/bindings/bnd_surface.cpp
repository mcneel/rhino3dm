#include "bindings.h"

BND_Surface::BND_Surface()
{

}

BND_Surface::BND_Surface(ON_Surface* surface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(surface, compref);
}

void BND_Surface::SetTrackedPointer(ON_Surface* surface, const ON_ModelComponentReference* compref)
{
  m_surface = surface;
  BND_Geometry::SetTrackedPointer(surface, compref);
}



#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSurfaceBindings(pybind11::module& m)
{
  py::class_<BND_Surface, BND_Geometry>(m, "Surface")
    .def_property_readonly("IsSolid", &BND_Surface::IsSolid)
    .def("Degree", &BND_Surface::Degree)
    .def("SpanCount", &BND_Surface::SpanCount)
    .def("PointAt", &BND_Surface::PointAt)
    .def("NormalAt", &BND_Surface::NormalAt)
    .def("IsClosed", &BND_Surface::IsClosed)
    .def("IsPeriodic", &BND_Surface::IsPeriodic)
    .def("IsSingular", &BND_Surface::IsSingular)
    .def("IsAtSingularity", &BND_Surface::IsAtSingularity)
    .def("IsAtSeam", &BND_Surface::IsAtSeam)
    .def("IsPlanar", &BND_Surface::IsPlanar)
    .def("IsSphere", &BND_Surface::IsSphere)
    .def("IsCylinder", &BND_Surface::IsCylinder)
    .def("IsCone", &BND_Surface::IsCone)
    .def("IsTorus", &BND_Surface::IsTorus)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSurfaceBindings(void*)
{
  class_<BND_Surface, base<BND_Geometry>>("Surface")
    ;
}
#endif
