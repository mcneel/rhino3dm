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

// https://mcneel.myjetbrains.com/youtrack/issue/RH-85153
RH_C_FUNCTION bool ONC_EvSectionalCurvature(
  ON_3DVECTOR_STRUCT ds,
  ON_3DVECTOR_STRUCT dt,
  ON_3DVECTOR_STRUCT dss,
  ON_3DVECTOR_STRUCT dst,
  ON_3DVECTOR_STRUCT dtt,
  ON_3DVECTOR_STRUCT planeNormal,
  ON_3dVector* k
)
{
  bool rc = false;
  if (k)
  {
    const ON_3dVector* _ds = (const ON_3dVector*)(&ds);
    const ON_3dVector* _dt = (const ON_3dVector*)(&dt);
    const ON_3dVector* _dss = (const ON_3dVector*)(&dss);
    const ON_3dVector* _dst = (const ON_3dVector*)(&dst);
    const ON_3dVector* _dtt = (const ON_3dVector*)(&dtt);
    const ON_3dVector* _planeNormal = (const ON_3dVector*)(&planeNormal);
    ON_3dVector _k;
    rc = ON_EvSectionalCurvature(*_ds, *_dt, *_dss, *_dst, *_dtt, *_planeNormal, _k);
    if (rc)
      *k = _k;
  }
  return rc;
}

typedef double (CALLBACK* INTEGRATE1DPROC)(ON__UINT_PTR sn, int limit_direction, double t);
typedef double (CALLBACK* INTEGRATE2DPROC)(ON__UINT_PTR sn, int limit_direction, double s, double t);

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION double ON_Integrate_1D(INTEGRATE1DPROC func, unsigned int serialNumber, ON_INTERVAL_STRUCT limits, double relative_tolerance, double absolute_tolerance, double* error_bound)
{
  if (func)
  {
    const ON_Interval* t = (const ON_Interval*)&limits;
    return ON_Integrate(func, serialNumber, *t, relative_tolerance, absolute_tolerance, error_bound);
  }
  return ON_DBL_QNAN;
}

RH_C_FUNCTION double ON_Integrate_1D_Curve(INTEGRATE1DPROC func, unsigned int serialNumber, const ON_Curve* curve, double relative_tolerance, double absolute_tolerance, double* error_bound)
{
  if (func && curve)
  {
    return ON_Integrate(*curve, func, serialNumber, curve->Domain(), relative_tolerance, absolute_tolerance, error_bound);
  }
  return ON_DBL_QNAN;
}


RH_C_FUNCTION double ON_Integrate_2D(INTEGRATE2DPROC func, unsigned int serialNumber, ON_INTERVAL_STRUCT limits1, ON_INTERVAL_STRUCT limits2,
  double relative_tolerance, double absolute_tolerance, double* error_bound)
{
  if (func)
  {
    const ON_Interval* l1 = (const ON_Interval*)&limits1;
    const ON_Interval* l2 = (const ON_Interval*)&limits2;
    return ON_Integrate(func, serialNumber, *l1, *l2, relative_tolerance, absolute_tolerance, error_bound);
  }
  return ON_DBL_QNAN;
}

RH_C_FUNCTION double ON_Integrate_2D_Surface(INTEGRATE2DPROC func, unsigned int serialNumber, const ON_Surface* surface,
  double relative_tolerance, double absolute_tolerance, double* error_bound)
{
  if (func && surface)
  {
    return ON_Integrate(*surface, func, serialNumber, surface->Domain(0), surface->Domain(1), relative_tolerance, absolute_tolerance, error_bound);
  }
  return ON_DBL_QNAN;
}

typedef double (CALLBACK* MINIMIZEPROC)(ON__UINT_PTR sn, const double* t, int lenT, double* grad, int lenG);
std::unordered_map<ON__UINT_PTR, MINIMIZEPROC*> _callbacks;
std::unordered_map<ON__UINT_PTR, int> _sizes;

double proc(ON__UINT_PTR p, const double* t, double* g)
{
  auto it = _sizes.find(p);
  if (it != _sizes.end())
  {
    int len = it->second;
    auto at = _callbacks.find(p);
    if (at != _callbacks.end())
    {
      return (*at->second)(p, t, len, g, len);
    }
  }
  return ON_DBL_QNAN;
}
std::mutex _getsetmutex;
RH_C_FUNCTION double ON_Math_Minimize(MINIMIZEPROC func, unsigned int serialNumber,
  int n,
  const ON_SimpleArray<ON_Interval>* search_domain,
  const double *t0,
  double terminate_value,
  double terminate_gradient,
  double relative_tolerance,
  double zero_tolerance,
  int maximum_iterations,
  double *t,
  bool* converged)
{
  if (func && search_domain && t0 && t && converged)
  {
    {
      std::lock_guard<std::mutex> lock(_getsetmutex);
      _callbacks[serialNumber] = &func;
      _sizes[serialNumber] = n;
    }

    double res = ON_Minimize(n, proc, serialNumber,
      *search_domain,
      t0,
      terminate_value,
      terminate_gradient,
      relative_tolerance,
      zero_tolerance,
      maximum_iterations,
      t,
      converged);
    
    {
      std::lock_guard<std::mutex> lock(_getsetmutex);
      _callbacks.erase(serialNumber);
      _sizes.erase(serialNumber);
    }

    return res;
  }
  return ON_DBL_QNAN;
}
#endif
