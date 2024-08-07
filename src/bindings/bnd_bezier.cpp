#include "bindings.h"

BND_NurbsCurve* BND_BezierCurve::ToNurbsCurve() const
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if (m_bezcurve.GetNurbForm(*nc) == 1)
  {
    return new BND_NurbsCurve(nc, nullptr);
  }
  delete nc;
  return nullptr;
}

BND_TUPLE BND_BezierCurve::Split(double t)
{
  BND_BezierCurve* left = new BND_BezierCurve();
  BND_BezierCurve* right = new BND_BezierCurve();
  bool success = m_bezcurve.Split(t, left->m_bezcurve, right->m_bezcurve);
  if (!success)
  {
    delete left;
    left = nullptr;
    delete right;
    right = nullptr;
  }
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, left);
  SetTuple(rc, 2, right);
  return rc;
}


#if defined(ON_PYTHON_COMPILE)

void initBezierBindings(rh3dmpymodule& m)
{
  py::class_<BND_BezierCurve>(m, "BezierCurve")
    .def_property_readonly("Dimension", &BND_BezierCurve::Dimension)
    .def_property_readonly("IsValid", &BND_BezierCurve::IsValid)
    .def("PointAt", &BND_BezierCurve::PointAt, py::arg("t"))
    .def("TangentAt", &BND_BezierCurve::TangentAt, py::arg("t"))
    .def("CurvatureAt", &BND_BezierCurve::CurvatureAt, py::arg("t"))
    .def("ToNurbsCurve", &BND_BezierCurve::ToNurbsCurve)
    .def_property_readonly("IsRational", &BND_BezierCurve::IsRational)
    .def_property_readonly("ControlVertexCount", &BND_BezierCurve::ControlVertexCount)
    .def("MakeRational", &BND_BezierCurve::MakeRational)
    .def("MakeNonRational", &BND_BezierCurve::MakeNonRational)
    .def("IncreaseDegree", &BND_BezierCurve::IncreaseDegree, py::arg("desiredDegree"))
    .def("ChangeDimension", &BND_BezierCurve::ChangeDimension, py::arg("desiredDimension"))
    .def("Split", &BND_BezierCurve::Split, py::arg("t"))
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBezierBindings(void*)
{
  class_<BND_BezierCurve>("BezierCurve")
    .property("dimension", &BND_BezierCurve::Dimension)
    .property("isValid", &BND_BezierCurve::IsValid)
    .function("pointAt", &BND_BezierCurve::PointAt)
    .function("tangentAt", &BND_BezierCurve::TangentAt)
    .function("curvatureAt", &BND_BezierCurve::CurvatureAt)
    .function("toNurbsCurve", &BND_BezierCurve::ToNurbsCurve, allow_raw_pointers())
    .property("isRational", &BND_BezierCurve::IsRational)
    .property("controlVertexCount", &BND_BezierCurve::ControlVertexCount)
    .function("makeRational", &BND_BezierCurve::MakeRational)
    .function("makeNonRational", &BND_BezierCurve::MakeNonRational)
    .function("increaseDegree", &BND_BezierCurve::IncreaseDegree)
    .function("changeDimension", &BND_BezierCurve::ChangeDimension)
    .function("split", &BND_BezierCurve::Split, allow_raw_pointers())
    ;
}
#endif
