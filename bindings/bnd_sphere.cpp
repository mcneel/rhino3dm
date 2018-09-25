#include "bindings.h"

BND_Sphere::BND_Sphere(ON_3dPoint center, double radius)
: ON_Sphere(center, radius)
{
}

BND_Brep* BND_Sphere::ToBrep()
{
  ON_Brep* brep = ON_BrepSphere(*this);
  if(brep)
    return new BND_Brep(brep);
  return nullptr;
}
