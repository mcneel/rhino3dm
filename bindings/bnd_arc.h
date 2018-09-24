#include "bindings.h"

#pragma once

class BND_Arc : public ON_Arc
{
public:
  BND_Arc(ON_3dPoint a, double b, double c);

  BND_NurbsCurve* ToNurbsCurve();
};
