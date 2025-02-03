#include "bindings.h"

BND_Surface::BND_Surface()
{

}

BND_Surface::BND_Surface(ON_Surface* surface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(surface, compref);
}

void BND_Surface::SetTrackedPointer(ON_Surface* surface, const ON_ModelComponentReference* compref)
{
  m_surface = surface;
  BND_GeometryBase::SetTrackedPointer(surface, compref);
}

BND_TUPLE BND_Surface::GetSpanVector(int direction)
{
  int count = m_surface->SpanCount(direction) + 1;
  if (count < 1)
    return NullTuple();

  ON_SimpleArray<double> span(count);
  span.SetCapacity(count);
  span.SetCount(count);

  if (m_surface->GetSpanVector(direction, span.Array()))
  {
    BND_TUPLE rc = CreateTuple(count);
    for (int i = 0; i < count; i++)
      SetTuple(rc, i, span[i]);
    return rc;
  }
  return NullTuple();
}

BND_Curve* BND_Surface::IsoCurve(int direction, double constantParameter) const
{
  ON_Curve* crv = m_surface->IsoCurve(direction, constantParameter);
  BND_Curve* rc = dynamic_cast<BND_Curve*>(BND_CommonObject::CreateWrapper(crv, nullptr));
  return rc;
}

BND_NurbsSurface* BND_Surface::ToNurbsSurfaceDefault() const
{
  int accuracy{ 0 };
  ON_NurbsSurface* p_NurbForm = ON_NurbsSurface::New();
  if (m_surface != nullptr)
  {
    accuracy = m_surface->GetNurbForm(*p_NurbForm, 0.0);
    if (!accuracy)
    {
      delete p_NurbForm;
      p_NurbForm = nullptr;
    }
  }

  if (p_NurbForm == nullptr)
    return nullptr;
  return new BND_NurbsSurface(p_NurbForm, &m_component_ref);
}

BND_TUPLE BND_Surface::ToNurbsSurface(double tolerance) const
{
  int accuracy{0};
  ON_NurbsSurface* p_NurbForm = ON_NurbsSurface::New();
  if (m_surface != nullptr)
  {
    accuracy = m_surface->GetNurbForm(*p_NurbForm, tolerance);
    if (!accuracy)
    {
      delete p_NurbForm;
      p_NurbForm = nullptr;
    }
  }

  if (p_NurbForm == nullptr)
    return NullTuple();

#if defined(ON_PYTHON_COMPILE) && defined(NANOBIND)
  BND_TUPLE rc = py::make_tuple(new BND_NurbsSurface(p_NurbForm, &m_component_ref), accuracy);
#else
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, new BND_NurbsSurface(p_NurbForm, &m_component_ref));
  SetTuple(rc, 1, accuracy);
#endif
  return rc;
}

BND_TUPLE BND_Surface::FrameAt(double u, double v) {
  ON_Plane frame;
  bool success = false;
  if (m_surface != nullptr)
  {
    success = m_surface->FrameAt(u, v, frame);
  }

#if defined(ON_PYTHON_COMPILE) && defined(NANOBIND)
  BND_TUPLE rc = py::make_tuple(success, BND_Plane::FromOnPlane(frame));
#else
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, BND_Plane::FromOnPlane(frame));
#endif
  return rc;
}

BND_TUPLE BND_Surface::GetSurfaceParameterFromNurbsFormParameter(double nurbsS, double nurbsT) const
{
  double s = 0;
  double t = 0;
  bool success = m_surface->GetSurfaceParameterFromNurbFormParameter(nurbsS, nurbsT, &s, &t);

#if defined(ON_PYTHON_COMPILE) && defined(NANOBIND)
  BND_TUPLE rc = py::make_tuple(success, s, t);
#else
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, s);
  SetTuple(rc, 2, t);
#endif
  return rc;
}

BND_TUPLE BND_Surface::GetNurbsFormParameterFromSurfaceParameter(double surfaceS, double surfaceT) const
{
  double s = 0;
  double t = 0;
  bool success = m_surface->GetNurbFormParameterFromSurfaceParameter(surfaceS, surfaceT, &s, &t);

#if defined(ON_PYTHON_COMPILE) && defined(NANOBIND)
  BND_TUPLE rc = py::make_tuple(success, s, t);
#else
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, s);
  SetTuple(rc, 2, t);
#endif
  return rc;
}


#if defined(ON_PYTHON_COMPILE)

