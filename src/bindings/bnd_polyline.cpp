#include "bindings.h"

BND_Point3dList::BND_Point3dList(const std::vector<ON_3dPoint>& points)
{
  int count = (int)points.size();
  const ON_3dPoint* pts = points.data();
  m_polyline.Append(count, pts);
}

BND_Polyline* BND_Polyline::CreateFromPoints(const std::vector<ON_3dPoint>& points)
{
  int count = (int)points.size();
  const ON_3dPoint* pts = points.data();
  BND_Polyline* rc = new BND_Polyline();
  rc->m_polyline.Append(count, pts);
  return rc;
}

ON_3dPoint BND_Point3dList::GetPoint(int index) const
{
#if defined(ON_PYTHON_COMPILE)
  if (index < 0 || index >= m_polyline.Count())
    throw pybind11::index_error();
#endif
  return m_polyline[index]; 
}

void BND_Point3dList::Transform(const BND_Transform& xform)
{
  m_polyline.Transform(xform.m_xform);
}

void BND_Point3dList::SetAllX(double xValue)
{
  const int count = m_polyline.Count();
  for (int i = 0; i < count; i++)
    m_polyline[i].x = xValue;
}
void BND_Point3dList::SetAllY(double yValue)
{
  const int count = m_polyline.Count();
  for (int i = 0; i < count; i++)
    m_polyline[i].y = yValue;
}
void BND_Point3dList::SetAllZ(double zValue)
{
  const int count = m_polyline.Count();
  for (int i = 0; i < count; i++)
    m_polyline[i].z = zValue;
}


double BND_Polyline::ClosestParameter(const ON_3dPoint& testPoint) const
{
  double t = 0;
  m_polyline.ClosestPointTo(testPoint, &t);
  return t;
}

BND_NurbsCurve* BND_Polyline::ToNurbsCurve() const
{
  ON_PolylineCurve plc(m_polyline);
  ON_NurbsCurve* nc = plc.NurbsCurve();
  if (nullptr == nc)
    return nullptr;
  return new BND_NurbsCurve(nc, nullptr);
}

BND_PolylineCurve* BND_Polyline::ToPolylineCurve() const
{
  if (m_polyline.Count() < 2)
    return nullptr;
  ON_PolylineCurve* plc = new ON_PolylineCurve(m_polyline);
  return new BND_PolylineCurve(plc, nullptr);
}

