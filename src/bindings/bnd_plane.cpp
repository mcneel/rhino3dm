#include "bindings.h"


BND_Plane::BND_Plane(ON_3dPoint origin, ON_3dVector normal)
{
  ON_Plane plane(origin, normal);
  *this = FromOnPlane(plane);
}
BND_Plane::BND_Plane(ON_3dPoint origin, ON_3dPoint xPoint, ON_3dPoint yPoint)
{
  ON_Plane plane(origin, xPoint, yPoint);
  *this = FromOnPlane(plane);
}
BND_Plane::BND_Plane(ON_3dPoint origin, ON_3dVector xDirection, ON_3dVector yDirection)
{
  ON_Plane plane(origin, xDirection, yDirection);
  *this = FromOnPlane(plane);
}
BND_Plane::BND_Plane(double a, double b, double c, double d)
{
  ON_PlaneEquation eq;
  eq.x = a;
  eq.y = b;
  eq.z = c;
  eq.d = d;
  ON_Plane plane;
  plane.CreateFromEquation(eq);
  *this = FromOnPlane(plane);
}

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

ON_3dPoint BND_Plane::PointAtUV(double u, double v) const
{
  ON_Plane plane = ToOnPlane();
  return plane.PointAt(u, v);
}

ON_3dPoint BND_Plane::PointAtUVW(double u, double v, double w) const
{
  ON_Plane plane = ToOnPlane();
  return plane.PointAt(u, v, w);
}

BND_Plane BND_Plane::Rotate(double angle, const ON_3dVector &axis)
{
  ON_Plane plane = ToOnPlane();
  if(plane.Rotate(angle, axis))
    return FromOnPlane(plane);

  return *this;
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

BND_Plane* BND_Plane::Decode(py::dict jsonObject)
{
  BND_Plane* rc = new BND_Plane();

  py::dict d = py::cast<py::dict>(jsonObject["Origin"]);
  rc->m_origin = PointFromDict(d);
  d = py::cast<py::dict>(jsonObject["XAxis"]);
  rc->m_xaxis = PointFromDict(d);
  d = py::cast<py::dict>(jsonObject["YAxis"]);
  rc->m_yaxis = PointFromDict(d);
  d = py::cast<py::dict>(jsonObject["ZAxis"]);
  rc->m_zaxis = PointFromDict(d);
  /*
  py::dict d = jsonObject["Origin"].cast<py::dict>();
  rc->m_origin = PointFromDict(d);
  d = jsonObject["XAxis"].cast<py::dict>();
  rc->m_xaxis = PointFromDict(d);
  d = jsonObject["YAxis"].cast<py::dict>();
  rc->m_yaxis = PointFromDict(d);
  d = jsonObject["ZAxis"].cast<py::dict>();
  rc->m_zaxis = PointFromDict(d);
   */
  return rc;
}
#endif

BND_Plane BND_PlaneHelper::WorldXY()
{
  return BND_Plane::WorldXY();
}

#if defined(ON_PYTHON_COMPILE)

void initPlaneBindings(rh3dmpymodule& m)
{
  py::class_<BND_Plane>(m, "Plane")
    .def_static("WorldXY", &BND_Plane::WorldXY)
    .def_static("WorldYZ", &BND_Plane::WorldYZ)
    .def_static("WorldZX", &BND_Plane::WorldZX)
    .def_static("Unset", &BND_Plane::Unset)
    .def(py::init<>())
    .def(py::init<ON_3dPoint, ON_3dVector>(), py::arg("origin"), py::arg("normal"))
    .def(py::init<ON_3dPoint, ON_3dPoint, ON_3dPoint>(), py::arg("origin"), py::arg("xPoint"), py::arg("yPoint"))
    .def(py::init<ON_3dPoint, ON_3dVector, ON_3dVector>(), py::arg("origin"), py::arg("xDirection"), py::arg("yDirection"))
    .def(py::init<double, double, double, double>(), py::arg("a"), py::arg("b"), py::arg("c"), py::arg("d"))
    .def("PointAt", &BND_Plane::PointAtUV, py::arg("u"), py::arg("v"))
    .def("PointAt", &BND_Plane::PointAtUVW, py::arg("u"), py::arg("v"), py::arg("w"))
    .def("Rotate", &BND_Plane::Rotate, py::arg("angle"), py::arg("axis"))
    .def_readwrite("Origin", &BND_Plane::m_origin)
    .def_readwrite("XAxis", &BND_Plane::m_xaxis)
    .def_readwrite("YAxis", &BND_Plane::m_yaxis)
    .def_readwrite("ZAxis", &BND_Plane::m_zaxis)
    .def("Encode", &BND_Plane::Encode)
    .def_static("Decode", &BND_Plane::Decode, py::arg("jsonObject"))
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
