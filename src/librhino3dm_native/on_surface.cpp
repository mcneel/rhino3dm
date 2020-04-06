#include "stdafx.h"

RH_C_FUNCTION ON_Surface* ON_Surface_DuplicateSurface(ON_Surface* pSurface)
{
  if( pSurface )
    return pSurface->DuplicateSurface();
  return nullptr;
}

RH_C_FUNCTION ON_Brep* ON_Surface_BrepForm(const ON_Surface* pConstSurface)
{
  if( pConstSurface )
    return pConstSurface->BrepForm();
  return nullptr;
}

RH_C_FUNCTION void ON_Surface_Domain( const ON_Surface* pConstSurface, int dir, ON_Interval* pDomain )
{
  if( pConstSurface && pDomain )
    *pDomain = pConstSurface->Domain(dir);
}

RH_C_FUNCTION bool ON_Surface_SetDomain(ON_Surface* pSurface, int direction, ON_INTERVAL_STRUCT domain)
{
  bool rc = false;
  if( pSurface )
  {
    const ON_Interval* _domain = (const ON_Interval*)&domain;
    rc = pSurface->SetDomain(direction, *_domain);
  }
  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DMIO_BUILD)

RH_C_FUNCTION bool ON_Surface_GetSurfaceSize( const ON_Surface* pConstSurface, double* width, double* height)
{
  bool rc = false;
  if( pConstSurface )
  {
    rc = pConstSurface->GetSurfaceSize(width, height)?true:false;
  }
  return rc;
}

#endif

RH_C_FUNCTION int ON_Surface_SpanCount(const ON_Surface* pConstSurface, int direction)
{
  int rc = 0;
  if( pConstSurface )
    rc = pConstSurface->SpanCount(direction);
  return rc;
}

RH_C_FUNCTION bool ON_Surface_GetSpanVector(const ON_Surface* pConstSurface, int direction, /*ARRAY*/double* span_vector)
{
  bool rc = false;
  if( pConstSurface )
  {
    rc = pConstSurface->GetSpanVector(direction, span_vector)?true:false;
  }
  return rc;
}

RH_C_FUNCTION int ON_Surface_Degree(const ON_Surface* pConstSurface, int direction)
{
  int rc = 0;
  if( pConstSurface )
    rc = pConstSurface->Degree(direction);
  return rc;
}

RH_C_FUNCTION int ON_Surface_IsIsoparametric(const ON_Surface* pConstSurface, const ON_Curve* pCurve, ON_INTERVAL_STRUCT curveDomain)
{
  int rc = 0;
  if( pConstSurface && pCurve )
  {
    const ON_Interval* pDomain = (const ON_Interval*)&curveDomain;
    if( !pDomain->IsValid() )
      pDomain = nullptr;
    rc = (int)(pConstSurface->IsIsoparametric(*pCurve, pDomain));
  }
  return rc;
}

