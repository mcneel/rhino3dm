#include "bindings.h"

BND_Curve::BND_Curve()
{

}

BND_Curve::BND_Curve(ON_Curve* curve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(curve, compref);
}

void BND_Curve::SetTrackedPointer(ON_Curve* curve, const ON_ModelComponentReference* compref)
{
  m_curve = curve;
  BND_GeometryBase::SetTrackedPointer(curve, compref);
}

void BND_Curve::SetDomain(const BND_Interval& i)
{
  m_curve->SetDomain(i.m_t0, i.m_t1);
}

BND_Interval BND_Curve::GetDomain() const
{
  return BND_Interval(m_curve->Domain());
}

BND_Polyline* BND_Curve::TryGetPolyline() const
{
  ON_SimpleArray<ON_3dPoint> pts;
  if (m_curve->IsPolyline(&pts) > 1)
  {
    BND_Polyline* rc = new BND_Polyline();
    rc->m_polyline = pts;
    return rc;
  }
  return nullptr;
}

BND_Arc* BND_Curve::TryGetArc(double tolerance) const
{
  ON_Arc arc;
  if (m_curve->IsArc(nullptr, &arc, tolerance))
  {
    BND_Arc* rc = new BND_Arc(arc);
    return rc;
  }
  return nullptr;
}

bool BND_Curve::IsCircle(double tolerance) const
{
  ON_Arc arc;
  if (m_curve->IsArc(nullptr, &arc, tolerance))
  {
    return arc.IsCircle();
  }
  return false;
}

BND_Circle* BND_Curve::TryGetCircle(double tolerance) const
{
  ON_Arc arc;
  if (m_curve->IsArc(nullptr, &arc, tolerance) && arc.IsCircle())
  {
    BND_Circle* rc = new BND_Circle(0);
    rc->m_circle = arc;
    return rc;
  }
  return false;
}

BND_Curve* BND_Curve::Trim(double t0, double t1) const
{
  ON_Curve* crv = m_curve->DuplicateCurve();
  if (!crv->Trim(ON_Interval(t0, t1)))
  {
    delete crv;
    return nullptr;
  }
  BND_Curve* rc = dynamic_cast<BND_Curve*>(BND_CommonObject::CreateWrapper(crv, nullptr));
  return rc;
}

BND_NurbsCurve* BND_Curve::ToNurbsCurve() const
{
  ON_NurbsCurve* nc = m_curve->NurbsCurve();
  if (nullptr == nc)
    return nullptr;
  return new BND_NurbsCurve(nc, nullptr);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initCurveBindings(pybind11::module& m)
{
  py::class_<BND_Curve, BND_GeometryBase>(m, "Curve")
    .def_property("Domain", &BND_Curve::GetDomain, &BND_Curve::SetDomain)
    .def_property_readonly("Dimension", &BND_GeometryBase::Dimension)
    .def("ChangeDimension", &BND_Curve::ChangeDimension)
    .def_property_readonly("SpanCount", &BND_Curve::SpanCount)
    .def_property_readonly("Degree", &BND_Curve::Degree)
    .def("IsLinear", &BND_Curve::IsLinear)
    .def("IsPolyline", &BND_Curve::IsPolyline)
    .def("TryGetPolyline", &BND_Curve::TryGetPolyline)
    .def("IsArc", &BND_Curve::IsArc)
    .def("TryGetArc", &BND_Curve::TryGetArc)
    .def("IsCircle", &BND_Curve::IsCircle)
    .def("TryGetCircle", &BND_Curve::TryGetCircle)
    .def("IsEllipse", &BND_Curve::IsEllipse)
    .def("IsPlanar", &BND_Curve::IsPlanar)
    .def("ChangeClosedCurveSeam", &BND_Curve::ChangeClosedCurveSeam)
    .def_property_readonly("IsClosed", &BND_Curve::IsClosed)
    .def_property_readonly("IsPeriodic", &BND_Curve::IsPeriodic)
    .def("Reverse", &BND_Curve::Reverse)
    .def("PointAt", &BND_Curve::PointAt)
    .def_property_readonly("PointAtStart", &BND_Curve::PointAtStart)
    .def_property_readonly("PointAtEnd", &BND_Curve::PointAtEnd)
    .def("SetStartPoint", &BND_Curve::SetStartPoint)
    .def("SetEndPoint", &BND_Curve::SetEndPoint)
    .def("TangentAt", &BND_Curve::TangentAt)
    .def("CurvatureAt", &BND_Curve::CurvatureAt)
    .def("Trim", &BND_Curve::Trim)
    .def("ToNurbsCurve", &BND_Curve::ToNurbsCurve)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCurveBindings(void*)
{
  class_<BND_Curve, base<BND_GeometryBase>>("Curve")
    .property("domain", &BND_Curve::GetDomain, &BND_Curve::SetDomain)
    .property("dimension", &BND_GeometryBase::Dimension)
    .function("changeDimension", &BND_Curve::ChangeDimension)
    .property("spanCount", &BND_Curve::SpanCount)
    .property("degree", &BND_Curve::Degree)
    .function("isLinear", &BND_Curve::IsLinear)
    .function("isPolyline", &BND_Curve::IsPolyline)
    .function("tryGetPolyline", &BND_Curve::TryGetPolyline, allow_raw_pointers())
    .function("isArc", &BND_Curve::IsArc)
    .function("tryGetArc", &BND_Curve::TryGetArc, allow_raw_pointers())
    .function("isCircle", &BND_Curve::IsCircle)
    .function("tryGetCircle", &BND_Curve::TryGetCircle, allow_raw_pointers())
    .function("isEllipse", &BND_Curve::IsEllipse)
    .function("isPlanar", &BND_Curve::IsPlanar)
    .function("changeClosedCurveSeam", &BND_Curve::ChangeClosedCurveSeam)
    .property("isClosed", &BND_Curve::IsClosed)
    .property("isPeriodic", &BND_Curve::IsPeriodic)
    .function("reverse", &BND_Curve::Reverse)
    .function("pointAt", &BND_Curve::PointAt)
    .property("pointAtStart", &BND_Curve::PointAtStart)
    .property("pointAtEnd", &BND_Curve::PointAtEnd)
    .function("setStartPoint", &BND_Curve::SetStartPoint)
    .function("setEndPoint", &BND_Curve::SetEndPoint)
    .function("tangentAt", &BND_Curve::TangentAt)
    .function("curvatureAt", &BND_Curve::CurvatureAt)
    .function("trim", &BND_Curve::Trim, allow_raw_pointers())
    .function("toNurbsCurve", &BND_Curve::ToNurbsCurve, allow_raw_pointers())
    ;
}
#endif
