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

void BND_Circle::SetCenter(ON_3dPoint center)
{
  m_circle.plane.origin = center;
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

BND_DICT BND_Circle::Encode() const
{
#if defined(ON_PYTHON_COMPILE)
  BND_DICT d;
#else
  emscripten::val d(emscripten::val::object());
#endif
#if defined(ON_PYTHON_COMPILE)
  d["radius"] = Radius();
  d["plane"] = PlaneToDict(m_circle.plane);
#else
  d.set("radius", emscripten::val(Radius()));
  d.set("plane", emscripten::val(PlaneToDict(m_circle.plane)));
#endif
  return d;
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initCircleBindings(pybind11::module& m)
{
  py::class_<BND_Circle>(m, "Circle")
    .def(py::init<double>(), py::arg("radius"))
    .def(py::init<ON_3dPoint, double>(), py::arg("center"), py::arg("radius"))
    .def_property_readonly("IsValid", &BND_Circle::IsValid)
    .def_property("Radius", &BND_Circle::Radius, &BND_Circle::SetRadius)
    .def_property("Diameter", &BND_Circle::Diameter, &BND_Circle::SetDiameter)
    .def_property("Center", &BND_Circle::Center, &BND_Circle::SetCenter)
    .def_property_readonly("Normal", &BND_Circle::Normal)
    .def_property_readonly("Circumference", &BND_Circle::Circumference)
    .def("PointAt", &BND_Circle::PointAt, py::arg("t"))
    .def("TangentAt", &BND_Circle::TangentAt, py::arg("t"))
    .def("DerivativeAt", &BND_Circle::DerivativeAt, py::arg("derivative"), py::arg("t"))
    .def("ClosestPoint", &BND_Circle::ClosestPoint, py::arg("testPoint"))
    .def("Translate", &BND_Circle::Translate, py::arg("delta"))
    .def("Reverse", &BND_Circle::Reverse)
    .def("ToNurbsCurve", &BND_Circle::ToNurbsCurve)
    .def("Encode", &BND_Circle::Encode)
    ;
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
    .property("radius", &BND_Circle::Radius, &BND_Circle::SetRadius)
    .property("diameter", &BND_Circle::Diameter, &BND_Circle::SetDiameter)
    .property("center", &BND_Circle::Center, &BND_Circle::SetCenter)
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
