#include "bindings.h"

BND_SurfaceProxy::BND_SurfaceProxy(ON_SurfaceProxy* surfaceproxy, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(surfaceproxy, compref);
}

void BND_SurfaceProxy::SetTrackedPointer(ON_SurfaceProxy* surfaceproxy, const ON_ModelComponentReference* compref)
{
  m_surfaceproxy = surfaceproxy;
  BND_Surface::SetTrackedPointer(surfaceproxy, compref);
}

BND_SurfaceProxy::BND_SurfaceProxy()
{
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSurfaceProxyBindings(pybind11::module& m)
{
  py::class_<BND_SurfaceProxy, BND_Surface>(m, "SurfaceProxy")
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSurfaceProxyBindings(void*)
{
  class_<BND_SurfaceProxy, base<BND_Surface>>("SurfaceProxy")
    ;
}
#endif
