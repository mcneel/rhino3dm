#include "stdafx.h"

#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION void ON_MassProperties_Delete( ON_MassProperties* ptr )
{
  if( ptr )
    delete ptr;
}

RH_C_FUNCTION double ON_MassProperties_Area(ON_MassProperties* pMassProp)
{
  double rc = ON_UNSET_VALUE;
  if (pMassProp)
    rc = pMassProp->Area();
  return rc;
}

RH_C_FUNCTION double ON_MassProperties_Length(ON_MassProperties* pMassProp)
{
  double rc = ON_UNSET_VALUE;
  if (pMassProp)
    rc = pMassProp->Length();
  return rc;
}

RH_C_FUNCTION void ON_MassProperties_Centroid( ON_MassProperties* pMassProp, ON_3dPoint* pt)
{
  if( pMassProp && pt )
  {
    *pt = pMassProp->Centroid();
  }
}

RH_C_FUNCTION void ON_MassProperties_CentroidError( ON_MassProperties* pMassProp, ON_3dVector* v)
{
  if( pMassProp && v )
  {
    v->x = pMassProp->m_x0_err;
    v->y = pMassProp->m_y0_err;
    v->z = pMassProp->m_z0_err;
  }
}

RH_C_FUNCTION double ON_MassProperties_Volume(ON_MassProperties* pMassProp)
{
  double rc = ON_UNSET_VALUE;
  if (pMassProp)
    rc = pMassProp->Volume();
  return rc;
}

RH_C_FUNCTION double ON_MassProperties_MassError(ON_MassProperties* pMassProp)
{
  double massError = 0.0;
  if (pMassProp)
    massError = pMassProp->m_mass_err;
  return massError;
}
RH_C_FUNCTION bool ON_MassProperties_Sum(ON_MassProperties* pMassProp, ON_MassProperties* pSummand)
{
  bool rc = false;

  if (pMassProp && pSummand)
    rc = pMassProp->Sum(1, pSummand, true);

  return rc;
}
RH_C_FUNCTION bool ON_MassProperties_Sum2(ON_MassProperties* pMassProp, const int count, /*ARRAY*/ const ON_MassProperties** ptr_summands, bool bAddTo)
{
  if (nullptr == pMassProp || nullptr == ptr_summands || count <= 0) return false;
  bool rc = false;

  // Need to convert the array of pointers to an array of objects
  ON_MassProperties* summands = new ON_MassProperties[count];
  for (int i = 0; i < count; i++)
  {
    // there is no operator= overload, but fortunately there isn't anything dynamic in there either,
    // so it acts like a struct
    summands[i] = *(ptr_summands[i]);
  }

  rc = pMassProp->Sum(count, summands, bAddTo);

  return rc;
}

RH_C_FUNCTION ON_MassProperties* ON_MassProperties_WeightedSum(const int count, /*ARRAY*/ const ON_MassProperties** ptr_summands, /*ARRAY*/const double* weights)
{
  if (nullptr == ptr_summands || nullptr == weights || count <= 0) return nullptr;

  ON_MassProperties* pResult = new ON_MassProperties();
 
  // Need to convert the array of pointers to an array of objects
  ON_MassProperties* summands = new ON_MassProperties[count];
  for (int i = 0; i < count; i++)
  {
    // there is no operator= overload, but fortunately there isn't anything dynamic in there either,
    // so it acts like a struct
    summands[i] = *(ptr_summands[i]);
  }
  bool bSuccess = pResult->WeightedSum(count, summands, weights);
  delete[] summands;
  if (!bSuccess)
  {
    delete pResult;
    pResult = nullptr;
  }
  return pResult;
}

RH_C_FUNCTION bool ON_MassProperties_Transform(ON_MassProperties* pMassProp, ON_Xform* pXform)
{
  bool rc = false;
  if (pMassProp && pXform && pXform->IsValid())
    rc = pMassProp->Transform(*pXform);
  return rc;
}

