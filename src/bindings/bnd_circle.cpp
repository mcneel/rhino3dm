#include "bnd_circle.h"
#include "bnd_plane.h"

// the old Circle.Encode's keys are lower case (Sphere's are not)
BND_DICT BND_Circle_Encode(const BND_Circle& circle)
{
  BND_DICT d = RH3DM_DictNew();
  RH3DM_DictSet(d, "radius", circle.m_radius);
  RH3DM_DictSet(d, "plane", BND_Plane_Encode(circle.m_plane));
  return d;
}
