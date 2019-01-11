#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initRevSurfaceBindings(pybind11::module& m);
#else
void initRevSurfaceBindings(void* m);
#endif

class BND_RevSurface : public BND_Surface
{
public:
  ON_RevSurface* m_revsurface = nullptr;
protected:
  void SetTrackedPointer(ON_RevSurface* revsurface, const ON_ModelComponentReference* compref);

public:
  BND_RevSurface();
  BND_RevSurface(ON_RevSurface* revsurface, const ON_ModelComponentReference* compref);
};
