#include "stdafx.h"
#include <map>

#if !defined(RHINO3DM_BUILD)
// ray shooter and mesh/mesh intersect not supported in stand alone OpenNURBS
#include "../../../rhino4/mesh_boolean_v7.h"
#endif

RH_C_FUNCTION bool ON_Intersect_LineLine(ON_Line* lineA, ON_Line* lineB, double* a, double* b)
{
  bool rc = false;
  if( lineA && lineB )
  {
    rc = ::ON_Intersect(*lineA, *lineB, a, b);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Intersect_LinePlane(ON_Line* line, const ON_PLANE_STRUCT* plane, double* parameterOnLine)
{
  bool rc = false;
  if( line && plane )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    rc = ::ON_Intersect(*line, temp, parameterOnLine);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Intersect_PlanePlane(const ON_PLANE_STRUCT* planeA, const ON_PLANE_STRUCT* planeB, ON_Line* line)
{
  bool rc = false;
  if( line && planeA && planeB )
  {
    ON_Plane tempA = FromPlaneStruct(*planeA);
    ON_Plane tempB = FromPlaneStruct(*planeB);
    rc = ::ON_Intersect(tempA, tempB, *line);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Intersect_PlanePlanePlane(const ON_PLANE_STRUCT* planeA, const ON_PLANE_STRUCT* planeB, const ON_PLANE_STRUCT* planeC, ON_3dPoint* intersectionPoint)
{
  bool rc = false;
  if( intersectionPoint && planeA && planeB && planeC )
  {
    ON_Plane tempA = FromPlaneStruct(*planeA);
    ON_Plane tempB = FromPlaneStruct(*planeB);
    ON_Plane tempC = FromPlaneStruct(*planeC);
    rc = ::ON_Intersect(tempA, tempB, tempC, *intersectionPoint);
  }
  return rc;
}

RH_C_FUNCTION int ON_Intersect_PlaneSphere(const ON_PLANE_STRUCT* plane, ON_Sphere* sphere, ON_CIRCLE_STRUCT* intersectionCircle)
{
  int rc = 0;
  if( plane && sphere && intersectionCircle )
  {
    sphere->plane.UpdateEquation();
    ON_Plane temp = FromPlaneStruct(*plane);
    ON_Circle circle = FromCircleStruct(*intersectionCircle);
    rc = ON_Intersect(temp, *sphere, circle);
    CopyToCircleStruct(*intersectionCircle, circle);
  }
  return rc;
}

RH_C_FUNCTION int ON_Intersect_LineSphere(ON_Line* line, ON_Sphere* sphere, ON_3dPoint* point1, ON_3dPoint* point2)
{
  int rc = 0;
  if( line && sphere && point1 && point2 )
  {
    sphere->plane.UpdateEquation();
    rc = ::ON_Intersect(*line, *sphere, *point1, *point2);
  }
  return rc;
}

RH_C_FUNCTION int ON_Intersect_LineCircle(const ON_Line* pLine, const ON_CIRCLE_STRUCT* pCircle, double* t1, ON_3dPoint* point1, double* t2, ON_3dPoint* point2)
{
  int rc = 0;
  if( pLine && pCircle && t1 && point1 && t2 && point2 )
  {
    ON_Circle circle = FromCircleStruct(*pCircle);
    rc = ::ON_Intersect(*pLine, circle, t1, *point1, t2, *point2);
  }
  return rc;
}

RH_C_FUNCTION int ON_Intersect_LineCylinder(ON_Line* line, ON_Cylinder* cylinder, ON_3dPoint* point1, ON_3dPoint* point2)
{
  int rc = 0;
  if( line && cylinder && point1 && point2 )
  {
    cylinder->circle.plane.UpdateEquation();
    rc = ::ON_Intersect(*line, *cylinder, *point1, *point2);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Intersect_BoundingBoxLine(ON_BoundingBox* box, ON_Line* line, double tolerance, ON_Interval* t)
{
  bool rc = false;
  if( box && line )
  {
    rc = ::ON_Intersect(*box, *line, tolerance, t);
  }
  return rc;
}

RH_C_FUNCTION int ON_Intersect_SphereSphere(ON_Sphere* sphereA, ON_Sphere* sphereB, ON_CIRCLE_STRUCT* pCircle)
{
  int rc = 0;
  if( sphereA && sphereB && pCircle )
  {
    sphereA->plane.UpdateEquation();
    sphereB->plane.UpdateEquation();
    ON_Circle circle = FromCircleStruct(*pCircle);
    rc = ::ON_Intersect(*sphereA, *sphereB, circle);
    CopyToCircleStruct(*pCircle, circle);
  }
  return rc;
}

// return number of points in a certain polyline
RH_C_FUNCTION int ON_Intersect_MeshPlanes2(ON_SimpleArray<ON_Polyline*>* pPolylines, int i)
{
  int rc = 0;
  if( pPolylines && i>=0 && i<pPolylines->Count() )
  {
    ON_Polyline* polyline = (*pPolylines)[i];
    if( polyline )
      rc = polyline->Count();
  }
  return rc;
}

RH_C_FUNCTION void ON_Intersect_MeshPlanes3(ON_SimpleArray<ON_Polyline*>* pPolylines, int i, int point_count, /*ARRAY*/ON_3dPoint* points)
{
  if( nullptr==pPolylines || i<0 || i>=pPolylines->Count() || point_count<0 || nullptr==points )
    return;
  ON_Polyline* polyline = (*pPolylines)[i];
  if( NULL==polyline || polyline->Count()!=point_count )
    return;

  const ON_3dPoint* source = polyline->Array();
  ::memcpy(points, source, sizeof(ON_3dPoint) * point_count);
}

RH_C_FUNCTION void ON_Intersect_MeshPlanes4(ON_SimpleArray<ON_Polyline*>* pPolylines)
{
  if( NULL==pPolylines )
    return;
  int count = pPolylines->Count();
  for( int i=0; i<count; i++ )
  {
    ON_Polyline* polyline = (*pPolylines)[i];
    if( polyline )
      delete polyline;
  }
  delete pPolylines;
}

///////////////////////////////////////////////////////////////////////////////
// ray shooter and mesh/mesh intersect not supported in stand alone OpenNURBS
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION ON_SimpleArray<ON_X_EVENT>* ON_Intersect_CurveSelf(const ON_Curve* pCurve, double tolerance)
{
  ON_SimpleArray<ON_X_EVENT>* rc = nullptr;
  if(pCurve)
  {
    rc = new ON_SimpleArray<ON_X_EVENT>();
    pCurve->IntersectSelf(*rc, tolerance);
  }
 
  return rc;
}

RH_C_FUNCTION ON_SimpleArray<ON_X_EVENT>* ON_Intersect_CurveCurve(
  const ON_Curve* pCurveA,
  const ON_Curve* pCurveB,
  double tolerance,
  double overlap_tolerance,
  ON_SimpleArray<int>* pInvalid, 
  ON_TextLog* pTextLog
)
{
  ON_SimpleArray<ON_X_EVENT>* rc = nullptr;
  if (pCurveA && pCurveB)
  {
    rc = new ON_SimpleArray<ON_X_EVENT>();
    pCurveA->IntersectCurve(pCurveB, *rc, tolerance, overlap_tolerance);
    if (rc->Count() > 0 && pInvalid)
    {
      for (int i = 0; i < rc->Count(); i++)
      {
        if (!(*rc)[i].IsValid(pTextLog, tolerance, overlap_tolerance, pCurveA, nullptr, pCurveB, nullptr, nullptr, nullptr, nullptr))
          pInvalid->Append(i);
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_SimpleArray<ON_X_EVENT>* ON_Intersect_CurveSurface(
  const ON_Curve* pCurve,
  const ON_Surface* pSurface,
  double tolerance,
  double overlap_tolerance,
  ON_SimpleArray<int>* pInvalid,
  ON_TextLog* pTextLog
)
{
  ON_SimpleArray<ON_X_EVENT>* rc = nullptr;
  if (pCurve && pSurface)
  {
    rc = new ON_SimpleArray<ON_X_EVENT>();
    pCurve->IntersectSurface(pSurface, *rc, tolerance, overlap_tolerance);
    if (rc->Count() > 0 && pInvalid)
    {
      for (int i = 0; i < rc->Count(); i++)
      {
        if (!(*rc)[i].IsValid(pTextLog, tolerance, overlap_tolerance, pCurve, nullptr, nullptr, nullptr, pSurface, nullptr, nullptr))
          pInvalid->Append(i);
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_SimpleArray<ON_X_EVENT>* ON_Intersect_CurveSurface2(
  const ON_Curve* pCurve,
  const ON_Surface* pSurface,
  double domain0,
  double domain1,
  double tolerance,
  double overlap_tolerance,
  ON_SimpleArray<int>* pInvalid,
  ON_TextLog* pTextLog
)
{
  ON_SimpleArray<ON_X_EVENT>* rc = nullptr;
  if (pCurve && pSurface)
  {
    ON_Interval domain(domain0, domain1);
    rc = new ON_SimpleArray<ON_X_EVENT>();
    pCurve->IntersectSurface(pSurface, *rc, tolerance, overlap_tolerance, &domain);
    if (rc->Count() > 0 && pInvalid)
    {
      for (int i = 0; i < rc->Count(); i++)
      {
        if (!(*rc)[i].IsValid(pTextLog, tolerance, overlap_tolerance, pCurve, &domain, nullptr, nullptr, pSurface, nullptr, nullptr))
          pInvalid->Append(i);
      }
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Intersect_IntersectArrayDelete(ON_SimpleArray<ON_X_EVENT>* pArray)
{
  if( pArray )
    delete pArray;
}

RH_C_FUNCTION int ON_Intersect_IntersectArrayCount(const ON_SimpleArray<ON_X_EVENT>* pArray)
{
  int rc = 0;
  if( pArray )
    rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION bool ON_Intersect_CurveIntersectData(const ON_SimpleArray<ON_X_EVENT>* pArray,
                                                   int index, int* type,
                                                   ON_3dPoint* startA, ON_3dPoint* endA,
                                                   ON_3dPoint* startB, ON_3dPoint* endB,
                                                   double* ua0, double* ua1,
                                                   double* ub0, double* ub1,
                                                   double* vb0, double* vb1)
{
  if(pArray && pArray->Count() > index && type && startA && endA 
    && startB && endB && ua0 && ua1 && ub0 && ub1 && vb0 && vb1)
  {
    const ON_X_EVENT* x = &(*pArray)[index];
    if (x)
    {
      *type = (int)(x->m_type);
      *startA = x->m_A[0];
      *endA = x->m_A[1];
      *startB = x->m_B[0];
      *endB = x->m_B[1];
      *ua0 = x->m_a[0];
      *ua1 = x->m_a[1];
      *ub0 = x->m_b[0];
      *ub1 = x->m_b[1];
      *vb0 = x->m_b[2];
      *vb1 = x->m_b[3];
      return true;
    }
  }
  return false;
}

RH_C_FUNCTION int ON_RayShooter_OneSurface(ON_3DPOINT_STRUCT _point, ON_3DVECTOR_STRUCT _direction, const ON_Surface* pConstSurface, ON_SimpleArray<ON_3dPoint>* pPoints, int maxReflections)
{
  int rc = 0;
  ON_3dPoint point(_point.val[0], _point.val[1], _point.val[2]);
  ON_3dVector direction(_direction.val[0], _direction.val[1], _direction.val[2]);
  if( pConstSurface && pPoints && maxReflections>0 && point.IsValid() && direction.Unitize() )
  {
    ON_RayShooter shooter;
    ON_X_EVENT hit;
    ON_3dPoint Q = point;
    ON_3dVector R = direction;
    ON_3dVector V[3];
    for( int i=0; i<maxReflections; i++ )
    {
      memset(&hit,0,sizeof(hit));
      ON_3dVector T = R;
      if( !T.Unitize() )
        break;
      if( !shooter.Shoot(Q,T,pConstSurface,hit) )
        break;
      Q = hit.m_A[0];
      pPoints->Append(Q);
      if( !hit.m_snodeB[0] )
        break;
      hit.m_snodeB[0]->Evaluate(hit.m_b[0], hit.m_b[1], 1, 3, &V[0].x);
      ON_3dVector N = ON_CrossProduct(V[1],V[2]);
      if ( !N.Unitize() )
        break;
      double d = N*T;
      R = T + (-2.0*d)*N; // R = reflection direction
    }
    rc = pPoints->Count();
  }
  return rc;
}

RH_C_FUNCTION int ON_RayShooter_ShootRay(
  const ON_SimpleArray<const ON_Geometry*>* pConstGeometry,
  ON_3DPOINT_STRUCT _point,
  ON_3DVECTOR_STRUCT _direction,
  int max_reflections,
  ON_SimpleArray<ON_3dPoint>* pPoints,
  ON_SimpleArray<int>* pBreps,
  ON_SimpleArray<int>* pComponents
  )
{
  int rc = 0;

  ON_3dPoint P(_point.val[0], _point.val[1], _point.val[2]);
  ON_3dVector D(_direction.val[0], _direction.val[1], _direction.val[2]);

  if (nullptr == pConstGeometry ||
      nullptr == pPoints        ||
      max_reflections <= 0      ||
      !P.IsValid()              || 
      !D.Unitize()
    )
    return rc;

  bool bReturnHits = (nullptr != pBreps && nullptr != pComponents);

  ON_SimpleArray<const ON_SurfaceTreeNode*> stree_list;
  std::map<unsigned int, ON_2dex> stree_map;

  for (int i = 0; i < pConstGeometry->Count(); i++)
  {
    const ON_Geometry* geometry = (*pConstGeometry)[i];
    if (geometry)
    {
      const ON_Surface* surface = ON_Surface::Cast(geometry);
      if (surface)
      {
        const ON_SurfaceTree* stree = surface->SurfaceTree();
        if (stree)
        {
          stree_list.Append(stree);
          if (bReturnHits)
          {
            ON_2dex dex(i, ON_UNSET_INT_INDEX);
            stree_map[stree->m_treesn] = dex;
          }
        }
        continue;
      }
      const ON_Brep* brep = ON_Brep::Cast(geometry);
      if (brep)
      {
        for (int fi = 0; fi < brep->m_F.Count(); fi++)
        {
          ON_BrepFace* face = brep->Face(fi);
          if (face)
          {
            const ON_SurfaceTree* stree = face->SurfaceTree();
            if (stree)
            {
              stree_list.Append(stree);
              if (bReturnHits)
              {
                ON_2dex dex(i, fi);
                stree_map[stree->m_treesn] = dex;
              }
            }
          }
        }
      }
    }
  }

  if (0 == stree_list.Count())
    return rc;

  ON_RayShooter ray;
  ON_X_EVENT hit;
  ON_3dPoint Q = P;
  ON_3dVector R = D;

  for (int i = 0; i <= max_reflections; i++)
  {
    ON_3dVector T = R;
    if (!T.Unitize())
      break;

    // Shoot the ray
    memset(&hit, 0, sizeof(hit));
    if (!ray.Shoot(Q, T, stree_list.Count(), stree_list.Array(), hit))
      break;

    // Get the "hit" surface tree node
    const ON_SurfaceTreeNode* stn = hit.m_snodeB[0];
    if (nullptr == stn)
      stn = hit.m_snodeB[1];
    if (nullptr == stn)
      break;

    // Look up the surface or face based on the surface tree node's serial number
    if (bReturnHits)
    {
      ON_2dex dex = ON_2dex::Unset;
      std::map<unsigned int, ON_2dex>::const_iterator pos = stree_map.find(stn->m_treesn);
      if (pos != stree_map.end())
        dex = stree_map[stn->m_treesn];
      pBreps->Append(dex.i);
      pComponents->Append(dex.j);
    }

    Q = hit.m_B[0]; // surface point

    // Add next set of out values
    pPoints->Append(Q);

    // Set R = reflection direction
    ON_3dVector N = hit.m_B[1]; // surface normal
    double d = -2.0 * (N.x * T.x + N.y * T.y + N.z * T.z);
    R.x = T.x + d * N.x;
    R.y = T.y + d * N.y;
    R.z = T.z + d * N.z;

    // Part of the fix for RR 22717.  See opennurbs_plus_xray.cpp
    // for the rest of the fix.
    ray.m_min_travel_distance = hit.m_A[0].DistanceTo(hit.m_B[0]);
    if (ray.m_min_travel_distance < 1.0e-8)
      ray.m_min_travel_distance = 1.0e-8;
  }

  int point_count = pPoints->Count();
  if (bReturnHits)
  {
    rc = (point_count == pBreps->Count() && point_count == pComponents->Count()) 
      ? point_count 
      : 0;
  }
  else
  {
    rc = point_count;
  }

  return rc;
}


RH_C_FUNCTION ON_SimpleArray<ON_Polyline*>* ON_Intersect_MeshMesh1(const ON_Mesh* pConstMeshA, const ON_Mesh* pConstMeshB, int* polyline_count, double tolerance)
{
  ON_SimpleArray<ON_Polyline*>* rc = nullptr;
  if( polyline_count ) *polyline_count = 0;
  if( pConstMeshA && pConstMeshB && polyline_count )
  {
    ON_ClassArray<ON_MMX_Polyline> plines;
    ON_ClassArray<ON_MMX_Polyline> overlapplines;
    if(::ON_MeshMeshIntersect(pConstMeshA, pConstMeshB, plines, overlapplines, tolerance, tolerance))
    {
      rc = new ON_SimpleArray<ON_Polyline*>();
      for( int i=0; i<plines.Count(); i++ )
      {
        ON_Polyline* pl = new ON_Polyline();
        const ON_MMX_Polyline& mmxpoly = plines[i];
        int c = mmxpoly.Count();
        for( int j=0; j<c; j++ )
          pl->Append(mmxpoly[j].m_A.m_P);
        pl->Clean(ON_ZERO_TOLERANCE);
        if( !pl->IsValid() )
        {
          delete pl;
          continue;
        }
        rc->Append(pl);
      }
      for( int i=0; i<overlapplines.Count(); i++ )
      {
        ON_Polyline* pl = new ON_Polyline();
        const ON_MMX_Polyline& mmxpoly = overlapplines[i];
        int c = mmxpoly.Count();
        for( int j=0; i<c; i++ )
          pl->Append(mmxpoly[j].m_A.m_P);
        pl->Clean(ON_ZERO_TOLERANCE);
        if( !pl->IsValid() )
        {
          delete pl;
          continue;
        }
        rc->Append(pl);
      }
      *polyline_count = rc->Count();
    }
  }
  return rc;
}

// ON_BoundingBox::MaximumDistance has a copy/paste bug in it in V4. Using local
// version of this function with the fix so things continue to work under V4 grasshopper
static double RhCmnMaxDistance_Helper(const ON_BoundingBox& bbox, const ON_3dPoint& P)
{
  ON_3dVector V;
  V.x = ( (P.x < 0.5*(bbox.m_min.x+bbox.m_max.x)) ? bbox.m_max.x : bbox.m_min.x) - P.x;
  V.y = ( (P.y < 0.5*(bbox.m_min.y+bbox.m_max.y)) ? bbox.m_max.y : bbox.m_min.y) - P.y;
  V.z = ( (P.z < 0.5*(bbox.m_min.z+bbox.m_max.z)) ? bbox.m_max.z : bbox.m_min.z) - P.z;
  return V.Length();
}


// [Giulio, 2020 12 24] Unused code. Can be deleted when we see fit
RH_C_FUNCTION double ON_Intersect_MeshRay_OLD(const ON_Mesh* pMesh, ON_3dRay* ray, ON_SimpleArray<int>* face_indices)
{
  double rc = -1.0;
  // it is ok if face_indices is null
  if( pMesh && ray )
  {
    const ON_MeshTree* mt = pMesh->MeshTree(true);

    ON_3dVector rayVec = ray->m_V;
    if( mt && rayVec.Unitize() )
    {
      // increase the range by a factor of 2 so we are confident the
      // line passes entirely through the mesh
      double rayRange = RhCmnMaxDistance_Helper( mt->m_bbox, ray->m_P ) * 2.0;
      ON_Line line(ray->m_P, ray->m_P + rayRange * rayVec );

      ON_SimpleArray<ON_CMX_EVENT> hits;
      mt->IntersectLine( line, hits );
      int hitCount = hits.Count();
      if( hitCount > 0 )
      {
        ON_SimpleArray<double> tvals;
        ON_SimpleArray<int> indices;
        // tMin should be between 0 and 1 for the line
        double tMin = 100.0;
        for( int i=0; i<hitCount; i++ )
        {
          const ON_CMX_EVENT& e = hits[i];
          if( e.m_C[0].m_t <= tMin )
          {
            tMin = e.m_C[0].m_t;
            if( face_indices )
            {
              tvals.Append(tMin);
              indices.Append(e.m_M[0].m_face_index);
            }
          }
          if( e.m_type == ON_CMX_EVENT::cmx_overlap && e.m_C[1].m_t <= tMin )
          {
            tMin = e.m_C[1].m_t;
            if( face_indices )
            {
              tvals.Append(tMin);
              indices.Append( e.m_M[1].m_face_index);
            }
          }
        }
        if( tMin >=0 && tMin <= 1.0 )
        {
          if( face_indices )
          {
            for( int i=0; i<tvals.Count(); i++ )
            {
              if( tvals[i]==tMin )
                face_indices->Append(indices[i]);
            }
          }

          double lineLength = line.Length();
          double rayLength = ray->m_V.Length();
          if( rayLength > ON_SQRT_EPSILON )
          {
            rc = tMin * lineLength / rayLength;
          }
        }
      }
    }
  }
  return rc;
}


RH_C_FUNCTION double ON_Intersect_MeshRay2(const ON_Mesh* pMesh, const ON_3dRay* ray, bool removeHidden, double tolerance, int* count_out, void** ts_out, void** faceIds_out)
{
  double rc = ON_DBL_NINF;
  if (count_out) *count_out = 0;
  // it is ok if face_indices is null
  if (pMesh && ray)
  {
    ON_SimpleArray<double>* ts_arr = (ts_out && *ts_out) ? new ON_SimpleArray<double>() : nullptr;
    ON_SimpleArray<int>* faceIds_out_arr = (faceIds_out && *faceIds_out) ? new ON_SimpleArray<int>() : nullptr;

    rc = MX::PublicIntersectionOps::MeshRayIntersect(*pMesh, *ray, faceIds_out_arr, ts_arr, removeHidden, tolerance);

    if (ts_out) *ts_out = ts_arr;
    if (faceIds_out) *faceIds_out = faceIds_out_arr;
    if (count_out && (ts_arr || faceIds_out_arr)) *count_out = ts_arr ? ts_arr->Count() : faceIds_out_arr->Count();
    if (!ts_out) delete ts_arr;
    if (!faceIds_out) delete faceIds_out_arr;
  }

  return rc;
}


RH_C_FUNCTION bool ON_Mesh_IsPointInside(const ON_Mesh* pConstMesh, ON_3DPOINT_STRUCT point, double tolerance, bool strictlyin)
{
  bool rc = false;
#if defined(RHINO3DMIO_BUILD)
  // do nothing, not supported
#else
  // 27 March 2012 - S. Baer
  // The low-level ON_Mesh::IsPointInside has not been completed and always returns false.
  // Calling an intersector for now and counting the number of crossings. Odd == inside.
  // I realize this isn't foolproof since points on faces may cause problems, but it could
  // hold us over until Dale completes the function (which looks nearly complete in TL_MeshTools.cpp)

  // These input parameters are not currently used, but should be once a proper OpenNURBS
  // implementation is done
  tolerance = 0;

  if (
    nullptr != pConstMesh && 
    /*pConstMesh->IsValid() && [Giulio] The validy test is not cached and should only be done outside the call, and only once */
    pConstMesh->IsClosed() && 
    pConstMesh->IsManifold()
    )
  {
    ON_3dPoint _point(point.val);
    if (_point.IsValid())
    {
      ON_BoundingBox bbox = pConstMesh->BoundingBox();
      
      if (bbox.IsPointIn(_point, strictlyin)) // eliminate the obvious
      {
        ON_3dRay ray { _point,
          ON_3dVector(_point.x < (bbox.m_max.x+bbox.m_min.x)*0.5 ? -1 : 1, 0, 0) // a random direction toward a likely "outside"
        }; 

        ON_SimpleArray<double> ts;
        if (ON_IntersectMeshRay(pConstMesh, ray, 0.0, ts) && ts.Count() > 0) //[Giulio, Feb 2021] band-aided by using new intersector
        {
          ON_SimpleArray<ON_3dPoint> hit_points;
          for (int i = 0; i < ts.Count(); i++)
            hit_points.Append(ray.m_P + (ray.m_V * ts[i]));

          // 6-Apr-2020 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-57774
          // Its possible for a point to be inside a closed mesh. But if the random line
          // happens to intersect the mesh at an edge, you will get two intersection events.
          // So sort and cull the events.
          if (hit_points.Count() > 1)
          {
            // sort and cull
            hit_points.QuickSort(&ON_CompareIncreasing<ON_3dPoint>);
            ON_3dPoint hit = *hit_points.Last();
            for (int i = hit_points.Count() - 2; i >= 0; i--)
            {
              if (hit_points[i].DistanceTo(hit) < ON_ZERO_TOLERANCE)
                hit_points.Remove(i);
              else
                hit = hit_points[i];
            }
          }

          rc = (hit_points.Count() % 2 == 1) ? true : false;
          if (rc && strictlyin)
            rc = (hit_points[0].DistanceTo(_point) > ON_ZERO_TOLERANCE);
        }
      }
    }
  }
#endif
  return rc;
}



RH_C_FUNCTION int ON_Intersect_MeshPolyline1(const ON_Mesh* pConstMesh, const ON_PolylineCurve* pConstPolyCurve, bool sort,
  void** points_out, void** faceIds_out)
{
  int rc = 0;
  if (pConstMesh && pConstPolyCurve)
  {
    ON_SimpleArray<ON_3dPoint>* points_out_arr = new ON_SimpleArray<ON_3dPoint>();
    ON_SimpleArray<int>* faceIds_out_arr = new ON_SimpleArray<int>();
    ON_SimpleArray<double> ts_arr;

    const ON_Polyline* poly_ptr = &pConstPolyCurve->m_pline;
    if (poly_ptr != nullptr)
    {
      MX::PublicIntersectionOps::MeshPolylineIntersect(*pConstMesh, *poly_ptr, faceIds_out_arr, &ts_arr, sort, 0.0);

      for (int i = 0; i < ts_arr.Count(); i++)
      {
        points_out_arr->Append(poly_ptr->PointAt(ts_arr[i]));
      }

      *points_out = (void**)points_out_arr;
      *faceIds_out = (void**)faceIds_out_arr;
      rc = ts_arr.Count();
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_Intersect_MeshLine(const ON_Mesh* pConstMesh, ON_3DPOINT_STRUCT from, ON_3DPOINT_STRUCT to, bool sort,
  void** points_out, bool faces, void** faceIds_out)
{
  int rc = 0;
  if( pConstMesh )
  {
    ON_Line line { ON_3dPoint(from.val), ON_3dPoint(to.val) };
    
    ON_SimpleArray<ON_3dPoint>* points_out_arr = new ON_SimpleArray<ON_3dPoint>();
    ON_SimpleArray<int>* faceIds_out_arr = faces ? new ON_SimpleArray<int>() : nullptr;
    ON_SimpleArray<double> ts_arr;

    if (MX::PublicIntersectionOps::MeshLineIntersect(*pConstMesh, line, faceIds_out_arr, &ts_arr, false, sort, 0.0) >= 0)
    {
      for (int i = 0; i < ts_arr.Count(); i++)
        points_out_arr->Append(line.PointAt(ts_arr[i]));

      *points_out = (void**)points_out_arr;
      *faceIds_out = (void**)faceIds_out_arr;
      rc = ts_arr.Count();
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Intersect_MeshPolyline_FillDelete(int count, void* points_out_now_in, void* faceIds_out_now_in, void* ts_out_now_in,
  /*ARRAY*/ON_3dPoint* points_out, /*ARRAY*/int* faceIds_out, /*ARRAY*/double* ts_out)
{
  if (points_out_now_in && points_out)
  {
    auto smplarr = ((ON_SimpleArray<ON_3dPoint>*)points_out_now_in);
    if (smplarr->Count() == count)
    {
      auto arr = smplarr->Array();
      memcpy(points_out, arr, sizeof(ON_3dPoint) * count);
      delete smplarr;
    }
  }
  if (faceIds_out_now_in && faceIds_out)
  {
    auto smplarr = ((ON_SimpleArray<int>*)faceIds_out_now_in);
    if (smplarr->Count() == count)
    {
      auto arr = smplarr->Array();
      memcpy(faceIds_out, arr, sizeof(int) * count);
      delete smplarr;
    }
  }
  if (ts_out_now_in && ts_out)
  {
    auto smplarr = ((ON_SimpleArray<double>*)ts_out_now_in);
    if (smplarr->Count() == count)
    {
      auto arr = smplarr->Array();
      memcpy(ts_out, arr, sizeof(double) * count);
      delete smplarr;
    }
  }
}

RH_C_FUNCTION bool RHC_Polyline_RemoveNearlyEqualSubsequentPoints(/*ARRAY*/ON_3dPoint* points, int* count, double tolerance)
{
  ON_Polyline pl;
  pl.SetArray(points, *count, *count);

  bool rc = MX::PublicIntersectionOps::RemoveNearlyEqualSubsequentPointsInPolyline(pl, tolerance);

  *count = pl.Count();
  pl.KeepArray();

  return rc;
}

#endif

