#include "bindings.h"

BND_Box::BND_Box(const BND_BoundingBox& bbox)
  : m_box(bbox.m_bbox)
{
}

bool BND_Box::Transform(const BND_Transform& xform)
{
  return m_box.Transform(xform.m_xform);
}

ON_3dPoint BND_Box::PointAt(double x, double y, double z) const 
{ 
  x = m_box.dx.ParameterAt(x);
  y = m_box.dy.ParameterAt(y);
  z = m_box.dz.ParameterAt(z);
  return m_box.plane.PointAt(x, y, z); 
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
    .def(py::init<const BND_BoundingBox&>(), py::arg("bbox"))
    .def_property_readonly("IsValid", &BND_Box::IsValid)
    .def_property_readonly("Center", &BND_Box::Center)
    .def_property_readonly("Area", &BND_Box::Area)
    .def_property_readonly("Volume", &BND_Box::Volume)
    .def("PointAt", &BND_Box::PointAt, py::arg("x"), py::arg("y"), py::arg("z"))
    .def("ClosestPoint", &BND_Box::ClosestPoint, py::arg("point"))
    .def("Transform", &BND_Box::Transform, py::arg("xform"))
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBoxBindings(void*)
{
  class_<BND_Box>("Box")
    .constructor<const BND_BoundingBox&>()
    .property("isValid", &BND_Box::IsValid)
    .property("center", &BND_Box::Center)
    .property("area", &BND_Box::Area)
    .property("volume", &BND_Box::Volume)
    .function("pointAt", &BND_Box::PointAt)
    .function("closestPoint", &BND_Box::ClosestPoint)
    .function("transform", &BND_Box::Transform)
    ;
}
#endif
