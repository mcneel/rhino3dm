#include "stdafx.h"

RH_C_FUNCTION ON_PolyCurve* ON_PolyCurve_New( ON_PolyCurve* pOther )
{
  if( !pOther )
    return new ON_PolyCurve();
  return new ON_PolyCurve(*pOther);
}

RH_C_FUNCTION int ON_PolyCurve_Count( const ON_PolyCurve* pCurve)
{
  int rc = 0;
  if( pCurve )
    rc = pCurve->Count();
  return rc;
}


RH_C_FUNCTION ON_Curve* ON_PolyCurve_SegmentCurve( const ON_PolyCurve* pCurve, int index)
{
  if( pCurve )
    return pCurve->SegmentCurve(index);
  return nullptr;
}

RH_C_FUNCTION double ON_PolyCurve_SegmentCurveParameter( const ON_PolyCurve* pCurve, double polycurveParameter)
{
  double rc = 0.0;
  if( pCurve )
    rc = pCurve->SegmentCurveParameter(polycurveParameter);
  return rc;
}

RH_C_FUNCTION double ON_PolyCurve_PolyCurveParameter( const ON_PolyCurve* pCurve, int segmentIndex, double segmentCurveParameter)
{
  double rc = 0.0;
  if( pCurve )
    rc = pCurve->PolyCurveParameter(segmentIndex, segmentCurveParameter);
  return rc;
}

RH_C_FUNCTION void ON_PolyCurve_SegmentDomain( const ON_PolyCurve* pCurve, int segmentIndex, ON_Interval* domain)
{
  if( pCurve && domain )
  {
    *domain = pCurve->SegmentDomain(segmentIndex);
  }
}

RH_C_FUNCTION int ON_PolyCurve_SegmentIndex( const ON_PolyCurve* pCurve, double polycurveParameter)
{
  int rc = 0;
  if( pCurve )
    rc = pCurve->SegmentIndex(polycurveParameter);
  return rc;
}

RH_C_FUNCTION int ON_PolyCurve_SegmentIndexes( const ON_PolyCurve* pCurve, ON_INTERVAL_STRUCT subDomain, int* index0, int* index1)
{
  int rc = 0;
  if( pCurve )
  {
    const ON_Interval* _subDomain = (const ON_Interval*)&subDomain;
    rc = pCurve->SegmentIndex(*_subDomain, index0, index1);
  }
  return rc;
}

RH_C_FUNCTION int ON_PolyCurve_HasGap( const ON_PolyCurve* pCurve)
{
  int rc = 0;
  if( pCurve )
    rc = pCurve->FindNextGap(0);
  return rc;
}

RH_C_FUNCTION bool ON_PolyCurve_GetBool( ON_PolyCurve* pCurve, int which)
{
  const int idxIsNested = 0;
  const int idxRemoveNestingEx = 1;
  bool rc = false;
  if( pCurve )
  {
    if( idxIsNested == which )
      rc = pCurve->IsNested();
    else if( idxRemoveNestingEx == which )
      rc = pCurve->RemoveNesting();
  }
  return rc;
}

RH_C_FUNCTION void ON_PolyCurve_SegmentCurves( const ON_PolyCurve* pCurve, ON_SimpleArray<ON_Curve*>* pCurveArray)
{
  if( pCurve && pCurveArray )
  {
    const ON_CurveArray& curves = pCurve->SegmentCurves();
    *pCurveArray = curves;
  }
}

RH_C_FUNCTION bool ON_PolyCurve_AppendAndMatch( ON_PolyCurve* pCurve, ON_Arc* arc)
{
  bool rc = false;
  if( pCurve && arc )
  {
    arc->plane.UpdateEquation();
    ON_ArcCurve* pArcCurve = new ON_ArcCurve(*arc);
    rc = pCurve->AppendAndMatch(pArcCurve)?true:false;
    if( !rc )
      delete pArcCurve;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PolyCurve_AppendAndMatch2( ON_PolyCurve* pPolyCurve, const ON_Curve* pCurve)
{
  bool rc = false;
  if( pPolyCurve && pCurve )
  {
    ON_Curve* pCopy = pCurve->DuplicateCurve();
    rc = pPolyCurve->AppendAndMatch(pCopy)?true:false;
    if( !rc )
      delete pCopy;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PolyCurve_Append(ON_PolyCurve* pPolyCurve, const ON_Curve* pCurve)
{
  bool rc = false;
  if (pPolyCurve && pCurve)
  {
    ON_Curve* pCopy = pCurve->DuplicateCurve();
    rc = pPolyCurve->Append(pCopy) ? true : false;
    if (!rc)
      delete pCopy;
  }
  return rc;
}

#if !defined(RHINO3DM_BUILD)  //not available in opennurbs build

RH_C_FUNCTION ON_Curve* RHC_RhinoCleanUpPolyCurve(const ON_PolyCurve* pPolyCurve)
{
  ON_Curve* rc = nullptr;
  if (pPolyCurve)
    rc = RhinoCleanUpPolyCurve(*pPolyCurve);
  return rc;
}


#endif
