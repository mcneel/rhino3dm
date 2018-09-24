#include "bindings.h"

BND_Circle::BND_Circle(double radius)
{
  m_plane = BND_Plane::WorldXY();
  m_radius = radius;
}
BND_Circle::BND_Circle(BND_Plane plane, double radius)
{
  m_plane = plane;
  m_radius = radius;
}
BND_Circle::BND_Circle(ON_3dPoint center, double radius)
{
  m_plane = BND_Plane::WorldXY();
  m_plane.m_origin = center;
  m_radius = radius;
}

ON_3dPoint BND_Circle::PointAt(double t) const
{
  ON_Plane plane = m_plane.ToOnPlane();
  ON_Circle circle(plane, m_radius);
  return circle.PointAt(t);
}

BND_NurbsCurve* BND_Circle::ToNurbsCurve() const
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  ON_Plane plane = m_plane.ToOnPlane();
  ON_Circle circle(plane, m_radius);
  if( 0==circle.GetNurbForm(*nc) )
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc);
}
