#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initBoundingBoxBindings(pybind11::module& m);
#else
void initBoundingBoxBindings();
#endif

class BND_BoundingBox : public ON_BoundingBox
{
public:
  BND_BoundingBox(const ON_3dPoint& min, const ON_3dPoint& max);
  BND_BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ);
  BND_BoundingBox(const ON_BoundingBox& bbox);

  bool Transform(const ON_Xform& xform);
};
