#include "bindings.h"

BND_Box::BND_Box(const BND_BoundingBox& bbox)
  : m_box(bbox.m_bbox)
{
}

//BND_Brep* BND_Box::ToBrep() const
//{
//  
//}
//
//BND_Extrusion* BND_Box::ToExtrusion() const
//{
//}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBoxBindings(pybind11::module& m)
{
  py::class_<BND_Box>(m, "Box")
    .def(py::init<const BND_BoundingBox&>())
    .def_property_readonly("IsValid", &BND_Box::IsValid)
    .def_property_readonly("Center", &BND_Box::Center)
    .def_property_readonly("Area", &BND_Box::Area)
    .def_property_readonly("Volume", &BND_Box::Volume)
    .def("PointAt", &BND_Box::PointAt)
    .def("ClosestPoint", &BND_Box::ClosestPoint)
    .def("Transform", &BND_Box::Transform)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBoxBindings(void*)
{
  class_<BND_Box>("Box")
    ;
}
#endif
