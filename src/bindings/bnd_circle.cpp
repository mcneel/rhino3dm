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

BND_Plane BND_Circle::Plane() const
{
  return BND_Plane::FromOnPlane(m_circle.plane);
}
void BND_Circle::SetPlane(BND_Plane& plane)
{
  m_circle.plane = plane.ToOnPlane();
}

void BND_Circle::SetCenter(ON_3dPoint center)
{
  m_circle.plane.origin = center;
  m_circle.plane.UpdateEquation();
}

BND_BoundingBox BND_Circle::BoundingBox() const
{
  return BND_BoundingBox(m_circle.BoundingBox());
}

bool BND_Circle::IsInPlane(BND_Plane plane, double tolerance) const
{
  ON_Plane pl = plane.ToOnPlane();
  return m_circle.IsInPlane(pl, tolerance);
}

ON_3dPoint BND_Circle::PointAt(double t) const
{
  return m_circle.PointAt(t);
}

BND_TUPLE BND_Circle::ClosestParameter(ON_3dPoint testPoint) const
{
  bool success = false;
  double t = 0;
  success = m_circle.ClosestPointTo(testPoint, &t);
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, t);
  return rc;
}

bool BND_Circle::Transform(BND_Transform xform)
{
  return m_circle.Transform(xform.m_xform);
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

void initCircleBindings(rh3dmpymodule& m)
{
  py::class_<BND_Circle>(m, "Circle")
    .def(py::init<double>(), py::arg("radius"))
    .def(py::init<ON_3dPoint, double>(), py::arg("center"), py::arg("radius"))
    .def_property_readonly("IsValid", &BND_Circle::IsValid)
    .def_property("Radius", &BND_Circle::Radius, &BND_Circle::SetRadius)
    .def_property("Diameter", &BND_Circle::Diameter, &BND_Circle::SetDiameter)
    .def_property("Plane", &BND_Circle::Plane, &BND_Circle::SetPlane)
    .def_property("Center", &BND_Circle::Center, &BND_Circle::SetCenter)
    .def_property_readonly("BoundingBox", &BND_Circle::BoundingBox)
    .def_property_readonly("Normal", &BND_Circle::Normal)
    .def_property_readonly("Circumference", &BND_Circle::Circumference)
    .def("IsInPlane", &BND_Circle::IsInPlane, py::arg("plane"), py::arg("tolerance"))
    .def("PointAt", &BND_Circle::PointAt, py::arg("t"))
    .def("TangentAt", &BND_Circle::TangentAt, py::arg("t"))
    .def("DerivativeAt", &BND_Circle::DerivativeAt, py::arg("derivative"), py::arg("t"))
    .def("ClosestParameter", &BND_Circle::ClosestParameter, py::arg("testPoint"))
    .def("ClosestPoint", &BND_Circle::ClosestPoint, py::arg("testPoint"))
    .def("Transform", &BND_Circle::Transform, py::arg("xform"))
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
    .property("plane", &BND_Circle::Plane, &BND_Circle::SetPlane)
    .property("center", &BND_Circle::Center, &BND_Circle::SetCenter)
    .property("boundingBox", &BND_Circle::BoundingBox)
    .property("normal", &BND_Circle::Normal)
    .property("circumference", &BND_Circle::Circumference)
    .function("isInPlane", &BND_Circle::IsInPlane)
    .function("pointAt", &BND_Circle::PointAt)
    .function("tangentAt", &BND_Circle::TangentAt)
    .function("derivativeAt", &BND_Circle::DerivativeAt)
    .function("closestParameter", &BND_Circle::ClosestParameter)
    .function("closestPoint", &BND_Circle::ClosestPoint)
    .function("transform", &BND_Circle::Transform)
    .function("translate", &BND_Circle::Translate)
    .function("reverse", &BND_Circle::Reverse)
    .function("toNurbsCurve", &BND_Circle::ToNurbsCurve, allow_raw_pointers())
    ;
}
#endif
