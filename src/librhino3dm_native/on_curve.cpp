#include "stdafx.h"

RH_C_FUNCTION bool ON_Curve_Domain(ON_Curve* pCurve, bool set, ON_Interval* ival)
{
  bool rc = false;
  if (pCurve && ival)
  {
    if (set)
    {
#if !defined(RHINO3DM_BUILD)
      // 6-Jun-2022 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-68909
      ON_PolyCurve* pPolyCurve = ON_PolyCurve::Cast(pCurve);
      if (pPolyCurve)
      {
        ON_Curve* pTmp = RhReparameterizeCurve(pPolyCurve, *ival, false);
        if (pTmp)
        {
          ON_PolyCurve* pPolyTmp = ON_PolyCurve::Cast(pTmp);
          if (pPolyTmp)
          {
            // use ON_PolyCurve::operator=
            *pPolyCurve = *pPolyTmp;
            rc = true;
          }
          delete pTmp; // don't leak
        }
      }
#endif
      if (!rc)
        rc = pCurve->SetDomain(*ival);
    }
    else
    {
      *ival = pCurve->Domain();
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_Curve_DuplicateCurve(ON_Curve* pCurve)
{
  RHCHECK_LICENSE
  ON_Curve* rc = nullptr;
  if( pCurve )
    rc = pCurve->DuplicateCurve();
  return rc;
}

bool ON_Curve_ChangeDimension(ON_Curve* curve, int desiredDimension)
{
  bool rc = false;
  if(curve)
    rc = curve->ChangeDimension(desiredDimension);
  return rc;
}

bool ON_Curve_ChangeClosedCurveSeam(ON_Curve* curve, double t)
{
  bool rc = false;
  if(curve)
    rc = curve->ChangeClosedCurveSeam(t)?true:false;
  return rc;
}

RH_C_FUNCTION int ON_Curve_SpanCount(const ON_Curve* pConstCurve)
{
  int rc = 0;
  if( pConstCurve )
    rc = pConstCurve->SpanCount();
  return rc;
}

RH_C_FUNCTION bool ON_Curve_SpanInterval(const ON_Curve* pConstCurve, int spanIndex, ON_Interval* spanDomain)
{
  bool rc = false;
  if( pConstCurve && spanDomain )
  {
    int count = pConstCurve->SpanCount();
    if( spanIndex >= 0 && spanIndex < count )
    {
      double* knots = new double[count + 1];
      pConstCurve->GetSpanVector(knots); 
      spanDomain->Set(knots[spanIndex], knots[spanIndex+1]);

      delete [] knots;
      rc = true;
    }
  }
  return rc;
}

int ON_Curve_Degree(const ON_Curve* constCurve)
{
  int rc = 0;
  if(constCurve)
    rc = constCurve->Degree();
  return rc;
}

int ON_Curve_HasNurbForm(const ON_Curve* constCurve)
{
  int rc = 0;
  if(constCurve)
    rc = constCurve->HasNurbForm();
  return rc;
}

bool ON_Curve_IsLinear(const ON_Curve* constCurve, double tolerance)
{
  bool rc = false;
  if(constCurve)
    rc = constCurve->IsLinear(tolerance)?true:false;
  return rc;
}

RH_C_FUNCTION int ON_Curve_IsPolyline1( const ON_Curve* pConstCurve, ON_3dPointArray* points )
{
  int pointCount = 0;
  if( pConstCurve )
    pointCount = pConstCurve->IsPolyline(points);
  return pointCount;
}

RH_C_FUNCTION void ON_Curve_IsPolyline2( const ON_Curve* pCurve, ON_3dPointArray* points, int* pointCount, ON_SimpleArray<double>* t )
{
  if( NULL == pointCount || NULL == pCurve )
    return;

  *pointCount = pCurve->IsPolyline( points, t );
  if( 0 == pointCount )
    return;
}

RH_C_FUNCTION bool ON_Curve_IsArc( const ON_Curve* pCurve, int ignore, ON_PLANE_STRUCT* plane, ON_Arc* arc, double tolerance )
{
  bool rc = false;
  if( pCurve )
  {
    // ignore = 0 (none)
    // ignore = 1 (ignore plane)
    // ignore = 2 (ignore plane and arc)
    if( ignore>0 )
      plane = nullptr;
    if( ignore>1 )
      arc = nullptr;
    ON_Plane temp;
    ON_Plane* pPlane = nullptr;
    if( plane )
    {
      temp = FromPlaneStruct(*plane);
      pPlane = &temp;
    }
    rc = pCurve->IsArc(pPlane,arc,tolerance)?true:false;
    if( plane )
      CopyToPlaneStruct(*plane, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_IsEllipse( const ON_Curve* pCurve, int ignore, ON_PLANE_STRUCT* plane, ON_Ellipse* ellipse, double tolerance )
{
  bool rc = false;
  if( pCurve )
  {
    // ignore = 0 (none)
    // ignore = 1 (ignore plane)
    // ignore = 2 (ignore plane and ellipse)
    if (ignore>0) plane = nullptr;
    if (ignore>1) ellipse = nullptr;

    ON_Plane temp;
    ON_Plane* pPlane = nullptr;
    if (plane)
    {
      temp = FromPlaneStruct(*plane);
      pPlane = &temp;
    }

    // [Giulio] RH-36085: OnCurve::IsEllipse() cannot be used.
    // It will otherwise only check for IsArc, and no other checks
    // will be performed. Create a NURBS curve instead.
    ON_NurbsCurve* nurbs_curve_ptr = pCurve->NurbsCurve(nullptr, tolerance);
    if (nullptr != nurbs_curve_ptr)
    {
      rc = nurbs_curve_ptr->IsEllipse(pPlane, ellipse, tolerance);
      delete nurbs_curve_ptr;
    }

    if (plane) CopyToPlaneStruct(*plane, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_IsPlanar( const ON_Curve* pCurve, bool ignorePlane, ON_PLANE_STRUCT* plane, double tolerance )
{
  bool rc = false;
  if(ignorePlane)
    plane = nullptr;
  if( pCurve )
  {
    ON_Plane temp;
    ON_Plane* pPlane = nullptr;
    if( plane )
    {
      temp = FromPlaneStruct(*plane);
      pPlane = &temp;
    }
    rc = pCurve->IsPlanar(pPlane, tolerance)?true:false;
    if( plane )
      CopyToPlaneStruct(*plane, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_IsInPlane(const ON_Curve* pCurve, const ON_PLANE_STRUCT* plane, double tolerance)
{
  bool rc = false;
  if( pCurve && plane )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    rc = pCurve->IsInPlane(temp,tolerance)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetBool( const ON_Curve* pCurve, int which )
{
  const int idxIsClosed = 0;
  const int idxIsPeriodic = 1;
  bool rc = false;
  if( pCurve )
  {
    if( idxIsClosed == which )
      rc = pCurve->IsClosed()?true:false;
    else if(idxIsPeriodic == which )
      rc = pCurve->IsPeriodic()?true:false;
  }
  return rc;
}

bool ON_Curve_IsClosed(const ON_Curve* constCurve)
{
  if (constCurve)
    return constCurve->IsClosed() ? true : false;
  return false;
}

bool ON_Curve_IsPeriodic(const ON_Curve* constCurve)
{
  if (constCurve)
    return constCurve->IsPeriodic() ? true : false;
  return false;
}

RH_C_FUNCTION bool ON_Curve_Reverse( ON_Curve* pCurve )
{
  bool rc = false;
  if( pCurve )
    rc = pCurve->Reverse()?true:false;
  return rc;
}

RH_C_FUNCTION bool ON_Curve_SetPoint( ON_Curve* pCurve, ON_3DPOINT_STRUCT pt, bool startpoint )
{
  bool rc = false;
  if( pCurve )
  {
    const ON_3dPoint* _pt = (const ON_3dPoint*)&pt;
    if( startpoint )
      rc = pCurve->SetStartPoint(*_pt)?true:false;
    else
      rc = pCurve->SetEndPoint(*_pt)?true:false;
  }
  return rc;
}

RH_C_FUNCTION void ON_Curve_PointAt(const ON_Curve* pCurve, double t, ON_3dPoint* pt, int which)
{
  RHCHECK_LICENSE
  const int idxPointAtT = 0;
  const int idxPointAtStart = 1;
  const int idxPointAtEnd = 2;
  const int idxPointAtMid = 3;
  if (pCurve && pt)
  {
    *pt = ON_3dPoint::UnsetPoint;
    if (idxPointAtT == which)
      *pt = pCurve->PointAt(t);
    else if (idxPointAtStart == which)
      *pt = pCurve->PointAtStart();
    else if (idxPointAtEnd == which)
      *pt = pCurve->PointAtEnd();
#if !defined(RHINO3DM_BUILD)
    else if (idxPointAtMid == which)
    {
      double s = 0.0;
      if (pCurve->GetNormalizedArcLengthPoint(0.5, &s))
        *pt = pCurve->PointAt(s);
    }
#endif
  }
}

RH_C_FUNCTION void ON_Curve_GetVector( const ON_Curve* pCurve, int which, double t, ON_3dVector* vec )
{
  const int idxDerivateAt = 0;
  const int idxTangentAt = 1;
  const int idxCurvatureAt = 2;
  if( pCurve && vec )
  {
    if( idxDerivateAt == which )
      *vec = pCurve->DerivativeAt(t);
    else if( idxTangentAt == which )
      *vec = pCurve->TangentAt(t);
    else if( idxCurvatureAt == which )
      *vec = pCurve->CurvatureAt(t);
  }
}

RH_C_FUNCTION bool ON_Curve_Evaluate( const ON_Curve* pCurve, int derivatives, int side, double t, ON_3dPointArray* outVectors )
{
  RHCHECK_LICENSE
  bool rc = false;
  
  if( pCurve && outVectors )
  {
    if( derivatives >= 0 )
    {
      outVectors->Reserve(derivatives+1);
      if (pCurve->Evaluate(t, derivatives, 3, &outVectors->Array()->x, side, nullptr))
      {
        outVectors->SetCount(derivatives+1);
        rc = true;
      }
    }
  }

  return rc;
}

RH_C_FUNCTION bool ON_Curve_FrameAt( const ON_Curve* pConstCurve, double t, ON_PLANE_STRUCT* plane, bool zero_twisting)
{
  RHCHECK_LICENSE
  bool rc = false;
  if( pConstCurve && plane )
  {
    ON_Plane temp;
#if defined(RHINO3DM_BUILD)
    rc = pConstCurve->FrameAt(t, temp)?true:false;
#else // rhino.exe build
    ON_FPU_ClearExceptionStatus();
    if( zero_twisting )
      rc = RhinoGetPerpendicularCurvePlane(pConstCurve, t, temp);
    else
      rc = pConstCurve->FrameAt(t, temp)?true:false;
#endif
    CopyToPlaneStruct(*plane, temp);
  }
  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION ON_Curve* ON_Curve_Reparameterize(const ON_Curve* pConstCurve)
{
  ON_Curve* rc = nullptr;
  if (pConstCurve)
    rc = RhReparameterizeCurve(pConstCurve, ON_Interval(), true);
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetClosestPoint( const ON_Curve* pCurve, ON_3DPOINT_STRUCT test_point, double* t, double maximum_distance)
{
  RHCHECK_LICENSE
  bool rc = false;
  if( pCurve )
  {
    const ON_3dPoint* pt = (const ON_3dPoint*)&test_point;
    rc = pCurve->GetClosestPoint(*pt, t, maximum_distance);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetLocalClosestPoint(const ON_Curve* pCurve, ON_3DPOINT_STRUCT test_point, double s, double* t)
{
  RHCHECK_LICENSE
  bool rc = false;
  if (pCurve && t)
  {
    const ON_3dPoint* pt = (const ON_3dPoint*)&test_point;
    rc = pCurve->GetLocalClosestPoint(*pt, s, t);
  }
  return rc;
}


RH_C_FUNCTION bool ON_Curve_GetLength(const ON_Curve* pCurve, double* length, double fractional_tol, ON_INTERVAL_STRUCT sub_domain, bool ignoreSubDomain)
{
  RHCHECK_LICENSE
  const ON_Interval* _sub_domain = nullptr;
  if (!ignoreSubDomain)
    _sub_domain = (const ON_Interval*)&sub_domain;
  bool rc = false;
  if (pCurve && length)
  {
    rc = pCurve->GetLength(length, fractional_tol, _sub_domain) ? true : false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_IsShort( const ON_Curve* pCurve, double tolerance, ON_INTERVAL_STRUCT sub_domain, bool ignoreSubDomain)
{
  const ON_Interval* _sub_domain = nullptr;
  if( !ignoreSubDomain )
    _sub_domain = (const ON_Interval*)&sub_domain;
  bool rc = false;
  if( pCurve )
  {
    rc = pCurve->IsShort(tolerance, _sub_domain);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_RemoveShortSegments( ON_Curve* pCurve, double tolerance )
{
  bool rc = false;
  if( pCurve )
    rc = pCurve->RemoveShortSegments(tolerance);
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetNormalizedArcLengthPoint( const ON_Curve* pCurve, double s, double* t, double fractional_tol, ON_INTERVAL_STRUCT sub_domain, bool ignoreSubDomain)
{
  bool rc = false;
  const ON_Interval* _sub_domain = nullptr;
  if( !ignoreSubDomain )
    _sub_domain = (const ON_Interval*)&sub_domain;
  if( pCurve && t )
  {
    rc = pCurve->GetNormalizedArcLengthPoint(s, t, fractional_tol, _sub_domain)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetNormalizedArcLengthPoints( const ON_Curve* pCurve, int count, /*ARRAY*/double* s, /*ARRAY*/double* t, double abs_tol, double frac_tol, ON_INTERVAL_STRUCT sub_domain, bool ignoreSubDomain)
{
  bool rc = false;
  const ON_Interval* _sub_domain = nullptr;
  if( !ignoreSubDomain )
    _sub_domain = (const ON_Interval*)&sub_domain;
  if( pCurve && count>0 && s && t )
  {
    rc = pCurve->GetNormalizedArcLengthPoints(count, s, t, abs_tol, frac_tol, _sub_domain)?true:false;
  }
  return rc;
}

#endif

RH_C_FUNCTION ON_Curve* ON_Curve_TrimExtend( const ON_Curve* pCurve, double t0, double t1, bool trimming)
{
  ON_Curve* rc = nullptr;
  if( pCurve )
  {
    if( trimming )
    {
      rc = ::ON_TrimCurve(*pCurve, ON_Interval(t0,t1));
    }
    else
    {
      ON_Curve* pNewCurve = pCurve->DuplicateCurve();
      if( pNewCurve )
      {
        if( pNewCurve->Extend(ON_Interval(t0,t1)) )
          rc = pNewCurve;
        else
          delete pNewCurve;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_Trim(ON_Curve* pCurve, double t0, double t1)
{
  // https://mcneel.myjetbrains.com/youtrack/issue/RH-89686
  bool rc = false;
  if (pCurve)
     rc = pCurve->Trim(ON_Interval(t0, t1));
  return rc;
}

RH_C_FUNCTION bool ON_Curve_Split( const ON_Curve* pCurve, double t, ON_Curve** left, ON_Curve** right )
{
  bool rc = false;
  if( pCurve && left && right )
  {
    rc = pCurve->Split(t, *left, *right)?true:false;
  }
  return rc;
}

RH_C_FUNCTION ON_NurbsCurve* ON_Curve_NurbsCurve(const ON_Curve* pCurve, double tolerance, ON_INTERVAL_STRUCT sub_domain, bool ignoreSubDomain)
{
  RHCHECK_LICENSE
  ON_NurbsCurve* rc = nullptr;
  if( pCurve )
  {
    const ON_Interval* _sub_domain = nullptr;
    if( !ignoreSubDomain )
      _sub_domain = (const ON_Interval*)&sub_domain;
    rc = pCurve->NurbsCurve(nullptr,tolerance,_sub_domain);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetNurbParameter(const ON_Curve* pCurve, double t_in, double* t_out, bool nurbToCurve)
{
  RHCHECK_LICENSE
  bool rc = false;
  if( pCurve && t_out )
  {
    if( nurbToCurve )
      rc = pCurve->GetCurveParameterFromNurbFormParameter(t_in,t_out)?true:false;
    else
      rc = pCurve->GetNurbFormParameterFromCurveParameter(t_in,t_out)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_IsClosable( const ON_Curve* curvePtr, double tolerance, double min_abs_size, double min_rel_size )
{
  bool rc = false;
  if( curvePtr )
  {
    rc = curvePtr->IsClosable(tolerance, min_abs_size, min_rel_size);
  }
  return rc;
}

RH_C_FUNCTION int ON_Curve_ClosedCurveOrientation(const ON_Curve* curvePtr, ON_Xform* xform)
{
  int rc = 0;
  if (curvePtr)
  {
    // 10-Feb-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-32952
    const ON_Xform* pXform = nullptr;
    if (nullptr != xform && !xform->IsIdentity() && !xform->IsZero())
      pXform = xform;
    rc = ON_ClosedCurveOrientation(*curvePtr, pXform);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetNextDiscontinuity(const ON_Curve* curvePtr, int continuityType, double t0, double t1, double* t)
{
  bool rc = false;
  if( curvePtr )
  {
    ON::continuity c = ON::Continuity(continuityType);
    rc = curvePtr->GetNextDiscontinuity(c, t0, t1, t);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_GetNextDiscontinuity2(const ON_Curve* curvePtr, int continuityType, double t0, double t1,
  double cosAngleTolerance, double curvatureTolerance, double* t)
{
  bool rc = false;
  if (curvePtr)
  {
    ON::continuity c = ON::Continuity(continuityType);
    rc = curvePtr->GetNextDiscontinuity(c, t0, t1, t, nullptr, nullptr, cosAngleTolerance, curvatureTolerance);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Curve_IsContinuous(const ON_Curve* curvePtr, int continuityType, double t)
{
  bool rc = false;
  if( curvePtr )
  {
    ON::continuity c = ON::Continuity(continuityType);
    rc = curvePtr->IsContinuous(c, t);
  }
  return rc;
}

RH_C_FUNCTION double ON_Curve_TorsionAt(const ON_Curve* pConstCurve, double t)
{
  // 27-Jul-2021 Dale Fugier
  double tau = ON_UNSET_VALUE;
  if (pConstCurve)
  {
    double v[12] = {};
    if (pConstCurve->Evaluate(t, 3, 3, v))
    {
      tau = 0.0;
      ON_3dVector d1(&v[3]);
      ON_3dVector d2(&v[6]);
      ON_3dVector d3(&v[9]);
      ON_3dVector b = ON_CrossProduct(d1, d2);
      double len2 = b * b;
      if (len2 > 0.0)
        tau = b * d3 / len2;
    }
  }
  return tau;
}

RH_C_FUNCTION bool ONC_JoinCurves(const ON_SimpleArray<const ON_Curve*>* pInCurves, ON_SimpleArray<ON_Curve*>* pOutCurves, double joinTolerance, bool bPreserveDirection, ON_SimpleArray<int>* key)
{
  // 18-Jan-2021 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-67058
  bool rc = false;
  if (pInCurves && pOutCurves)
  {
    int count = ON_JoinCurves(*pInCurves, *pOutCurves, joinTolerance, bPreserveDirection, key);
    rc = (count > 0);
  }
  return rc;
}

RH_C_FUNCTION bool ONC_SortCurveEnds(const ON_SimpleArray<const ON_Curve*>* pInCurves, ON_SimpleArray<int>* pOutSizes,
  ON_SimpleArray<int>* pOutIds, ON_SimpleArray<int>* pOutRev, ON_SimpleArray<int>* pOutSingles, double joinTol)
{
  bool rc = false;
  if (joinTol < ON_ZERO_TOLERANCE) joinTol = ON_ZERO_TOLERANCE;

  if (pInCurves && pOutSizes && pOutIds && pOutRev && pOutSingles)
  {
    ON_ClassArray<ON_SimpleArray<CurveJoinSeg>> sortings;
    rc = ON_SortCurveEnds(*pInCurves, joinTol, 0, false, false, sortings, *pOutSingles);
    if (rc)
    {
      pOutSizes->SetCapacity(sortings.Count());
      for (const ON_SimpleArray<CurveJoinSeg>& sorting : sortings)
      {
        pOutSizes->Append(sorting.Count());
        for (const CurveJoinSeg& seg : sorting)
        {
          pOutIds->Append(seg.id);
          pOutRev->Append(seg.bRev ? 1 : 0);
        }
      }
    }
  }

  return rc;
}


/////////////////////////////////////////////////////////////////////////////
// Meshing, intersections and mass property calculations are not available in
// stand alone opennurbs

#if !defined(RHINO3DM_BUILD) //in rhino.exe

RH_C_FUNCTION ON_SimpleArray<ON_X_EVENT>* ON_Curve_IntersectPlane(const ON_Curve* pConstCurve, ON_PLANE_STRUCT* plane, double tolerance)
{
  RHCHECK_LICENSE
  ON_SimpleArray<ON_X_EVENT>* rc = nullptr;
  if(pConstCurve && plane)
  {
    rc = new ON_SimpleArray<ON_X_EVENT>();
    ON_Plane _plane = ::FromPlaneStruct(*plane);
    if( pConstCurve->IntersectPlane( _plane.plane_equation, *rc, tolerance, tolerance) < 1 )
    {
      // no intersections found. No need to create a list of intersections
      delete rc;
      rc = nullptr;
    }
  }
 
  return rc;
}

RH_C_FUNCTION ON_MassProperties* ON_Curve_AreaMassProperties(const ON_Curve* pCurve, double rel_tol, double abs_tol, double curve_planar_tol)
{
  RHCHECK_LICENSE
  ON_MassProperties* rc = nullptr;
  if (pCurve)
  {
    ON_Plane plane;
    if (pCurve->IsPlanar(&plane, curve_planar_tol) && pCurve->IsClosed())
    {
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-44692
      // Check the orientation and flip the plane if necessary.
      if (ON_ClosedCurveOrientation(*pCurve, plane) < 0)
        plane.Flip();

      ON_BoundingBox bbox = pCurve->BoundingBox();
      ON_3dPoint basepoint = bbox.Center();
      basepoint = plane.ClosestPointTo(basepoint);

      rc = new ON_MassProperties();
      bool getresult = pCurve->AreaMassProperties(basepoint, plane.Normal(), *rc, true, true, true, true, rel_tol, abs_tol);
      if (!getresult)
      {
        delete rc;
        rc = nullptr;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_MassProperties* ON_Curve_LengthMassProperties(const ON_Curve* pCurve, bool bLength, bool bFirstMoments, bool bSecondMoments, bool bProductMoments, double rel_tol, double abs_tol)
{
  ON_MassProperties* rc = nullptr;
  if (pCurve)
  {
    rc = new ON_MassProperties();
    bool success = pCurve->LengthMassProperties(*rc, bLength, bFirstMoments, bSecondMoments, bProductMoments, rel_tol, abs_tol);
    if (!success)
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_MassProperties* ON_Curve_LengthMassProperties2(const ON_SimpleArray<const ON_Curve*>* pConstArray, bool bLength, bool bFirstMoments, bool bSecondMoments, bool bProductMoments, double rel_tol, double abs_tol)
{
  ON_MassProperties* rc = nullptr;
  if (pConstArray && pConstArray->Count() > 0)
  {
    for (int i = 0; i < pConstArray->Count(); i++)
    {
      const ON_Curve* pCurve = (*pConstArray)[i];
      if (nullptr == pCurve)
        continue;

      ON_MassProperties mp;
      bool success = pCurve->LengthMassProperties(mp, bLength, bFirstMoments, bSecondMoments, bProductMoments, rel_tol, abs_tol);
      if (success)
      {
        if (nullptr == rc)
          rc = new ON_MassProperties(mp);
        else
          rc->Sum(1, &mp, true);
      }
    }
  }
  return rc;
}

RH_C_FUNCTION bool RHC_RhinoTweenCurves( const ON_Curve* pStartCurve, const ON_Curve* pEndCurve, int num_curves, double tolerance, ON_SimpleArray<ON_Curve*>* outputCurves )
{
  RHCHECK_LICENSE
  bool rc = false;
  if( pStartCurve && pEndCurve && outputCurves )
    rc = RhinoTweenCurves( pStartCurve, pEndCurve, num_curves, tolerance, *outputCurves );
  return rc;
}

RH_C_FUNCTION bool RHC_RhinoTweenCurvesWithMatching( const ON_Curve* pStartCurve, const ON_Curve* pEndCurve, int num_curves, double tolerance, ON_SimpleArray<ON_Curve*>* outputCurves )
{
  RHCHECK_LICENSE
  bool rc = false;
  if( pStartCurve && pEndCurve && outputCurves )
    rc = RhinoTweenCurvesWithMatching( pStartCurve, pEndCurve, num_curves, tolerance, *outputCurves );
  return rc;
}

RH_C_FUNCTION bool RHC_RhinoTweenCurveWithSampling( const ON_Curve* pStartCurve, const ON_Curve* pEndCurve, int num_curves, int num_samples, double tolerance, ON_SimpleArray<ON_Curve*>* outputCurves )
{
  bool rc = false;
  if( pStartCurve && pEndCurve && outputCurves )
    rc = RhinoTweenCurveWithSampling( pStartCurve, pEndCurve, num_curves, num_samples, tolerance, *outputCurves );
  return rc;
}
#endif

RH_C_FUNCTION bool ON_CurveProxy_IsReversed( const ON_CurveProxy* pConstCurveProxy )
{
  if( pConstCurveProxy )
    return pConstCurveProxy->ProxyCurveIsReversed();
  return false;
}

#if !defined(RHINO3DM_BUILD) //in rhino.exe

RH_C_FUNCTION int RHC_RhinoIsCurveConicSection(const ON_Curve* pConstCurve, ON_3dPoint* pFocus1, ON_3dPoint* pFocus2, ON_3dPoint* pCenter)
{
  int rc = -1;
  if (nullptr != pConstCurve)
  {
    rc = (int) RhinoIsCurveConicSection(pConstCurve, pFocus1, pFocus2, pCenter);
  }
  return rc;
}

RH_C_FUNCTION bool RHC_RhinoCurve2View(const ON_Curve* curve1, const ON_Curve* curve2, ON_3DVECTOR_STRUCT vector1, ON_3DVECTOR_STRUCT vector2, ON_SimpleArray<ON_Curve*>* outputCurves, double tolerance, double angle_tolerance) 
{
  RHCHECK_LICENSE
  bool rc = false;

	if (curve1 && curve2 && outputCurves) {

		const ON_3dVector* _v1 = (const ON_3dVector*)(&vector1);
		const ON_3dVector* _v2 = (const ON_3dVector*)(&vector2);

		rc = RhinoCurve2View(*curve1, *curve2, *_v1, *_v2, *outputCurves, tolerance, angle_tolerance);

	}

	return rc;
}

RH_C_FUNCTION ON_Curve* RHC_RhCreateRevisionCloud(const ON_Curve* pConstCurve, int segmentCount, double arcAngle, bool flip)
{
  ON_Curve* rc = nullptr;
  if (pConstCurve)
    rc = RhCreateRevisionCloud(pConstCurve, segmentCount, arcAngle, flip);
  return rc;
}

RH_C_FUNCTION bool RHC_CreateTextOutlines(
	const RHMONO_STRING* str,
	const RHMONO_STRING* font_str,
	double text_height,
	int text_style,
	bool close_contours,
	ON_PLANE_STRUCT* pln,
	double join_tol,
	double small_caps_scale,
	ON_SimpleArray<ON_Curve*>* outputCurves)
{
  RHCHECK_LICENSE
  bool rc = false;

	INPUTSTRINGCOERCE(string, str);
	INPUTSTRINGCOERCE(font_string, font_str);

	int style = 0;
	style = RHINO_CLAMP(text_style, 0, 3);

	const ON_Font* font = ON_Font::GetManagedFont(font_string, (0 != (style & 1)), (0 != (style & 2)));
	if (nullptr == font)
		font = &ON_Font::Default;

	ON_ClassArray< ON_ClassArray< ON_SimpleArray< ON_Curve* > > > out_glyphs;
	rc = (1.0 != small_caps_scale)
			? RhinoGetTextOutlinesWithSmallCaps(string, font, text_height, close_contours, join_tol, small_caps_scale, out_glyphs)
			: RhinoGetTextOutlines(string, font, text_height, close_contours, join_tol, out_glyphs);

	//set glyphOutputCurves
	ON_SimpleArray <ON_Curve*> output_curves;
	ON_Xform xform(1);
	ON_Plane plane = FromPlaneStruct(*pln);
	xform.Rotation(ON_Plane::World_xy, plane);
	if (out_glyphs)
	{
		output_curves.Empty();
		for (int i = 0; i < out_glyphs.Count(); i++)
		{
			
			for (int j = 0; j < out_glyphs[i].Count(); j++)
			{
				for (int k = 0; k < out_glyphs[i][j].Count(); k++)
				{
					ON_Curve* crv = out_glyphs[i][j][k];
					if (crv)
					{
						if (!xform.IsIdentity())
							crv->Transform(xform);

						output_curves.Append(crv);
					}
				}
			}
		}
	}

	if (output_curves) *outputCurves = output_curves;

	return rc; 

}

RH_C_FUNCTION bool ONC_CombineShortSegments(ON_Curve* ptrCurve, double tolerance)
{
  if (ptrCurve)
    return ON_CombineShortSegments(*ptrCurve, tolerance);
  return false;
}

RH_C_FUNCTION bool RHC_RhExtractCurveControlPolygon(const ON_Curve* pCurve, ON_Polyline* pPolyline)
{
  bool rc = false;
  if (pCurve && pPolyline)
    rc = RhExtractCurveControlPolygon(pCurve, *pPolyline);
  return rc;
}

#endif

// ONC_SpanVector uses only public opennurbs (ON_Curve::SpanVector), so it is
// available to Rhino3dm builds and must stay outside the block above.
RH_C_FUNCTION void ONC_SpanVector(const ON_Curve* curve, ON_SimpleArray<double>* vector)
{
  if (vector)
  {
    vector->Empty();
    if (curve)
    {
      *vector = curve->SpanVector();
    }
  }
}


RH_C_FUNCTION ON_CurveKinkDefinition* ON_CurveKinkDefinition_New(ON_CurveKinkDefinition* p)
{
  if (p)
    return new ON_CurveKinkDefinition(*p);

  return new ON_CurveKinkDefinition();
}

RH_C_FUNCTION void ON_CurveKinkDefinition_Delete(ON_CurveKinkDefinition* p)
{
  if (p)
    delete p;
}

RH_C_FUNCTION ON_CurveKinkDefinition* ON_CurveKinkDefinition_Duplicate(ON_CurveKinkDefinition* p)
{
  ON_CurveKinkDefinition* rc = nullptr;
  if (p)
    rc = new ON_CurveKinkDefinition(*p);
  return rc;
}

RH_C_FUNCTION double ON_CurveKinkDefinition_KinkAngleDegrees(const ON_CurveKinkDefinition* p)
{
  if (p)
    return p->KinkAngleDegrees();
  return ON_DBL_QNAN;
}

RH_C_FUNCTION void ON_CurveKinkDefinition_SetKinkAngleDegrees(ON_CurveKinkDefinition* p, double value)
{
  if (p)
    p->SetKinkAngleDegrees(value);
}

RH_C_FUNCTION double ON_CurveKinkDefinition_CurvatureKinkZeroTolerance(const ON_CurveKinkDefinition* p)
{
  if (p)
    return p->CurvatureKinkZeroTolerance();
  return ON_DBL_QNAN;
}

RH_C_FUNCTION void ON_CurveKinkDefinition_SetCurvatureKinkZeroTolerance(ON_CurveKinkDefinition* p, double value)
{
  if (p)
    p->SetCurvatureKinkZeroTolerance(value);
}

RH_C_FUNCTION double ON_CurveKinkDefinition_CurvatureKinkRadiusRatio(const ON_CurveKinkDefinition* p)
{
  if (p)
    return p->CurvatureKinkRadiusRatio();
  return ON_DBL_QNAN;
}

RH_C_FUNCTION void ON_CurveKinkDefinition_SetCurvatureKinkRadiusRatio(ON_CurveKinkDefinition* p, double value)
{
  if (p)
    p->SetCurvatureKinkRadiusRatio(value);
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_KinkAtTangentChange(const ON_CurveKinkDefinition* p)
{
  if (p)
    return p->KinkAtTangentChange();
  return false;
}

RH_C_FUNCTION void ON_CurveKinkDefinition_SetKinkAtTangentChange(ON_CurveKinkDefinition* p, bool value)
{
  if (p)
    p->SetKinkAtTangentChange(value);
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_KinkAtCurvatureChange(const ON_CurveKinkDefinition* p)
{
  if (p)
    return p->KinkAtCurvatureChange();
  return false;
}

RH_C_FUNCTION void ON_CurveKinkDefinition_SetKinkAtCurvatureChange(ON_CurveKinkDefinition* p, bool value)
{
  if (p)
    p->SetKinkAtCurvatureChange(value);
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_IsTangentKink(const ON_CurveKinkDefinition* p, const ON_Curve* pCurve, double t)
{
  if (p && pCurve)
    return p->IsTangentKink(*pCurve, t);

  return false;
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_IsCurvatureKink(const ON_CurveKinkDefinition* p, const ON_Curve* pCurve, double t)
{
  if (p && pCurve)
    return p->IsCurvatureKink(*pCurve, t);

  return false;
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_IsKink(const ON_CurveKinkDefinition* p, const ON_Curve* pCurve, double t)
{
  if (p && pCurve)
    return p->IsKink(*pCurve, t);

  return false;
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_IsTangentKink_FromVectors(const ON_CurveKinkDefinition* p, ON_3DVECTOR_STRUCT tangentFromBelow, ON_3DVECTOR_STRUCT tangentFromAbove)
{
  if (p)
  {
    const ON_3dVector* _tangentFromBelow = (const ON_3dVector*)(&tangentFromBelow);
    const ON_3dVector* _tangentFromAbove = (const ON_3dVector*)(&tangentFromAbove);

    return p->IsTangentKink(*_tangentFromBelow, *_tangentFromAbove);
  }

  return false;
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_IsCurvatureKink_FromVectors(const ON_CurveKinkDefinition* p, ON_3DVECTOR_STRUCT curvatureFromBelow, ON_3DVECTOR_STRUCT curvatureFromAbove)
{
  if (p)
  {
    const ON_3dVector* _curvatureFromBelow = (const ON_3dVector*)(&curvatureFromBelow);
    const ON_3dVector* _curvatureFromAbove = (const ON_3dVector*)(&curvatureFromAbove);

    return p->IsCurvatureKink(*_curvatureFromBelow, *_curvatureFromAbove);
  }

  return false;
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_IsCurvatureZero_FromVector(const ON_CurveKinkDefinition* p, ON_3DVECTOR_STRUCT curvature)
{
  if (p)
  {
    const ON_3dVector* _curvature = (const ON_3dVector*)(&curvature);
    return p->IsCurvatureZero(*_curvature);
  }
  return false;
}


RH_C_FUNCTION bool ON_CurveKinkDefinition_IsCurvatureZero(const ON_CurveKinkDefinition* p, double curvature)
{
  if (p)
  {
    return p->IsCurvatureZero(curvature);
  }
  return false;
}

RH_C_FUNCTION bool ON_CurveKinkDefinition_ComputeCurvatureRadiusRatio_FromVectors(const ON_CurveKinkDefinition* p, 
  ON_3DVECTOR_STRUCT curvatureFromBelow, ON_3DVECTOR_STRUCT curvatureFromAbove,
  double* radiusOfCurvatureRatio, double* curvatureVectorAngleDegrees)
{
  if (p && radiusOfCurvatureRatio && curvatureVectorAngleDegrees)
  {
    const ON_3dVector* _curvatureFromBelow = (const ON_3dVector*)(&curvatureFromBelow);
    const ON_3dVector* _curvatureFromAbove = (const ON_3dVector*)(&curvatureFromAbove);
    return p->ComputeCurvatureRadiusRatio(*_curvatureFromBelow, *_curvatureFromAbove, 
      *radiusOfCurvatureRatio, *curvatureVectorAngleDegrees);
  }
  return false;
}

// Depends on Rhino application code; not available in an opennurbs-only (Rhino3dm) build.
#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_Curve* RHC_RhinoCreateCatenaryCurveThroughPoint(
  const ON_3DPOINT_STRUCT catenary_start_struct,
  const ON_3DPOINT_STRUCT catenary_end_struct,
  const ON_3DVECTOR_STRUCT axis_dir_struct,
  const ON_3DPOINT_STRUCT through_point_struct,
  const bool bSmooth,
  const int point_count,
  ON_3dPoint* apex_out,
  double* parameter_out,
  double* length_out,
  double* max_deviation_out)
{
  const ON_3dPoint* catenary_start = (const ON_3dPoint*)&catenary_start_struct;
  const ON_3dPoint* catenary_end = (const ON_3dPoint*)&catenary_end_struct;
  const ON_3dVector* axis_dir = (const ON_3dVector*)&axis_dir_struct;
  const ON_3dPoint* through_point = (const ON_3dPoint*)&through_point_struct;

  return RhinoCatenaryThroughPoint(
    *catenary_start,
    *catenary_end,
    *axis_dir,
    *through_point,
    bSmooth,
    point_count,
    apex_out,
    parameter_out,
    length_out,
    max_deviation_out);
}

RH_C_FUNCTION ON_Curve* RHC_RhinoCreateCatenaryCurveFromLength(
  const ON_3DPOINT_STRUCT catenary_start_struct,
  const ON_3DPOINT_STRUCT catenary_end_struct,
  const ON_3DVECTOR_STRUCT axis_dir_struct,
  const double catenary_length,
  const bool bSmooth,
  const int point_count,
  ON_3dPoint* apex_out,
  double* parameter_out,
  double* length_out,
  double* max_deviation_out)
{
  const ON_3dPoint* catenary_start = (const ON_3dPoint*)&catenary_start_struct;
  const ON_3dPoint* catenary_end = (const ON_3dPoint*)&catenary_end_struct;
  const ON_3dVector* axis_dir = (const ON_3dVector*)&axis_dir_struct;

  return RhinoCatenaryFromLength(
    *catenary_start,
    *catenary_end,
    *axis_dir,
    catenary_length,
    bSmooth,
    point_count,
    apex_out,
    parameter_out,
    length_out,
    max_deviation_out);
}

RH_C_FUNCTION ON_Curve* RHC_RhinoCreateCatenaryCurveFromParameter(
  const ON_3DPOINT_STRUCT catenary_start_struct,
  const ON_3DPOINT_STRUCT catenary_end_struct,
  const ON_3DVECTOR_STRUCT axis_dir_struct,
  const double catenary_parameter,
  const bool bSmooth,
  const int point_count,
  ON_3dPoint* apex_out,
  double* parameter_out,
  double* length_out,
  double* max_deviation_out)
{
  const ON_3dPoint* catenary_start = (const ON_3dPoint*)&catenary_start_struct;
  const ON_3dPoint* catenary_end = (const ON_3dPoint*)&catenary_end_struct;
  const ON_3dVector* axis_dir = (const ON_3dVector*)&axis_dir_struct;
  
  return RhinoCatenaryFromParameter(
    *catenary_start,
    *catenary_end,
    *axis_dir,
    catenary_parameter,
    bSmooth,
    point_count,
    apex_out,
    parameter_out,
    length_out,
    max_deviation_out);
}

RH_C_FUNCTION ON_Curve* RHC_RhinoCreateCatenaryCurveFromApex(
  const ON_3DPOINT_STRUCT catenary_start_struct,
  const ON_3DPOINT_STRUCT catenary_end_struct,
  const ON_3DVECTOR_STRUCT axis_dir_struct,
  const ON_3DPOINT_STRUCT catenary_apex_struct,
  const bool bSmooth,
  const int point_count,
  ON_3dPoint* apex_out,
  double* parameter_out,
  double* length_out,
  double* max_deviation_out)
{
  const ON_3dPoint* catenary_start = (const ON_3dPoint*)&catenary_start_struct;
  const ON_3dPoint* catenary_end = (const ON_3dPoint*)&catenary_end_struct;
  const ON_3dVector* axis_dir = (const ON_3dVector*)&axis_dir_struct;
  const ON_3dPoint* catenary_apex = (const ON_3dPoint*)&catenary_apex_struct;

  return RhinoCatenaryFromApex(
    *catenary_start,
    *catenary_end,
    *axis_dir,
    *catenary_apex,
    bSmooth,
    point_count,
    apex_out,
    parameter_out,
    length_out,
    max_deviation_out);
}
#endif

// The ON_NurbsCurveFitParameters/ON_NurbsCurveFitBuilder API and
// ON_Curve::NurbsCurveFit / RebuildToMatchTemplateCurve require OPENNURBS_PLUS,
// so these exports are unavailable in an opennurbs-only (Rhino3dm) build.
#if defined(OPENNURBS_PLUS)
RH_C_FUNCTION ON_NurbsCurve* ON_Curve_NurbsCurveFit(const ON_Curve* curve, ON_INTERVAL_STRUCT domain,
  ON_NurbsCurveFitParameters* fit_parameters,
  ON_Line* maximum_separation,
  double* this_separation_parameter,
  double* nurbs_separation_parameter
)
{
  if (nullptr == curve || nullptr == fit_parameters || nullptr == maximum_separation || nullptr == this_separation_parameter || nullptr == nurbs_separation_parameter)
    return nullptr;
  if (false == curve->Domain().IsIncreasing())
    return nullptr;

  *maximum_separation = ON_Line::ZeroLine;
  this_separation_parameter[0] = ON_UNSET_VALUE;
  nurbs_separation_parameter[0] = ON_UNSET_VALUE;

  ON_Interval _domain(domain.val[0], domain.val[1]);

  ON_NurbsCurveFitParameters local_fit_parameters(*fit_parameters);

  ON_NurbsCurve* result = curve->NurbsCurveFit(_domain,
    local_fit_parameters,
    nullptr,
    *maximum_separation,
    *this_separation_parameter,
    *nurbs_separation_parameter
  );
  return result;
}

RH_C_FUNCTION ON_NurbsCurve* ON_Curve_RebuildToMatchTemplateCurve(
  const ON_Curve* pConstCurve,
  const ON_Curve* pConstTemplateCurve,
  bool bFlipSourceDirection,
  bool bPreserveEndTangents,
  bool bMakeSubDFriendly,
  ON_Line* maximum_deviation
)
{
  if (nullptr == pConstCurve || nullptr == pConstTemplateCurve || nullptr == maximum_deviation)
    return nullptr;

  *maximum_deviation = ON_Line::NanLine;

  ON_NurbsCurve* result = pConstCurve->RebuildToMatchTemplateCurve(
    *pConstTemplateCurve,
    bFlipSourceDirection,
    bPreserveEndTangents,
    bMakeSubDFriendly,
    nullptr,
    maximum_deviation
  );
  return result;
}

RH_C_FUNCTION ON_NurbsCurveFitParameters* ON_NurbsCurveFitParameters_New()
{
  return new ON_NurbsCurveFitParameters();
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_Delete(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters)
{
  if (pNurbsCurveFitParameters)
    delete pNurbsCurveFitParameters;
}
enum NurbsCurveFitParametersDouble : int
{
  KinkAngleRadians = 0,
  KinkAngleDegrees = 1,
  SmoothingCoefficient = 2,
  UniformityCoefficient = 3,
  CurvatureBiasCoefficient = 4,
  Get_PointCountRangeTolerance = 5,
};

RH_C_FUNCTION double ON_NurbsCurveFitParameters_GetDouble(const ON_NurbsCurveFitParameters* pConstNurbsCurveFitParameters, enum NurbsCurveFitParametersDouble which)
{
  double rc = 0;
  if (pConstNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersDouble::KinkAngleRadians == which)
      rc = pConstNurbsCurveFitParameters->KinkAngleRadians();
    else if (NurbsCurveFitParametersDouble::KinkAngleDegrees == which)
      rc = pConstNurbsCurveFitParameters->KinkAngleDegrees();
    else if (NurbsCurveFitParametersDouble::SmoothingCoefficient == which)
      rc = pConstNurbsCurveFitParameters->SmoothingCoefficient();
    else if (NurbsCurveFitParametersDouble::UniformityCoefficient == which)
      rc = pConstNurbsCurveFitParameters->UniformityCoefficient();
    else if (NurbsCurveFitParametersDouble::CurvatureBiasCoefficient == which)
      rc = pConstNurbsCurveFitParameters->CurvatureBiasCoefficient();
    else if (NurbsCurveFitParametersDouble::Get_PointCountRangeTolerance == which)
      rc = pConstNurbsCurveFitParameters->PointCountRangeTolerance();
  }
  return rc;
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_SetDouble(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, enum NurbsCurveFitParametersDouble which, double val)
{
  if (pNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersDouble::KinkAngleRadians == which)
      pNurbsCurveFitParameters->SetKinkAngleRadians(val);
    else if (NurbsCurveFitParametersDouble::KinkAngleDegrees == which)
      pNurbsCurveFitParameters->SetKinkAngleDegrees(val);
    else if (NurbsCurveFitParametersDouble::SmoothingCoefficient == which)
      pNurbsCurveFitParameters->SetSmoothingCoefficient(val);
    else if (NurbsCurveFitParametersDouble::UniformityCoefficient == which)
      pNurbsCurveFitParameters->SetUniformityCoefficient(val);
    else if (NurbsCurveFitParametersDouble::CurvatureBiasCoefficient == which)
      pNurbsCurveFitParameters->SetCurvatureBiasCoefficient(val);
  }
}

enum NurbsCurveFitParametersBool : int
{
  SubDFriendly = 0,
  Closed = 1,
  OptimizeCurve = 2,
  ApplyTangentMatchingAtKinks = 3,
};

RH_C_FUNCTION bool ON_NurbsCurveFitParameters_GetBool(const ON_NurbsCurveFitParameters* pConstNurbsCurveFitParameters, enum NurbsCurveFitParametersBool which)
{
  bool rc = 0;
  if (pConstNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersBool::SubDFriendly == which)
      rc = pConstNurbsCurveFitParameters->SubDFriendly();
    if (NurbsCurveFitParametersBool::Closed == which)
      rc = pConstNurbsCurveFitParameters->Closed();
    if (NurbsCurveFitParametersBool::OptimizeCurve == which)
      rc = pConstNurbsCurveFitParameters->OptimizeCurve();
    if (NurbsCurveFitParametersBool::ApplyTangentMatchingAtKinks == which)
      rc = pConstNurbsCurveFitParameters->ApplyTangentMatchingAtKinks();
  }
  return rc;
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_SetBool(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, enum NurbsCurveFitParametersBool which, bool val)
{
  if (pNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersBool::SubDFriendly == which)
      pNurbsCurveFitParameters->SetSubDFriendly(val);
    if (NurbsCurveFitParametersBool::Closed == which)
      pNurbsCurveFitParameters->SetClosed(val);
    if (NurbsCurveFitParametersBool::OptimizeCurve == which)
      pNurbsCurveFitParameters->SetOptimizeCurve(val);
    if (NurbsCurveFitParametersBool::ApplyTangentMatchingAtKinks == which)
      pNurbsCurveFitParameters->SetApplyTangentMatchingAtKinks(val);
  }
}

enum NurbsCurveFitParametersByte : int
{
  TangentMatching = 0,
  KinkSplitting = 1,
  SmoothingIntensity = 2,
  UniformityIntensity = 3,
  CurvatureBiasIntensity = 4,
};

RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetByte(const ON_NurbsCurveFitParameters* pConstNurbsCurveFitParameters, enum NurbsCurveFitParametersByte which)
{
  int rc = 0;
  if (pConstNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersByte::TangentMatching == which)
      rc = (int)(pConstNurbsCurveFitParameters->TangentMatching());
    else if (NurbsCurveFitParametersByte::KinkSplitting == which)
      rc = (int)(pConstNurbsCurveFitParameters->KinkSplitting());
    else if (NurbsCurveFitParametersByte::SmoothingIntensity == which)
      rc = (int)(pConstNurbsCurveFitParameters->SmoothingIntensity());
    else if (NurbsCurveFitParametersByte::UniformityIntensity == which)
      rc = (int)(pConstNurbsCurveFitParameters->UniformityIntensity());
    else if (NurbsCurveFitParametersByte::CurvatureBiasIntensity == which)
      rc = (int)(pConstNurbsCurveFitParameters->CurvatureBiasIntensity());
  }
  return rc;
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_SetByte(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, enum NurbsCurveFitParametersByte which, int val)
{
  if (pNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersByte::TangentMatching == which)
      pNurbsCurveFitParameters->SetTangentMatching((ON_NurbsCurveFitParameters::TangentMatch)val);
    else if (NurbsCurveFitParametersByte::KinkSplitting == which)
      pNurbsCurveFitParameters->SetKinkSplitting((ON_NurbsCurveFitParameters::KinkSplit)val);
    else if (NurbsCurveFitParametersByte::SmoothingIntensity == which)
      pNurbsCurveFitParameters->SetSmoothingIntensity((ON_NurbsCurveFitParameters::Intensity)val);
    else if (NurbsCurveFitParametersByte::UniformityIntensity == which)
      pNurbsCurveFitParameters->SetUniformityIntensity((ON_NurbsCurveFitParameters::Intensity)val);
    else if (NurbsCurveFitParametersByte::CurvatureBiasIntensity == which)
      pNurbsCurveFitParameters->SetCurvatureBiasIntensity((ON_NurbsCurveFitParameters::Intensity)val);
  }
}

enum NurbsCurveFitParametersInt : int
{
  Degree = 0,
  PointCount = 1,
  Get_ConstrainedPointCount = 2,
  Get_PointCountRangeMinimum = 3,
  Get_PointCountRangeMaximum = 4,
  Get_ClampedControlPointCount = 5,
  Get_PeriodicControlPointCount = 6,
  Get_SampleCount = 7,
};

RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetInt(const ON_NurbsCurveFitParameters* pConstNurbsCurveFitParameters, enum NurbsCurveFitParametersInt which)
{
  int rc = 0;
  if (pConstNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersInt::Degree == which)
      rc = pConstNurbsCurveFitParameters->Degree();
    else if (NurbsCurveFitParametersInt::PointCount == which)
      rc = pConstNurbsCurveFitParameters->PointCount();
    else if (NurbsCurveFitParametersInt::Get_ConstrainedPointCount == which)
      rc = pConstNurbsCurveFitParameters->ConstrainedPointCount();
    else if (NurbsCurveFitParametersInt::Get_PointCountRangeMinimum == which)
      rc = pConstNurbsCurveFitParameters->PointCountRangeMinimum();
    else if (NurbsCurveFitParametersInt::Get_PointCountRangeMaximum == which)
      rc = pConstNurbsCurveFitParameters->PointCountRangeMaximum();
    else if (NurbsCurveFitParametersInt::Get_ClampedControlPointCount == which)
      rc = pConstNurbsCurveFitParameters->ClampedControlPointCount();
    else if (NurbsCurveFitParametersInt::Get_PeriodicControlPointCount == which)
      rc = pConstNurbsCurveFitParameters->PeriodicControlPointCount();
    else if (NurbsCurveFitParametersInt::Get_SampleCount == which)
      rc = pConstNurbsCurveFitParameters->SampleCount();
  }
  return rc;
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_SetInt(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, enum NurbsCurveFitParametersInt which, int val)
{
  if (pNurbsCurveFitParameters)
  {
    if (NurbsCurveFitParametersInt::Degree == which)
      pNurbsCurveFitParameters->SetDegree(val);
    else if (NurbsCurveFitParametersInt::PointCount == which)
      pNurbsCurveFitParameters->SetPointCount(val);

  }
}

RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetCurvatureBiasIntensityFromCoefficient(double coefficient)
{
  ON_NurbsCurveFitParameters::Intensity rc = ON_NurbsCurveFitParameters::Intensity::None;
  if (coefficient >= 0.0 && coefficient <= ON_NurbsCurveFitParameters::MaximumCurvatureBiasCoefficient)
  {
    if (0.0 == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::None;
    else if (ON_NurbsCurveFitParameters::LowCurvatureBiasCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Low;
    else if (ON_NurbsCurveFitParameters::ModerateCurvatureBiasCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Moderate;
    else if (ON_NurbsCurveFitParameters::MediumCurvatureBiasCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Medium;
    else if (ON_NurbsCurveFitParameters::HighCurvatureBiasCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::High;
    else if (ON_NurbsCurveFitParameters::ExtremeCurvatureBiasCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Extreme;
    else
    {
      rc = ON_NurbsCurveFitParameters::Intensity::Custom;
    }
  }
  else
  {
    // invalid input
    rc = ON_NurbsCurveFitParameters::Intensity::None;
  }
  return (int)rc;
}

RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetUniformityIntensityFromCoefficient(double coefficient)
{
  ON_NurbsCurveFitParameters::Intensity rc = ON_NurbsCurveFitParameters::Intensity::None;
  if (coefficient >= 0.0 && coefficient <= ON_NurbsCurveFitParameters::MaximumUniformityCoefficient)
  {
    if (0.0 == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::None;
    else if (ON_NurbsCurveFitParameters::LowUniformityCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Low;
    else if (ON_NurbsCurveFitParameters::ModerateUniformityCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Moderate;
    else if (ON_NurbsCurveFitParameters::MediumUniformityCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Medium;
    else if (ON_NurbsCurveFitParameters::HighUniformityCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::High;
    else if (ON_NurbsCurveFitParameters::ExtremeUniformityCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Extreme;
    else
    {
      rc = ON_NurbsCurveFitParameters::Intensity::Custom;
    }
  }
  else
  {
    // invalid input
    rc = ON_NurbsCurveFitParameters::Intensity::None;
  }
  return (int)rc;
}

RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetSmoothingIntensityFromCoefficient(double coefficient)
{
  ON_NurbsCurveFitParameters::Intensity rc = ON_NurbsCurveFitParameters::Intensity::None;
  if (coefficient >= 0.0 && coefficient <= ON_NurbsCurveFitParameters::MaximumSmoothingCoefficient)
  {
    if (0.0 == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::None;
    else if (ON_NurbsCurveFitParameters::LowSmoothingCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Low;
    else if (ON_NurbsCurveFitParameters::ModerateSmoothingCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Moderate;
    else if (ON_NurbsCurveFitParameters::MediumSmoothingCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Medium;
    else if (ON_NurbsCurveFitParameters::HighSmoothingCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::High;
    else if (ON_NurbsCurveFitParameters::ExtremeSmoothingCoefficient == coefficient)
      rc = ON_NurbsCurveFitParameters::Intensity::Extreme;
    else
    {
      rc = ON_NurbsCurveFitParameters::Intensity::Custom;
    }
  }
  else
  {
    // invalid input
    rc = ON_NurbsCurveFitParameters::Intensity::None;
  }
  return (int)rc;
}

RH_C_FUNCTION double ON_NurbsCurveFitParameters_GetCurvatureBiasCoefficientFromIntensity(int intensity)
{
  double value = ON_DBL_QNAN;
  switch ((ON_NurbsCurveFitParameters::Intensity)intensity)
  {
  case ON_NurbsCurveFitParameters::Intensity::None:
    value = 0.0;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Low:
    value = ON_NurbsCurveFitParameters::LowCurvatureBiasCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Moderate:
    value = ON_NurbsCurveFitParameters::ModerateCurvatureBiasCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Medium:
    value = ON_NurbsCurveFitParameters::MediumCurvatureBiasCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::High:
    value = ON_NurbsCurveFitParameters::HighCurvatureBiasCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Extreme:
    value = ON_NurbsCurveFitParameters::ExtremeCurvatureBiasCoefficient;
    break;
  default:
    value = 0.0;
    break;
  }
  return value;
}
RH_C_FUNCTION double ON_NurbsCurveFitParameters_GetSmoothingCoefficientFromIntensity(int intensity)
{
  double value = ON_DBL_QNAN;
  switch ((ON_NurbsCurveFitParameters::Intensity)intensity)
  {
  case ON_NurbsCurveFitParameters::Intensity::None:
    value = 0.0;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Low:
    value = ON_NurbsCurveFitParameters::LowSmoothingCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Moderate:
    value = ON_NurbsCurveFitParameters::ModerateSmoothingCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Medium:
    value = ON_NurbsCurveFitParameters::MediumSmoothingCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::High:
    value = ON_NurbsCurveFitParameters::HighSmoothingCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Extreme:
    value = ON_NurbsCurveFitParameters::ExtremeSmoothingCoefficient;
    break;
  default:
    value = 0.0;
    break;
  }
  return value;
}
RH_C_FUNCTION double ON_NurbsCurveFitParameters_GetUniformityCoefficientFromIntensity(int intensity)
{
  double value = ON_DBL_QNAN;
  switch ((ON_NurbsCurveFitParameters::Intensity)intensity)
  {
  case ON_NurbsCurveFitParameters::Intensity::None:
    value = 0.0;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Low:
    value = ON_NurbsCurveFitParameters::LowUniformityCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Moderate:
    value = ON_NurbsCurveFitParameters::ModerateUniformityCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Medium:
    value = ON_NurbsCurveFitParameters::MediumUniformityCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::High:
    value = ON_NurbsCurveFitParameters::HighUniformityCoefficient;
    break;
  case ON_NurbsCurveFitParameters::Intensity::Extreme:
    value = ON_NurbsCurveFitParameters::ExtremeUniformityCoefficient;
    break;
  default:
    value = 0.0;
    break;
  }
  return value;
}

RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetMinimumDegree()
{
  return ON_NurbsCurveFitParameters::MinimumDegree;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetMaximumDegree()
{
  return ON_NurbsCurveFitParameters::MaximumDegree;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetDefaultDegree()
{
  return ON_NurbsCurveFitParameters::DefaultDegree;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetMinimumClampedPointCount()
{
  return ON_NurbsCurveFitParameters::MinimumClampedPointCount;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetMinimumClosedPointCount()
{
  return ON_NurbsCurveFitParameters::MinimumClosedPointCount;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetMaximumPointCount()
{
  return ON_NurbsCurveFitParameters::MaximumPointCount;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetMinimumSampleCount()
{
  return ON_NurbsCurveFitParameters::MinimumSampleCount;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetMaximumSampleCount()
{
  return ON_NurbsCurveFitParameters::MaximumSampleCount;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_GetDefaultSampleCount()
{
  return ON_NurbsCurveFitParameters::DefaultSampleCount;
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_MinimumPointCountForDegree(int degree, bool bClosed, bool bSubDFriendly)
{
  return ON_NurbsCurveFitParameters::MinimumPointCountForDegree(degree, bClosed, bSubDFriendly);
}
RH_C_FUNCTION int ON_NurbsCurveFitParameters_MaximumPointCountForDegree(int point_count, bool bClosed, bool bSubDFreiendly)
{
  return ON_NurbsCurveFitParameters::MaximumDegreeForPointCount(point_count, bClosed, bSubDFreiendly);
}

RH_C_FUNCTION bool ON_NurbsCurveFitParameters_ValidInput(
  long sample_point_count,
  int degree,
  int control_point_count,
  bool bClosed,
  ON_INTERVAL_STRUCT curve_domain
)
{
  const ON_Interval* _curve_domain = (const ON_Interval*)&curve_domain;

  return ON_NurbsCurveFitParameters::ValidInput((size_t)sample_point_count, (unsigned)degree, (unsigned)control_point_count, bClosed, *_curve_domain);
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_PointCountRange(const ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, int* i_out, int* j_out)
{
  if (nullptr != pNurbsCurveFitParameters && nullptr != i_out && nullptr != j_out)
  {
    ON_2dex range = pNurbsCurveFitParameters->PointCountRange();
    *i_out = range.i;
    *j_out = range.j;
  }
}

RH_C_FUNCTION int ON_NurbsCurveFitParameters_VariablePointCount(const ON_NurbsCurveFitParameters* pNurbsCurveFitParameters)
{
  if(nullptr != pNurbsCurveFitParameters)
    return pNurbsCurveFitParameters->VariablePointCount();
  return 0;
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_SetPointCountRange(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, int minimum, int maximum)
{
  if (pNurbsCurveFitParameters)
    pNurbsCurveFitParameters->SetPointCountRange(minimum, maximum);
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_SetPointCountRange2(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, int minimum, int maximum, double tolerance)
{
  if (pNurbsCurveFitParameters)
    pNurbsCurveFitParameters->SetPointCountRange(minimum, maximum, tolerance);
}

RH_C_FUNCTION void ON_NurbsCurveFitParameters_PointCountRange2(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, int* minimum, int* maximum, double* tolerance)
{
  if (pNurbsCurveFitParameters)
    pNurbsCurveFitParameters->GetPointCountRange(*minimum, *maximum, *tolerance);
}

RH_C_FUNCTION ON_NurbsCurveFitParameters* ON_NurbsCurveFitParameters_AssignmentOperator(ON_NurbsCurveFitParameters* pNurbsCurveFitParameters, const ON_NurbsCurveFitParameters* pOther)
{
  if (pNurbsCurveFitParameters && pOther)
    *pNurbsCurveFitParameters = *pOther;
  return pNurbsCurveFitParameters;
}

RH_C_FUNCTION ON_NurbsCurve* ON_Curve_NurbsCurveFit2(const ON_Curve* curve,
  ON_INTERVAL_STRUCT domain,
  ON_NurbsCurveFitParameters* fit_parameters_ptr,
  ON_Line* maximum_separation,
  double* this_separation_parameter,
  double* nurbs_separation_parameter)
{
  *maximum_separation = ON_Line::NanLine;
  *this_separation_parameter = ON_DBL_QNAN;
  *nurbs_separation_parameter = ON_DBL_QNAN;
  ON_NurbsCurveFitParameters curve_options = *fit_parameters_ptr;
  ON_Interval _domain(domain.val[0], domain.val[1]);

  const int degree = curve_options.Degree();
  if (degree < 1)
    return nullptr; // calculation failed

  if (curve_options.PointCount() < degree)
    return nullptr; // calculation failed

  const ON_Interval this_domain = curve->Domain();
  if (false == _domain.IsIncreasing() || false == (this_domain.Includes(_domain, true)))
    _domain = this_domain;

  // Since this is the simple SDK entry point for rebuilding curves,
  // local_rp is fit_parameters with the common adjustments made
  // for users who don't tailor the settings to a specific input curve.
  ON_NurbsCurveFitParameters local_fp(curve_options);

  const bool bIsClosed = _domain == this_domain && curve->IsClosed();
  local_fp.SetClosed(bIsClosed);

  ON_NurbsCurveFitBuilder builder;
  if (false == builder.InitializeFromInputCurve(curve, _domain, ON_NurbsCurveFitParameters::KinkSplit::None != local_fp.KinkSplitting()))
    return nullptr; // calculation failed

  //if (false == builder.CalculateNurbsCurveFit(local_fp, true, 0))
  //  return nullptr; // calculation failed

  // fix for kinks
  if (curve_options.OptimizeCurve() && ON_NurbsCurveFitParameters::KinkSplit::None != curve_options.KinkSplitting())
  {
    // Fix for RH-85985 
    // Automatically grow point count if kink splitting requires more points
    unsigned max_kink_segments_point_count = 0;
    const ON_CurveKinkDefinition kdef = curve_options.KinkDefinition();
    const int degree = curve_options.Degree();
    const int desired_point_count = curve_options.PointCountRangeMinimum();

    {
      if (builder.SetKinkSegmentsIntervals(kdef) >= 2)
      {
        const unsigned kink_segments_point_count = builder.SetKinkSegmentsPointCounts(degree, desired_point_count);
        if (kink_segments_point_count > max_kink_segments_point_count)
          max_kink_segments_point_count = kink_segments_point_count;
      }
    }
    if (((int)max_kink_segments_point_count) > desired_point_count && false == curve_options.VariablePointCount())
    {
      // kink splitting required increasing point count and user wants all curves to have the same number of points.
      curve_options.SetPointCount(((int)max_kink_segments_point_count));
      {
        if (builder.SetKinkSegmentsIntervals(kdef) >= 2)
        {
          // update any curves that were processed before the one that set the final max_kink_segments_point_count.
          builder.SetKinkSegmentsPointCounts(degree, curve_options.PointCount());
        }
      }
    }
  }

  // The value of bPeriodic is false if the input curve
  // is open and true if the input curve is closed.
  // Thus, when multiple curves are selected (curve_count > 0)
  // the and some are open while others are closed, the
  // value of ON_NurbsCurveFitParameters::Periodic() can vary
  // and that is why ths local variable curve_options
  // is set the way it is below. Aside from the Periodic
  // setting, all of the remaining options in m_fit_parameters
  // apply to both open and closed curves.
  if (ON_NurbsCurveFitParameters::KinkSplit::None != curve_options.KinkSplitting() && builder.m_kink_segments_point_count > curve_options.PointCountRangeMinimum())
  {
    if (curve_options.VariablePointCount() && builder.m_kink_segments_point_count < curve_options.PointCountRangeMaximum())
      curve_options.SetPointCountRange(builder.m_kink_segments_point_count, curve_options.PointCountRangeMaximum(), curve_options.PointCountRangeTolerance());
    else
      curve_options.SetPointCount(builder.m_kink_segments_point_count);
  }

  if (false == builder.TangentMatchCandidate())
    curve_options.SetTangentMatching(ON_NurbsCurveFitParameters::TangentMatch::None);

  curve_options.SetClosed(builder.IsClosed());

  const bool bSuccessfulRebuild = builder.CalculateNurbsCurveFit(
    curve_options,
    true,
    0);

  if (bSuccessfulRebuild)
  {
    // calculation succeeded.
    *maximum_separation = builder.m_maximum_separation;
    *this_separation_parameter = builder.m_maximum_separation_parameters[0];
    *nurbs_separation_parameter = builder.m_maximum_separation_parameters[1];

    return new ON_NurbsCurve(builder.m_nurbs_curve_fit);
    //const double sep = builder.MaximumSeparation().Length();
  }
  return nullptr; // calculation failed
}

#endif
