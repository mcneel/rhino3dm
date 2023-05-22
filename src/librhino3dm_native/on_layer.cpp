#include "stdafx.h"

RH_C_FUNCTION ON_Layer* ON_Layer_New()
{
  return new ON_Layer();
}

RH_C_FUNCTION void ON_Layer_Default(ON_Layer* pLayer)
{
  if (pLayer)
  {
    // Dale Lear asks - do you want ON_Layer::Default or ON_Layer::Unset.
    // Why is this wrapped this way in the first place? Just assign the
    // value on the line that calls this function instead of having
    // this silly C function.
    *pLayer = ON_Layer::Default;
  }
}

RH_C_FUNCTION void ON_Layer_CopyAttributes(ON_Layer* pLayer, const ON_Layer* constOtherLayer)
{
  if (pLayer && constOtherLayer)
    (*pLayer) = *constOtherLayer;
}

RH_C_FUNCTION int ON_Layer_GetColor(const ON_Layer* pLayer, bool regularColor)
{
  int rc = 0;
  if( pLayer )
  {
    unsigned int abgr;
    if( regularColor )
      abgr = (unsigned int)(pLayer->Color());
    else
      abgr = (unsigned int)(pLayer->PlotColor());
    rc = (int)ABGR_to_ARGB(abgr);
  }
  return rc;
}

RH_C_FUNCTION void ON_Layer_SetColor(ON_Layer* pLayer, int argb, bool regularColor)
{
  if( pLayer )
  {
    unsigned int abgr = ARGB_to_ABGR(argb);
    if( regularColor )
      pLayer->SetColor( ON_Color(abgr) );
    else
      pLayer->SetPlotColor( ON_Color(abgr) );
  }
}

RH_C_FUNCTION int ON_Layer_GetIndex(const ON_Layer* pLayer)
{
  int rc = -1;
  if( pLayer )
    rc = pLayer->Index();
  return rc;
}

enum LayerInt : int
{
  idxLinetypeIndex = 0,
  idxRenderMaterialIndex = 1,
  idxIgesLevel = 3,
};

