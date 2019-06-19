#include "stdafx.h"

void CopyToPlaneStruct(ON_PLANE_STRUCT& ps, const ON_Plane& plane)
{
  memcpy(&ps, &plane, sizeof(ON_PLANE_STRUCT));
}

ON_Plane FromPlaneStruct(const ON_PLANE_STRUCT& ps)
{
  ON_Plane plane;
  memcpy(&plane, &ps, sizeof(ON_PLANE_STRUCT));
  plane.UpdateEquation();
  return plane;
}



RH_C_FUNCTION bool ON_Plane_CreateFromNormal(ON_PLANE_STRUCT* p, ON_3DPOINT_STRUCT origin, ON_3DVECTOR_STRUCT normal)
{
  bool rc = false;
  if (p)
  {
    const ON_3dPoint* _origin = (const ON_3dPoint*)&origin;
    const ON_3dVector* _normal = (const ON_3dVector*)&normal;
    ON_Plane temp;
    rc = temp.CreateFromNormal(*_origin, *_normal);
    CopyToPlaneStruct(*p, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Plane_CreateFromFrame(ON_PLANE_STRUCT* p, ON_3DPOINT_STRUCT origin, ON_3DVECTOR_STRUCT x, ON_3DVECTOR_STRUCT y)
{
  bool rc = false;
  if (p)
  {
    const ON_3dPoint* _origin = (const ON_3dPoint*)&origin;
    const ON_3dVector* _x = (const ON_3dVector*)&x;
    const ON_3dVector* _y = (const ON_3dVector*)&y;
    ON_Plane temp;
    rc = temp.CreateFromFrame(*_origin, *_x, *_y);
    CopyToPlaneStruct(*p, temp);
  }
  return rc;
}
RH_C_FUNCTION bool ON_Plane_CreateFromPoints(ON_PLANE_STRUCT* p, ON_3DPOINT_STRUCT origin, ON_3DPOINT_STRUCT x, ON_3DPOINT_STRUCT y)
{
  bool rc = false;
  if (p)
  {
    const ON_3dPoint* _origin = (const ON_3dPoint*)&origin;
    const ON_3dPoint* _x = (const ON_3dPoint*)&x;
    const ON_3dPoint* _y = (const ON_3dPoint*)&y;
    ON_Plane temp;
    rc = temp.CreateFromPoints(*_origin, *_x, *_y);
    CopyToPlaneStruct(*p, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Plane_CreateFromEquation(ON_PLANE_STRUCT* p, double a, double b, double c, double d)
{
  bool rc = false;
  if (p)
  {
    ON_PlaneEquation eq;
    eq.x = a;
    eq.y = b;
    eq.z = c;
    eq.d = d;
    ON_Plane temp;
    rc = temp.CreateFromEquation(eq);
    CopyToPlaneStruct(*p, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Plane_IsValid(const ON_PLANE_STRUCT* p)
{
  bool rc = false;
  if (p)
  {
    ON_Plane temp = FromPlaneStruct(*p);
    rc = temp.IsValid();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Plane_UpdateEquation(const ON_PLANE_STRUCT* p)
{
  bool rc = false;
  if (p)
  {
    ON_Plane temp = FromPlaneStruct(*p);
    rc = temp.UpdateEquation();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Plane_Transform(ON_PLANE_STRUCT* p, ON_Xform* xf)
{
  bool rc = false;
  if (p && xf)
  {
    ON_Plane temp = FromPlaneStruct(*p);
    rc = temp.Transform(*xf);
    CopyToPlaneStruct(*p, temp);
  }
  return rc;
}

RH_C_FUNCTION double ON_Plane_DistanceTo(ON_PLANE_STRUCT* p, ON_3DPOINT_STRUCT pt)
{
  double rc = 0;
  if (p)
  {
    const ON_3dPoint* _pt = (const ON_3dPoint*)&pt;
    ON_Plane temp = FromPlaneStruct(*p);
    rc = temp.DistanceTo(*_pt);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Plane_GetDistanceToBoundingBox(ON_PLANE_STRUCT* plane, ON_3DPOINT_STRUCT bboxMin, ON_3DPOINT_STRUCT bboxMax, double* min, double* max)
{
  bool rc = false;
  if (plane && min && max)
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    ON_BoundingBox bbox(ON_3dPoint(bboxMin.val), ON_3dPoint(bboxMax.val));
    rc = temp.GetDistanceToBoundingBox(bbox, min, max);
  }
  return rc;
}

RH_C_FUNCTION void ON_Plane_GetEquation(ON_PLANE_STRUCT* plane, /*ARRAY*/double* eq)
{
  if (plane && eq)
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    eq[0] = temp.plane_equation.x;
    eq[1] = temp.plane_equation.y;
    eq[2] = temp.plane_equation.z;
    eq[3] = temp.plane_equation.d;
  }
}

RH_C_FUNCTION bool ON_Plane_IsOrthogonalFrame(ON_3DVECTOR_STRUCT x, ON_3DVECTOR_STRUCT y, ON_3DVECTOR_STRUCT z)
{
  const ON_3dVector* _x = (const ON_3dVector*)&x;
  const ON_3dVector* _y = (const ON_3dVector*)&y;
  const ON_3dVector* _z = (const ON_3dVector*)&z;

  bool rc = ON_IsOrthogonalFrame(*_x, *_y, *_z);
  return rc;
}

RH_C_FUNCTION bool ON_Plane_IsOrthonormalFrame(ON_3DVECTOR_STRUCT x, ON_3DVECTOR_STRUCT y, ON_3DVECTOR_STRUCT z)
{
  const ON_3dVector* _x = (const ON_3dVector*)&x;
  const ON_3dVector* _y = (const ON_3dVector*)&y;
  const ON_3dVector* _z = (const ON_3dVector*)&z;

  bool rc = ON_IsOrthonormalFrame(*_x, *_y, *_z);
  return rc;
}

RH_C_FUNCTION bool ON_Plane_IsRightHandFrame(ON_3DVECTOR_STRUCT x, ON_3DVECTOR_STRUCT y, ON_3DVECTOR_STRUCT z)
{
  const ON_3dVector* _x = (const ON_3dVector*)&x;
  const ON_3dVector* _y = (const ON_3dVector*)&y;
  const ON_3dVector* _z = (const ON_3dVector*)&z;

  bool rc = ON_IsRightHandFrame(*_x, *_y, *_z);
  return rc;
}