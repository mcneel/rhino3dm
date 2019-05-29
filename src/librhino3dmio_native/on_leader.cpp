#include "stdafx.h"

RH_C_FUNCTION ON_Leader* ON_V6_Leader_New()
{
  return new ON_Leader();
}


RH_C_FUNCTION ON_Leader* ON_V6_Leader_Create(const RHMONO_STRING* rtfstr, const ON_PLANE_STRUCT* plane, int pointCount, /*ARRAY*/const ON_3dPoint* points, const ON_DimStyle* style)
{
  if (nullptr == plane)
    return nullptr;
  ON_Leader* rc = new ON_Leader();
  INPUTSTRINGCOERCE(_rtfstr, rtfstr);
  ON_Plane pl = FromPlaneStruct(*plane);
  if (!rc->Create(_rtfstr, style, pointCount, points, pl, false, 0.0))
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}


// Points 
RH_C_FUNCTION void ON_V6_Leader_Set3dPoints(ON_Leader* leader, int count, /*ARRAY*/const ON_3dPoint* points)
{
  if (leader && count > 0 && points)
  {
    leader->SetPoints3d(count, points);
  }
}

RH_C_FUNCTION int ON_V6_Leader_PointCount(const ON_Leader* constLeader)
{
  if (constLeader)
    return constLeader->PointCount();
  return 0;
}

RH_C_FUNCTION void ON_V6_Leader_Get2dPoints(const ON_Leader* constLeader, ON_2dPointArray* points_out)
{
  if (constLeader && points_out)
  {
    const ON_2dPointArray& points = constLeader->Points2d();
    points_out->Append(points.Count(), points.Array());
  }
}

RH_C_FUNCTION void ON_V6_Leader_Set2dPoints(ON_Leader* leader, int count, /*ARRAY*/const ON_2dPoint* points)
{
  if (leader && count > 0 && points)
  {
    leader->SetPoints2d(count, points);
  }
}

// Curve
RH_C_FUNCTION const ON_NurbsCurve* ON_V6_Leader_Curve(const ON_Leader* constLeader, const ON_DimStyle* dimstyle)
{
  ON_NurbsCurve* curve = nullptr;
  if (constLeader)
  {
    const ON_NurbsCurve* constcrv = constLeader->Curve(dimstyle);
    if (nullptr != constcrv)
      curve = constcrv->Duplicate();
  }
  return curve;
}

