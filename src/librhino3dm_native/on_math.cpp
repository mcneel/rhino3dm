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
