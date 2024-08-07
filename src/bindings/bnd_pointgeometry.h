#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPointGeometryBindings(rh3dmpymodule& m);
#else
void initPointGeometryBindings(void* m);
#endif

class BND_Point : public BND_GeometryBase
{
  ON_Point* m_point = nullptr;
protected:
  void SetTrackedPointer(ON_Point* point, const ON_ModelComponentReference* compref);

public:
  BND_Point();
  BND_Point(ON_Point* point, const ON_ModelComponentReference* compref);
  BND_Point(ON_3dPoint location);

  ON_3dPoint GetLocation() const { return m_point->point; }
  void SetLocation(ON_3dPoint loc) { m_point->point = loc; }
};
