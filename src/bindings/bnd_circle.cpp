#include "bindings.h"


BND_Circle::BND_Circle(double radius)
{
  m_circle.radius = radius;
}
BND_Circle::BND_Circle(const BND_Plane& plane, double radius)
{
  m_circle.plane = plane.ToOnPlane();
  m_circle.radius = radius;
  m_circle.plane.UpdateEquation();
}
BND_Circle::BND_Circle(ON_3dPoint center, double radius)
{
  m_circle.plane.origin = center;
  m_circle.radius = radius;
  m_circle.plane.UpdateEquation();
}

ON_3dPoint BND_Circle::PointAt(double t) const
{
  return m_circle.PointAt(t);
}

BND_NurbsCurve* BND_Circle::ToNurbsCurve() const
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if( 0==m_circle.GetNurbForm(*nc) )
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initCircleBindings(pybind11::module& m)
{
  py::class_<BND_Circle>(m, "Circle")
    .def(py::init<double>())
    .def(py::init<ON_3dPoint, double>())
    .def_property_readonly("IsValid", &BND_Circle::IsValid)
    .def_property_readonly("Radius", &BND_Circle::Radius)
    .def_property_readonly("Diameter", &BND_Circle::Diameter)
    .def_property_readonly("Center", &BND_Circle::Center)
    .def_property_readonly("Normal", &BND_Circle::Normal)
    .def_property_readonly("Circumference", &BND_Circle::Circumference)
    .def("PointAt", &BND_Circle::PointAt)
    .def("TangentAt", &BND_Circle::TangentAt)
    .def("DerivativeAt", &BND_Circle::DerivativeAt)
    .def("ClosestPoint", &BND_Circle::ClosestPoint)
    .def("Translate", &BND_Circle::Translate)
    .def("Reverse", &BND_Circle::Reverse)
    .def("ToNurbsCurve", &BND_Circle::ToNurbsCurve);
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCircleBindings(void*)
{
  class_<BND_Circle>("Circle")
    .constructor<double>()
    .constructor<ON_3dPoint, double>()
    .property("isValid", &BND_Circle::IsValid)
    .property("radius", &BND_Circle::Radius)
    .property("diameter", &BND_Circle::Diameter)
    .property("center", &BND_Circle::Center)
    .property("normal", &BND_Circle::Normal)
    .property("circumference", &BND_Circle::Circumference)
    .function("pointAt", &BND_Circle::PointAt)
    .function("tangentAt", &BND_Circle::TangentAt)
    .function("derivativeAt", &BND_Circle::DerivativeAt)
    .function("closestPoint", &BND_Circle::ClosestPoint)
    .function("translate", &BND_Circle::Translate)
    .function("reverse", &BND_Circle::Reverse)
    .function("toNurbsCurve", &BND_Circle::ToNurbsCurve, allow_raw_pointers())
    ;
}
#endif
