#include "bindings.h"

BND_Geometry::BND_Geometry()
{
}

BND_Geometry::BND_Geometry(ON_Geometry* geometry)
{
  m_geometry.reset(geometry);
  m_object = m_geometry;
}

void BND_Geometry::SetSharedGeometryPointer(const std::shared_ptr<ON_Geometry>& sp)
{
  m_geometry = sp;
  m_object = sp;
}

int BND_Geometry::Dimension() const
{
  return m_geometry->Dimension();
}

BND_BoundingBox BND_Geometry::BoundingBox() const
{
  ON_BoundingBox bbox = m_geometry->BoundingBox();
  return BND_BoundingBox(bbox);
}

bool BND_Geometry::Rotate(double rotation_angle, const ON_3dVector& rotation_axis, const ON_3dPoint& rotation_center)
{
  return m_geometry->Rotate(rotation_angle, rotation_axis, rotation_center);
}
