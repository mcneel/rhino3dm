#include "bindings.h"

#pragma once

class BND_Sphere : public ON_Sphere
{
public:
  BND_Sphere(ON_3dPoint center, double radius);
  class BND_Brep* ToBrep();
};
