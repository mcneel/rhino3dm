#include "stdafx.h"

RH_C_FUNCTION ON_PlaneSurface* ON_PlaneSurface_New(const ON_PLANE_STRUCT* plane, ON_INTERVAL_STRUCT xExtents, ON_INTERVAL_STRUCT yExtents)
{
  ON_PlaneSurface* rc = nullptr;
  if( plane )
  {
    const ON_Interval* _x = (const ON_Interval*)&xExtents;
    const ON_Interval* _y = (const ON_Interval*)&yExtents;

    ON_Plane temp = FromPlaneStruct(*plane);
    temp.UpdateEquation();
    rc = new ON_PlaneSurface(temp);

    if (rc)
    {
      rc->SetExtents(0, *_x, true);
      rc->SetExtents(1, *_y, true);
    }
  }
  return rc;
}

RH_C_FUNCTION ON_PlaneSurface* ON_PlaneSurface_New2(const ON_PLANE_STRUCT* plane)
{
  ON_PlaneSurface* rc = nullptr;
  if (plane)
  {
    ON_Plane temp = FromPlaneStruct(*plane);
    temp.UpdateEquation();
    rc = new ON_PlaneSurface(temp);
    if (nullptr != rc)
    {
      ON_Interval extents(0.0, 1.0);
      rc->SetExtents(0, extents, true);
      rc->SetExtents(1, extents, true);
    }
  }
  return rc;
}

RH_C_FUNCTION ON_PlaneSurface* ON_PlaneSurface_New3()
{
  ON_PlaneSurface* rc = new ON_PlaneSurface();
  if (nullptr != rc)
  {
    ON_Interval extents(0.0, 1.0);
    rc->SetExtents(0, extents, true);
    rc->SetExtents(1, extents, true);
  }
  return rc;
}

RH_C_FUNCTION void ON_PlaneSurface_GetPlane(const ON_PlaneSurface* pPlaneSurface, ON_PLANE_STRUCT* pPlane)
{
  if (pPlaneSurface && pPlane)
  {
    CopyToPlaneStruct(*pPlane, pPlaneSurface->m_plane);
  }
}

RH_C_FUNCTION void ON_PlaneSurface_SetPlane(ON_PlaneSurface* pPlaneSurface, const ON_PLANE_STRUCT* pPlane)
{
  if (pPlaneSurface && pPlane)
  {
    ON_Plane temp = FromPlaneStruct(*pPlane);
    temp.UpdateEquation();
    pPlaneSurface->m_plane = temp;
  }
}

RH_C_FUNCTION void ON_PlaneSurface_GetExtents(const ON_PlaneSurface* pPlaneSurface, int direction, ON_Interval* pExtents)
{
  if (pPlaneSurface && pExtents)
  {
    direction = RHINO_CLAMP(direction, 0, 1);
    *pExtents = pPlaneSurface->Extents(direction);
  }
}

RH_C_FUNCTION void ON_PlaneSurface_SetExtents(ON_PlaneSurface* pPlaneSurface, int direction, ON_INTERVAL_STRUCT extents, bool bSyncDomain)
{
  if (pPlaneSurface)
  {
    direction = RHINO_CLAMP(direction, 0, 1);
    const ON_Interval* pExtents = (const ON_Interval*)&extents;
    pPlaneSurface->SetExtents(direction, *pExtents, bSyncDomain);
  }
}

RH_C_FUNCTION ON_Mesh* ON_PlaneSurface_CreateMesh(const ON_PlaneSurface* pPlaneSurface)
{
  ON_Mesh* rc = nullptr;
  if (pPlaneSurface)
    rc = pPlaneSurface->CreateMesh(nullptr);
  return rc;
}

