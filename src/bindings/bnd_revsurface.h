#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initRevSurfaceBindings(rh3dmpymodule& m);
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

  static BND_RevSurface* Create1(const class BND_Curve& revoluteCurve, const ON_Line& axisOfRevolution, double startAngleRadians, double endAngleRadians);
};
