#include "stdafx.h"

RH_C_FUNCTION ON_PolylineCurve* ON_PolylineCurve_New( ON_PolylineCurve* pOther )
{
  RHCHECK_LICENSE
  if( pOther )
    return new ON_PolylineCurve(*pOther);
  return new ON_PolylineCurve();
}

RH_C_FUNCTION ON_PolylineCurve* ON_PolylineCurve_New2(int point_count, /*ARRAY*/const ON_3dPoint* points)
{
  RHCHECK_LICENSE
  if( point_count<1 || NULL==points )
    return new ON_PolylineCurve();
  
  CHack3dPointArray pts(point_count, (ON_3dPoint*)points);
  return new ON_PolylineCurve(pts);
}

RH_C_FUNCTION int ON_PolylineCurve_PointCount(const ON_PolylineCurve* pCurve)
{
  int rc = 0;
  if( pCurve )
    rc = pCurve->PointCount();
  return rc;
}

RH_C_FUNCTION void ON_PolylineCurve_GetSetParameter(ON_PolylineCurve* pCurve, int index, double* t, bool set)
{
  if (pCurve && t && index >= 0 && index < pCurve->m_t.Count())
  {
    if (set)
      pCurve->m_t[index] = *t;
    else
      *t = pCurve->m_t[index];
  }
}

RH_C_FUNCTION void ON_PolylineCurve_GetSetPoint(ON_PolylineCurve* pCurve, int index, ON_3dPoint* point, bool set)
{
  if( pCurve && point && index>=0 && index<pCurve->m_pline.Count() )
  {
    if( set )
      pCurve->m_pline[index] = *point;
    else
      *point = pCurve->m_pline[index];
  }
}

// return number of points in a certain polyline curve
RH_C_FUNCTION int ON_SimpleArray_PolylineCurve_GetCount(ON_SimpleArray<ON_PolylineCurve*>* pPolylineCurves, int i)
{
  int rc = 0;
  if( pPolylineCurves && i>=0 && i<pPolylineCurves->Count() )
  {
    ON_PolylineCurve* polyline = (*pPolylineCurves)[i];
    if( polyline )
      rc = polyline->PointCount();
  }
  return rc;
}

RH_C_FUNCTION void ON_SimpleArray_PolylineCurve_GetPoints(ON_SimpleArray<ON_PolylineCurve*>* pPolylineCurves, int i, int point_count, /*ARRAY*/ON_3dPoint* points)
{
  if( nullptr==pPolylineCurves || i<0 || i>=pPolylineCurves->Count() || point_count<0 || nullptr==points )
    return;
  ON_PolylineCurve* polyline = (*pPolylineCurves)[i];
  if( nullptr==polyline || polyline->PointCount()!=point_count )
    return;

 
  const ON_3dPoint* source = polyline->m_pline.Array();
  ::memcpy(points, source, sizeof(ON_3dPoint) * point_count);
}

RH_C_FUNCTION void ON_SimpleArray_PolylineCurve_Delete(ON_SimpleArray<ON_PolylineCurve*>* pPolylineCurves, bool delete_individual_curves)
{
  if( pPolylineCurves )
  {
    if( delete_individual_curves )
    {
      for( int i=0; i<pPolylineCurves->Count(); i++ )
      {
        ON_PolylineCurve* pCurve = (*pPolylineCurves)[i];
        if( pCurve )
          delete pCurve;
      }
    }
    delete pPolylineCurves;
  }
}


#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION void ON_PolylineCurve_Draw(const ON_PolylineCurve* pCrv, CRhinoDisplayPipeline* pDisplayPipeline, int argb, int thickness)
{
  if( pCrv && pDisplayPipeline )
  {
    int abgr = ARGB_to_ABGR(argb);
    pDisplayPipeline->DrawPolyline(pCrv->m_pline, abgr, thickness);
  }
}
#endif

RH_C_FUNCTION void ON_PolylineCurve_CopyValues(const ON_PolylineCurve* pConstCurve, ON_3dPointArray* pPoints)
{
  // http://mcneel.myjetbrains.com/youtrack/issue/RH-30969
  if (pConstCurve && pPoints)
  {
    const int count = pConstCurve->PointCount();
    if (count > 0)
    {
      pPoints->Append(count, pConstCurve->m_pline.Array());
    }
  }
}

RH_C_FUNCTION bool ON_Polyline_CreateInscribedPolygon(const ON_CIRCLE_STRUCT* pCircle, int side_count, ON_3dPointArray* pPoints)
{
  bool rc = false;
  if (nullptr != pCircle && nullptr != pPoints)
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    if (circle.IsValid())
    {
      ON_Polyline polyline;
      rc = polyline.CreateInscribedPolygon(circle, side_count);
      if (rc)
        *pPoints = polyline;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Polyline_CreateCircumscribedPolygon(const ON_CIRCLE_STRUCT* pCircle, int side_count, ON_3dPointArray* pPoints)
{
  bool rc = false;
  if (nullptr != pCircle && nullptr != pPoints)
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    if (circle.IsValid())
    {
      ON_Polyline polyline;
      rc = polyline.CreateCircumscribedPolygon(circle, side_count);
      if (rc)
        *pPoints = polyline;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Polyline_CreateStarPolygon(const ON_CIRCLE_STRUCT* pCircle, double other_radius, int corner_count, ON_3dPointArray* pPoints)
{
  bool rc = false;
  if (nullptr != pCircle && nullptr != pPoints)
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    if (circle.IsValid())
    {
      ON_Polyline polyline;
      rc = polyline.CreateStarPolygon(circle, other_radius, corner_count);
      if (rc)
        *pPoints = polyline;
    }
  }
  return rc;
}
