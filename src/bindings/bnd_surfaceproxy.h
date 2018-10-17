#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSurfaceProxyBindings(pybind11::module& m);
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
