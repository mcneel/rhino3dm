#include "stdafx.h"

RH_C_FUNCTION double ON_Quaternion_Length( const ON_Quaternion* q)
{
  double rc = 0.0;
  if( q )
    rc = q->Length();
  return rc;
}

RH_C_FUNCTION bool ON_Quaternion_Unitize( ON_Quaternion* q)
{
  bool rc = false;
  if( q )
    rc = q->Unitize();
  return rc;
}

RH_C_FUNCTION void ON_Quaternion_SetRotation( ON_Quaternion* q, const ON_PLANE_STRUCT* plane0, const ON_PLANE_STRUCT* plane1)
{
  if( q && plane0 && plane1 )
  {
    ON_Plane temp0 = FromPlaneStruct(*plane0);
    ON_Plane temp1 = FromPlaneStruct(*plane1);
    q->SetRotation(temp0, temp1);
  }
}
RH_C_FUNCTION bool ON_Quaternion_GetRotation( const ON_Quaternion* q, ON_PLANE_STRUCT* plane)
{
  bool rc = false;
  if( q && plane )
  {
    ON_Plane temp;
    rc = q->GetRotation(temp);
    CopyToPlaneStruct(*plane, temp);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Quaternion_GetRotation2(const ON_Quaternion* pConstQuaternion, ON_Xform* pXform)
{
  bool rc = false;
  if (pConstQuaternion && pXform)
    rc = pConstQuaternion->GetRotation(*pXform);
  return rc;
}

RH_C_FUNCTION void ON_Quaternion_Rotate( const ON_Quaternion* q, ON_3DVECTOR_STRUCT vin, ON_3dVector* vout)
{
  if( q && vout )
  {
    const ON_3dVector* _vin = (const ON_3dVector*)&vin;
    *vout = q->Rotate(*_vin);
  }
}

RH_C_FUNCTION void ON_Quaternion_Slerp(const ON_Quaternion* a, const ON_Quaternion* b, double t, ON_Quaternion* q)
{
  if (a && b && q)
  {
    *q = ON_Quaternion::Slerp(*a, *b, t);
  }
}

RH_C_FUNCTION void ON_Quaternion_Lerp(const ON_Quaternion* a, const ON_Quaternion* b, double t, ON_Quaternion* q)
{
  if (a && b && q)
  {
    *q = (1 - t) * (*a) + t * (*b);
    q->Unitize();
  }
}

RH_C_FUNCTION void ON_Quaternion_RotateTowards(const ON_Quaternion* a, const ON_Quaternion* b, double maxRadians, ON_Quaternion* q)
{
  if (a && b && q)
  {
    *q = ON_Quaternion::RotateTowards(*a, *b, maxRadians);
  }
}

RH_C_FUNCTION void ON_Quaternion_RotationZYX(double yaw, double pitch, double roll, ON_Quaternion* q)
{
  if (q)
    *q = ON_Quaternion::RotationZYX(yaw, pitch, roll);
}

RH_C_FUNCTION void ON_Quaternion_RotationZYZ(double alpha, double beta, double gamma, ON_Quaternion* q)
{
  if (q)
    *q = ON_Quaternion::RotationZYZ(alpha, beta, gamma);
}

RH_C_FUNCTION bool ON_Quaternion_GetYawPitchRoll(const ON_Quaternion* q, double* yaw, double* pitch, double* roll)
{
  bool rc = false;
  if (q && yaw && pitch && roll)
    rc = q->GetYawPitchRoll(*yaw, *pitch, *roll);
  return rc;
}

RH_C_FUNCTION bool ON_Quaternion_GetEulerZYZ(const ON_Quaternion* q, double* alpha, double* beta, double* gamma)
{
  bool rc = false;
  if (q && alpha && beta && gamma)
    rc = q->GetEulerZYZ(*alpha, *beta, *gamma);
  return rc;
}
