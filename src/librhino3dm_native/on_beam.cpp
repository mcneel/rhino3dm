#include "stdafx.h"

RH_C_FUNCTION ON_Extrusion* ON_Extrusion_New(const ON_Extrusion* pOther)
{
  if( pOther )
    return new ON_Extrusion(*pOther);
  return new ON_Extrusion();
}

RH_C_FUNCTION ON_Brep* ON_Extrusion_BrepForm(const ON_Extrusion* pConstExtrusion, bool splitKinkyFaces)
{
  ON_Brep* rc = nullptr;
  if( pConstExtrusion )
  {
    rc = pConstExtrusion->BrepForm(nullptr, splitKinkyFaces);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Extrusion_SetPathAndUp( ON_Extrusion* pExtrusion, ON_3DPOINT_STRUCT a, ON_3DPOINT_STRUCT b, ON_3DVECTOR_STRUCT up )
{
  bool rc = false;
  if( pExtrusion )
  {
    const ON_3dPoint* _a = (const ON_3dPoint*)(&a);
    const ON_3dPoint* _b = (const ON_3dPoint*)(&b);
    const ON_3dVector* _up = (const ON_3dVector*)(&up);
    rc = pExtrusion->SetPathAndUp(*_a, *_b, *_up);
  }
  return rc;
}

RH_C_FUNCTION void ON_Extrusion_GetPoint( const ON_Extrusion* pConstExtrusion, bool pathStart, ON_3dPoint* pt )
{
  if( pConstExtrusion && pt )
  {
    *pt = pathStart ? pConstExtrusion->PathStart() : pConstExtrusion->PathEnd();
  }
}

RH_C_FUNCTION void ON_Extrusion_GetPathTangent(const ON_Extrusion* pConstExtrusion, ON_3dVector* vec)
{
  if( pConstExtrusion && vec )
  {
    *vec = pConstExtrusion->PathTangent();
  }
}

RH_C_FUNCTION void ON_Extrusion_GetMiterPlaneNormal(const ON_Extrusion* pConstExtrusion, int end, ON_3dVector* normal)
{
  if( pConstExtrusion && normal )
  {
    pConstExtrusion->GetMiterPlaneNormal(end, *normal);
  }
}

RH_C_FUNCTION void ON_Extrusion_SetMiterPlaneNormal(ON_Extrusion* pExtrusion, int end, ON_3DVECTOR_STRUCT normal)
{
  if( pExtrusion )
  {
    const ON_3dVector* _normal = (const ON_3dVector*)(&normal);
    pExtrusion->SetMiterPlaneNormal(*_normal, end);
  }
}

RH_C_FUNCTION int ON_Extrusion_IsMitered(const ON_Extrusion* pConstExtrusion)
{
  int rc = -1;
  if( pConstExtrusion )
    rc = pConstExtrusion->IsMitered();
  return rc;
}

RH_C_FUNCTION bool ON_Extrusion_IsSolid(const ON_Extrusion* pConstExtrusion)
{
  bool rc = false;
  if( pConstExtrusion )
    rc = pConstExtrusion->IsSolid();
  return rc;
}

RH_C_FUNCTION int ON_Extrusion_IsCapped(const ON_Extrusion* pConstExtrusion)
{
  int rc = -1;
  if( pConstExtrusion )
    rc = pConstExtrusion->IsCapped();
  return rc;
}

RH_C_FUNCTION int ON_Extrusion_CapCount(const ON_Extrusion* pConstExtrusion)
{
  int rc = 0;
  if( pConstExtrusion )
    rc = pConstExtrusion->CapCount();
  return rc;
}

RH_C_FUNCTION ON_Extrusion* ON_Extrusion_CreateCylinder(const ON_Cylinder* cylinder, bool capBottom, bool capTop)
{
  ON_Extrusion* rc = nullptr;
  if( cylinder )
  {
    (const_cast<ON_Cylinder*>(cylinder))->circle.plane.UpdateEquation();
    rc = ON_Extrusion::Cylinder(*cylinder, capBottom, capTop);
  }
  return rc;
}

RH_C_FUNCTION ON_Extrusion* ON_Extrusion_CreatePipe(const ON_Cylinder* cylinder, double otherRadius, bool capBottom, bool capTop)
{
  ON_Extrusion* rc = nullptr;
  if( cylinder )
  {
    (const_cast<ON_Cylinder*>(cylinder))->circle.plane.UpdateEquation();
    rc = ON_Extrusion::Pipe(*cylinder, otherRadius, capBottom, capTop);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Extrusion_GetProfileTransformation(const ON_Extrusion* pConstExtrusion, double s, ON_Xform* xform)
{
  bool rc = false;
  if( pConstExtrusion && xform )
  {
    rc = pConstExtrusion->GetProfileTransformation(s, *xform);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Extrusion_GetPlane(const ON_Extrusion* pConstExtrusion, bool profilePlane, double s, ON_PLANE_STRUCT* plane)
{
  bool rc = false;
  if( pConstExtrusion && plane )
  {
    ON_Plane _plane;
    if( profilePlane )
      rc = pConstExtrusion->GetProfilePlane(s, _plane);
    else
      rc = pConstExtrusion->GetPathPlane(s, _plane);

    if( rc )
      CopyToPlaneStruct(*plane, _plane);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Extrusion_SetOuterProfile(ON_Extrusion* pExtrusion, const ON_Curve* pCurve, bool cap)
{
  bool rc = false;
  if( pExtrusion && pCurve )
  {
    ON_Curve* outer_profile = pCurve->DuplicateCurve();
    if( outer_profile )
    {
      rc = pExtrusion->SetOuterProfile(outer_profile, cap);
      if( !rc )
        delete outer_profile;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Extrusion_AddInnerProfile(ON_Extrusion* pExtrusion, const ON_Curve* pCurve)
{
  bool rc = false;
  if( pExtrusion && pCurve )
  {
    ON_Curve* profile = pCurve->DuplicateCurve();
    if( profile )
    {
      rc = pExtrusion->AddInnerProfile(profile);
      if( !rc )
        delete profile;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_Extrusion_ProfileCount(const ON_Extrusion* pConstExtrusion)
{
  int rc = 0;
  if( pConstExtrusion )
    rc = pConstExtrusion->ProfileCount();
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_Extrusion_Profile3d(const ON_Extrusion* pConstExtrusion, int profileIndex, double s)
{
  ON_Curve* rc = nullptr;
  if( pConstExtrusion )
  {
    rc = pConstExtrusion->Profile3d(profileIndex, s);
  }
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_Extrusion_Profile3d2(const ON_Extrusion* pConstExtrusion, ON_2INTS componentIndex)
{
  ON_Curve* rc = nullptr;
  if( pConstExtrusion )
  {
    const ON_COMPONENT_INDEX* ci = (const ON_COMPONENT_INDEX*)&componentIndex;
    rc = pConstExtrusion->Profile3d(*ci);
  }
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_Extrusion_WallEdge(const ON_Extrusion* pConstExtrusion, ON_2INTS componentIndex)
{
  ON_Curve* rc = nullptr;
  if( pConstExtrusion )
  {
    const ON_COMPONENT_INDEX* ci = (const ON_COMPONENT_INDEX*)&componentIndex;
    rc = pConstExtrusion->WallEdge(*ci);
  }
  return rc;
}

RH_C_FUNCTION ON_Surface* ON_Extrusion_WallSurface(const ON_Extrusion* pConstExtrusion, ON_2INTS componentIndex)
{
  ON_Surface* rc = nullptr;
  if( pConstExtrusion )
  {
    const ON_COMPONENT_INDEX* ci = (const ON_COMPONENT_INDEX*)&componentIndex;
    rc = pConstExtrusion->WallSurface(*ci);
  }
  return rc;
}

RH_C_FUNCTION ON_LineCurve* ON_Extrusion_PathLineCurve(const ON_Extrusion* pConstExtrusion)
{
  ON_LineCurve* rc = nullptr;
  if( pConstExtrusion )
  {
    rc = pConstExtrusion->PathLineCurve(nullptr);
  }
  return rc;
}

RH_C_FUNCTION int ON_Extrusion_ProfileIndex(const ON_Extrusion* pConstExtrusion, double profile_parameter)
{
  int rc = -1;
  if( pConstExtrusion )
  {
    rc = pConstExtrusion->ProfileIndex(profile_parameter);
  }
  return rc;
}

RH_C_FUNCTION ON_Extrusion* ON_Extrusion_CreateFrom3dCurve(const ON_Curve* pConstCurve, double height, bool cap)
{
  ON_Extrusion* rc = nullptr;
  if( pConstCurve )
  {
    rc = ON_Extrusion::CreateFrom3dCurve(*pConstCurve, nullptr, height, cap);
  }
  return rc;
}

RH_C_FUNCTION ON_Extrusion* ON_Extrusion_CreateFrom3dCurve2(const ON_Curve* pConstCurve, const ON_PLANE_STRUCT* pPlane, double height, bool cap)
{
  ON_Extrusion* rc = nullptr;
  if (pConstCurve && pPlane)
  {
    ON_Plane plane = FromPlaneStruct(*pPlane);
    rc = ON_Extrusion::CreateFrom3dCurve(*pConstCurve, &plane, height, cap);
  }
  return rc;
}


RH_C_FUNCTION const ON_Mesh* ON_Extrusion_GetMesh(const ON_Extrusion* pConstExtrusion, int meshtype)
{
  return 
    (nullptr != pConstExtrusion)
    ? pConstExtrusion->m_mesh_cache.Mesh(ON::MeshType(meshtype))
    : nullptr;
}

RH_C_FUNCTION bool ON_Extrusion_SetMesh( ON_Extrusion* pExtrusion, ON_Mesh* mesh, int meshtype)
{
  bool rc = false;
  if (pExtrusion && mesh)
  {
    rc = pExtrusion->SetMesh(ON::MeshType(meshtype), mesh);
  }
  return rc;
}