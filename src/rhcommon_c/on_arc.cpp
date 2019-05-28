#include "stdafx.h"

RH_C_FUNCTION void ON_Arc_Create1( ON_Arc* pArc, const ON_CIRCLE_STRUCT* pCircle, double angle_radians )
{
  if( pArc && pCircle )
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    pArc->Create(circle, angle_radians);
  }
}
RH_C_FUNCTION void ON_Arc_Create2( ON_Arc* pArc, const ON_CIRCLE_STRUCT* pCircle, ON_INTERVAL_STRUCT interval)
{
  if( pArc && pCircle )
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    const ON_Interval* _interval = (const ON_Interval*)&interval;
    pArc->Create(circle, *_interval);
  }
}
RH_C_FUNCTION void ON_Arc_Create3( ON_Arc* pArc, const ON_PLANE_STRUCT* plane, double radius, double angle_radians )
{
  if( pArc && plane )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    pArc->Create(temp, radius, angle_radians);
  }
}

RH_C_FUNCTION void ON_Arc_Create4( ON_Arc* pArc, ON_3DPOINT_STRUCT center, double radius, double angle_radians )
{
  if( pArc )
  {
    const ON_3dPoint* _center = (const ON_3dPoint*)&center;
    pArc->Create(*_center,radius,angle_radians);
  }
}

RH_C_FUNCTION void ON_Arc_Create5( ON_Arc* pArc, const ON_PLANE_STRUCT* plane, ON_3DPOINT_STRUCT center, double radius, double angle_radians )
{
  if( pArc && plane )
  {
    const ON_3dPoint* _center = (const ON_3dPoint*)&center;
    ON_Plane temp = FromPlaneStruct(*plane);
    pArc->Create(temp,*_center,radius,angle_radians);
  }
}

RH_C_FUNCTION void ON_Arc_Create6( ON_Arc* pArc, ON_3DPOINT_STRUCT p, ON_3DPOINT_STRUCT q, ON_3DPOINT_STRUCT r )
{
  if( pArc )
  {
    const ON_3dPoint* _p = (const ON_3dPoint*)&p;
    const ON_3dPoint* _q = (const ON_3dPoint*)&q;
    const ON_3dPoint* _r = (const ON_3dPoint*)&r;
    pArc->Create(*_p, *_q, *_r);
  }
}

RH_C_FUNCTION bool ON_Arc_IsValid(ON_Arc* pArc)
{
  bool rc = false;
  if( pArc )
  {
    pArc->plane.UpdateEquation();
    rc = pArc->IsValid() ? true : false;
  }
  return rc;
}

RH_C_FUNCTION void ON_Arc_BoundingBox(ON_Arc* pArc, ON_BoundingBox* bbox)
{
  if( pArc && bbox )
  {
    pArc->plane.UpdateEquation();
    if( !pArc->GetTightBoundingBox(*bbox) )
    {
      *bbox = pArc->BoundingBox();
    }
  }
}

RH_C_FUNCTION bool ON_Arc_Transform( ON_Arc* pArc, ON_Xform* xf)
{
  bool rc = false;
  if( pArc && xf )
  {
    pArc->plane.UpdateEquation();
    rc = pArc->Transform(*xf);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Arc_ClosestPointTo(ON_Arc* pArc,
                                         ON_3DPOINT_STRUCT testPoint,
                                         double* t)
{
  bool rc = false;
  const ON_3dPoint* _testPoint = (const ON_3dPoint*)&testPoint;

  if (pArc)
  {
    pArc->plane.UpdateEquation();
    rc = pArc->ClosestPointTo(*_testPoint, t);
  }

  return rc;
}

RH_C_FUNCTION int ON_Arc_GetNurbForm(ON_Arc* pArc, ON_NurbsCurve* nurbs_curve)
{
  int rc = 0;
  if( pArc && nurbs_curve )
  {
    pArc->plane.UpdateEquation();
    rc = pArc->GetNurbForm(*nurbs_curve);
  }
  return rc;
}

/*MANUAL*/RH_C_FUNCTION bool ON_Arc_Copy(ON_Arc* pRdnArc, ON_Arc* pRhCmnArc, bool rdn_to_rhc)
{
  bool rc = false;
  if( pRdnArc && pRhCmnArc )
  {
    if( rdn_to_rhc )
      *pRhCmnArc = *pRdnArc;
    else
      *pRdnArc = *pRhCmnArc;
    rc = true;
  }
  return rc;
}

