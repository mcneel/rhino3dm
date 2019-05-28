#include "stdafx.h"

RH_C_FUNCTION ON_ArcCurve* ON_ArcCurve_New( ON_ArcCurve* pOther )
{
  if(nullptr == pOther )
    return new ON_ArcCurve();
  return new ON_ArcCurve(*pOther);
}

RH_C_FUNCTION ON_ArcCurve* ON_ArcCurve_New2( ON_Arc* arc )
{
  if(nullptr == arc )
    return new ON_ArcCurve();
  arc->plane.UpdateEquation();
  return new ON_ArcCurve(*arc);
}

RH_C_FUNCTION ON_ArcCurve* ON_ArcCurve_New3( ON_Arc* arc, double t0, double t1 )
{
  if(nullptr == arc )
    return new ON_ArcCurve();
  arc->plane.UpdateEquation();
  return new ON_ArcCurve(*arc, t0, t1);
}

ON_ArcCurve* ON_ArcCurve_New4( const ON_CIRCLE_STRUCT* pCircle )
{
  if( nullptr == pCircle )
    return new ON_ArcCurve();
  ON_Circle circle = FromCircleStruct(*pCircle);
  return new ON_ArcCurve(circle);
}

RH_C_FUNCTION ON_ArcCurve* ON_ArcCurve_New5( const ON_CIRCLE_STRUCT* pCircle, double t0, double t1 )
{
  if(nullptr == pCircle )
    return new ON_ArcCurve();
  ON_Circle circle = FromCircleStruct(*pCircle);
  return new ON_ArcCurve(circle, t0, t1);
}

bool ON_ArcCurve_IsCircle( const ON_ArcCurve* constArcCurve)
{
  bool rc = false;
  if(constArcCurve)
    rc = constArcCurve->IsCircle();
  return rc;
}

RH_C_FUNCTION double ON_ArcCurve_GetDouble(const ON_ArcCurve* pCurve, int which)
{
  const int idxRadius = 0;
  const int idxAngleRadians = 1;
  const int idxAngleDegrees = 2;
  double rc = 0;
  if( pCurve )
  {
    if( idxRadius == which )
      rc = pCurve->Radius();
    else if(idxAngleRadians == which )
      rc = pCurve->AngleRadians();
    else if( idxAngleDegrees == which )
      rc = pCurve->AngleDegrees();
  }
  return rc;
}

RH_C_FUNCTION void ON_ArcCurve_GetArc(const ON_ArcCurve* pConstCurve, ON_Arc* rc)
{
  if( pConstCurve && rc )
  {
    *rc = pConstCurve->m_arc;
    rc->plane.UpdateEquation();
  }
}
