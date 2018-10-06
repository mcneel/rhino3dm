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
  BND_Geometry::SetTrackedPointer(curve, compref);
}

void BND_Curve::SetDomain(const BND_Interval& i)
{
  m_curve->SetDomain(i.m_t0, i.m_t1);
}

BND_Interval BND_Curve::GetDomain() const
{
  return BND_Interval(m_curve->Domain());
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initCurveBindings(pybind11::module& m)
{
  py::class_<BND_Curve, BND_Geometry>(m, "Curve")
    .def_property("Domain", &BND_Curve::GetDomain, &BND_Curve::SetDomain)
    .def_property_readonly("Dimension", &BND_Geometry::Dimension)
    .def("ChangeDimension", &BND_Curve::ChangeDimension)
    .def_property_readonly("SpanCount", &BND_Curve::SpanCount)
    .def_property_readonly("Degree", &BND_Curve::Degree)
    .def("IsLinear", &BND_Curve::IsLinear)
    .def_property_readonly("IsPolyline", &BND_Curve::IsPolyline)
    .def("IsArc", &BND_Curve::IsArc)
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
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCurveBindings()
{
  class_<BND_Curve, base<BND_Geometry>>("Curve")
    .property("domain", &BND_Curve::GetDomain, &BND_Curve::SetDomain)
    .property("dimension", &BND_Geometry::Dimension)
    .function("changeDimension", &BND_Curve::ChangeDimension)
    .property("spanCount", &BND_Curve::SpanCount)
    .property("degree", &BND_Curve::Degree)
    .function("isLinear", &BND_Curve::IsLinear)
    .property("isPolyline", &BND_Curve::IsPolyline)
    .function("isArc", &BND_Curve::IsArc)
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
    ;
}
#endif
