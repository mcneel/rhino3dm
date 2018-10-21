#include "bindings.h"


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBezierBindings(pybind11::module& m)
{
  py::class_<BND_BezierCurve>(m, "BezierCurve")
    .def_property_readonly("Dimension", &BND_BezierCurve::Dimension)
    .def_property_readonly("IsValid", &BND_BezierCurve::IsValid)
    .def("PointAt", &BND_BezierCurve::PointAt)
    .def("TangentAt", &BND_BezierCurve::TangentAt)
    .def("CurvatureAt", &BND_BezierCurve::CurvatureAt)
    .def_property_readonly("IsRational", &BND_BezierCurve::IsRational)
    .def_property_readonly("ControlVertexCount", &BND_BezierCurve::ControlVertexCount)
    .def("MakeRational", &BND_BezierCurve::MakeRational)
    .def("MakeNonRational", &BND_BezierCurve::MakeNonRational)
    .def("IncreaseDegree", &BND_BezierCurve::IncreaseDegree)
    .def("ChangeDimension", &BND_BezierCurve::ChangeDimension)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBezierBindings(void*)
{
  class_<BND_BezierCurve>("BezierCurve")
    ;
}
#endif
