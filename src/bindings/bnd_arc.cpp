#include "bindings.h"

BND_Arc::BND_Arc(const BND_Circle& circle, double angleRadians)
{
  m_arc = ON_Arc(circle.m_circle, angleRadians);
  m_arc.plane.UpdateEquation();
}

BND_Arc::BND_Arc(ON_3dPoint center, double radius, double angleRadians)
  : m_arc(center, radius, angleRadians)
{
  m_arc.plane.UpdateEquation();
}

BND_Arc::BND_Arc(ON_3dPoint startPoint, ON_3dPoint pointOnInterior, ON_3dPoint endPoint)
  : m_arc(startPoint, pointOnInterior,endPoint)
{
  m_arc.plane.UpdateEquation();
}

BND_Arc::BND_Arc(ON_3dPoint pointA, ON_3dVector tangentA, ON_3dPoint pointB)
  : m_arc(pointA, tangentA, pointB)
{
  m_arc.plane.UpdateEquation();
}

void BND_Arc::SetCenter(ON_3dPoint pt)
{
  m_arc.plane.SetOrigin(pt);
  m_arc.plane.UpdateEquation();
}

BND_Interval BND_Arc::AngleDomain() const
{
  BND_Interval rc;
  rc.m_t0 = m_arc.Domain().m_t[0];
  rc.m_t1 = m_arc.Domain().m_t[1];
  return rc;
}

void BND_Arc::SetAngleDomain(BND_Interval interval)
{
  m_arc.SetAngleIntervalRadians(ON_Interval(interval.m_t0, interval.m_t1));
}

double BND_Arc::StartAngle() const
{
  return m_arc.Domain().m_t[0];
}

void BND_Arc::SetStartAngle(double t)
{
  ON_Interval domain = m_arc.Domain();
  domain.m_t[0] = t;
  m_arc.SetAngleIntervalRadians(domain);
}

double BND_Arc::EndAngle() const
{
  return m_arc.Domain().m_t[1];
}

void BND_Arc::SetEndAngle(double t)
{
  ON_Interval domain = m_arc.Domain();
  domain.m_t[1] = t;
  m_arc.SetAngleIntervalRadians(domain);
}

double BND_Arc::StartAngleDegrees() const
{
  return m_arc.DomainDegrees().m_t[0];
}

void BND_Arc::SetStartAngleDegrees(double t)
{
  t = ON_RadiansFromDegrees(t);
  SetStartAngle(t);
}

double BND_Arc::EndAngleDegrees() const
{
  return m_arc.DomainDegrees().m_t[1];
}

void BND_Arc::SetEndAngleDegrees(double t)
{
  t = ON_RadiansFromDegrees(t);
  SetEndAngle(t);
}

bool BND_Arc::Trim(const BND_Interval& domain)
{
  return m_arc.Trim(ON_Interval(domain.m_t0, domain.m_t1));
}

BND_BoundingBox BND_Arc::BoundingBox() const
{
  ON_BoundingBox bbox = m_arc.BoundingBox();
  return BND_BoundingBox(bbox);
}

double BND_Arc::ClosestParameter(ON_3dPoint testPoint) const
{
  double t = 0;
  m_arc.ClosestPointTo(testPoint, &t);
  return t;
}

