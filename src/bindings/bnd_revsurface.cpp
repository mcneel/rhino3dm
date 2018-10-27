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

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initRevSurfaceBindings(pybind11::module& m)
{
  py::class_<BND_RevSurface, BND_Surface>(m, "RevSurface")
    .def(py::init<>())
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initRevSurfaceBindings(void*)
{
  class_<BND_RevSurface, base<BND_Surface>>("RevSurface")
    .constructor<>()
    ;
}
#endif
