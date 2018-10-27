#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPointGeometryBindings(pybind11::module& m);
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
};
