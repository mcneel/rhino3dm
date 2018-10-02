#include "bindings.h"

BND_Curve::BND_Curve()
{

}

BND_Curve::BND_Curve(ON_Curve* curve)
{
  m_curve.reset(curve);
  SetSharedCurvePointer(m_curve);
}

void BND_Curve::SetSharedCurvePointer(const std::shared_ptr<ON_Curve>& sp)
{
  m_curve = sp;
  SetSharedGeometryPointer(sp);
}

void BND_Curve::SetDomain(const BND_Interval& i)
{
  m_curve->SetDomain(i.m_t0, i.m_t1);
}

BND_Interval BND_Curve::GetDomain() const
{
  return BND_Interval(m_curve->Domain());
}

bool BND_Curve::ChangeDimension(int desiredDimension)
{
  return m_curve->ChangeDimension(desiredDimension);
}

int BND_Curve::SpanCount() const
{
  return m_curve->SpanCount();
}

int BND_Curve::Degree() const
{
  return m_curve->Degree();
}

bool BND_Curve::IsLinear(double tolerance) const
{
  return m_curve->IsLinear(tolerance);
}

bool BND_Curve::IsPolyline() const
{
  return m_curve->IsPolyline();
}

ON_3dPoint BND_Curve::PointAtStart() const
{
  return m_curve->PointAtStart();
}

ON_3dPoint BND_Curve::PointAtEnd() const
{
  return m_curve->PointAtEnd();
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
    .def_property_readonly("PointAtStart", &BND_Curve::PointAtStart)
    .def_property_readonly("PointAtEnd", &BND_Curve::PointAtEnd)
    .def_property_readonly("IsLinear", &BND_Curve::IsLinear)
    .def_property_readonly("IsPolyline", &BND_Curve::IsPolyline);
}
#endif
