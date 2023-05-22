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

RH_C_FUNCTION void ON_Curve_PointAt( const ON_Curve* pCurve, double t, ON_3dPoint* pt, int which )
{
  RHCHECK_LICENSE
  const int idxPointAtT = 0;
  const int idxPointAtStart = 1;
  const int idxPointAtEnd = 2;
  if( pCurve && pt )
  {
    if( idxPointAtT == which )
      *pt = pCurve->PointAt(t);
    else if( idxPointAtStart == which )
      *pt = pCurve->PointAtStart();
    else if( idxPointAtEnd == which )
      *pt = pCurve->PointAtEnd();
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
    // https://mcneel.myjetbrains.com/youtrack/issue/RH-46324
    // For whatever reason, a ON_NurbsCurve*, not a TL_NurbsCurve*, was passed in
    // which means it's vtable is incorrectly set. This fix will ensure NURBS
    // curves always return a length.
    const ON_NurbsCurve* on_nc = ON_NurbsCurve::Cast(pCurve);
    if (nullptr != on_nc)
    {
      const TL_NurbsCurve* tl_nc = TL_NurbsCurve::Promote(on_nc);
      if (nullptr != tl_nc)
        rc = tl_nc->GetLength(length, fractional_tol, _sub_domain) ? true : false;
    }
    if (!rc)
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

RH_C_FUNCTION bool ONC_JoinCurves(const ON_SimpleArray<const ON_Curve*>* pInCurves, ON_SimpleArray<ON_Curve*>* pOutCurves, double joinTolerance, bool bPreserveDirection)
{
  // 18-Jan-2021 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-67058
  bool rc = false;
  if (pInCurves && pOutCurves)
  {
    int count = ON_JoinCurves(*pInCurves, *pOutCurves, joinTolerance, bPreserveDirection, nullptr);
    rc = (count > 0);
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
#endif
