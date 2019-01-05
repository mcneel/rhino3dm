#include "bindings.h"

BND_Plane BND_Plane::FromOnPlane(const ON_Plane& plane)
{
  BND_Plane wasmPlane;
  wasmPlane.m_origin = plane.origin;
  wasmPlane.m_xaxis = plane.xaxis;
  wasmPlane.m_yaxis = plane.yaxis;
  wasmPlane.m_zaxis = plane.zaxis;
  return wasmPlane;
}

ON_Plane BND_Plane::ToOnPlane() const
{
  ON_Plane plane;
  plane.origin = m_origin;
  plane.xaxis = m_xaxis;
  plane.yaxis = m_yaxis;
  plane.zaxis = m_zaxis;
  plane.UpdateEquation();
  return plane;
}


BND_Plane BND_Plane::WorldXY()
{
  BND_Plane rc = FromOnPlane(ON_Plane::World_xy);
  return rc;
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPlaneBindings(pybind11::module& m)
{
  py::class_<BND_Plane>(m, "Plane")
    .def_static("WorldXY", &BND_Plane::WorldXY)
    .def_readwrite("Origin", &BND_Plane::m_origin)
    .def_readwrite("XAxis", &BND_Plane::m_xaxis)
    .def_readwrite("YAxis", &BND_Plane::m_yaxis)
    .def_readwrite("ZAxis", &BND_Plane::m_zaxis)
    ;
}
#endif


#if defined ON_WASM_COMPILE
using namespace emscripten;

void initPlaneBindings(void*)
{
  value_object<BND_Plane>("Plane")
    .field("origin", &BND_Plane::m_origin)
    .field("xAxis", &BND_Plane::m_xaxis)
    .field("yAxis", &BND_Plane::m_yaxis)
    .field("zAxis", &BND_Plane::m_zaxis);
}
#endif