RH_C_FUNCTION ON_PlaneSurface* ON_PlaneSurface_CreatePlaneThroughBox(ON_Line* pLine, ON_3DVECTOR_STRUCT normal, ON_BoundingBox* pBox)
{
  ON_PlaneSurface* rc = nullptr;
  if (pLine && pBox)
  {
    if (pLine->Length() < ON_SQRT_EPSILON)
      return nullptr;

    ON_3dVector _normal(normal.val[0], normal.val[1], normal.val[2]);
    if (_normal.Length() < ON_SQRT_EPSILON)
      return nullptr;

    _normal.Unitize();

    if (_normal.IsParallelTo(pLine->Direction()))
      return nullptr;

    ON_Plane plane(pLine->from, _normal, pLine->Direction());

    rc = new ON_PlaneSurface();
    rc->CreatePlaneThroughBox(plane, *pBox); // use default padding
  }
  return rc;
}

RH_C_FUNCTION ON_PlaneSurface* ON_PlaneSurface_CreatePlaneThroughBox2(const ON_PLANE_STRUCT* pPlane, ON_BoundingBox* pBox)
{
  ON_PlaneSurface* rc = nullptr;
  if (pPlane && pBox)
  {
    ON_Plane _plane = FromPlaneStruct(*pPlane);
    _plane.UpdateEquation();
    rc = new ON_PlaneSurface();
    rc->CreatePlaneThroughBox(_plane, *pBox); // use default padding
  }
  return rc;
}

////////////////////////////////////////////////////////////////

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

RH_C_FUNCTION bool ON_ClippingPlaneSurce_ParticipationEnabled(const ON_ClippingPlaneSurface* pConstClippingPlaneSurface)
{
  if (pConstClippingPlaneSurface)
    return pConstClippingPlaneSurface->m_clipping_plane.ParticipationListsEnabled();
  return true;
}

RH_C_FUNCTION void ON_ClippingPlaneSurface_SetParticipationEnabled(ON_ClippingPlaneSurface* pClippingPlaneSurface, bool enabled)
{
  if (pClippingPlaneSurface)
    pClippingPlaneSurface->m_clipping_plane.SetParticipationListsEnabled(enabled);
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

RH_C_FUNCTION void ON_ClippingPlaneSurface_SetClipList(
  ON_ClippingPlaneSurface* pClippingPlaneSurface,
  const ON_SimpleArray<ON_UUID>* idArray,
  const ON_SimpleArray<int>* layerIndexArray,
  bool isExclusionList)
{
  if (pClippingPlaneSurface)
  {
    pClippingPlaneSurface->m_clipping_plane.SetParticipationLists(idArray, layerIndexArray, isExclusionList);
  }
}

RH_C_FUNCTION void ON_ClippingPlaneSurface_GetClipList(
  const ON_ClippingPlaneSurface* pClippingPlaneSurface,
  ON_SimpleArray<ON_UUID>* idArray,
  ON_SimpleArray<int>* layerIndexArray,
  bool* isExclusionList)
{
  if (pClippingPlaneSurface)
  {
    if (idArray)
    {
      idArray->Empty();
      const ON_UuidList* list = pClippingPlaneSurface->m_clipping_plane.ObjectClipParticipationList();
      if (list)
      {
        idArray->Append(list->Count(), list->Array());
      }
    }
    if (layerIndexArray)
    {
      layerIndexArray->Empty();
      const ON_SimpleArray<int>* list = pClippingPlaneSurface->m_clipping_plane.LayerClipParticipationList();
      if (list)
      {
        *layerIndexArray = *list;
      }
    }
    if (isExclusionList)
    {
      *isExclusionList = pClippingPlaneSurface->m_clipping_plane.ClipParticipationListsAreExclusionLists();
    }
  }
}

RH_C_FUNCTION void ON_ClippingPlaneSurface_ClearParticipationLists(ON_ClippingPlaneSurface* pClippingPlaneSurface)
{
  if (pClippingPlaneSurface)
  {
    pClippingPlaneSurface->m_clipping_plane.SetParticipationLists(nullptr, nullptr, true);
  }
}
