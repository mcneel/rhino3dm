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
BND_Plane BND_Plane::WorldYZ()
{
  BND_Plane rc;
  rc.m_origin.Set(0, 0, 0);
  rc.m_xaxis.Set(0, 1, 0);
  rc.m_yaxis.Set(0, 0, 1);
  rc.m_zaxis.Set(1, 0, 0);
  return rc;
}
BND_Plane BND_Plane::WorldZX()
{
  BND_Plane rc;
  rc.m_origin.Set(0, 0, 0);
  rc.m_xaxis.Set(0, 0, 1);
  rc.m_yaxis.Set(1, 0, 0);
  rc.m_zaxis.Set(0, 1, 0);
  return rc;
}
BND_Plane BND_Plane::Unset()
{
  BND_Plane rc = FromOnPlane(ON_Plane::UnsetPlane);
  return rc;
}

#if defined(ON_WASM_COMPILE)
emscripten::val BND_Plane::Encode() const
{
  emscripten::val v(emscripten::val::object());

  emscripten::val origin(emscripten::val::object());
  origin.set("X", emscripten::val(m_origin.x));
  origin.set("Y", emscripten::val(m_origin.y));
  origin.set("Z", emscripten::val(m_origin.z));
  v.set("Origin", origin);
  emscripten::val xaxis(emscripten::val::object());
  xaxis.set("X", emscripten::val(m_xaxis.x));
  xaxis.set("Y", emscripten::val(m_xaxis.y));
  xaxis.set("Z", emscripten::val(m_xaxis.z));
  v.set("XAxis", xaxis);
  emscripten::val yaxis(emscripten::val::object());
  yaxis.set("X", emscripten::val(m_yaxis.x));
  yaxis.set("Y", emscripten::val(m_yaxis.y));
  yaxis.set("Z", emscripten::val(m_yaxis.z));
  v.set("YAxis", yaxis);
  emscripten::val zaxis(emscripten::val::object());
  zaxis.set("X", emscripten::val(m_zaxis.x));
  zaxis.set("Y", emscripten::val(m_zaxis.y));
  zaxis.set("Z", emscripten::val(m_zaxis.z));
  v.set("ZAxis", zaxis);
  return v;
}

emscripten::val BND_Plane::toJSON(emscripten::val key)
{
  return Encode();
}

BND_Plane* BND_Plane::Decode(emscripten::val jsonObject)
{
  BND_Plane* plane = new BND_Plane();
  emscripten::val origin = jsonObject["Origin"].as<emscripten::val>();
  plane->m_origin.x = origin["X"].as<double>();
  plane->m_origin.y = origin["Y"].as<double>();
  plane->m_origin.z = origin["Z"].as<double>();
  emscripten::val xaxis = jsonObject["XAxis"].as<emscripten::val>();
  plane->m_xaxis.x = xaxis["X"].as<double>();
  plane->m_xaxis.y = xaxis["Y"].as<double>();
  plane->m_xaxis.z = xaxis["Z"].as<double>();
  emscripten::val yaxis = jsonObject["YAxis"].as<emscripten::val>();
  plane->m_yaxis.x = yaxis["X"].as<double>();
  plane->m_yaxis.y = yaxis["Y"].as<double>();
  plane->m_yaxis.z = yaxis["Z"].as<double>();
  emscripten::val zaxis = jsonObject["ZAxis"].as<emscripten::val>();
  plane->m_zaxis.x = zaxis["X"].as<double>();
  plane->m_zaxis.y = zaxis["Y"].as<double>();
  plane->m_zaxis.z = zaxis["Z"].as<double>();
  return plane;
}

#endif

#if defined(ON_PYTHON_COMPILE)
BND_DICT BND_Plane::Encode() const
{
  BND_DICT d;
  d["Origin"] = PointToDict(m_origin);
  d["XAxis"] = PointToDict(m_xaxis);
  d["YAxis"] = PointToDict(m_yaxis);
  d["ZAxis"] = PointToDict(m_zaxis);
  return d;
}

BND_Plane* BND_Plane::Decode(pybind11::dict jsonObject)
{
  BND_Plane* rc = new BND_Plane();

  pybind11::dict d = jsonObject["Origin"].cast<pybind11::dict>();
  rc->m_origin = PointFromDict(d);
  d = jsonObject["XAxis"].cast<pybind11::dict>();
  rc->m_xaxis = PointFromDict(d);
  d = jsonObject["YAxis"].cast<pybind11::dict>();
  rc->m_yaxis = PointFromDict(d);
  d = jsonObject["ZAxis"].cast<pybind11::dict>();
  rc->m_zaxis = PointFromDict(d);
  return rc;
}
#endif

BND_Plane BND_PlaneHelper::WorldXY()
{
  return BND_Plane::WorldXY();
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPlaneBindings(pybind11::module& m)
{
  py::class_<BND_Plane>(m, "Plane")
    .def_static("WorldXY", &BND_Plane::WorldXY)
    .def_static("WorldYZ", &BND_Plane::WorldYZ)
    .def_static("WorldZX", &BND_Plane::WorldZX)
    .def_static("Unset", &BND_Plane::Unset)
    .def_readwrite("Origin", &BND_Plane::m_origin)
    .def_readwrite("XAxis", &BND_Plane::m_xaxis)
    .def_readwrite("YAxis", &BND_Plane::m_yaxis)
    .def_readwrite("ZAxis", &BND_Plane::m_zaxis)
    .def("Encode", &BND_Plane::Encode)
    .def_static("Decode", &BND_Plane::Decode)
    ;
}
#endif


#if defined ON_WASM_COMPILE
using namespace emscripten;

void initPlaneBindings(void*)
{
  value_object<BND_Plane>("SimplePlane")
    .field("origin", &BND_Plane::m_origin)
    .field("xAxis", &BND_Plane::m_xaxis)
    .field("yAxis", &BND_Plane::m_yaxis)
    .field("zAxis", &BND_Plane::m_zaxis);

  class_<BND_PlaneHelper>("Plane")
      .class_function("worldXY", &BND_PlaneHelper::WorldXY);

}
#endif
