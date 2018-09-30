#include "bindings.h"

/*to build
cd extension
mkdir build
cd build
cmake -A x64 ..
cmake --build . --config Release --target _rhino3dm
*/

namespace py = pybind11;

PYBIND11_MODULE(_rhino3dm, m)
{
  m.doc() = "rhino3dm python package. OpenNURBS wrappers with a RhinoCommon style";

  py::class_<ON_2dPoint>(m, "Point2d")
    .def(py::init<double, double>())
    .def_readwrite("X", &ON_2dPoint::x)
    .def_readwrite("Y", &ON_2dPoint::y);

  py::class_<ON_3dPoint>(m, "Point3d")
    .def(py::init<double, double, double>())
    .def_readwrite("X", &ON_3dPoint::x)
    .def_readwrite("Y", &ON_3dPoint::y)
    .def_readwrite("Z", &ON_3dPoint::z);

  py::class_<ON_4dPoint>(m, "Point4d")
    .def(py::init<double, double, double, double>())
    .def_readwrite("X", &ON_4dPoint::x)
    .def_readwrite("Y", &ON_4dPoint::y)
    .def_readwrite("Z", &ON_4dPoint::z)
    .def_readwrite("W", &ON_4dPoint::w);

  py::class_<ON_2dVector>(m, "Vector2d")
    .def(py::init<double, double>())
    .def_readwrite("X", &ON_2dVector::x)
    .def_readwrite("Y", &ON_2dVector::y);

  py::class_<ON_3dVector>(m, "Vector3d")
    .def(py::init<double, double, double>())
    .def_readwrite("X", &ON_3dVector::x)
    .def_readwrite("Y", &ON_3dVector::y)
    .def_readwrite("Z", &ON_3dVector::z);

  py::class_<ON_3fPoint>(m, "Point3f")
    .def(py::init<float, float, float>())
    .def_readwrite("X", &ON_3fPoint::x)
    .def_readwrite("Y", &ON_3fPoint::y)
    .def_readwrite("Z", &ON_3fPoint::z);

  py::class_<BND_Interval>(m, "Interval")
    .def_readwrite("T0", &BND_Interval::m_t0)
    .def_readwrite("T1", &BND_Interval::m_t1);

  py::class_<BND_BoundingBox>(m, "BoundingBox")
    .def(py::init<ON_3dPoint, ON_3dPoint>())
    .def(py::init<double, double, double, double, double, double>())
    .def_property_readonly("Min", &BND_BoundingBox::Min)
    .def_property_readonly("Max", &BND_BoundingBox::Max)
    .def("Transform", &BND_BoundingBox::Transform);

  py::class_<BND_Arc>(m, "Arc")
    .def(py::init<ON_3dPoint, double, double>())
    .def("ToNurbsCurve", &BND_Arc::ToNurbsCurve);

  py::class_<BND_Object>(m, "CommonObject")
    .def("Encode", &BND_Object::Encode)
    .def_static("Decode", &BND_Object::Decode);

  py::class_<BND_Geometry, BND_Object>(m, "GeometryBase")
    .def("GetBoundingBox", &BND_Geometry::BoundingBox)
    .def("Rotate", &BND_Geometry::Rotate);

  py::class_<BND_Brep, BND_Geometry>(m, "Brep")
    .def(py::init<>());

  py::class_<BND_Curve, BND_Geometry>(m, "Curve")
    .def_property("Domain", &BND_Curve::GetDomain, &BND_Curve::SetDomain)
    .def_property_readonly("Dimension", &BND_Geometry::Dimension)
    .def("ChangeDimension", &BND_Curve::ChangeDimension)
    .def_property_readonly("SpanCount", &BND_Curve::SpanCount)
    .def_property_readonly("Degree", &BND_Curve::Degree)
    .def_property_readonly("PointAtStart", &BND_Curve::PointAtStart)
    .def_property_readonly("PointAtEnd", &BND_Curve::PointAtEnd)
    .def_property_readonly("IsLinear", &BND_Curve::IsLinear)
    .def_property_readonly("IsPolyline", &BND_Curve::IsPolyline);

  py::class_<BND_NurbsCurve, BND_Curve>(m, "NurbsCurve")
    .def(py::init<int, int>())
    .def(py::init<int, bool, int, int>());

  py::class_<BND_Mesh, BND_Geometry>(m, "Mesh")
    .def(py::init<>())
    .def_property_readonly("Vertices", &BND_Mesh::GetVertices)
    .def_property_readonly("Faces", &BND_Mesh::GetFaces);

  py::class_<BND_MeshVertexList>(m, "MeshVertexList")
    .def("__len__", &BND_MeshVertexList::Count)
    .def("SetCount", &BND_MeshVertexList::SetCount)
    .def("__getitem__", &BND_MeshVertexList::GetVertex)
    .def("__setitem__", &BND_MeshVertexList::SetVertex)
    .def("__iter__", [](BND_MeshVertexList &s) { return py::make_iterator(s.begin(), s.end()); },
      py::keep_alive<0, 1>() /* Essential: keep object alive while iterator exists */);
    //.def("__iter__", range(&BND_MeshVertexList::begin, &BND_MeshVertexList::end));

  py::class_<BND_MeshFaceList>(m, "MeshFaceList")
    .def("__len__", &BND_MeshFaceList::Count)
    .def("__getitem__", &BND_MeshFaceList::GetFace);

  py::class_<ON_Line>(m, "Line")
    .def(py::init<ON_3dPoint, ON_3dPoint>())
    .def_property_readonly("Length", &ON_Line::Length);

  py::class_<BND_Circle>(m, "Circle")
    .def(py::init<double>())
    .def(py::init<ON_3dPoint, double>())
    .def_readwrite("Plane", &BND_Circle::m_plane)
    .def_readwrite("Radius", &BND_Circle::m_radius)
    .def("PointAt", &BND_Circle::PointAt)
    .def("ToNurbsCurve", &BND_Circle::ToNurbsCurve);

  py::class_<BND_PolyCurve, BND_Curve>(m, "Polycurve");

  py::class_<BND_PolylineCurve, BND_Curve>(m, "Polylinecurve")
    .def(py::init<>())
    .def_property_readonly("PointCount", &BND_PolylineCurve::PointCount)
    .def("Point", &BND_PolylineCurve::Point);

  py::class_<BND_Sphere>(m, "Sphere")
    .def(py::init<ON_3dPoint, double>())
    .def_property_readonly("Center", &BND_Sphere::Center)
    .def_property_readonly("Radius", &BND_Sphere::Radius)
    .def("ToBrep", &BND_Sphere::ToBrep);

  py::class_<BND_Viewport, BND_Object>(m, "ViewportInfo")
    .def(py::init<>())
    .def_property_readonly("IsValidCameraFrame", &BND_Viewport::IsValidCameraFrame)
    .def_property_readonly("isValidCamer", &BND_Viewport::IsValidCamera)
    .def_property_readonly("IsValidFrustum", &BND_Viewport::IsValidFrustum)
    .def_property("IsParallelProjection", &BND_Viewport::IsParallelProjection, &BND_Viewport::SetProjectionToParallel)
    .def_property("IsPerspectiveProjection", &BND_Viewport::IsPerspectiveProjection, &BND_Viewport::SetProjectionToPerspective)
    .def_property_readonly("IsTwoPointPerspectiveProjection", &BND_Viewport::IsTwoPointPerspectiveProjection)
    .def("ChangeToParallelProjection", &BND_Viewport::ChangeToParallelProjection)
    .def("ChangeToPerspectiveProjection", &BND_Viewport::ChangeToPerspectiveProjection)
    .def("ChangeToTwoPointPerspectiveProjection", &BND_Viewport::ChangeToTwoPointPerspectiveProjection)
    .def_property_readonly("CameraLocation", &BND_Viewport::CameraLocation)
    .def_property_readonly("CameraDirection", &BND_Viewport::CameraDirection)
    .def_property_readonly("CameraUp", &BND_Viewport::CameraUp)
    .def("SetCameraLocation", &BND_Viewport::SetCameraLocation)
    .def("SetCameraDirection", &BND_Viewport::SetCameraDirection)
    .def("SetCameraUp", &BND_Viewport::SetCameraUp)
    .def_property_readonly("CameraX", &BND_Viewport::CameraX)
    .def_property_readonly("CameraY", &BND_Viewport::CameraY)
    .def_property_readonly("CameraZ", &BND_Viewport::CameraZ)
    .def("SetFrustum", &BND_Viewport::SetFrustum)
    .def_property_readonly("ScreenPortAspect", &BND_Viewport::ScreenPortAspect)
    .def_property("CameraAngle", &BND_Viewport::GetCameraAngle, &BND_Viewport::SetCameraAngle)
    .def_property("Camera35mmLensLength", &BND_Viewport::GetCamera35mmLensLength, &BND_Viewport::SetCamera35mmLensLength)
    .def("GetXform", &BND_Viewport::GetXform)
    .def("DollyExtents", &BND_Viewport::DollyExtents);

  py::class_<ON_Xform>(m, "Transform")
    .def(py::init<>())
    .def(py::init<double>())
    ;

  py::class_<BND_ONXModel>(m, "File3dm")
    .def(py::init<>())
    .def_static("Read", &BND_ONXModel::Read)
    .def_static("ReadNotes", &BND_ONXModel::ReadNotes)
    .def_static("ReadArchiveVersion", &BND_ONXModel::ReadArchiveVersion)
    .def("Write", &BND_ONXModel::Write)
    .def_property("StartSectionComments", &BND_ONXModel::GetStartSectionComments, &BND_ONXModel::SetStartSectionComments)
    .def_property("ApplicationName", &BND_ONXModel::GetApplicationName, &BND_ONXModel::SetApplicationName)
    .def_property("ApplicationUrl", &BND_ONXModel::GetApplicationUrl, &BND_ONXModel::SetApplicationUrl)
    .def_property("ApplicationDetails", &BND_ONXModel::GetApplicationDetails, &BND_ONXModel::SetApplicationDetails)
    .def_property_readonly("CreatedBy", &BND_ONXModel::GetCreatedBy)
    .def_property_readonly("LastEditedBy", &BND_ONXModel::GetLastEditedBy)
    .def_property("Revision", &BND_ONXModel::GetRevision, &BND_ONXModel::SetRevision)
    .def_property_readonly("Objects", &BND_ONXModel::Objects)
    ;

  py::class_<BND_ONXModel_ObjectTable>(m, "File3dmObjectTable");
}


