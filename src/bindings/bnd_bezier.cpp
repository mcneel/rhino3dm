#include "bindings.h"


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBezierBindings(pybind11::module& m)
{
  py::class_<BND_BezierCurve>(m, "BezierCurve")
    .def_property_readonly("Dimension", &BND_BezierCurve::Dimension)
    .def_property_readonly("IsValid", &BND_BezierCurve::IsValid)
    .def("PointAt", &BND_BezierCurve::PointAt, py::arg("t"))
    .def("TangentAt", &BND_BezierCurve::TangentAt, py::arg("t"))
    .def("CurvatureAt", &BND_BezierCurve::CurvatureAt, py::arg("t"))
    .def_property_readonly("IsRational", &BND_BezierCurve::IsRational)
    .def_property_readonly("ControlVertexCount", &BND_BezierCurve::ControlVertexCount)
    .def("MakeRational", &BND_BezierCurve::MakeRational)
    .def("MakeNonRational", &BND_BezierCurve::MakeNonRational)
    .def("IncreaseDegree", &BND_BezierCurve::IncreaseDegree, py::arg("desiredDegree"))
    .def("ChangeDimension", &BND_BezierCurve::ChangeDimension, py::arg("desiredDimension"))
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
    .property("isRational", &BND_BezierCurve::IsRational)
    .property("controlVertexCount", &BND_BezierCurve::ControlVertexCount)
    .function("makeRational", &BND_BezierCurve::MakeRational)
    .function("makeNonRational", &BND_BezierCurve::MakeNonRational)
    .function("increaseDegree", &BND_BezierCurve::IncreaseDegree)
    .function("changeDimension", &BND_BezierCurve::ChangeDimension)
    ;
}
#endif
