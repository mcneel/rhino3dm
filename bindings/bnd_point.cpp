#include "bindings.h"

BND_Interval::BND_Interval(const ON_Interval& i)
{
  m_t0 = i.m_t[0];
  m_t1 = i.m_t[1];
}

ON_3dPoint BND_Point3d::Transform(const ON_3dPoint& pt, const BND_Xform& transform)
{
  ON_3dPoint rc = transform * pt;
  return rc;
}
