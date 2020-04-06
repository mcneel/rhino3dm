#include "stdafx.h"

#if !defined(RHINO3DMIO_BUILD)

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

RH_C_FUNCTION void ON_Quaternion_Rotate( const ON_Quaternion* q, ON_3DVECTOR_STRUCT vin, ON_3dVector* vout)
{
  if( q && vout )
  {
    const ON_3dVector* _vin = (const ON_3dVector*)&vin;
    *vout = q->Rotate(*_vin);
  }
}

#endif