BND_Polyline* BND_Polyline::CreateInscribedPolygon(BND_Circle& circle, int sideCount)
{
  BND_Polyline* rc = new BND_Polyline();
  if (!rc->m_polyline.CreateInscribedPolygon(circle.m_circle, sideCount))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_Polyline* BND_Polyline::CreateCircumscribedPolygon(BND_Circle& circle, int sideCount)
{
  BND_Polyline* rc = new BND_Polyline();
  if (!rc->m_polyline.CreateCircumscribedPolygon(circle.m_circle, sideCount))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_Polyline* BND_Polyline::CreateStarPolygon(BND_Circle& circle, double radius, int cornerCount)
{
  BND_Polyline* rc = new BND_Polyline();
  if (!rc->m_polyline.CreateStarPolygon(circle.m_circle, radius, cornerCount))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_TUPLE BND_Polyline::GetSegments() const
{
  int count = m_polyline.SegmentCount();
  if( count < 2 ) 
  {
    return NullTuple();
  }
  BND_TUPLE rc = CreateTuple(count);

  for (int i = 0; i < count - 1; i++)
  {
    BND_LineCurve* lc = new BND_LineCurve(m_polyline[i], m_polyline[i+1]);
    SetTuple(rc, i, lc);
  }

  return rc;

}

BND_LineCurve* BND_Polyline::SegmentAt(int index) const
{

  if ( index < 0 ) { return nullptr; }
  if ( index >= m_polyline.Count() - 1 ) { return nullptr; }

  return new BND_LineCurve(m_polyline[index], m_polyline[index+1]);

}
#if defined(ON_PYTHON_COMPILE)
void BND_Point3dList::Append2 (int count, pybind11::object points)
{
  for (auto item : points)
  {
    ON_3dPoint point = item.cast<ON_3dPoint>();
    m_polyline.Append(point)
  }
}
#endif

//////////////////////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPolylineBindings(pybind11::module& m)
{
  py::class_<BND_Point3dList>(m, "Point3dList")
    .def(py::init<>())
    .def(py::init<int>(), py::arg("initialCapacity"))
    .def(py::init<const std::vector<ON_3dPoint>&>(), py::arg("points"))
    .def_property("Capacity", &BND_Point3dList::GetCapacity, &BND_Point3dList::SetCapacity)
    .def_property("Count", &BND_Point3dList::GetCount, &BND_Point3dList::SetCount)
    .def("__len__", &BND_Point3dList::GetCount)
    .def("__getitem__", &BND_Point3dList::GetPoint)
    .def("__setitem__", &BND_Point3dList::SetPoint)
    .def("Clear", &BND_Point3dList::Clear)
    .def("Insert", &BND_Point3dList::Insert, py::arg("index"), py::arg("item"))
    .def("RemoveAt", &BND_Point3dList::RemoveAt, py::arg("index"))
    .def_property_readonly("BoundingBox", &BND_Point3dList::BoundingBox)
    .def("Add", &BND_Point3dList::Add, py::arg("x"), py::arg("y"), py::arg("z"))
    .def("Transform", &BND_Point3dList::Transform, py::arg("xform"))
    .def("SetAllX", &BND_Point3dList::SetAllX, py::arg("x"))
    .def("SetAllY", &BND_Point3dList::SetAllY, py::arg("y"))
    .def("SetAllZ", &BND_Point3dList::SetAllZ, py::arg("z"))
    .def("Append", &BND_Point3dList::Append2, py::arg("count"), py::arg("points"))
    ;

  py::class_<BND_Polyline,BND_Point3dList>(m, "Polyline")
    .def(py::init<>())
    .def(py::init<int>(), py::arg("initialCapacity"))
    .def(py::init<const std::vector<ON_3dPoint>&>(), py::arg("collection"))
    .def_property_readonly("IsValid", &BND_Polyline::IsValid)
    .def_property_readonly("SegmentCount", &BND_Polyline::SegmentCount)
    .def_property_readonly("IsClosed", &BND_Polyline::IsClosed)
    .def("IsClosedWithinTolerance", &BND_Polyline::IsClosedWithinTolerance, py::arg("tolerance"))
    .def_property_readonly("Length", &BND_Polyline::Length)
    .def("PointAt", &BND_Polyline::PointAt, py::arg("t"))
    .def("TangentAt", &BND_Polyline::TangentAt, py::arg("t"))
    .def("ClosesPoint", &BND_Polyline::ClosestPoint, py::arg("testPoint"))
    .def("ClosestParameter", &BND_Polyline::ClosestParameter, py::arg("testPoint"))
    .def("ToNurbsCurve", &BND_Polyline::ToNurbsCurve)
    .def("ToPolylineCurve", &BND_Polyline::ToPolylineCurve)
    .def("GetSegments", &BND_Polyline::GetSegments)
    .def("SegmentAt", &BND_Polyline::SegmentAt, py::arg("index"))
    .def_static("CreateInscribedPolygon", &BND_Polyline::CreateInscribedPolygon, py::arg("circle"), py::arg("sideCount"))
    .def_static("CreateCircumscribedPolygon", &BND_Polyline::CreateCircumscribedPolygon, py::arg("circle"), py::arg("sideCount"))
    .def_static("CreateStarPolygon", &BND_Polyline::CreateStarPolygon, py::arg("circle"), py::arg("radius"), py::arg("cornerCounts"))
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPolylineBindings(void*)
{
  class_<BND_Point3dList>("Point3dList")
    .constructor<>()
    .constructor<int>()
    //.constructor<const std::vector<ON_3dPoint>&>()
    .property("capacity", &BND_Point3dList::GetCapacity, &BND_Point3dList::SetCapacity)
    .property("count", &BND_Point3dList::GetCount, &BND_Point3dList::SetCount)
    .function("get", &BND_Point3dList::GetPoint)
    .function("set", &BND_Point3dList::SetPoint)
    .function("clear", &BND_Point3dList::Clear)
    .function("insert", &BND_Point3dList::Insert)
    .function("removeAt", &BND_Point3dList::RemoveAt)
    .property("boundingBox", &BND_Point3dList::BoundingBox)
    .function("add", &BND_Point3dList::Add)
    .function("transform", &BND_Point3dList::Transform)
    .function("setAllX", &BND_Point3dList::SetAllX)
    .function("setAllY", &BND_Point3dList::SetAllY)
    .function("setAllZ", &BND_Point3dList::SetAllZ)
    .function("append", &BND_Point3dList::Append)
    ;

  class_<BND_Polyline, base<BND_Point3dList>>("Polyline")
    .constructor<>()
    .constructor<int>()
    //.constructor<const std::vector<ON_3dPoint>&>()
    .property("isValid", &BND_Polyline::IsValid)
    .property("segmentCount", &BND_Polyline::SegmentCount)
    .property("isClosed", &BND_Polyline::IsClosed)
    .function("isClosedWithinTolerance", &BND_Polyline::IsClosedWithinTolerance)
    .property("length", &BND_Polyline::Length)
    .function("pointAt", &BND_Polyline::PointAt)
    .function("tangentAt", &BND_Polyline::TangentAt)
    .function("closesPoint", &BND_Polyline::ClosestPoint)
    .function("closestParameter", &BND_Polyline::ClosestParameter)
    .function("toNurbsCurve", &BND_Polyline::ToNurbsCurve, allow_raw_pointers())
    .function("toPolylineCurve", &BND_Polyline::ToPolylineCurve, allow_raw_pointers())
    .function("getSegments", &BND_Polyline::GetSegments, allow_raw_pointers())
    .function("segmentAt", &BND_Polyline::SegmentAt, allow_raw_pointers())
    .class_function("createInscribedPolygon", &BND_Polyline::CreateInscribedPolygon, allow_raw_pointers())
    .class_function("createCircumscribedPolygon", &BND_Polyline::CreateCircumscribedPolygon, allow_raw_pointers())
    .class_function("createStarPolygon", &BND_Polyline::CreateStarPolygon, allow_raw_pointers())
    .class_function("createFromPoints", &BND_Polyline::CreateFromPoints, allow_raw_pointers()) 
    ;
}
#endif
