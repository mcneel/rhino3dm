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


////////////////////////////////////

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
  if (0 == arc.m_arc.GetNurbForm(*nc))
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

BND_NurbsCurve* BND_NurbsCurve::CreateFromCircle(const BND_Circle& circle)
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if (0 == circle.m_circle.GetNurbForm(*nc))
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

BND_NurbsCurve* BND_NurbsCurve::CreateFromEllipse(const class BND_Ellipse& ellipse)
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if (0 == ellipse.m_ellipse.GetNurbForm(*nc))
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

BND_NurbsCurve* BND_NurbsCurve::Create(bool periodic, int degree, const BND_Point3dList& points)
{
  if (degree < 1)
    return nullptr;

  const int dimension = 3;
  const double knot_delta = 1.0;
  int order = degree + 1;
  int count = points.GetCount();
  if( count < 2 )
    return nullptr;
  const ON_3dPoint* point_array = points.m_polyline.Array();

  ON_NurbsCurve* nc = new ON_NurbsCurve();
  bool rc = periodic ? nc->CreatePeriodicUniformNurbs(dimension, order, count, point_array, knot_delta) :
    nc->CreateClampedUniformNurbs(dimension, order, count, point_array, knot_delta);

  if (false == rc)
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
    .def_property_readonly("ControlPolygonLength", &BND_NurbsCurvePointList::ControlPolygonLength)
    .def("ChangeEndWeights", &BND_NurbsCurvePointList::ChangeEndWeights, py::arg("w0"), py::arg("w1"))
    .def("MakeRational", &BND_NurbsCurvePointList::MakeRational)
    .def("MakeNonRational", &BND_NurbsCurvePointList::MakeNonRational)
    ;

  py::class_<BND_NurbsCurve, BND_Curve>(m, "NurbsCurve")
    .def_static("CreateFromLine", &BND_NurbsCurve::CreateFromLine, py::arg("line"))
    .def_static("CreateFromArc", &BND_NurbsCurve::CreateFromArc, py::arg("arc"))
    .def_static("CreateFromCircle", &BND_NurbsCurve::CreateFromCircle, py::arg("circle"))
    .def_static("CreateFromEllipse", &BND_NurbsCurve::CreateFromEllipse, py::arg("ellipse"))
    .def_static("Create", &BND_NurbsCurve::Create, py::arg("periodic"), py::arg("degree"), py::arg("points"))
    .def(py::init<int, int>(), py::arg("degree"), py::arg("pointcount"))
    .def(py::init<int, bool, int, int>(), py::arg("dimension"), py::arg("rational"), py::arg("order"), py::arg("pointcount"))
    .def_property_readonly("Order", &BND_NurbsCurve::Order)
    .def_property_readonly("IsRational", &BND_NurbsCurve::IsRational)
    .def("IncreaseDegree", &BND_NurbsCurve::IncreaseDegree, py::arg("desiredDegree"))
    .def_property_readonly("HasBezierSpans", &BND_NurbsCurve::HasBezierSpans)
    .def("MakePiecewiseBezier", &BND_NurbsCurve::MakePiecewiseBezier, py::arg("setEndWeightsToOne"))
    .def("Reparameterize", &BND_NurbsCurve::Reparameterize)
    .def("GrevilleParameter", &BND_NurbsCurve::GrevilleParameter, py::arg("index"))
    .def("GrevillePoint", &BND_NurbsCurve::GrevillePoint, py::arg("index"))
    .def_property_readonly("Points", &BND_NurbsCurve::Points)
    .def_property_readonly("Knots", &BND_NurbsCurve::Knots)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initNurbsCurveBindings(void*)
{
  class_<BND_NurbsCurveKnotList>("NurbsCurveKnotList")
    .property("count", &BND_NurbsCurveKnotList::Count)
    .function("get", &BND_NurbsCurveKnotList::GetKnot)
    .function("set", &BND_NurbsCurveKnotList::SetKnot)
    .function("insertKnot", &BND_NurbsCurveKnotList::InsertKnot)
    .function("knotMultiplicity", &BND_NurbsCurveKnotList::KnotMultiplicity)
    .function("createUniformKnots", &BND_NurbsCurveKnotList::CreateUniformKnots)
    .function("createPeriodicKnots", &BND_NurbsCurveKnotList::CreatePeriodicKnots)
    .property("isClampedStart", &BND_NurbsCurveKnotList::IsClampedStart)
    .property("isClampedEnd", &BND_NurbsCurveKnotList::IsClampedEnd)
    .function("superfluousKnot", &BND_NurbsCurveKnotList::SuperfluousKnot)
    ;
  ;

  class_<BND_NurbsCurvePointList>("NurbsCurvePointList")
    .property("count", &BND_NurbsCurvePointList::Count)
    .function("get", &BND_NurbsCurvePointList::GetControlPoint)
    .function("set", &BND_NurbsCurvePointList::SetControlPoint)
    .property("controlPolygonLength", &BND_NurbsCurvePointList::ControlPolygonLength)
    .function("changeEndWeights", &BND_NurbsCurvePointList::ChangeEndWeights)
    .function("makeRational", &BND_NurbsCurvePointList::MakeRational)
    .function("makeNonRational", &BND_NurbsCurvePointList::MakeNonRational)
    ;


  class_<BND_NurbsCurve, base<BND_Curve>>("NurbsCurve")
    .class_function("createFromLine", &BND_NurbsCurve::CreateFromLine, allow_raw_pointers())
    .class_function("createFromArc", &BND_NurbsCurve::CreateFromArc, allow_raw_pointers())
    .class_function("createFromCircle", &BND_NurbsCurve::CreateFromCircle, allow_raw_pointers())
    .class_function("createFromEllipse", &BND_NurbsCurve::CreateFromEllipse, allow_raw_pointers())
    .class_function("create", &BND_NurbsCurve::Create, allow_raw_pointers())
    .constructor<int, int>()
    .constructor<int, bool, int, int>()
    .property("order", &BND_NurbsCurve::Order)
    .property("isRational", &BND_NurbsCurve::IsRational)
    .function("increaseDegree", &BND_NurbsCurve::IncreaseDegree)
    .property("hasBezierSpans", &BND_NurbsCurve::HasBezierSpans)
    .function("makePiecewiseBezier", &BND_NurbsCurve::MakePiecewiseBezier)
    .function("reparameterize", &BND_NurbsCurve::Reparameterize)
    .function("grevilleParameter", &BND_NurbsCurve::GrevilleParameter)
    .function("grevillePoint", &BND_NurbsCurve::GrevillePoint)
    .function("points", &BND_NurbsCurve::Points, allow_raw_pointers())
    .function("knots", &BND_NurbsCurve::Knots, allow_raw_pointers())
    ;
}

#endif