RH_C_FUNCTION int ON_Layer_GetInt(const ON_Layer* pLayer, enum LayerInt which)
{
  int rc = -1;
  if( pLayer )
  {
    switch(which)
    {
    case idxLinetypeIndex:
      rc = pLayer->LinetypeIndex();
      break;
    case idxRenderMaterialIndex:
      rc = pLayer->RenderMaterialIndex();
      break;
    case idxIgesLevel:
      rc = pLayer->IgesLevel();
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Layer_SetInt(ON_Layer* pLayer, enum LayerInt which, int val)
{
  if( pLayer )
  {
    switch(which)
    {
    case idxLinetypeIndex:
      pLayer->SetLinetypeIndex(val);
      break;
    case idxRenderMaterialIndex:
      pLayer->SetRenderMaterialIndex(val);
      break;
    case idxIgesLevel:
      pLayer->SetIgesLevel(val);
      break;
    }
  }
}

enum LayerBool : int
{
  idxIsVisible = 0,
  idxIsLocked = 1,
  idxIsExpanded = 2,
  idxPersistentVisibility = 3,
  idxPersistentLocking = 4,
  idxClipParticipationForAll = 5,
  idxClipParticipationForNone = 6,
  idxModelIsVisible = 7,
  idxModelPersistentVisibility = 8,
  idxPerViewportIsVisibleInNewDetails = 9
};

RH_C_FUNCTION bool ON_Layer_GetSetBool(ON_Layer* pLayer, enum LayerBool which, bool set, bool val)
{
  bool rc = val;
  if (pLayer)
  {
    if (set)
    {
      if (idxIsVisible == which)
        pLayer->SetVisible(val);
      else if (idxIsLocked == which)
        pLayer->SetLocked(val);
      else if (idxIsExpanded == which)
        pLayer->m_bExpanded = val;
      else if (idxPersistentVisibility == which)
        pLayer->SetPersistentVisibility(val);
      else if (idxPersistentLocking == which)
        pLayer->SetPersistentLocking(val);
      else if (idxModelIsVisible == which)
        pLayer->SetModelVisible(val);
      else if (idxModelPersistentVisibility == which)
        pLayer->SetModelPersistentVisibility(val);
      else if (idxPerViewportIsVisibleInNewDetails == which)
        pLayer->SetPerViewportIsVisibleInNewDetails(val);
    }
    else
    {
      if (idxIsVisible == which)
        rc = pLayer->IsVisible();
      else if (idxIsLocked == which)
        rc = pLayer->IsLocked();
      else if (idxIsExpanded == which)
        rc = pLayer->m_bExpanded;
      else if (idxPersistentVisibility == which)
        rc = pLayer->PersistentVisibility();
      else if (idxPersistentLocking == which)
        rc = pLayer->PersistentLocking();
      else if (idxClipParticipationForAll == which || idxClipParticipationForNone == which)
      {
        bool forall = false;
        bool fornone = false;
        ON_UuidList uuidlist;
        bool isParticipation = false;
        pLayer->GetClipParticipation(forall, fornone, uuidlist, isParticipation);
        if (idxClipParticipationForAll == which)
          rc = forall;
        else
          rc = fornone;
      }
      else if (idxModelIsVisible == which)
        rc = pLayer->ModelIsVisible();
      else if (idxModelPersistentVisibility == which)
        rc = pLayer->ModelPersistentVisibility();
      else if (idxPerViewportIsVisibleInNewDetails == which)
        rc = pLayer->PerViewportIsVisibleInNewDetails();
    }
  }
  return rc;
}


RH_C_FUNCTION void ON_Layer_UnsetPersistentVisibility(ON_Layer* pLayer)
{
  if( pLayer )
    pLayer->UnsetPersistentVisibility();
}

RH_C_FUNCTION void ON_Layer_UnsetPersistentLocking(ON_Layer* pLayer)
{
  if( pLayer )
    pLayer->UnsetPersistentLocking();
}

RH_C_FUNCTION double ON_Layer_GetPlotWeight(const ON_Layer* pLayer)
{
  double rc = 0;
  if( pLayer )
    rc = pLayer->PlotWeight();
  return rc;
}

RH_C_FUNCTION void ON_Layer_SetPlotWeight(ON_Layer* pLayer, double value)
{
  if( pLayer )
    pLayer->SetPlotWeight(value);
}

RH_C_FUNCTION ON_UUID ON_Layer_GetGuid(const ON_Layer* pLayer, bool layerId)
{
  if( pLayer )
  {
    if( layerId )
      return pLayer->Id();
    else
      return pLayer->ParentId();
  }
  return ::ON_nil_uuid;
}

RH_C_FUNCTION void ON_Layer_SetGuid(ON_Layer* pLayer, bool layerId, ON_UUID value)
{
  if( pLayer )
  {
    if( layerId )
      pLayer->SetId(value);
    else
      pLayer->SetParentId( value );
  }
}

RH_C_FUNCTION bool ON_Layer_PathOperation(bool getLeafName, const RHMONO_STRING* _path, ON_wString* resultString)
{
  bool rc = false;
  if(_path && resultString)
  {
    INPUTSTRINGCOERCE(path, _path);
    if(getLeafName)
    {
      *resultString = ON_ModelComponent::NameLeaf(path);
    }
    else
    {
      bool bIncludeReference = true;
      *resultString = ON_ModelComponent::NameParent(path, bIncludeReference);
    }
    rc = resultString->IsNotEmpty();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Layer_HasPerViewportSettings(const ON_Layer* pLayer, ON_UUID viewportId, unsigned int settingsMask)
{
  bool rc = false;
  if (nullptr != pLayer)
    rc = pLayer->HasPerViewportSettings(viewportId, settingsMask);
  return rc;
}

RH_C_FUNCTION void ON_Layer_DeletePerViewportSettings(ON_Layer* pLayer, ON_UUID viewportId, unsigned int settingsMask)
{
  if (nullptr != pLayer)
  {
    if (settingsMask == (unsigned int)ON_Layer::PER_VIEWPORT_SETTINGS::per_viewport_color)
      pLayer->DeletePerViewportColor(viewportId);
    else if (settingsMask == (unsigned int)ON_Layer::PER_VIEWPORT_SETTINGS::per_viewport_plot_color)
      pLayer->DeletePerViewportPlotColor(viewportId);
    else if (settingsMask == (unsigned int)ON_Layer::PER_VIEWPORT_SETTINGS::per_viewport_plot_weight)
      pLayer->DeletePerViewportPlotWeight(viewportId);
    else if (settingsMask == (unsigned int)ON_Layer::PER_VIEWPORT_SETTINGS::per_viewport_visible)
      pLayer->DeletePerViewportVisible(viewportId);
    else if (settingsMask == (unsigned int)ON_Layer::PER_VIEWPORT_SETTINGS::per_viewport_persistent_visibility)
      pLayer->UnsetPerViewportPersistentVisibility(viewportId);
    else if (settingsMask == (unsigned int)ON_Layer::PER_VIEWPORT_SETTINGS::per_viewport_all_settings)
      pLayer->DeletePerViewportSettings(viewportId);
  }
}

RH_C_FUNCTION int ON_Layer_GetPerViewportColor(const ON_Layer* pLayer, ON_UUID viewportId, bool regularColor)
{
  int rc = 0;
  if (nullptr != pLayer)
  {
    unsigned int abgr;
    if (regularColor)
      abgr = (unsigned int) pLayer->PerViewportColor(viewportId);
    else
      abgr = (unsigned int)  pLayer->PerViewportPlotColor(viewportId);
    rc = (int)ABGR_to_ARGB(abgr);
  }
  return rc;
}

RH_C_FUNCTION void ON_Layer_SetPerViewportColor(ON_Layer* pLayer, ON_UUID viewportId, int argb, bool regularColor)
{
  if (nullptr != pLayer)
  {
    unsigned int abgr = ARGB_to_ABGR(argb);
    if (regularColor)
      pLayer->SetPerViewportColor(viewportId, ON_Color(abgr));
    else
      pLayer->SetPerViewportPlotColor(viewportId, ON_Color(abgr));
  }
}

RH_C_FUNCTION double ON_Layer_GetPerViewportPlotWeight(const ON_Layer* pLayer, ON_UUID viewportId)
{
  double rc = 0.0;
  if (nullptr != pLayer)
    rc = pLayer->PerViewportPlotWeight(viewportId);
  return rc;
}

RH_C_FUNCTION void ON_Layer_SetPerViewportPlotWeight(ON_Layer* pLayer, ON_UUID viewportId, double plotWeight)
{
  if (nullptr != pLayer)
    pLayer->SetPerViewportPlotWeight(viewportId, plotWeight);
}

RH_C_FUNCTION bool ON_Layer_PerViewportVisibility(const ON_Layer* pLayer, ON_UUID viewportId, bool regularVisibility)
{
  bool rc = false;
  if (nullptr != pLayer)
  {
    if (regularVisibility)
      rc = pLayer->PerViewportIsVisible(viewportId);
    else
      rc = pLayer->PerViewportPersistentVisibility(viewportId);
  }
  return rc;
}

RH_C_FUNCTION void ON_Layer_SetPerViewportVisibility(ON_Layer* pLayer, ON_UUID viewportId, bool visible, bool regularVisibility)
{
  if (nullptr != pLayer)
  {
    if (regularVisibility)
      pLayer->SetPerViewportVisible(viewportId, visible);
    else
      pLayer->SetPerViewportPersistentVisibility(viewportId, visible);
  }
}

RH_C_FUNCTION void ON_Layer_SetClipParticipation(ON_Layer* pLayer, bool forAll, bool forNone, const ON_SimpleArray<ON_UUID>* pIds)
{
  if (pLayer)
  {
    if (forAll)
    {
      pLayer->SetClipParticipationForAll();
    }
    else if (forNone)
    {
      pLayer->SetClipParticipationForNone();
    }
    else if (pIds)
    {
      pLayer->SetClipParticipationList(pIds->Array(), pIds->Count(), true);
    }
  }
}

RH_C_FUNCTION void ON_Layer_ClipParticipationList(const ON_Layer* pConstLayer, ON_SimpleArray<ON_UUID>* uuids)
{
  if (pConstLayer && uuids)
  {
    bool forall = true;
    bool fornone = true;
    ON_UuidList uuidlist;
    bool isParticipation = false;
    pConstLayer->GetClipParticipation(forall, fornone, uuidlist, isParticipation);
    uuidlist.GetUuids(*uuids);
  }
}

RH_C_FUNCTION void ON_Layer_UnsetModelPersistentVisibility(ON_Layer* pLayer)
{
  if (pLayer)
    pLayer->UnsetModelPersistentVisibility();
}

RH_C_FUNCTION void ON_Layer_DeleteModelVisible(ON_Layer* pLayer)
{
  if (pLayer)
    pLayer->DeleteModelVisible();
}

RH_C_FUNCTION ON_SectionStyle* ON_Layer_GetCustomSectionStyle(const ON_Layer* layer)
{
  if (layer)
  {
    const ON_SectionStyle* sectionstyle = layer->CustomSectionStyle();
    if (sectionstyle)
      return new ON_SectionStyle(*sectionstyle);
  }
  return nullptr;
}

RH_C_FUNCTION void ON_Layer_SetCustomSectionStyle(ON_Layer* layer, const ON_SectionStyle* sectionstyle)
{
  if (layer)
  {
    if (sectionstyle)
      layer->SetCustomSectionStyle(*sectionstyle);
    else
      layer->RemoveCustomSectionStyle();
  }
}
