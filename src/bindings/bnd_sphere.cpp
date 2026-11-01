#include "bnd_sphere.h"
#include "bnd_plane.h"

BND_DICT BND_Sphere_Encode(const BND_Sphere& sphere)
{
  BND_DICT d = RH3DM_DictNew();
  RH3DM_DictSet(d, "Radius", sphere.m_radius);
  RH3DM_DictSet(d, "EquatorialPlane", BND_Plane_Encode(sphere.m_plane));
  return d;
}

// the whole equatorial plane, as the old Python Decode read it (the old JS one kept only its origin)
BND_Sphere BND_Sphere_Decode(BND_DICT jsonObject)
{
  BND_Sphere sphere;
  sphere.m_radius = RH3DM_DictGet<double>(jsonObject, "Radius");
  sphere.m_plane = BND_Plane_Decode(RH3DM_DictGet<BND_DICT>(jsonObject, "EquatorialPlane"));
  return sphere;
}
