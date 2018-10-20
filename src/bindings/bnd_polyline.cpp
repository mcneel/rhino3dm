#include "bindings.h"

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
  if (!rc->m_polyline.CreateInscribedPolygon(circle.ToONCircle(), sideCount))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_Polyline* BND_Polyline::CreateCircumscribedPolygon(BND_Circle& circle, int sideCount)
{
  BND_Polyline* rc = new BND_Polyline();
  if (!rc->m_polyline.CreateCircumscribedPolygon(circle.ToONCircle(), sideCount))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_Polyline* BND_Polyline::CreateStarPolygon(BND_Circle& circle, double radius, int cornerCount)
{
  BND_Polyline* rc = new BND_Polyline();
  if (!rc->m_polyline.CreateStarPolygon(circle.ToONCircle(), radius, cornerCount))
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
    .def(py::init<int>())
    .def_property("Capacity", &BND_Point3dList::GetCapacity, &BND_Point3dList::SetCapacity)
    .def_property("Count", &BND_Point3dList::GetCount, &BND_Point3dList::SetCount)
    .def("__len__", &BND_Point3dList::GetCount)
    .def("__getitem__", &BND_Point3dList::GetPoint)
    .def("__setitem__", &BND_Point3dList::SetPoint)
    .def("Clear", &BND_Point3dList::Clear)
    .def("Insert", &BND_Point3dList::Insert)
    .def("RemoveAt", &BND_Point3dList::RemoveAt)
    .def_property_readonly("BoundingBox", &BND_Point3dList::BoundingBox)
    .def("Add", &BND_Point3dList::Add)
    .def("Transform", &BND_Point3dList::Transform)
    .def("SetAllX", &BND_Point3dList::SetAllX)
    .def("SetAllY", &BND_Point3dList::SetAllY)
    .def("SetAllZ", &BND_Point3dList::SetAllZ)
    ;

  py::class_<BND_Polyline,BND_Point3dList>(m, "Polyline")
    .def(py::init<>())
    .def(py::init<int>())
    .def_property_readonly("IsValid", &BND_Polyline::IsValid)
    .def_property_readonly("SegmentCount", &BND_Polyline::SegmentCount)
    .def_property_readonly("IsClosed", &BND_Polyline::IsClosed)
    .def("IsClosedWithinTolerance", &BND_Polyline::IsClosedWithinTolerance)
    .def_property_readonly("Length", &BND_Polyline::Length)
    .def("PointAt", &BND_Polyline::PointAt)
    .def("TangentAt", &BND_Polyline::TangentAt)
    .def("ClosesPoint", &BND_Polyline::ClosestPoint)
    .def("ClosestParameter", &BND_Polyline::ClosestParameter)
    .def("ToNurbsCurve", &BND_Polyline::ToNurbsCurve)
    .def("ToPolylineCurve", &BND_Polyline::ToPolylineCurve)
    .def_static("CreateInscribedPolygon", &BND_Polyline::CreateInscribedPolygon)
    .def_static("CreateCircumscribedPolygon", &BND_Polyline::CreateCircumscribedPolygon)
    .def_static("CreateStarPolygon", &BND_Polyline::CreateStarPolygon)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPolylineBindings(void*)
{
  class_<BND_Polyline>("Polyline")
    .constructor<>()
    ;
}
#endif