RH_C_FUNCTION int ON_Surface_IsIsoparametric2(const ON_Surface* pConstSurface, ON_3DPOINT_STRUCT bbox_min, ON_3DPOINT_STRUCT bbox_max)
{
  int rc = 0;
  if( pConstSurface )
  {
    const ON_3dPoint* _min = (const ON_3dPoint*)&bbox_min;
    const ON_3dPoint* _max = (const ON_3dPoint*)&bbox_max;
    ON_BoundingBox bbox(*_min, *_max);
    rc = (int)(pConstSurface->IsIsoparametric(bbox));
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_IsPlanar( const ON_Surface* pConstSurface, ON_PLANE_STRUCT* plane, double tolerance, bool computePlane)
{
  bool rc = false;
  if( pConstSurface )
  {
    ON_Plane* pPlane = nullptr;
    ON_Plane temp;
    if( plane && computePlane )
    {
      temp = FromPlaneStruct(*plane);
      pPlane = &temp;
    }
    rc = pConstSurface->IsPlanar( pPlane, tolerance )?true:false;
    if( rc && pPlane && plane )
      CopyToPlaneStruct(*plane, *pPlane);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_IsSphere( const ON_Surface* pConstSurface, ON_Sphere* sphere, double tolerance, bool computeSphere)
{
  bool rc = false;
  if( pConstSurface )
  {
    ON_Sphere* fillin = nullptr;
    if( computeSphere )
      fillin = sphere;
    rc = pConstSurface->IsSphere( fillin, tolerance )?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_IsCylinder(const ON_Surface* pConstSurface, ON_Cylinder* cylinder, double tolerance, bool computeCylinder)
{
  bool rc = false;
  if (pConstSurface)
  {
    ON_Cylinder* fillin = nullptr;
    if (computeCylinder)
      fillin = cylinder;

    rc = pConstSurface->IsCylinder(fillin, tolerance) ? true : false;

    // 2-Sep-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-35469
    // If a developer wants to attempt to reverse engineer a surface that looks
    // like a cylinder, computing Height1 and Height2, they can do so in their
    // own code.
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_IsCone( const ON_Surface* pConstSurface, ON_Cone* cone, double tolerance, bool computeCone)
{
  bool rc = false;
  if( pConstSurface )
  {
    ON_Cone* fillin = nullptr;
    if( computeCone )
      fillin = cone;
    rc = pConstSurface->IsCone( fillin, tolerance )?true:false;
  }
  return rc;
}
RH_C_FUNCTION bool ON_Surface_IsTorus( const ON_Surface* pConstSurface, ON_Torus* torus, double tolerance, bool computeTorus)
{
  bool rc = false;
  if( pConstSurface )
  {
    ON_Torus* fillin = nullptr;
    if( computeTorus )
      fillin = torus;
    rc = pConstSurface->IsTorus( fillin, tolerance )?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_GetBool(const ON_Surface* pConstSurface, int direction, int which)
{
  const int idxIsClosed = 0;
  const int idxIsPeriodic = 1;
  const int idxIsSingular = 2;
  const int idxIsSolid = 3;
  bool rc = false;
  if( pConstSurface )
  {
    if( idxIsClosed == which )
      rc = pConstSurface->IsClosed(direction)?true:false;
    else if( idxIsPeriodic == which )
      rc = pConstSurface->IsPeriodic(direction)?true:false;
    else if( idxIsSingular == which )
      rc = pConstSurface->IsSingular(direction)?true:false;
    else if( idxIsSolid == which )
    {
      rc = pConstSurface->IsSolid();
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_IsAtSingularity(const ON_Surface* pConstSurface, double s, double t, bool exact)
{
  bool rc = false;
  if( pConstSurface )
  {
    rc = pConstSurface->IsAtSingularity(s,t,exact);
  }
  return rc;
}

RH_C_FUNCTION int ON_Surface_IsAtSeam(const ON_Surface* pConstSurface, double s, double t)
{
  int rc = 0;
  if( pConstSurface )
    rc = pConstSurface->IsAtSeam(s,t);
  return rc;
}

RH_C_FUNCTION bool ON_Surface_GetNextDiscontinuity(const ON_Surface* pConstSurface, int direction, int continuityType, double t0, double t1, double* t)
{
  bool rc = false;
  if( pConstSurface )
  {
    rc = pConstSurface->GetNextDiscontinuity(direction, ON::Continuity(continuityType), t0, t1, t);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_IsContinuous(const ON_Surface* pConstSurface, int continuityType, double s, double t)
{
  bool rc = false;
  if( pConstSurface )
  {
    rc = pConstSurface->IsContinuous(ON::Continuity(continuityType), s, t);
  }
  return rc;
}

RH_C_FUNCTION void ON_Surface_NormalAt(const ON_Surface* pConstSurface, double u, double v, ON_3dVector* vector)
{
  if( pConstSurface && vector )
  {
    *vector = pConstSurface->NormalAt(u,v);
    const ON_BrepFace* pFace = ON_BrepFace::Cast(pConstSurface);
    if( pFace && pFace->m_bRev )
      vector->Reverse();
  }
}

RH_C_FUNCTION bool ON_Surface_FrameAt(const ON_Surface* pConstSurface, double u, double v, ON_PLANE_STRUCT* frame)
{
  bool rc = false;
  if( pConstSurface && frame )
  {
    ON_Plane temp;
    rc = pConstSurface->FrameAt(u,v,temp)?true:false;
    const ON_BrepFace* pFace = ON_BrepFace::Cast(pConstSurface);
    if( pFace && pFace->m_bRev )
      temp.Flip();
    CopyToPlaneStruct(*frame, temp);
  }
  return rc;
}

RH_C_FUNCTION ON_Surface* ON_Surface_Trim(const ON_Surface* pConstSurface, ON_INTERVAL_STRUCT u_domain, ON_INTERVAL_STRUCT v_domain)
{
  ON_Surface* rc = nullptr;
  if( pConstSurface )
  {
    rc = pConstSurface->DuplicateSurface();
    if( rc )
    {
      bool success = rc->Trim(0, ON_Interval(u_domain.val[0], u_domain.val[1])) &&
                     rc->Trim(1, ON_Interval(v_domain.val[0], v_domain.val[1]));
      if( !success )
      {
        delete rc;
        rc = nullptr;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_Surface_IsoCurve(const ON_Surface* pConstSurface, int direction, double constantParameter)
{
  if( pConstSurface )
    return pConstSurface->IsoCurve(direction, constantParameter);
  return nullptr;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DMIO_BUILD)

RH_C_FUNCTION ON_Curve* ON_Surface_Pushup(const ON_Surface* pConstSurface, const ON_Curve* pCurve2d, double tolerance, ON_INTERVAL_STRUCT curve2dSubdomain)
{
  ON_Curve* rc = nullptr;
  if( pConstSurface && pCurve2d )
  {
    const ON_Interval* pSubDomain = (const ON_Interval*)&curve2dSubdomain;
    if( !pSubDomain->IsValid() )
      pSubDomain = nullptr;
    rc = pConstSurface->Pushup(*pCurve2d, tolerance, pSubDomain);
  }
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_Surface_Pullback(const ON_Surface* pConstSurface, const ON_Curve* pCurve3d, double tolerance, ON_INTERVAL_STRUCT curve3dSubdomain)
{
  ON_Curve* rc = nullptr;
  if( pConstSurface && pCurve3d )
  {
    const ON_Interval* pSubDomain = (const ON_Interval*)&curve3dSubdomain;
    if( !pSubDomain->IsValid() )
      pSubDomain = nullptr;
    rc = pConstSurface->Pullback(*pCurve3d, tolerance, pSubDomain);
  }
  return rc;
}

#endif

RH_C_FUNCTION int ON_Surface_HasNurbsForm(const ON_Surface* pConstSurface)
{
  int rc = 0;
  if( pConstSurface )
    rc = pConstSurface->HasNurbForm();
  return rc;
}

RH_C_FUNCTION bool ON_Surface_EvPoint( const ON_Surface* pConstSurface, double s, double t, ON_3dPoint* point )
{
  bool rc = false;
  if( pConstSurface && point )
  {
    rc = pConstSurface->EvPoint(s,t,*point)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_EvCurvature( const ON_Surface* pConstSurface, 
                                           double s, double t, 
                                           ON_3dPoint* point, 
                                           ON_3dVector* normal, 
                                           ON_3dVector* kappa1, 
                                           ON_3dVector* kappa2, 
                                           double* gauss, 
                                           double* mean, 
                                           double* k1,
                                           double* k2)
{
  bool rc = false;
  
  if( pConstSurface && point && normal && kappa1 && kappa2 )
  {
    if( pConstSurface->EvNormal(s, t, *point, *normal) )
    {
      ON_3dPoint origin;
      ON_3dVector du, dv, duu, duv, dvv;

      if( pConstSurface->Ev2Der(s, t, origin, du, dv, duu, duv, dvv) )
      {
        if( ON_EvPrincipalCurvatures(du, dv, duu, duv, dvv, *normal, gauss, mean, k1, k2, *kappa1, *kappa2) )
        {
          rc = true;
        }
      }
    }
  }

  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DMIO_BUILD)

RH_C_FUNCTION bool ON_Surface_GetClosestPoint( const ON_Surface* pConstSurface, ON_3DPOINT_STRUCT test_point, double* s, double* t )
{
  bool rc = false;
  if( pConstSurface && s && t )
  {
    const ON_3dPoint* _test_point = (const ON_3dPoint*)&test_point;
    rc = pConstSurface->GetClosestPoint( *_test_point, s, t );
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_GetLocalClosestPoint(const ON_Surface* pConstSurface, ON_3DPOINT_STRUCT test_point, double su, double sv, double* s, double* t)
{
  bool rc = false;
  if (pConstSurface && s && t)
  {
    const ON_3dPoint* _test_point = (const ON_3dPoint*)&test_point;
    rc = pConstSurface->GetLocalClosestPoint(*_test_point, su, sv, s, t);
  }
  return rc;
}

#endif

RH_C_FUNCTION ON_NurbsSurface* ON_Surface_GetNurbForm(ON_Surface* pSurface, double tolerance, int* accuracy)
{
  ON_NurbsSurface* pNurbForm = nullptr;
  if (pSurface && accuracy)
  {
    pNurbForm = ON_NurbsSurface::New();
    *accuracy = pSurface->GetNurbForm(*pNurbForm, tolerance);

    if (!*accuracy)
    {
      delete pNurbForm;
      pNurbForm = nullptr;
    }
  }

  return pNurbForm;
}


RH_C_FUNCTION bool ON_Surface_TranslateParameter(const ON_Surface* constSurfacePtr, double fromS, double fromT, double* toS, double* toT, bool surfaceParameterFromNurbForm)
{
  bool rc = false;
	if (constSurfacePtr && toS && toT)
	{
    if (surfaceParameterFromNurbForm)
    {
      rc = constSurfacePtr->GetSurfaceParameterFromNurbFormParameter(fromS, fromT, toS, toT);
    }
    else
    {
      rc = constSurfacePtr->GetNurbFormParameterFromSurfaceParameter(fromS, fromT, toS, toT);
    }
	}
  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DMIO_BUILD)

RH_C_FUNCTION ON_Surface* ON_Surface_Offset( const ON_Surface* pConstSurface, double offset, double tolerance)
{
  if( pConstSurface )
    return pConstSurface->Offset(offset, tolerance);
  return nullptr;
}

#endif

RH_C_FUNCTION ON_Surface* ON_Surface_Reverse( const ON_Surface* pConstSurface, int direction )
{
  ON_Surface* rc = nullptr;
  if( pConstSurface )
  {
    rc = pConstSurface->DuplicateSurface();
    if( rc )
    {
      if( !rc->Reverse(direction) )
      {
        delete rc;
        rc = nullptr;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_Reverse2( ON_Surface* pSurface, int direction )
{
  if( pSurface )
    return pSurface->Reverse(direction)?true:false;
  return false;
}

RH_C_FUNCTION ON_Surface* ON_Surface_Transpose( const ON_Surface* pConstSurface )
{
  ON_Surface* rc = nullptr;
  if( pConstSurface )
  {
    rc = pConstSurface->DuplicateSurface();
    if( rc )
    {
      if( !rc->Transpose() )
      {
        delete rc;
        rc = nullptr;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Surface_Transpose2( ON_Surface* pSurface )
{
  if( pSurface )
    return pSurface->Transpose()? true: false;
  return false;
}

RH_C_FUNCTION void ON_Surface_Split(const ON_Surface* pConstSurface, int direction, double c, ON_SimpleArray<ON_Surface*>* pSurfaceArray)
{
  if( pConstSurface && pSurfaceArray )
  {
    ON_Surface* pSurf1 = nullptr;
    ON_Surface* pSurf2 = nullptr;
    if( !pConstSurface->Split(direction, c, pSurf1, pSurf2) )
    {
      if( pSurf1 )
        delete pSurf1;
      if( pSurf2 )
        delete pSurf2;
      pSurf1 = nullptr;
      pSurf2 = nullptr;
    }
    if( pSurf1 )
      pSurfaceArray->Append(pSurf1);
    if( pSurf2 )
      pSurfaceArray->Append(pSurf2);
  }
}

RH_C_FUNCTION bool ON_Surface_Evaluate(const ON_Surface* pConstSurface, double u, double v, int numDer, int stride, /*ARRAY*/double* der_array)
{
  bool rc = false;
  if( pConstSurface && der_array )
  {
    rc = pConstSurface->Evaluate(u,v,numDer, stride, der_array) ? true:false;
  }
  return rc;
}

// move to on_sumsurface.cpp once we have one
RH_C_FUNCTION ON_SumSurface* ON_SumSurface_Create(const ON_Curve* pConstCurveA, const ON_Curve* pConstCurveB)
{
  ON_SumSurface* rc = nullptr;
  if( pConstCurveA && pConstCurveB )
  {
    rc = ON_SumSurface::New();
    if( rc )
    {
      if( !rc->Create( *pConstCurveA, *pConstCurveB ) )
      {
        delete rc;
        rc = nullptr;
      }
    }
  }
  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DMIO_BUILD)

RH_C_FUNCTION int ON_Surface_ClosestSide( const ON_Surface* pConstSurface, double u, double v )
{
  ON_Surface::ISO edge_index = ON_Surface::not_iso;
  if( pConstSurface )
  {
    // Find the edge that is closest to the test point
    ON_3dPoint pt;
    if( pConstSurface->EvPoint(u,v,pt) )
    {
      double dist = ON_UNSET_VALUE;
      ON_Brep brep;
      ON_Surface* pSrf = pConstSurface->DuplicateSurface();
      if( pSrf && brep.Create(pSrf) )
      {
        for( int i=0; i<brep.m_T.Count(); i++ )
        {
          const ON_BrepEdge* edge = brep.m_T[i].Edge();
          if( edge )
          {
            double t = 0.0;
            if( edge->GetClosestPoint(pt, &t) )
            {
              double d = pt.DistanceTo( edge->PointAt(t) );
              if( dist == ON_UNSET_VALUE || d < dist )
              {
                dist = d;
                edge_index = brep.m_T[i].m_iso;
              }
            }
          }
        }
      }
    }
  }
  return (int)edge_index;
}

#endif

////////////////////////////////////////////////////////////////////////////////////
// Meshing and mass property calculations are not available in stand alone opennurbs

#if !defined(RHINO3DMIO_BUILD)

RH_C_FUNCTION ON_Mesh* ON_Surface_CreateMesh(const ON_Surface* pConstSurface, const ON_MeshParameters* pConstMeshingParameters)
{
  if (nullptr == pConstSurface)
    return nullptr;

  ON_Mesh* rc = nullptr;
  if (pConstMeshingParameters)
    rc = pConstSurface->CreateMesh(*pConstMeshingParameters);
  else
    rc = pConstSurface->CreateMesh(ON_MeshParameters::DefaultMesh);
  return rc;
}

RH_C_FUNCTION ON_MassProperties* ON_Surface_MassProperties(bool bArea, const ON_Surface* pConstSurface, bool bVolume, bool bFirstMoments, bool bSecondMoments, bool bProductMoments, double relativeTolerance, double absoluteTolerance)
{
  ON_MassProperties* rc = nullptr;
  if (pConstSurface)
  {
    rc = new ON_MassProperties();
    bool success = bArea ?
      pConstSurface->AreaMassProperties(*rc, bVolume, bFirstMoments, bSecondMoments, bProductMoments, relativeTolerance, absoluteTolerance) :
      pConstSurface->VolumeMassProperties(*rc, bVolume, bFirstMoments, bSecondMoments, bProductMoments, ON_UNSET_POINT, relativeTolerance, absoluteTolerance);
    if (!success)
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

#endif
