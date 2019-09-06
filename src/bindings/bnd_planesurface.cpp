#include "bindings.h"

BND_PlaneSurface::BND_PlaneSurface()
{
  SetTrackedPointer(new ON_PlaneSurface(), nullptr);
}

BND_PlaneSurface::BND_PlaneSurface(ON_PlaneSurface* planesurface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(planesurface, compref);
}

void BND_PlaneSurface::SetTrackedPointer(ON_PlaneSurface* planesurface, const ON_ModelComponentReference* compref)
{
  m_planesurface = planesurface;
  BND_Surface::SetTrackedPointer(planesurface, compref);
}

BND_PlaneSurface::BND_PlaneSurface(const BND_Plane& plane, const BND_Interval& xExtents, const BND_Interval& yExtents)
{
  ON_Plane _plane = plane.ToOnPlane();
  ON_Interval x(xExtents.m_t0, xExtents.m_t1);
  ON_Interval y(yExtents.m_t0, yExtents.m_t1);
  m_planesurface = new ON_PlaneSurface(_plane);
  m_planesurface->SetExtents(0, x, true);
  m_planesurface->SetExtents(1, y, true);
  SetTrackedPointer(m_planesurface, nullptr);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPlaneSurfaceBindings(pybind11::module& m)
{
  py::class_<BND_PlaneSurface, BND_Surface>(m, "PlaneSurface")
    .def(py::init<>())
    .def(py::init<const BND_Plane&, const BND_Interval&, const BND_Interval&>(), py::arg("plane"), py::arg("xExtents"), py::arg("yExtents"))
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPlaneSurfaceBindings(void*)
{
  class_<BND_PlaneSurface, base<BND_Surface>>("PlaneSurface")
    .constructor<>()
    ;
}
#endif
