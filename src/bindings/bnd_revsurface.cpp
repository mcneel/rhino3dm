#include "bindings.h"

BND_RevSurface::BND_RevSurface()
{
  SetTrackedPointer(new ON_RevSurface(), nullptr);
}

BND_RevSurface::BND_RevSurface(ON_RevSurface* revsrf, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(revsrf, compref);
}

void BND_RevSurface::SetTrackedPointer(ON_RevSurface* revsrf, const ON_ModelComponentReference* compref)
{
  m_revsurface = revsrf;
  BND_Surface::SetTrackedPointer(revsrf, compref);
}

BND_RevSurface* BND_RevSurface::Create1(const BND_Curve& revoluteCurve, const ON_Line& axisOfRevolution, double startAngle, double endAngle)
{
  ON_RevSurface* rc = ON_RevSurface::New();
  if (rc)
  {
    rc->m_curve = revoluteCurve.m_curve->DuplicateCurve();
    rc->m_axis = axisOfRevolution;
    ON_Interval domain(startAngle, endAngle);
    if (domain.IsDecreasing())
      rc->m_angle.Set(domain.m_t[0], domain.m_t[1] + 2.0*ON_PI);
    else
      rc->m_angle.Set(domain.m_t[0], domain.m_t[1]);
    return new BND_RevSurface(rc, nullptr);
  }
  return nullptr;
}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)

void initRevSurfaceBindings(rh3dmpymodule& m)
{
  py::class_<BND_RevSurface, BND_Surface>(m, "RevSurface")
    .def(py::init<>())
    .def_static("Create", &BND_RevSurface::Create1, py::arg("revoluteCurve"), py::arg("axisOfRevolution"), py::arg("startAngle"), py::arg("endAngle"))
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initRevSurfaceBindings(void*)
{
  class_<BND_RevSurface, base<BND_Surface>>("RevSurface")
    .constructor<>()
    .class_function("create", &BND_RevSurface::Create1, allow_raw_pointers())
    ;
}
#endif
