#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPlaneSurfaceBindings(pybind11::module& m);
#else
void initPlaneSurfaceBindings(void* m);
#endif

class BND_PlaneSurface : public BND_Surface
{
  ON_PlaneSurface* m_planesurface = nullptr;
protected:
  void SetTrackedPointer(ON_PlaneSurface* planesurface, const ON_ModelComponentReference* compref);

public:
  BND_PlaneSurface();
  BND_PlaneSurface(ON_PlaneSurface* planesurface, const ON_ModelComponentReference* compref);
};
