#include "bindings.h"

BND_Point3dList::BND_Point3dList(const std::vector<ON_3dPoint>& points)
{
  int count = (int)points.size();
  const ON_3dPoint* pts = points.data();
  m_polyline.Append(count, pts);
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

#if defined(ON_PYTHON_COMPILE)
BND_Point3dList BND_Point3dList::FromPythonObject(pybind11::object points)
{
  BND_Point3dList list;
  pybind11::tuple _tuple;
  pybind11::list _list;

  // I realize this is dumb and not the appropriate way to cast, but I can't
  // figure out how to dynamic_cast<> object types in pybind11
  if (pybind11::isinstance(points, _tuple.get_type()) || pybind11::isinstance(points, _list.get_type()))
  {
    for (auto item : points)
    {
      if (pybind11::isinstance(item, _tuple.get_type()))
      {
        _tuple = item.cast<pybind11::tuple>();
        list.Add(_tuple[0].cast<double>(), _tuple[1].cast<double>(), _tuple[2].cast<double>());
      }
      else if (pybind11::isinstance(item, _list.get_type()))
      {
        _list = item.cast<pybind11::list>();
        list.Add(_list[0].cast<double>(), _list[1].cast<double>(), _list[2].cast<double>());
      }
      else
      {
        ON_3dPoint point = item.cast<ON_3dPoint>();
        list.Add(point.x, point.y, point.z);
      }
    }
  }
  else
  {
    list = points.cast<BND_Point3dList>();
  }

  return list;
}
#endif

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
    ;

  class_<BND_Polyline, base<BND_Point3dList>>("Polyline")
    .constructor<>()
    .constructor<int>()
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
    .class_function("createInscribedPolygon", &BND_Polyline::CreateInscribedPolygon, allow_raw_pointers())
    .class_function("createCircumscribedPolygon", &BND_Polyline::CreateCircumscribedPolygon, allow_raw_pointers())
    .class_function("createStarPolygon", &BND_Polyline::CreateStarPolygon, allow_raw_pointers())
    ;
}
#endif
