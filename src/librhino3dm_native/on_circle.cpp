#include "stdafx.h"

void CopyToCircleStruct(ON_CIRCLE_STRUCT& cs, const ON_Circle& circle)
{
  memcpy(&cs, &circle, sizeof(ON_CIRCLE_STRUCT));
}

ON_Circle FromCircleStruct(const ON_CIRCLE_STRUCT& cs)
{
  ON_Circle circle;
  memcpy(&circle, &cs, sizeof(ON_CIRCLE_STRUCT));
  circle.plane.UpdateEquation();
  return circle;
}


RH_C_FUNCTION void ON_Circle_Create3Pt(ON_CIRCLE_STRUCT* c, ON_3DPOINT_STRUCT p, ON_3DPOINT_STRUCT q, ON_3DPOINT_STRUCT r)
{
  if( c )
  {
    const ON_3dPoint* _p = (const ON_3dPoint*)&p;
    const ON_3dPoint* _q = (const ON_3dPoint*)&q;
    const ON_3dPoint* _r = (const ON_3dPoint*)&r;
    ON_Circle _c(*_p, *_q, *_r);
    _c.plane.UpdateEquation();
    CopyToCircleStruct(*c, _c);
  }
}

RH_C_FUNCTION bool ON_Circle_CreatePtVecPt(ON_CIRCLE_STRUCT* c, ON_3DPOINT_STRUCT p, ON_3DVECTOR_STRUCT tan_at_p, ON_3DPOINT_STRUCT q)
{
  bool rc = false;
  if( c )
  {
    const ON_3dPoint* _p = (const ON_3dPoint*)&p;
    const ON_3dVector* _tan_at_p = (const ON_3dVector*)&tan_at_p;
    const ON_3dPoint* _q = (const ON_3dPoint*)&q;
    ON_Circle _c;
    rc = _c.Create( *_p, *_tan_at_p, *_q );
    _c.plane.UpdateEquation();
    CopyToCircleStruct(*c, _c);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Circle_IsInPlane(const ON_CIRCLE_STRUCT* c, const ON_PLANE_STRUCT* plane, double tolerance)
{
  bool rc = false;
  if ( c && plane )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    ON_Circle circle = FromCircleStruct(*c);
    rc = circle.IsInPlane(temp, tolerance);
  }
  return rc;
}


RH_C_FUNCTION void ON_Circle_BoundingBox(const ON_CIRCLE_STRUCT* c, ON_BoundingBox* bbox)
{
  if( c && bbox )
  {
    ON_Circle circle = FromCircleStruct(*c);
    *bbox = circle.BoundingBox();
  }
}

RH_C_FUNCTION bool ON_Circle_Transform( ON_CIRCLE_STRUCT* c, ON_Xform* xf)
{
  bool rc = false;
  if( c && xf )
  {
    ON_Circle circle = FromCircleStruct(*c);
    rc = circle.Transform(*xf);
    CopyToCircleStruct(*c, circle);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Circle_ClosestPointTo( const ON_CIRCLE_STRUCT* c,
                                             ON_3DPOINT_STRUCT testPoint,
                                             double* t)
{
  bool rc = false;
  const ON_3dPoint* _testPoint = (const ON_3dPoint*)&testPoint;

  if (c)
  {
    ON_Circle circle = FromCircleStruct(*c);
    rc = circle.ClosestPointTo(*_testPoint, t);
  }

  return rc;
}

RH_C_FUNCTION int ON_Circle_GetNurbForm(const ON_CIRCLE_STRUCT* pCircle, ON_NurbsCurve* nurbs_curve)
{
  int rc = 0;
  if( pCircle && nurbs_curve )
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    rc = circle.GetNurbForm(*nurbs_curve);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Circle_TryFitTTT(const ON_Curve* c1, const ON_Curve* c2, const ON_Curve* c3, 
                                       double seed1, double seed2, double seed3, 
                                       ON_CIRCLE_STRUCT* circleFit)
{
#if !defined(RHINO3DM_BUILD)
  if (!c1 || !c2 || !c3) { return false; }
  if (!circleFit) { return false; }

  // 2-Sep-2015 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-31084
  // Moved this calculation code into a Rhino SDK function.
  ON_Circle circle;
  if (RhinoCalculateCircleTanTanTan(c1, c2, c3, seed1, seed2, seed3, circle))
  {
    if (circle.IsValid())
    {
      CopyToCircleStruct(*circleFit, circle);
      return true;
    }
  }
#endif
  return false;
}

RH_C_FUNCTION bool ON_Circle_TryFitTT(const ON_Curve* c1, const ON_Curve* c2, 
                                      double seed1, double seed2,
                                      ON_CIRCLE_STRUCT* circleFit)
{
#if !defined(RHINO3DM_BUILD)
  if (!c1 || !c2) { return false; }
  if (!circleFit) { return false; }

  // 2-Sep-2015 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-31084
  // Moved this calculation code into a Rhino SDK function.
  ON_Circle circle;
  if (RhinoCalculateCircleTanTan(c1, c2, seed1, seed2, circle))
  {
    if (circle.IsValid())
    {
      CopyToCircleStruct(*circleFit, circle);
      return true;
    }
  }
#endif
  return false;
 }