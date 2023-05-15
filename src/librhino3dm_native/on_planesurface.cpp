#include "stdafx.h"

RH_C_FUNCTION ON_PlaneSurface* ON_PlaneSurface_New(const ON_PLANE_STRUCT* plane, ON_INTERVAL_STRUCT xExtents, ON_INTERVAL_STRUCT yExtents)
{
  ON_PlaneSurface* rc = nullptr;
  if( plane )
  {
    const ON_Interval* _x = (const ON_Interval*)&xExtents;
    const ON_Interval* _y = (const ON_Interval*)&yExtents;

    ON_Plane temp = FromPlaneStruct(*plane);
    rc = new ON_PlaneSurface(temp);

    if (rc)
    {
      rc->SetExtents(0, *_x, true);
      rc->SetExtents(1, *_y, true);
    }
  }
  return rc;
}

////////////////////////////////////////////////////////////////

RH_C_FUNCTION void ON_ClippingPlaneSurface_GetPlane(const ON_ClippingPlaneSurface* pConstClippingPlaneSurface, ON_PLANE_STRUCT* plane)
{
  if( pConstClippingPlaneSurface && plane )
  {
    CopyToPlaneStruct(*plane, pConstClippingPlaneSurface->m_plane);
  }
}

RH_C_FUNCTION void ON_ClippingPlaneSurface_SetPlane(ON_ClippingPlaneSurface* pClippingPlaneSurface, const ON_PLANE_STRUCT* plane)
{
  if( pClippingPlaneSurface && plane )
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    pClippingPlaneSurface->m_plane = temp;
  }
}

RH_C_FUNCTION int ON_ClippingPlaneSurface_ViewportIdCount(const ON_ClippingPlaneSurface* pConstClippingPlaneSurface)
{
  int rc = 0;
  if( pConstClippingPlaneSurface )
    rc = pConstClippingPlaneSurface->m_clipping_plane.m_viewport_ids.Count();
  return rc;
}

RH_C_FUNCTION ON_UUID ON_ClippingPlaneSurface_ViewportId(const ON_ClippingPlaneSurface* pConstClippingPlaneSurface, int i)
{
  if( pConstClippingPlaneSurface && i>=0 && i<pConstClippingPlaneSurface->m_clipping_plane.m_viewport_ids.Count() )
  {
    const ON_UUID* ids = pConstClippingPlaneSurface->m_clipping_plane.m_viewport_ids.Array();
    if( ids )
      return ids[i];
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION bool ON_ClippingPlaneSurface_AddClipViewport(ON_ClippingPlaneSurface* pClippingPlaneSurface, ON_UUID uuid)
{
  // 17-Nov-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-32856
  bool rc = false;
  if (nullptr != pClippingPlaneSurface)
    rc = pClippingPlaneSurface->m_clipping_plane.m_viewport_ids.AddUuid(uuid, true);
  return rc;
}

RH_C_FUNCTION bool ON_ClippingPlaneSurface_RemoveClipViewport(ON_ClippingPlaneSurface* pClippingPlaneSurface, ON_UUID uuid)
{
  // 17-Nov-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-32856
  bool rc = false;
  if (nullptr != pClippingPlaneSurface)
  {
    rc = pClippingPlaneSurface->m_clipping_plane.m_viewport_ids.RemoveUuid(uuid);
    pClippingPlaneSurface->m_clipping_plane.m_viewport_ids.Compact();
  }
  return rc;
}

RH_C_FUNCTION double ON_ClippingPlaneSurface_GetDepth(const ON_ClippingPlaneSurface* pConstClippingPlaneSurface)
{
  if (pConstClippingPlaneSurface)
    return pConstClippingPlaneSurface->m_clipping_plane.Depth();
  return 0;
}

RH_C_FUNCTION void ON_ClippingPlaneSurface_SetDepthEnabled(ON_ClippingPlaneSurface* pClippingPlaneSurface, bool on)
{
  if (pClippingPlaneSurface)
    pClippingPlaneSurface->m_clipping_plane.SetDepthEnabled(on);
}

RH_C_FUNCTION bool ON_ClippingPlaneSurface_GetDepthEnabled(const ON_ClippingPlaneSurface* pConstClippingPlaneSurface)
{
  if (pConstClippingPlaneSurface)
    return pConstClippingPlaneSurface->m_clipping_plane.DepthEnabled();
  return false;
}

RH_C_FUNCTION void ON_ClippingPlaneSurface_SetDepth(ON_ClippingPlaneSurface* pClippingPlaneSurface, double depth)
{
  if (pClippingPlaneSurface)
    pClippingPlaneSurface->m_clipping_plane.SetDepth(depth);
}
