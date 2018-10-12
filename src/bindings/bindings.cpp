#include "bindings.h"

#if defined ON_PYTHON_COMPILE

namespace py = pybind11;

PYBIND11_MODULE(_rhino3dm, m)
{
  m.doc() = "rhino3dm python package. OpenNURBS wrappers with a RhinoCommon style";

  initDefines(m);
  initObjectBindings(m);
  init3dmAttributesBindings(m);
  initPointBindings(m);
  initArcBindings(m);
  initBoundingBoxBindings(m);
  initGeometryBindings(m);
  initBrepBindings(m);
  initCurveBindings(m);
  initNurbsCurveBindings(m);
  initMeshBindings(m);

  py::class_<ON_Line>(m, "Line")
    .def(py::init<ON_3dPoint, ON_3dPoint>())
    .def_property_readonly("Length", &ON_Line::Length);

  initCircleBindings(m);
  initArcCurveBindings(m);
  initLineCurveBindings(m);
  initPolyCurveBindings(m);
  initPolylineCurveBindings(m);
  initSurfaceBindings(m);
  initNurbsSurfaceBindings(m);
  initSphereBindings(m);
  initViewportBindings(m);

  py::class_<ON_Xform>(m, "Transform")
    .def(py::init<>())
    .def(py::init<double>())
    .def_static("Translation", py::overload_cast<double, double, double>(&ON_Xform::TranslationTransformation))
    .def_static("Scale", py::overload_cast<const ON_3dPoint&, double>(&ON_Xform::ScaleTransformation))
    .def_static("Rotation", [](double angle, ON_3dVector rotationAxis, ON_3dPoint rotationCenter) {
      ON_Xform rc(1);
      rc.Rotation(angle, rotationAxis, rotationCenter);
      return rc;
    })
    ;

  initExtensionsBindings(m);
}

#else

using namespace emscripten;


EMSCRIPTEN_BINDINGS(rhino3dm) {
  initDefines();
  initPointBindings();
  initPlaneBindings();

  class_<ON_UUID>("Guid");

    enum_<ON::coordinate_system>("CoordinateSystem")
        .value("WORLD", ON::coordinate_system::world_cs)
        .value("CAMERA", ON::coordinate_system::camera_cs)
        .value("CLIP", ON::coordinate_system::clip_cs)
        .value("SCREEN", ON::coordinate_system::screen_cs);

  initBoundingBoxBindings();

    class_<BND_Circle>("Circle")
        .constructor<double>()
        .constructor<ON_3dPoint, double>()
        .property("plane", &BND_Circle::m_plane)
        .property("radius", &BND_Circle::m_radius)
        .function("pointAt", &BND_Circle::PointAt)
        .function("toNurbsCurve", &BND_Circle::ToNurbsCurve, allow_raw_pointers());

    class_<BND_Arc>("Arc")
        .constructor<ON_3dPoint, double, double>()
        .function("toNurbsCurve", &BND_Arc::ToNurbsCurve, allow_raw_pointers());

    class_<BND_Object>("CommonObject")
        .function("encode", &BND_Object::Encode)
        .class_function("decode", &BND_Object::Decode, allow_raw_pointers());

    class_<BND_Brep, base<BND_Geometry>>("Brep");

    initCurveBindings();

    initGeometryBindings();

    class_<ON_Line>("Line")
        .constructor<ON_3dPoint, ON_3dPoint>()
        .property("from", &ON_Line::from)
        .property("to", &ON_Line::to)
        .property("length", &ON_Line::Length);

    class_<BND_LineCurve, base<BND_Curve>>("LineCurve");

    initNurbsCurveBindings();

    class_<BND_Mesh, base<BND_Geometry>>("Mesh")
        .constructor<>()
        .function("vertices", &BND_Mesh::GetVertices)
        .function("faces", &BND_Mesh::GetFaces)
        .function("normals", &BND_Mesh::GetNormals);
        ;

    class_<BND_MeshVertexList>("MeshVertexList")
        .property("count", &BND_MeshVertexList::Count)
        .function("setCount", &BND_MeshVertexList::SetCount)
        .function("get", &BND_MeshVertexList::GetVertex)
        .function("set", &BND_MeshVertexList::SetVertex);

    class_<BND_MeshFaceList>("MeshFaceList")
        .property("count", &BND_MeshFaceList::Count)
        .function("get", &BND_MeshFaceList::GetFace);

    class_<BND_MeshNormalList>("MeshNormalList")
          .property("count", &BND_MeshNormalList::Count)
          .function("get", &BND_MeshNormalList::GetNormal)
          .function("set", &BND_MeshNormalList::SetNormal);


    class_<BND_PolyCurve, base<BND_Curve>>("Polycurve");

    class_<BND_PolylineCurve, base<BND_Curve>>("Polylinecurve")
        .property("pointCount", &BND_PolylineCurve::PointCount)
        .function("point", &BND_PolylineCurve::Point);
  initLineCurveBindings();

  initSurfaceBindings();
  initNurbsSurfaceBindings();
    class_<BND_Sphere>("Sphere")
        .constructor<ON_3dPoint,double>()
        .property("center", &BND_Sphere::Center)
        .property("radius", &BND_Sphere::Radius)
        .function("toBrep", &BND_Sphere::ToBrep, allow_raw_pointers());

  initViewportBindings();

    class_<BND_Xform>("Transform");

  initExtensionsBindings();
}

#endif
