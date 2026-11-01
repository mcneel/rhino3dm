#include "bnd_boundingbox.h"

using namespace bnd_shared;

BND_DICT BND_BoundingBox_Encode(const BND_BoundingBox& box)
{
  BND_DICT d = RH3DM_DictNew();
  RH3DM_DictSet(d, "Min", XYZ(box.m_min));
  RH3DM_DictSet(d, "Max", XYZ(box.m_max));
  return d;
}

BND_BoundingBox BND_BoundingBox_Decode(BND_DICT jsonObject)
{
  BND_BoundingBox box;
  box.m_min = PointFromDict(RH3DM_DictGet<BND_DICT>(jsonObject, "Min"));
  box.m_max = PointFromDict(RH3DM_DictGet<BND_DICT>(jsonObject, "Max"));
  return box;
}
