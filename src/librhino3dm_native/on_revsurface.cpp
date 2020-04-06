#include "stdafx.h"

// 9-Sep-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-28908

RH_C_FUNCTION ON_RevSurface* ON_RevSurface_Create(const ON_Curve* pConstProfile, const ON_Line* axis, double startAngle, double endAngle)
{
  ON_RevSurface* rc = nullptr;
  if (pConstProfile && axis)
  {
    rc = ON_RevSurface::New();
    if (rc)
    {
      rc->m_curve = pConstProfile->DuplicateCurve();
      rc->m_axis = *axis;
      ON_Interval domain(startAngle, endAngle);
      if (domain.IsDecreasing())
        rc->m_angle.Set(domain.m_t[0], domain.m_t[1] + 2.0*ON_PI);
      else
        rc->m_angle.Set(domain.m_t[0], domain.m_t[1]);
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_Curve* ON_RevSurface_Curve(const ON_RevSurface* pConstSurface)
{
  if (nullptr != pConstSurface)
    return pConstSurface->m_curve;
  return nullptr;
}

RH_C_FUNCTION void ON_RevSurface_Axis(const ON_RevSurface* pConstSurface, ON_Line* pLine)
{
  if (nullptr != pConstSurface && nullptr != pLine)
    *pLine = pConstSurface->m_axis;
}

RH_C_FUNCTION void ON_RevSurface_Angle(const ON_RevSurface* pConstSurface, ON_Interval* pAngle)
{
  if (nullptr != pConstSurface && nullptr != pAngle)
    *pAngle = pConstSurface->m_angle;
}
