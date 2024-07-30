#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initSurfaceProxyBindings(py::module_& m);
#else
namespace py = pybind11;
void initSurfaceProxyBindings(py::module& m);
#endif

#else
void initSurfaceProxyBindings(void* m);
#endif

class BND_SurfaceProxy : public BND_Surface
{
  ON_SurfaceProxy* m_surfaceproxy = nullptr;
public:
  BND_SurfaceProxy(ON_SurfaceProxy* surfaceproxy, const ON_ModelComponentReference* compref);

protected:
  BND_SurfaceProxy();
  void SetTrackedPointer(ON_SurfaceProxy* surfaceproxy, const ON_ModelComponentReference* compref);
};