RH_C_FUNCTION bool ON_MassProperties_GetMoments(const ON_MassProperties* pConstMassProps, int which, ON_3dVector* moment, ON_3dVector* error )
{
  const int idx_wc_firstmoments = 0;
  const int idx_wc_secondmoments = 1;
  const int idx_wc_productmoments = 2;
  const int idx_wc_momentsofinertia = 3;
  const int idx_wc_radiiofgyration = 4;
  const int idx_cc_secondmoments = 5;
  const int idx_cc_momentsofinertia = 6;
  const int idx_cc_radiiofgyration = 7;
  const int idx_cc_productmoments = 8;

  bool rc = false;
  if( pConstMassProps && moment && error )
  {
    switch( which )
    {
    case idx_wc_firstmoments:
      if( pConstMassProps->m_bValidFirstMoments )
      {
        moment->Set( pConstMassProps->m_world_x, pConstMassProps->m_world_y, pConstMassProps->m_world_z );
        error->Set( pConstMassProps->m_world_x_err, pConstMassProps->m_world_y_err, pConstMassProps->m_world_z_err );
        rc = true;
      }
      break;
    case idx_wc_secondmoments:
      if( pConstMassProps->m_bValidSecondMoments )
      {
        moment->Set( pConstMassProps->m_world_xx, pConstMassProps->m_world_yy, pConstMassProps->m_world_zz );
        error->Set( pConstMassProps->m_world_xx_err, pConstMassProps->m_world_yy_err, pConstMassProps->m_world_zz_err );
        rc = true;
      }
      break;
    case idx_wc_productmoments:
      if( pConstMassProps->m_bValidProductMoments )
      {
        moment->Set( pConstMassProps->m_world_xy, pConstMassProps->m_world_yz, pConstMassProps->m_world_zx );
        error->Set( pConstMassProps->m_world_xy_err, pConstMassProps->m_world_yz_err, pConstMassProps->m_world_zx_err );
        rc = true;
      }
      break;
    case idx_wc_momentsofinertia:
      if( pConstMassProps->m_bValidSecondMoments )
      {
        moment->Set( pConstMassProps->m_world_yy + pConstMassProps->m_world_zz,
                     pConstMassProps->m_world_zz + pConstMassProps->m_world_xx,
                     pConstMassProps->m_world_xx + pConstMassProps->m_world_yy );
        error->Set( pConstMassProps->m_world_yy_err + pConstMassProps->m_world_zz_err,
                    pConstMassProps->m_world_zz_err + pConstMassProps->m_world_xx_err,
                    pConstMassProps->m_world_xx_err + pConstMassProps->m_world_yy_err );
        rc = true;
      }
      break;
    case idx_wc_radiiofgyration:
      if( pConstMassProps->m_bValidSecondMoments && pConstMassProps->m_bValidMass && pConstMassProps->m_mass > 0.0 )
      {
        *moment = pConstMassProps->WorldCoordRadiiOfGyration();
        rc = true;
      }
      break;
    case idx_cc_secondmoments:
      if( pConstMassProps->m_bValidSecondMoments )
      {
        moment->Set(pConstMassProps->m_ccs_xx, pConstMassProps->m_ccs_yy, pConstMassProps->m_ccs_zz);
        error->Set(pConstMassProps->m_ccs_xx_err, pConstMassProps->m_ccs_yy_err, pConstMassProps->m_ccs_zz_err);
        rc = true;
      }
      break;
    case idx_cc_momentsofinertia:
      if( pConstMassProps->m_bValidSecondMoments )
      {
        moment->Set( pConstMassProps->m_ccs_yy + pConstMassProps->m_ccs_zz,
                     pConstMassProps->m_ccs_zz + pConstMassProps->m_ccs_xx,
                     pConstMassProps->m_ccs_xx + pConstMassProps->m_ccs_yy );
        error->Set( pConstMassProps->m_ccs_yy_err + pConstMassProps->m_ccs_zz_err,
                    pConstMassProps->m_ccs_zz_err + pConstMassProps->m_ccs_xx_err,
                    pConstMassProps->m_ccs_xx_err + pConstMassProps->m_ccs_yy_err );
        rc = true;
      }
      break;
    case idx_cc_radiiofgyration:
      if( pConstMassProps->m_bValidSecondMoments && pConstMassProps->m_bValidMass && pConstMassProps->m_mass > 0.0 )
      {
        *moment = pConstMassProps->CentroidCoordRadiiOfGyration();
        rc = true;
      }
      break;
    case idx_cc_productmoments:
      if (pConstMassProps->m_bValidSecondMoments)
      {
        moment->Set(pConstMassProps->m_ccs_xy, pConstMassProps->m_ccs_yz, pConstMassProps->m_ccs_zx);
        error->Set(pConstMassProps->m_ccs_xy_err, pConstMassProps->m_ccs_yz_err, pConstMassProps->m_ccs_zx_err);
        rc = true;
      }
      break;

    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_MassProperties_GetPrincipalMoments(
  const ON_MassProperties* pConstMassProps, bool world, 
  double* xx, ON_3dVector* ax, 
  double* yy, ON_3dVector* ay, 
  double* zz, ON_3dVector* az
)
{
  bool rc = false;
  if (pConstMassProps && xx && ax && yy && ay && zz && az)
  {
    if (world)
      rc = pConstMassProps->WorldCoordPrincipalMoments(xx, *ax, yy, *ay, zz, *az);
    else
      rc = pConstMassProps->CentroidCoordPrincipalMoments(xx, *ax, yy, *ay, zz, *az);
  }
  return rc;
}

RH_C_FUNCTION bool ON_MassProperties_GetPrincipalMomentsOfInertia(
  const ON_MassProperties* pConstMassProps, bool world,
  double* xx, ON_3dVector* ax,
  double* yy, ON_3dVector* ay,
  double* zz, ON_3dVector* az
)
{
  bool rc = false;
  if (pConstMassProps && xx && ax && yy && ay && zz && az)
  {
    if (world)
      rc = pConstMassProps->WorldCoordPrincipalMomentsOfInertia(xx, *ax, yy, *ay, zz, *az);
    else
      rc = pConstMassProps->CentroidCoordPrincipalMomentsOfInertia(xx, *ax, yy, *ay, zz, *az);
  }
  return rc;
}

#endif
