#include "bindings.h"

BND_PolyCurve::BND_PolyCurve()
{
  SetTrackedPointer(new ON_PolyCurve(), nullptr);
}

BND_PolyCurve::BND_PolyCurve(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(polycurve, compref);
}

void BND_PolyCurve::SetTrackedPointer(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref)
{
  m_polycurve = polycurve;
  BND_Curve::SetTrackedPointer(polycurve, compref);
}

BND_Curve* BND_PolyCurve::SegmentCurve(int index) const
{
  ON_Curve* curve = m_polycurve->SegmentCurve(index);
  BND_Curve* rc = dynamic_cast<BND_Curve*>(BND_CommonObject::CreateWrapper(curve, &m_component_ref));
  return rc;
}

BND_TUPLE BND_PolyCurve::Explode() const
{

  int count =  SegmentCount();
  if( count > 0) {
    BND_TUPLE rc = CreateTuple(count);
    for (int i = 0; i < count; i++) {

      BND_Curve* curve = SegmentCurve(i);

      if (curve)
      {
        ON_Curve* crv = curve->m_curve->DuplicateCurve();
        SetTuple(rc, i, dynamic_cast<BND_Curve*>(BND_CommonObject::CreateWrapper(crv, nullptr)));
      }

    }

    return rc;
  }

  return NullTuple();

/*
  int count = SegmentCount();
  std::vector<BND_Curve*> rc;
  for (int i = 0; i < count; i++)
  {
    BND_Curve* curve = SegmentCurve(i);
    if (curve)
    {
      ON_Curve* crv = curve->m_curve->DuplicateCurve();
      rc.push_back(dynamic_cast<BND_Curve*>(BND_CommonObject::CreateWrapper(crv, nullptr)));
    }
  }
  return rc;
  */
}

bool BND_PolyCurve::Append1(const ON_Line& line)
{
  return m_polycurve->AppendAndMatch(new ON_LineCurve(line));
}

bool BND_PolyCurve::Append2(BND_Arc& arc)
{
  return m_polycurve->AppendAndMatch(new ON_ArcCurve(arc.m_arc));
}

bool BND_PolyCurve::Append3(const BND_Curve& curve)
{
  ON_Curve* crv = curve.m_curve->DuplicateCurve();
  return m_polycurve->AppendAndMatch(crv);
}

bool BND_PolyCurve::AppendSegment(const BND_Curve& curve)
{
  ON_Curve* crv = curve.m_curve->DuplicateCurve();
  return m_polycurve->Append(crv);
}

double BND_PolyCurve::SegmentCurveParameter(double t) const
{
  return m_polycurve->SegmentCurveParameter(t);
}

double BND_PolyCurve::PolyCurveParameter(int segmentIndex, double segmentCurveParameter) const
{
  return m_polycurve->PolyCurveParameter(segmentIndex, segmentCurveParameter);
}

BND_Interval BND_PolyCurve::SegmentDomain(int segmentIndex) const
{
  ON_Interval rc = m_polycurve->SegmentDomain(segmentIndex);
  return BND_Interval(rc);
}

int BND_PolyCurve::SegmentIndex(double polycurveParameter) const
{
  return m_polycurve->SegmentIndex(polycurveParameter);
}



/////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)

void initPolyCurveBindings(rh3dmpymodule& m)
{
  py::class_<BND_PolyCurve, BND_Curve>(m, "PolyCurve")
    .def(py::init<>())
    .def_property_readonly("SegmentCount", &BND_PolyCurve::SegmentCount)
    .def("SegmentCurve", &BND_PolyCurve::SegmentCurve, py::arg("index"))
    .def_property_readonly("IsNested", &BND_PolyCurve::IsNested)
    .def_property_readonly("HasGap", &BND_PolyCurve::HasGap)
    .def("RemoveNesting", &BND_PolyCurve::RemoveNesting)
    .def("Explode", &BND_PolyCurve::Explode)
    .def("Append", &BND_PolyCurve::Append1, py::arg("line"))
    .def("Append", &BND_PolyCurve::Append2, py::arg("arc"))
    .def("Append", &BND_PolyCurve::Append3, py::arg("curve"))
    .def("AppendSegment", &BND_PolyCurve::AppendSegment, py::arg("curve"))
    .def("SegmentCurveParameter", &BND_PolyCurve::SegmentCurveParameter, py::arg("polycurveParameter"))
    .def("PolyCurveParameter", &BND_PolyCurve::PolyCurveParameter, py::arg("segmentIndex"), py::arg("segmentCurveParameter"))
    .def("SegmentDomain", &BND_PolyCurve::SegmentDomain, py::arg("segmentIndex"))
    .def("SegmentIndex", &BND_PolyCurve::SegmentIndex, py::arg("polycurveParameter"))
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPolyCurveBindings(void*)
{
  class_<BND_PolyCurve, base<BND_Curve>>("PolyCurve")
    .constructor<>()
    .property("segmentCount", &BND_PolyCurve::SegmentCount)
    .function("segmentCurve", &BND_PolyCurve::SegmentCurve, allow_raw_pointers())
    .property("isNested", &BND_PolyCurve::IsNested)
    .property("hasGap", &BND_PolyCurve::HasGap)
    .function("removeNesting", &BND_PolyCurve::RemoveNesting)
    .function("explode", &BND_PolyCurve::Explode)
    .function("appendLine", &BND_PolyCurve::Append1)
    .function("appendArc", &BND_PolyCurve::Append2)
    .function("appendCurve", &BND_PolyCurve::Append3)
    .function("appendSegment", &BND_PolyCurve::AppendSegment)
    .function("segmentCurveParameter", &BND_PolyCurve::SegmentCurveParameter)
    .function("polyCurveParameter", &BND_PolyCurve::PolyCurveParameter)
    .function("segmentDomain", &BND_PolyCurve::SegmentDomain)
    .function("segmentIndex", &BND_PolyCurve::SegmentIndex)
  ;
}
#endif
