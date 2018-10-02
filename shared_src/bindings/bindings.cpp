#include "bindings.h"

#if defined ON_PYTHON_COMPILE

namespace py = pybind11;

PYBIND11_MODULE(_rhino3dm, m)
{
  m.doc() = "rhino3dm python package. OpenNURBS wrappers with a RhinoCommon style";

  init3dmAttributesBindings(m);

  initPointBindings(m);

  initArcBindings(m);

  initBoundingBoxBindings(m);

  initObjectBindings(m);

  initGeometryBindings(m);

  initBrepBindings(m);

  initCurveBindings(m);

  initNurbsCurveBindings(m);

  initMeshBindings(m);

  py::class_<ON_Line>(m, "Line")
    .def(py::init<ON_3dPoint, ON_3dPoint>())
    .def_property_readonly("Length", &ON_Line::Length);

  initCircleBindings(m);

  initLineCurveBindings(m);

  initPolyCurveBindings(m);

  initPolylineCurveBindings(m);

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

    value_array<ON_2dPoint>("Point2dSimple")
        .element(&ON_2dPoint::x)
        .element(&ON_2dPoint::y);

    value_array<ON_3dPoint>("Point3dSimple")
        .element(&ON_3dPoint::x)
        .element(&ON_3dPoint::y)
        .element(&ON_3dPoint::z);

    class_<BND_Point3d>("Point3d")
        .class_function("transform", &BND_Point3d::Transform);

    value_array<ON_4dPoint>("Point4dSimple")
        .element(&ON_4dPoint::x)
        .element(&ON_4dPoint::y)
        .element(&ON_4dPoint::z)
        .element(&ON_4dPoint::w);

    value_array<ON_3dVector>("Vector3dSimple")
        .element(&ON_3dVector::x)
        .element(&ON_3dVector::y)
        .element(&ON_3dVector::z);

    value_array<ON_3fPoint>("Point3fSimple")
        .element(&ON_3fPoint::x)
        .element(&ON_3fPoint::y)
        .element(&ON_3fPoint::z);

    value_array<BND_Interval>("IntervalSimple")
        .element(&BND_Interval::m_t0)
        .element(&BND_Interval::m_t1);

    value_object<BND_Plane>("Plane")
        .field("origin", &BND_Plane::m_origin)
        .field("xAxis", &BND_Plane::m_xaxis)
        .field("yAxis", &BND_Plane::m_yaxis)
        .field("zAxis", &BND_Plane::m_zaxis);

    enum_<ON::coordinate_system>("CoordinateSystem")
        .value("WORLD", ON::coordinate_system::world_cs)
        .value("CAMERA", ON::coordinate_system::camera_cs)
        .value("CLIP", ON::coordinate_system::clip_cs)
        .value("SCREEN", ON::coordinate_system::screen_cs);

    class_<BND_BoundingBox>("BoundingBox")
        .constructor<ON_3dPoint, ON_3dPoint>()
        .constructor<double, double, double, double, double, double>()
        .property("min", &BND_BoundingBox::Min)
        .property("max", &BND_BoundingBox::Max)
        .function("transform", &BND_BoundingBox::Transform);

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

    class_<BND_Curve, base<BND_Geometry>>("Curve")
        .property("domain", &BND_Curve::GetDomain, &BND_Curve::SetDomain)
        .property("dimension", &BND_Geometry::Dimension)
        .function("changeDimension", &BND_Curve::ChangeDimension)
        .property("spanCount", &BND_Curve::SpanCount)
        .property("degree", &BND_Curve::Degree)
        .property("pointAtStart", &BND_Curve::PointAtStart)
        .property("pointAtEnd", &BND_Curve::PointAtEnd)
        .function("isLinear", &BND_Curve::IsLinear)
        .function("isPolyline", &BND_Curve::IsPolyline);

    class_<BND_Geometry, base<BND_Object>>("GeometryBase")
        .function("getBoundingBox", &BND_Geometry::BoundingBox);

    class_<ON_Line>("Line")
        .constructor<ON_3dPoint, ON_3dPoint>()
        .property("from", &ON_Line::from)
        .property("to", &ON_Line::to)
        .property("length", &ON_Line::Length);

    class_<BND_LineCurve, base<BND_Curve>>("LineCurve");

    class_<BND_NurbsCurve, base<BND_Curve>>("NurbsCurve")
        .constructor<int, int>()
        .constructor<int, bool, int, int>();

    class_<BND_Mesh, base<BND_Geometry>>("Mesh")
        .constructor<>()
        .function("vertices", &BND_Mesh::GetVertices)
        .function("faces", &BND_Mesh::GetFaces);

    class_<BND_MeshVertexList>("MeshVertexList")
        .property("count", &BND_MeshVertexList::Count)
        .function("setCount", &BND_MeshVertexList::SetCount)
        .function("get", &BND_MeshVertexList::GetVertex)
        .function("set", &BND_MeshVertexList::SetVertex);

    class_<BND_MeshFaceList>("MeshFaceList")
        .property("count", &BND_MeshFaceList::Count)
        .function("get", &BND_MeshFaceList::GetFace);

    class_<BND_PolyCurve, base<BND_Curve>>("Polycurve");

    class_<BND_PolylineCurve, base<BND_Curve>>("Polylinecurve")
        .property("pointCount", &BND_PolylineCurve::PointCount)
        .function("point", &BND_PolylineCurve::Point);

    class_<BND_Sphere>("Sphere")
        .constructor<ON_3dPoint,double>()
        .property("center", &BND_Sphere::Center)
        .property("radius", &BND_Sphere::Radius)
        .function("toBrep", &BND_Sphere::ToBrep, allow_raw_pointers());

    class_<BND_Viewport, base<BND_Object>>("ViewportInfo")
        .constructor<>()
        .property("isValidCameraFrame", &BND_Viewport::IsValidCameraFrame)
        .property("isValidCamer", &BND_Viewport::IsValidCamera)
        .property("isValidFrustum", &BND_Viewport::IsValidFrustum)
        .property("isParallelProjection", &BND_Viewport::IsParallelProjection, &BND_Viewport::SetProjectionToParallel)
        .property("isPerspectiveProjection", &BND_Viewport::IsPerspectiveProjection, &BND_Viewport::SetProjectionToPerspective)
        .property("isTwoPointPerspectiveProjection", &BND_Viewport::IsTwoPointPerspectiveProjection)
        .function("changeToParallelProjection", &BND_Viewport::ChangeToParallelProjection)
        .function("changeToPerspectiveProjection", &BND_Viewport::ChangeToPerspectiveProjection)
        .function("changeToTwoPointPerspectiveProjection", &BND_Viewport::ChangeToTwoPointPerspectiveProjection)
        .property("cameraLocation", &BND_Viewport::CameraLocation)
        .property("cameraDirection", &BND_Viewport::CameraDirection)
        .property("cameraUp", &BND_Viewport::CameraUp)
        .function("setCameraLocation", &BND_Viewport::SetCameraLocation)
        .function("setCameraDirection", &BND_Viewport::SetCameraDirection)
        .function("setCameraUp", &BND_Viewport::SetCameraUp)
        .property("cameraX", &BND_Viewport::CameraX)
        .property("cameraY", &BND_Viewport::CameraY)
        .property("cameraZ", &BND_Viewport::CameraZ)
        .function("setFrustum", &BND_Viewport::SetFrustum)
        .function("getFrustum", &BND_Viewport::GetFrustum)
        .property("screenPort", &BND_Viewport::GetScreenPort, &BND_Viewport::SetScreenPort)
        .property("screenPortAspect", &BND_Viewport::ScreenPortAspect)
        .property("cameraAngle", &BND_Viewport::GetCameraAngle, &BND_Viewport::SetCameraAngle)
        .property("camera35mmLensLength", &BND_Viewport::GetCamera35mmLensLength, &BND_Viewport::SetCamera35mmLensLength)
        .function("getXform", &BND_Viewport::GetXform, allow_raw_pointers())
        .function("dollyExtents", &BND_Viewport::DollyExtents);

    class_<BND_Xform>("Transform");
}

#endif
