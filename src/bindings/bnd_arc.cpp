#include "bindings.h"

BND_Arc::BND_Arc(const BND_Circle& circle, double angleRadians)
{
  m_arc = ON_Arc(circle.ToONCircle(), angleRadians);
}

BND_Arc::BND_Arc(ON_3dPoint center, double radius, double angleRadians)
  : m_arc(center, radius, angleRadians)
{
}

BND_Arc::BND_Arc(ON_3dPoint startPoint, ON_3dPoint pointOnInterior, ON_3dPoint endPoint)
  : m_arc(startPoint, pointOnInterior,endPoint)
{
}

BND_Arc::BND_Arc(ON_3dPoint pointA, ON_3dVector tangentA, ON_3dPoint pointB)
  : m_arc(pointA, tangentA, pointB)
{
}


BND_NurbsCurve* BND_Arc::ToNurbsCurve() const
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if(0==m_arc.GetNurbForm(*nc))
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
    .def(py::init<const BND_Circle&, double>())
    .def(py::init<ON_3dPoint, double, double>())
    .def(py::init<ON_3dPoint, ON_3dPoint, ON_3dPoint>())
    .def(py::init<ON_3dPoint, ON_3dVector, ON_3dPoint>())
    .def_property_readonly("IsValid", &BND_Arc::IsValid)
    .def_property_readonly("IsCircle", &BND_Arc::IsCircle)
    .def_property("Radius", &BND_Arc::GetRadius, &BND_Arc::SetRadius)
    .def_property("Diameter", &BND_Arc::GetDiameter, &BND_Arc::SetDiameter)
    .def_property("Center", &BND_Arc::GetCenter, &BND_Arc::SetCenter)
    .def_property_readonly("Circumference", &BND_Arc::Circumference)
    .def_property_readonly("Length", &BND_Arc::Length)
    .def_property_readonly("StartPoint", &BND_Arc::StartPoint)
    .def_property_readonly("MidPoint", &BND_Arc::MidPoint)
    .def_property_readonly("EndPoint", &BND_Arc::EndPoint)
    .def("PointAt", &BND_Arc::PointAt)
    .def("TangentAt", &BND_Arc::TangentAt)
    .def("ClosestPoint", &BND_Arc::ClosestPoint)
    .def("Reverse", &BND_Arc::Reverse)
    .def("ToNurbsCurve", &BND_Arc::ToNurbsCurve);
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initArcBindings(void*)
{
  class_<BND_Arc>("Arc")
    .constructor<const BND_Circle&, double>()
    .constructor<ON_3dPoint, double, double>()
// the following won't work with the way we are currently defining Points/Vectors
//    .constructor<ON_3dPoint, ON_3dPoint, ON_3dPoint>()
//    .constructor<ON_3dPoint, ON_3dVector, ON_3dPoint>()
    .property("isValid", &BND_Arc::IsValid)
    .property("isCircle", &BND_Arc::IsCircle)
    .property("radius", &BND_Arc::GetRadius, &BND_Arc::SetRadius)
    .property("diameter", &BND_Arc::GetDiameter, &BND_Arc::SetDiameter)
    .property("center", &BND_Arc::GetCenter, &BND_Arc::SetCenter)
    .property("circumference", &BND_Arc::Circumference)
    .property("length", &BND_Arc::Length)
    .property("startPoint", &BND_Arc::StartPoint)
    .property("midPoint", &BND_Arc::MidPoint)
    .property("endPoint", &BND_Arc::EndPoint)
    .function("pointAt", &BND_Arc::PointAt)
    .function("tangentAt", &BND_Arc::TangentAt)
    .function("closestPoint", &BND_Arc::ClosestPoint)
    .function("reverse", &BND_Arc::Reverse)
    .function("toNurbsCurve", &BND_Arc::ToNurbsCurve, allow_raw_pointers());
}
#endif
