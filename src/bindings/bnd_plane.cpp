#include "bnd_plane.h"
#include "bnd_point3d_gen.h"
#include "bnd_vector3d_gen.h"

using namespace bnd_shared;

static BND_Vector3d VectorFromDict(const BND_DICT& d)
{
  BND_Point3d p = PointFromDict(d);
  BND_Vector3d v;
  v.m_x = p.m_x; v.m_y = p.m_y; v.m_z = p.m_z;
  return v;
}

BND_DICT BND_Plane_Encode(const BND_Plane& plane)
{
  BND_DICT d = RH3DM_DictNew();
  RH3DM_DictSet(d, "Origin", XYZ(plane.m_origin));
  RH3DM_DictSet(d, "XAxis", XYZ(plane.m_xaxis));
  RH3DM_DictSet(d, "YAxis", XYZ(plane.m_yaxis));
  RH3DM_DictSet(d, "ZAxis", XYZ(plane.m_zaxis));
  return d;
}

BND_Plane BND_Plane_Decode(BND_DICT jsonObject)
{
  BND_Plane plane;
  plane.m_origin = PointFromDict(RH3DM_DictGet<BND_DICT>(jsonObject, "Origin"));
  plane.m_xaxis = VectorFromDict(RH3DM_DictGet<BND_DICT>(jsonObject, "XAxis"));
  plane.m_yaxis = VectorFromDict(RH3DM_DictGet<BND_DICT>(jsonObject, "YAxis"));
  plane.m_zaxis = VectorFromDict(RH3DM_DictGet<BND_DICT>(jsonObject, "ZAxis"));
  return plane;
}
