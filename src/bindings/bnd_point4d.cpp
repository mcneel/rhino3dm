#include "bnd_point4d.h"

using namespace bnd_shared;

BND_DICT BND_Point4d_Encode(const BND_Point4d& p)
{
  BND_DICT d = XYZ(p);
  RH3DM_DictSet(d, "W", p.m_w);
  return d;
}