bool BND_Arc::Transform(const BND_Transform& xform)
{
  return m_arc.Transform(xform.m_xform);
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

void initArcBindings(rh3dmpymodule& m)
{
  py::class_<BND_Arc>(m, "Arc")
    .def(py::init<const BND_Circle&, double>(), py::arg("circle"), py::arg("angleRadians"))
    .def(py::init<ON_3dPoint, double, double>(), py::arg("center"), py::arg("radius"), py::arg("angleRadians"))
    .def(py::init<ON_3dPoint, ON_3dPoint, ON_3dPoint>(), py::arg("startPoint"), py::arg("pointOnInterior"), py::arg("endPoint"))
    .def(py::init<ON_3dPoint, ON_3dVector, ON_3dPoint>(), py::arg("pointA"), py::arg("tangentA"), py::arg("pointB"))
    .def_property_readonly("IsValid", &BND_Arc::IsValid)
    .def_property_readonly("IsCircle", &BND_Arc::IsCircle)
    .def_property("Radius", &BND_Arc::GetRadius, &BND_Arc::SetRadius)
    .def_property("Diameter", &BND_Arc::GetDiameter, &BND_Arc::SetDiameter)
    .def_property("Plane", &BND_Arc::GetPlane, &BND_Arc::SetPlane)
    .def_property("Center", &BND_Arc::GetCenter, &BND_Arc::SetCenter)
    .def_property_readonly("Circumference", &BND_Arc::Circumference)
    .def_property_readonly("Length", &BND_Arc::Length)
    .def_property_readonly("StartPoint", &BND_Arc::StartPoint)
    .def_property_readonly("MidPoint", &BND_Arc::MidPoint)
    .def_property_readonly("EndPoint", &BND_Arc::EndPoint)
    .def_property("AngleDomain", &BND_Arc::AngleDomain, &BND_Arc::SetAngleDomain)
    .def_property("StartAngle", &BND_Arc::StartAngle, &BND_Arc::SetStartAngle)
    .def_property("EndAngle", &BND_Arc::EndAngle, &BND_Arc::SetEndAngle)
    .def_property("AngleRadians", &BND_Arc::GetAngleRadians, &BND_Arc::SetAngleRadians)
    .def_property("StartAngleDegrees", &BND_Arc::StartAngleDegrees, &BND_Arc::SetStartAngleDegrees)
    .def_property("EndAngleDegrees", &BND_Arc::EndAngleDegrees, &BND_Arc::SetEndAngleDegrees)
    .def_property("AngleDegrees", &BND_Arc::GetAngleDegrees, &BND_Arc::SetAngleDegrees)
    .def("Trim", &BND_Arc::Trim, py::arg("domain"))
    .def("BoundingBox", &BND_Arc::BoundingBox)
    .def("PointAt", &BND_Arc::PointAt, py::arg("t"))
    .def("TangentAt", &BND_Arc::TangentAt, py::arg("t"))
    .def("ClosestParameter", &BND_Arc::ClosestParameter, py::arg("testPoint"))
    .def("ClosestPoint", &BND_Arc::ClosestPoint, py::arg("testPoint"))
    .def("Reverse", &BND_Arc::Reverse)
    .def("Transform", &BND_Arc::Transform, py::arg("xform"))
    .def("ToNurbsCurve", &BND_Arc::ToNurbsCurve);
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

static BND_Arc* JsConstructFromPoints(ON_3dPoint a, ON_3dPoint b, ON_3dPoint c) {
  return new BND_Arc(a,b,c);
  }

void initArcBindings(void*)
{
  class_<BND_Arc>("Arc")
    .constructor<const BND_Circle&, double>()
    .constructor<ON_3dPoint, double, double>()
    .class_function("createFromPoints", &JsConstructFromPoints, allow_raw_pointers())
//    .constructor<ON_3dPoint, ON_3dVector, ON_3dPoint>()
    .property("isValid", &BND_Arc::IsValid)
    .property("isCircle", &BND_Arc::IsCircle)
    .property("radius", &BND_Arc::GetRadius, &BND_Arc::SetRadius)
    .property("diameter", &BND_Arc::GetDiameter, &BND_Arc::SetDiameter)
    .property("plane", &BND_Arc::GetPlane, &BND_Arc::SetPlane)
    .property("center", &BND_Arc::GetCenter, &BND_Arc::SetCenter)
    .property("circumference", &BND_Arc::Circumference)
    .property("length", &BND_Arc::Length)
    .property("startPoint", &BND_Arc::StartPoint)
    .property("midPoint", &BND_Arc::MidPoint)
    .property("endPoint", &BND_Arc::EndPoint)
    .property("angleDomain", &BND_Arc::AngleDomain, &BND_Arc::SetAngleDomain)
    .property("startAngle", &BND_Arc::StartAngle, &BND_Arc::SetStartAngle)
    .property("endAngle", &BND_Arc::EndAngle, &BND_Arc::SetEndAngle)
    .property("angle", &BND_Arc::GetAngleRadians, &BND_Arc::SetAngleRadians)
    .property("startAngleDegrees", &BND_Arc::StartAngleDegrees, &BND_Arc::SetStartAngleDegrees)
    .property("endAngleDegrees", &BND_Arc::EndAngleDegrees, &BND_Arc::SetEndAngleDegrees)
    .property("angleDegrees", &BND_Arc::GetAngleDegrees, &BND_Arc::SetAngleDegrees)
    .function("trim", &BND_Arc::Trim)
    .function("boundingBox", &BND_Arc::BoundingBox)
    .function("pointAt", &BND_Arc::PointAt)
    .function("tangentAt", &BND_Arc::TangentAt)
    .function("closestParameter", &BND_Arc::ClosestParameter)
    .function("closestPoint", &BND_Arc::ClosestPoint)
    .function("reverse", &BND_Arc::Reverse)
    .function("transform", &BND_Arc::Transform)
    .function("toNurbsCurve", &BND_Arc::ToNurbsCurve, allow_raw_pointers());
}
#endif