void initSurfaceBindings(rh3dmpymodule& m)
{
  py::class_<BND_Surface, BND_GeometryBase>(m, "Surface")
    .def_property_readonly("IsSolid", &BND_Surface::IsSolid)
    .def("SetDomain", &BND_Surface::SetDomain, py::arg("direction"), py::arg("domain"))
    .def("Degree", &BND_Surface::Degree, py::arg("direction"))
    .def("SpanCount", &BND_Surface::SpanCount, py::arg("direction"))
    .def("PointAt", &BND_Surface::PointAt, py::arg("u"), py::arg("v"))
    .def("FrameAt", &BND_Surface::FrameAt, py::arg("u"), py::arg("v"))
    .def("Domain", &BND_Surface::Domain, py::arg("direction"))
    .def("GetSpanVector", &BND_Surface::GetSpanVector, py::arg("direction"))
    .def("NormalAt", &BND_Surface::NormalAt, py::arg("u"), py::arg("v"))
    .def("IsClosed", &BND_Surface::IsClosed, py::arg("direction"))
    .def("IsPeriodic", &BND_Surface::IsPeriodic, py::arg("direction"))
    .def("IsSingular", &BND_Surface::IsSingular, py::arg("side"))
    .def("IsAtSingularity", &BND_Surface::IsAtSingularity, py::arg("u"), py::arg("v"), py::arg("exact"))
    .def("IsAtSeam", &BND_Surface::IsAtSeam, py::arg("u"), py::arg("v"))
    .def("IsoCurve", &BND_Surface::IsoCurve, py::arg("direction"), py::arg("constantParameter"))
    .def("ToNurbsSurface", &BND_Surface::ToNurbsSurfaceDefault)
    .def("ToNurbsSurface", &BND_Surface::ToNurbsSurface, py::arg("tolerance"))
    .def("IsPlanar", &BND_Surface::IsPlanar, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsSphere", &BND_Surface::IsSphere, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsCylinder", &BND_Surface::IsCylinder, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsCone", &BND_Surface::IsCone, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("IsTorus", &BND_Surface::IsTorus, py::arg("tolerance")=ON_ZERO_TOLERANCE)
    .def("GetSurfaceParameterFromNurbsFormParameter", &BND_Surface::GetSurfaceParameterFromNurbsFormParameter, py::arg("nurbsS"), py::arg("nurbsT"))
    .def("GetNurbsFormParameterFromSurfaceParameter", &BND_Surface::GetNurbsFormParameterFromSurfaceParameter, py::arg("surfaceS"), py::arg("surfaceT"))
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSurfaceBindings(void*)
{
  class_<BND_Surface, base<BND_GeometryBase>>("Surface")
    .property("isSolid", &BND_Surface::IsSolid)
    .function("setDomain", &BND_Surface::SetDomain)
    .function("degree", &BND_Surface::Degree)
    .function("spanCount", &BND_Surface::SpanCount)
    .function("pointAt", &BND_Surface::PointAt)
    .function("domain", &BND_Surface::Domain)
    .function("getSpanVector", &BND_Surface::GetSpanVector)
    .function("normalAt", &BND_Surface::NormalAt)
    .function("frameAt", &BND_Surface::FrameAt)
    .function("isClosed", &BND_Surface::IsClosed)
    .function("isPeriodic", &BND_Surface::IsPeriodic)
    .function("isSingular", &BND_Surface::IsSingular)
    .function("isAtSingularity", &BND_Surface::IsAtSingularity)
    .function("isAtSeam", &BND_Surface::IsAtSeam)
    .function("isoCurve", &BND_Surface::IsoCurve, allow_raw_pointers())
    .function("toNurbsSurface", &BND_Surface::ToNurbsSurfaceDefault, allow_raw_pointers())
    .function("toNurbsSurfaceTolerance", &BND_Surface::ToNurbsSurface, allow_raw_pointers())
    .function("isPlanar", &BND_Surface::IsPlanar)
    .function("isSphere", &BND_Surface::IsSphere)
    .function("isCylinder", &BND_Surface::IsCylinder)
    .function("isCone", &BND_Surface::IsCone)
    .function("isTorus", &BND_Surface::IsTorus)
    .function("getSurfaceParameterFromNurbsFormParameter", &BND_Surface::GetSurfaceParameterFromNurbsFormParameter)
    .function("getNurbsFormParameterFromSurfaceParameter", &BND_Surface::GetNurbsFormParameterFromSurfaceParameter)
    ;
}
#endif
