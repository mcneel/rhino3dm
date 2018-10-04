#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initGeometryBindings(pybind11::module& m);
#endif

class BND_Geometry : public BND_Object
{
  ON_Geometry* m_geometry = nullptr;
protected:
  BND_Geometry();
  void SetTrackedPointer(ON_Geometry* geometry, const ON_ModelComponentReference* compref);

public:
  BND_Geometry(ON_Geometry* geometry, const ON_ModelComponentReference* compref);
  int Dimension() const;
  BND_BoundingBox BoundingBox() const;
  bool Rotate(double rotation_angle, const ON_3dVector& rotation_axis, const ON_3dPoint& rotation_center);
};
