#include "stdafx.h"

RH_C_FUNCTION ON_NurbsSurface* ON_Sphere_GetNurbsForm(ON_Sphere* sphere)
{
  ON_NurbsSurface* rc = nullptr;
  if( sphere )
  {
    sphere->plane.UpdateEquation();
    // Always use static constructor
    //ON_NurbsSurface* ns = new ON_NurbsSurface();
    ON_NurbsSurface* ns = ON_NurbsSurface::New();
    int success = sphere->GetNurbForm(*ns);
    if( 0==success )
    {
      delete ns;
    }
    else
      rc = ns;
  }
  return rc;
}

RH_C_FUNCTION ON_RevSurface* ON_Sphere_RevSurfaceForm(ON_Sphere* sphere)
{
  ON_RevSurface* rc = nullptr;
  if( sphere )
  {
    sphere->plane.UpdateEquation();
    rc = sphere->RevSurfaceForm(false);
  }
  return rc;
}

/// THESE SHOULD MOVE ONCE I'VE SET UP SEPARATE CPP FILES

RH_C_FUNCTION ON_NurbsSurface* ON_Cone_GetNurbForm(ON_Cone* cone)
{
  ON_NurbsSurface* rc = nullptr;
  if( cone )
  {
    cone->plane.UpdateEquation();
    rc = ON_NurbsSurface::New();
    if( !cone->GetNurbForm(*rc) )
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_RevSurface* ON_Cone_RevSurfaceForm(ON_Cone* cone)
{
  ON_RevSurface* rc = nullptr;
  if( cone )
  {
    cone->plane.UpdateEquation();
    rc = cone->RevSurfaceForm();
  }
  return rc;
}

RH_C_FUNCTION ON_NurbsSurface* ON_Cylinder_GetNurbForm(ON_Cylinder* cylinder)
{
  ON_NurbsSurface* rc = nullptr;
  if( cylinder )
  {
    cylinder->circle.plane.UpdateEquation();
    rc = ON_NurbsSurface::New();
    if( !cylinder->GetNurbForm(*rc) )
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_RevSurface* ON_Cylinder_RevSurfaceForm(ON_Cylinder* cylinder)
{
  ON_RevSurface* rc = nullptr;
  if( cylinder )
  {
    cylinder->circle.plane.UpdateEquation();
    rc = cylinder->RevSurfaceForm();
  }
  return rc;
}


RH_C_FUNCTION ON_NurbsSurface* ON_Torus_GetNurbForm(ON_Torus* torus)
{
  ON_NurbsSurface* rc = nullptr;
  if( torus )
  {
    torus->plane.UpdateEquation();
    rc = ON_NurbsSurface::New();
    if( !torus->GetNurbForm(*rc) )
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_RevSurface* ON_Torus_RevSurfaceForm(ON_Torus* torus)
{
  ON_RevSurface* rc = nullptr;
  if( torus )
  {
    torus->plane.UpdateEquation();
    rc = torus->RevSurfaceForm();
  }
  return rc;
}
