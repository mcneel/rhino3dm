#include "bindings.h"

BND_Arc::BND_Arc(ON_3dPoint a, double b, double c) : ON_Arc(a,b,c)
{
}

BND_NurbsCurve* BND_Arc::ToNurbsCurve()
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  if(0==GetNurbForm(*nc))
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc);
}
