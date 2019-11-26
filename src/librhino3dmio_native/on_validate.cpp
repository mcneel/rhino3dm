#include "stdafx.h"

#if !defined(RHINO3DMIO_BUILD)
#include "../../../opennurbs/opennurbs_plus_validate.h"

RH_C_FUNCTION bool ONC_ValidateSurfaceCVSpacing(const ON_NurbsSurface* pConstNurbsSurface, double closeTol, double stackTol,
  ON_SimpleArray<ON_2dex>* closeIds, ON_SimpleArray<ON_2dex>* stackedIds)
{
  bool rc = true;
  if (pConstNurbsSurface && closeIds && stackedIds)
  {
    rc = ON_ValidateSurfaceCVSpacing(*pConstNurbsSurface, closeTol, stackTol, *closeIds, *stackedIds);
  }
  return rc;
}

RH_C_FUNCTION bool ONC_ValidateCurveCVSpacing(const ON_NurbsCurve* pConstNurbsCurve, double closeTol, double stackTol,
  ON_SimpleArray<int>* closeIds, ON_SimpleArray<int>* stackedIds)
{
  bool rc = true;
  if (pConstNurbsCurve && closeIds && stackedIds)
  {
    double len = 0;
    rc = ON_ValidateCurveCVSpacing(*pConstNurbsCurve, closeTol, stackTol, *closeIds, *stackedIds, len);
  }
  return rc;
}

#endif
