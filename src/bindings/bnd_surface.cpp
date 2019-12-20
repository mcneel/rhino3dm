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
  BND_GeometryBase::SetTrackedPointer(surface, compref);
}

std::tuple<BND_NurbsSurface*, int> BND_Surface::ToNurbsSurface(double tolerance)
{
  int accuracy{0};
  ON_NurbsSurface* p_NurbForm = ON_NurbsSurface::New();
  if (m_surface != nullptr)
  {
    accuracy = m_surface->GetNurbForm(*p_NurbForm, tolerance);
    if (!accuracy)
    {
      delete p_NurbForm;
      p_NurbForm = nullptr;
    }
  }
  if (p_NurbForm == nullptr)
    return std::make_tuple(nullptr, 0);
  return std::make_tuple(new BND_NurbsSurface(p_NurbForm, &m_component_ref), accuracy);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSurfaceBindings(pybind11::module& m)
{
  py::class_<BND_Surface, BND_GeometryBase>(m, "Surface")
    .def_property_readonly("IsSolid", &BND_Surface::IsSolid)
    .def("Degree", &BND_Surface::Degree, py::arg("direction"))
    .def("SpanCount", &BND_Surface::SpanCount, py::arg("direction"))
    .def("PointAt", &BND_Surface::PointAt, py::arg("u"), py::arg("v"))
    .def("NormalAt", &BND_Surface::NormalAt, py::arg("u"), py::arg("v"))
    .def("IsClosed", &BND_Surface::IsClosed, py::arg("direction"))
    .def("IsPeriodic", &BND_Surface::IsPeriodic, py::arg("direction"))
    .def("IsSingular", &BND_Surface::IsSingular, py::arg("side"))
    .def("IsAtSingularity", &BND_Surface::IsAtSingularity, py::arg("u"), py::arg("v"), py::arg("exact"))
    .def("IsAtSeam", &BND_Surface::IsAtSeam, py::arg("u"), py::arg("v"))
    .def("ToNurbsSurface", &BND_Surface::ToNurbsSurfaceDefault)
    .def("ToNurbsSurface", &BND_Surface::ToNurbsSurface, py::arg("tolerance"))
    .def("IsPlanar", &BND_Surface::IsPlanar, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsSphere", &BND_Surface::IsSphere, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsCylinder", &BND_Surface::IsCylinder, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsCone", &BND_Surface::IsCone, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsTorus", &BND_Surface::IsTorus, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSurfaceBindings(void*)
{
  class_<BND_Surface, base<BND_GeometryBase>>("Surface")
    .property("isSolid", &BND_Surface::IsSolid)
    .function("degree", &BND_Surface::Degree)
    .function("spanCount", &BND_Surface::SpanCount)
    .function("pointAt", &BND_Surface::PointAt)
    .function("normalAt", &BND_Surface::NormalAt)
    .function("isClosed", &BND_Surface::IsClosed)
    .function("isPeriodic", &BND_Surface::IsPeriodic)
    .function("isSingular", &BND_Surface::IsSingular)
    .function("isAtSingularity", &BND_Surface::IsAtSingularity)
    .function("isAtSeam", &BND_Surface::IsAtSeam)
    .function("toNurbsSurface", &BND_Surface::ToNurbsSurfaceDefault, allow_raw_pointers())
    //.function("toNurbsSurface", &BND_Surface::ToNurbsSurface)
    .function("isPlanar", &BND_Surface::IsPlanar)
    .function("isSphere", &BND_Surface::IsSphere)
    .function("isCylinder", &BND_Surface::IsCylinder)
    .function("isCone", &BND_Surface::IsCone)
    .function("isTorus", &BND_Surface::IsTorus)
    ;
}
#endif
