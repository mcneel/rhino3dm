#include "bindings.h"

BND_BoundingBox::BND_BoundingBox(const ON_3dPoint& min, const ON_3dPoint& max)
: ON_BoundingBox(min, max)
{

}

BND_BoundingBox::BND_BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
: ON_BoundingBox(ON_3dPoint(minX, minY, minZ), ON_3dPoint(maxX, maxY, maxZ))
{

}

BND_BoundingBox::BND_BoundingBox(const ON_BoundingBox& bbox)
{
  m_min = bbox.m_min;
  m_max = bbox.m_max;
}

bool BND_BoundingBox::Transform(const ON_Xform& xform)
{
  return ON_BoundingBox::Transform(xform);
}
