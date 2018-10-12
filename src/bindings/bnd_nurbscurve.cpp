#include "bindings.h"

BND_NurbsCurvePointList::BND_NurbsCurvePointList(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_nurbs_curve = nurbscurve;
}

ON_4dPoint BND_NurbsCurvePointList::GetControlPoint(int index) const
{
  ON_4dPoint pt;
  m_nurbs_curve->GetCV(index, pt);
  return pt;
}

void BND_NurbsCurvePointList::SetControlPoint(int index, ON_4dPoint point)
{
  m_nurbs_curve->SetCV(index, point);
}


BND_NurbsCurveKnotList::BND_NurbsCurveKnotList(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_nurbs_curve = nurbscurve;
}


BND_NurbsCurve::BND_NurbsCurve(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(nurbscurve, compref);
}

BND_NurbsCurve::BND_NurbsCurve(int degree, int pointCount)
{
  ON_NurbsCurve* nurbscurve = ON_NurbsCurve::New(3, false, degree+1, pointCount);
  SetTrackedPointer(nurbscurve, nullptr);
}

BND_NurbsCurve::BND_NurbsCurve(int dimension, bool rational, int order, int pointCount)
{
  ON_NurbsCurve* nurbscurve = ON_NurbsCurve::New(dimension, rational, order, pointCount);
  SetTrackedPointer(nurbscurve, nullptr);
}

void BND_NurbsCurve::SetTrackedPointer(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference* compref)
{
  m_nurbscurve = nurbscurve;
  BND_Curve::SetTrackedPointer(nurbscurve, compref);
}

BND_NurbsCurve* BND_NurbsCurve::CreateFromLine(const ON_Line& line)
{
  if (!line.IsValid())
    return nullptr;

  ON_NurbsCurve* crv = new ON_NurbsCurve(3, false, 2, 2);
  crv->SetCV(0, line.from);
  crv->SetCV(1, line.to);
  crv->MakeClampedUniformKnotVector(1.0);
  return new BND_NurbsCurve(crv, nullptr);
}

BND_NurbsCurve* BND_NurbsCurve::CreateFromArc(const BND_Arc& arc)
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if (0 == arc.GetNurbForm(*nc))
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

BND_NurbsCurve* BND_NurbsCurve::CreateFromCircle(const BND_Circle& circle)
{
  ON_Circle c = circle.ToONCircle();
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if (0 == c.GetNurbForm(*nc))
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

ON_3dPoint BND_NurbsCurve::GrevillePoint(int index) const
{
  double t = GrevilleParameter(index);
  return PointAt(t);
}

BND_NurbsCurveKnotList BND_NurbsCurve::Knots()
{
  return BND_NurbsCurveKnotList(m_nurbscurve, m_component_ref);
}
BND_NurbsCurvePointList BND_NurbsCurve::Points()
{
  return BND_NurbsCurvePointList(m_nurbscurve, m_component_ref);
}

///////////////////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initNurbsCurveBindings(pybind11::module& m)
{
  py::class_<BND_NurbsCurveKnotList>(m, "NurbsCurveKnotList")
    .def("__len__", &BND_NurbsCurveKnotList::Count)
    .def("__getitem__", &BND_NurbsCurveKnotList::GetKnot)
    .def("__setitem__", &BND_NurbsCurveKnotList::SetKnot)
    .def("InsertKnot", &BND_NurbsCurveKnotList::InsertKnot)
    .def("KnotMultiplicity", &BND_NurbsCurveKnotList::KnotMultiplicity)
    .def("CreateUniformKnots", &BND_NurbsCurveKnotList::CreateUniformKnots)
    .def("CreatePeriodicKnots", &BND_NurbsCurveKnotList::CreatePeriodicKnots)
    .def_property_readonly("IsClampedStart", &BND_NurbsCurveKnotList::IsClampedStart)
    .def_property_readonly("IsClampedEnd", &BND_NurbsCurveKnotList::IsClampedEnd)
    .def("SuperfluousKnot", &BND_NurbsCurveKnotList::SuperfluousKnot)
    ;
  ;

  py::class_<BND_NurbsCurvePointList>(m, "NurbsCurvePointList")
    .def("__len__", &BND_NurbsCurvePointList::Count)
    .def("__getitem__", &BND_NurbsCurvePointList::GetControlPoint)
    .def("__setitem__", &BND_NurbsCurvePointList::SetControlPoint)
    ;

  py::class_<BND_NurbsCurve, BND_Curve>(m, "NurbsCurve")
    .def_static("CreateFromLine", &BND_NurbsCurve::CreateFromLine)
    .def_static("CreateFromArc", &BND_NurbsCurve::CreateFromArc)
    .def_static("CreateFromCircle", &BND_NurbsCurve::CreateFromCircle)
    .def(py::init<int, int>())
    .def(py::init<int, bool, int, int>())
    .def_property_readonly("Order", &BND_NurbsCurve::Order)
    .def_property_readonly("IsRational", &BND_NurbsCurve::IsRational)
    .def("IncreaseDegree", &BND_NurbsCurve::IncreaseDegree)
    .def_property_readonly("HasBezierSpans", &BND_NurbsCurve::HasBezierSpans)
    .def("MakePiecewiseBezier", &BND_NurbsCurve::MakePiecewiseBezier)
    .def("Reparameterize", &BND_NurbsCurve::Reparameterize)
    .def("GrevilleParameter", &BND_NurbsCurve::GrevilleParameter)
    .def("GrevillePoint", &BND_NurbsCurve::GrevillePoint)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initNurbsCurveBindings(void*)
{
  class_<BND_NurbsCurve, base<BND_Curve>>("NurbsCurve")
    .constructor<int, int>()
    .constructor<int, bool, int, int>();
}

#endif
