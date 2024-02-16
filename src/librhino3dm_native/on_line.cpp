#include "stdafx.h"
#if !defined(RHINO3DM_BUILD)
#include "../../../rhino4/mesh_boolean_v7.h" // triangle coordinates more consistent with ray method
#endif

RH_C_FUNCTION double ON_Line_DistanceToPoint( const ON_Line* pLine, ON_3DPOINT_STRUCT point, bool minDist)
{
  double rc = -1;
  if( pLine )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)&point;
    if( minDist )
      rc = pLine->MinimumDistanceTo(*_point);
    else
      rc = pLine->MaximumDistanceTo(*_point);
  }
  return rc;
}

RH_C_FUNCTION double ON_Line_DistanceToLine( const ON_Line* pLine, const ON_Line* pOtherLine, bool minDist)
{
  double rc = -1;
  if( pLine && pOtherLine)
  {
    if( minDist )
      rc = pLine->MinimumDistanceTo(*pOtherLine);
    else
      rc = pLine->MaximumDistanceTo(*pOtherLine);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Line_Transform( ON_Line* pLine, const ON_Xform* xform )
{
  bool rc = false;
  if( pLine && xform )
  {
    rc = pLine->Transform(*xform);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Line_InPlane( const ON_Line* pConstLine, ON_PLANE_STRUCT* plane )
{
  bool rc = false;
  if( pConstLine && plane )
  {
    ON_Plane temp;
    rc = pConstLine->InPlane(temp);
    CopyToPlaneStruct(*plane, temp);
  }
  return rc;
}


#if !defined(RHINO3DM_BUILD) //not available in standalone opennurbs
//  7-Feb-2013 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-16086
// 27-Nov-2023 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-78612
RH_C_FUNCTION bool RHC_RhGetTanPerpPoint(const ON_Curve* pConstCurve0, const ON_Curve* pConstCurve1, double* t0, double* t1, bool perpendicular0, bool perpendicular1, ON_Line* pLine)
{
  bool rc = false;
  if (pConstCurve0 && pConstCurve1 && pLine && t0 && t1)
  {
    double abs_tol = 0.001;
    CRhinoDoc* doc = RhinoApp().ActiveDoc();
    if (doc)
      abs_tol = doc->AbsoluteTolerance();

    bool on_rc = RhinoGetTanPerpPoint(*pConstCurve0, *pConstCurve1, perpendicular0, perpendicular1, abs_tol, *t0, *t1);
    if (on_rc)
    {
      pLine->from = pConstCurve0->PointAt(*t0);
      pLine->to = pConstCurve1->PointAt(*t1);
      rc = pLine->IsValid();
    }
  }
  return rc;
}
#endif

// 2020-Sep-22, Giulio http://mcneel.myjetbrains.com/youtrack/issue/RH-59727
RH_C_FUNCTION bool ON_Triangle_BarycentricCoordsAt(ON_TRIANGLE_STRUCT triangle, const ON_3DPOINT_STRUCT test,
  ON_2dPoint* result, double* height)
{
  if (!result) return false;

  ON_Triangle tri{ triangle.val };
  ON_3dPoint testpt{ test.val };
#if defined(RHINO3DM_BUILD) 
  bool rc = tri.GetBarycentricCoordinates(testpt, false, &result->x, &result->y);
  if (rc && height)
  {
    ON_3dVector n = tri.Normal();
    if (n.IsZero()) n = ON_3dVector::XAxis;
    double len = ON_DotProduct(n, testpt - tri.PointAt(result->x, result->y));
    *height = len / n.Length();
  }
  return rc;
#else
  //not available in standalone opennurbs
  ON_2dPoint temp = MX_ClosestTriangleEdgeCoords(tri, testpt, height);
  result->x = temp.x;
  result->y = temp.y;
#endif

  return true;
}

