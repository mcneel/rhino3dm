#include "stdafx.h"

RH_C_FUNCTION bool ONC_EvNormal(
  int limit_dir,
  ON_3DVECTOR_STRUCT ds,
  ON_3DVECTOR_STRUCT dt,
  ON_3DVECTOR_STRUCT dss,
  ON_3DVECTOR_STRUCT dst,
  ON_3DVECTOR_STRUCT dtt,
  ON_3dVector* n
)
{
  bool rc = false;
  if (n)
  {
    const ON_3dVector* _ds = (const ON_3dVector*)(&ds);
    const ON_3dVector* _dt = (const ON_3dVector*)(&dt);
    const ON_3dVector* _dss = (const ON_3dVector*)(&dss);
    const ON_3dVector* _dst = (const ON_3dVector*)(&dst);
    const ON_3dVector* _dtt = (const ON_3dVector*)(&dtt);
    ON_3dVector _n;
    rc = ON_EvNormal(limit_dir , *_ds, *_dt, *_dss, *_dst, *_dtt, _n);
    if (rc)
    {
      *n = _n;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ONC_EvNormalPartials(
  ON_3DVECTOR_STRUCT ds,
  ON_3DVECTOR_STRUCT dt,
  ON_3DVECTOR_STRUCT dss,
  ON_3DVECTOR_STRUCT dst,
  ON_3DVECTOR_STRUCT dtt,
  ON_3dVector* ns,
  ON_3dVector* nt
)
{
  bool rc = false;
  if (ns && nt)
  {
    const ON_3dVector* _ds = (const ON_3dVector*)(&ds);
    const ON_3dVector* _dt = (const ON_3dVector*)(&dt);
    const ON_3dVector* _dss = (const ON_3dVector*)(&dss);
    const ON_3dVector* _dst = (const ON_3dVector*)(&dst);
    const ON_3dVector* _dtt = (const ON_3dVector*)(&dtt);
    ON_3dVector _ns, _nt;
    rc = ON_EvNormalPartials(*_ds, *_dt, *_dss, *_dst, *_dtt, _ns, _nt);
    if (rc)
    {
      *ns = _ns;
      *nt = _nt;
    }
  }
  return rc;
}

typedef double (*Callback1Delegate)(ON__UINT_PTR context, int limit_direction, double t);
typedef double (*Callback2Delegate)(ON__UINT_PTR context, int limit_direction, double s, double t);

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION double ON_Integrate_1D(void* func, void* context, ON_INTERVAL_STRUCT limits, double relative_tolerance, double absolute_tolerance, double* error_bound)
{
  if (context && func)
  {
    const ON_Interval* t = (const ON_Interval*)&limits;
    return ON_Integrate((Callback1Delegate)func, (ON__UINT_PTR)context, *t, relative_tolerance, absolute_tolerance, error_bound);
  }

  // DALE LEAR thinks you should return ON_DBL_QNAN if the input is bogus
  return 0.0;
}

RH_C_FUNCTION double ON_Integrate_1D_Curve(void* func, void* context, const ON_Curve* curve, double relative_tolerance, double absolute_tolerance, double* error_bound)
{
  if (curve && func && context)
    return ON_Integrate(*curve, (Callback1Delegate)func, (ON__UINT_PTR)context, curve->Domain(), relative_tolerance, absolute_tolerance, error_bound);

  // DALE LEAR thinks you should return ON_DBL_QNAN if the input is bogus
  return 0.0;
}

RH_C_FUNCTION double ON_Integrate_2D(void* func, void* context, ON_INTERVAL_STRUCT limits1, ON_INTERVAL_STRUCT limits2, double relative_tolerance, double absolute_tolerance,
  double* error_bound)
{
  if (context && func)
  {
    const ON_Interval* s = (const ON_Interval*)&limits1;
    const ON_Interval* t = (const ON_Interval*)&limits2;
    return ON_Integrate((Callback2Delegate)func, (ON__UINT_PTR)context, *s, *t, relative_tolerance, absolute_tolerance, error_bound);
  }

  // DALE LEAR thinks you should return ON_DBL_QNAN if the input is bogus
  return 0.0;
}

RH_C_FUNCTION double ON_Integrate_2D_Surface(void* func, void* context, const ON_Surface* surface, double relative_tolerance, double absolute_tolerance, double* error_bound)
{
  if (surface && func && context)
    return ON_Integrate(*surface, (Callback2Delegate)func, (ON__UINT_PTR)context, surface->Domain(0), surface->Domain(1), relative_tolerance, absolute_tolerance, error_bound);

  // DALE LEAR thinks you should return ON_DBL_QNAN if the input is bogus
  return 0.0;
}
#endif