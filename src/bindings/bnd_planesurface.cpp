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

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPlaneSurfaceBindings(pybind11::module& m)
{
  py::class_<BND_PlaneSurface, BND_Surface>(m, "PlaneSurface")
    .def(py::init<>())
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
