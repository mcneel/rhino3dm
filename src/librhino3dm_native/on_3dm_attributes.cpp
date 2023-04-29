#include "stdafx.h"

RH_C_FUNCTION ON_3dmObjectAttributes* ON_3dmObjectAttributes_New(const ON_3dmObjectAttributes* pOther)
{
  if( NULL==pOther )
    return new ON_3dmObjectAttributes();
  return new ON_3dmObjectAttributes(*pOther);
}

RH_C_FUNCTION void ON_3dmObjectAttributes_Delete(ON_3dmObjectAttributes* pointer)
{
  if (pointer) delete pointer;
}

enum ObjectAttrsInteger : int
{
  oaiMode = 0,
  oaiLineTypeSource = 1,
  oaiColorSource = 2,
  oaiPlotColorSource = 3,
  oaiPlotWeightSource = 4,
  //oaiDisplayMode = 5,
  oaiLayerIndex = 6,
  oaiLinetypeIndex = 7,
  oaiMaterialIndex = 8,
  oaiMaterialSource = 9,
  oaiObjectDecoration = 10,
  oaiWireDensity = 11,
  oaiSpace = 12,
  oaiGroupCount = 13,
  oaiDisplayOrder = 14,
  oaiClipParticipationSource = 15,
  oaiSectionAttributesSource = 16,
};

RH_C_FUNCTION int ON_3dmObjectAttributes_GetSetInt( ON_3dmObjectAttributes* ptr, enum ObjectAttrsInteger which, bool set, int setValue )
{
  int rc = setValue;
  if( ptr )
  {
    if( set )
    {
      switch( which )
      {
      case oaiMode:
        ptr->SetMode(ON::ObjectMode(setValue));
        break;
      case oaiLineTypeSource:
        ptr->SetLinetypeSource(ON::ObjectLinetypeSource(setValue));
        break;
      case oaiColorSource:
        ptr->SetColorSource(ON::ObjectColorSource(setValue));
        break;
      case oaiPlotColorSource:
        ptr->SetPlotColorSource(ON::PlotColorSource(setValue));
        break;
      case oaiPlotWeightSource:
        ptr->SetPlotWeightSource(ON::PlotWeightSource(setValue));
        break;
      ///case oaiDisplayMode:
      ///  ptr->SetDisplayMode( ON::DisplayMode(setValue) );
      ///  break;
      case oaiLayerIndex:
        ptr->m_layer_index = setValue;
        break;
      case oaiLinetypeIndex:
        ptr->m_linetype_index = setValue;
        break;
      case oaiMaterialIndex:
        ptr->m_material_index = setValue;
        break;
      case oaiMaterialSource:
        ptr->SetMaterialSource(ON::ObjectMaterialSource(setValue));
        break;
      case oaiObjectDecoration:
        ptr->m_object_decoration = ON::ObjectDecoration(setValue);
        break;
      case oaiWireDensity:
        // 28-Feb-2012 Dale Fugier, -1 is acceptable
        // ptr->m_wire_density = set_value<0?0:set_value;
        // 8-Dec-2022 Dale Fugier, RH-71846
        // 12 Dec 2022 S. Baer - remove use of rhino specific macro
        if (setValue < -255)
          setValue = -255;
        if (setValue > 255)
          setValue = 255;
        ptr->m_wire_density = setValue;
        break;
      case oaiSpace:
        ptr->m_space = ON::ActiveSpace(setValue);
        break;
      case oaiGroupCount:
        // no set available
        break;
      case oaiDisplayOrder:
        ptr->m_display_order = setValue;
        break;
      case oaiClipParticipationSource:
        ptr->SetClipParticipationSource(ON::ClipParticipationSourceFromUnsigned(setValue));
        break;
      case oaiSectionAttributesSource:
        ptr->SetSectionAttributesSource(ON::SectionAttributesSourceFromUnsigned(setValue));
        break;
      }
    }
    else
    {
      switch( which )
      {
      case oaiMode:
        rc = (int)ptr->Mode();
        break;
      case oaiLineTypeSource:
        rc = (int)ptr->LinetypeSource();
        break;
      case oaiColorSource:
        rc = (int)ptr->ColorSource();
        break;
      case oaiPlotColorSource:
        rc = (int)ptr->PlotColorSource();
        break;
      case oaiPlotWeightSource:
        rc = (int)ptr->PlotWeightSource();
        break;
      //case oaiDisplayMode:
      //  rc = (int)ptr->DisplayMode();
      //  break;
      case oaiLayerIndex:
        rc = ptr->m_layer_index;
        break;
      case oaiLinetypeIndex:
        rc = ptr->m_linetype_index;
        break;
      case oaiMaterialIndex:
        rc = ptr->m_material_index;
        break;
      case oaiMaterialSource:
        rc = (int)ptr->MaterialSource();
        break;
      case oaiObjectDecoration:
        rc = (int)ptr->m_object_decoration;
        break;
      case oaiWireDensity:
        rc = ptr->m_wire_density;
        break;
      case oaiSpace:
        rc = (int)ptr->m_space;
        break;
      case oaiGroupCount:
        rc = ptr->GroupCount();
        break;
      case oaiDisplayOrder:
        rc = ptr->m_display_order;
        break;
      case oaiClipParticipationSource:
        rc = (int)ptr->ClipParticipationSource();
        break;
      case oaiSectionAttributesSource:
        rc = (int)ptr->SectionAttributesSource();
        break;
      }
    }
  }
  return rc;
}

enum ObjectAttrsBool : int
{
  oabIsInstanceDefinitionObject = 0,
  oabIsVisible = 1,
  oabCastsShadows = 2,
  oabReceivesShadows = 3,
  oabClipParticipationForAll = 4,
  oabClipParticipationForNone = 5,
  oabHatchBoundaryVisible = 6,
};

RH_C_FUNCTION bool ON_3dmObjectAttributes_Transform(ON_3dmObjectAttributes* ptr, ON_Xform* xform)
{
  return (ptr && xform && ptr->Transform(nullptr, *xform));
}


RH_C_FUNCTION bool ON_3dmObjectAttributes_GetSetBool(ON_3dmObjectAttributes* ptr, enum ObjectAttrsBool which, bool set, bool setValue)
{
  bool rc = setValue;
  if( ptr )
  {
    if( set )
    {
      switch(which)
      {
      case oabIsInstanceDefinitionObject:
        // nothing to set
        break;
      case oabIsVisible:
        ptr->SetVisible(setValue);
        break;
      case oabCastsShadows:
        ptr->m_rendering_attributes.m_bCastsShadows = setValue;
        break;
      case oabReceivesShadows:
        ptr->m_rendering_attributes.m_bReceivesShadows = setValue;
        break;
      case oabHatchBoundaryVisible:
        ptr->SetHatchBoundaryVisible(setValue);
        break;
      case oabClipParticipationForAll:
      case oabClipParticipationForNone:
        break; // do nothing
      }
    }
    else
    {
      switch(which)
      {
      case oabIsInstanceDefinitionObject:
        rc = ptr->IsInstanceDefinitionObject();
        break;
      case oabIsVisible:
        rc = ptr->IsVisible();
        break;
      case oabCastsShadows:
        rc = ptr->m_rendering_attributes.m_bCastsShadows;
        break;
      case oabReceivesShadows:
        rc = ptr->m_rendering_attributes.m_bReceivesShadows;
        break;
      case oabClipParticipationForAll:
      case oabClipParticipationForNone:
        {
          bool forall = false;
          bool fornone = false;
          ON_UuidList uuidlist;
          ptr->GetClipParticipation(forall, fornone, uuidlist);
          if (oabClipParticipationForAll == which)
            rc = forall;
          else
            rc = fornone;
        }
        break;
      case oabHatchBoundaryVisible:
        rc = ptr->HatchBoundaryVisible();
        break;
      }
    }
  }
  return rc;
}

//RH_C_FUNCTION unsigned int ON_3dmObjectAttributes_ApplyParentalControl(ON_3dmObjectAttributes* ptr, const ON_3dmObjectAttributes* parent_attr, unsigned int control_limits)
//{
//  unsigned int rc = 0;
//  if( ptr && parent_attr )
//    rc = ptr->ApplyParentalControl(*parent_attr, control_limits);
//  return rc;
//}

RH_C_FUNCTION ON_UUID ON_3dmObjectAttributes_m_uuid(const ON_3dmObjectAttributes* pConstObjectAttributes)
{
  if( NULL == pConstObjectAttributes )
    return ::ON_nil_uuid;
  return pConstObjectAttributes->m_uuid;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_set_m_uuid(ON_3dmObjectAttributes* pAttributes, ON_UUID id)
{
  if( pAttributes )
    pAttributes->m_uuid = id;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_GetSetString(ON_3dmObjectAttributes* pAttributes, int which, bool set, const RHMONO_STRING* str, CRhCmnStringHolder* pStringHolder)
{
  INPUTSTRINGCOERCE(_str, str);
  const int idxName = 0;
  const int idxUrl = 1;
  if(pAttributes)
  {
    if(set)
    {
      if( idxName == which )
        pAttributes->m_name = _str;
      else if( idxUrl == which )
        pAttributes->m_url = _str;
    }
    else
    {
      if( pStringHolder )
      {
        if( idxName == which )
          pStringHolder->Set(pAttributes->m_name);
        else if( idxUrl == which )
          pStringHolder->Set(pAttributes->m_url);
      }
    }
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_GetSetColor(ON_3dmObjectAttributes* pAttributes, int which, bool set, int setValue)
{
  const int idxColor = 0;
  const int idxPlotColor = 1;
  const int idxHatchBackgroundFill = 2;

  int rc = setValue;
  if (pAttributes)
  {
    if (set)
    {
      ON_Color color = ARGB_to_ABGR(setValue);
      if (idxColor == which)
        pAttributes->m_color = color;
      else if (idxPlotColor == which)
        pAttributes->m_plot_color = color;
      else if (idxHatchBackgroundFill == which)
        pAttributes->SetHatchBackgrounFillColor(color);
    }
    else
    {
      ON_Color color;
      if (idxColor == which)
        color = pAttributes->m_color;
      else if (idxPlotColor == which)
        color = pAttributes->m_plot_color;
      else if (idxHatchBackgroundFill == which)
        color = pAttributes->HatchBackgroundFillColor();
      rc = (int)ABGR_to_ARGB((unsigned int)color);
    }
  }
  return rc;
}

enum ObjectAttrsDouble : int
{
  oadPlotWeight = 0,
  oadLinetypePatternScale = 1,
};


RH_C_FUNCTION double ON_3dmObjectAttributes_GetSetDouble(ON_3dmObjectAttributes* pAttributes, enum ObjectAttrsDouble which, bool set, double setValue)
{
  double rc = setValue;
  if(pAttributes)
  {
    if (set)
    {
      switch (which)
      {
      case oadPlotWeight:
        pAttributes->m_plot_weight_mm = setValue;
        break;
      case oadLinetypePatternScale:
        pAttributes->SetLinetypePatternScale(setValue);
        break;
      }
    }
    else
    {
      switch (which)
      {
      case oadPlotWeight:
        rc = pAttributes->m_plot_weight_mm;
        break;
      case oadLinetypePatternScale:
        rc = pAttributes->LinetypePatternScale();
        break;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_UUID ON_3dmObjectAttributes_ViewportId(ON_3dmObjectAttributes* pAttributes, bool set, ON_UUID setValue)
{
  if(NULL == pAttributes)
    return ::ON_nil_uuid;
  if( set )
    pAttributes->m_viewport_id = setValue;
  return pAttributes->m_viewport_id;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_GroupList(const ON_3dmObjectAttributes* ptr, /*ARRAY*/int* list)
{
  if( ptr && list )
  {
    int count = ptr->GroupCount();
    if( count > 0 )
    {
      const int* src = ptr->GroupList();
      if( src )
        ::memcpy(list, src, count * sizeof(int));
    }
  }
}

RH_C_FUNCTION void ON_3dmObjectAttributes_GroupOp(ON_3dmObjectAttributes* ptr, int whichOp, int index)
{
  if( NULL == ptr )
    return;

  const int idxAddToGroup = 0;
  const int idxRemoveFromGroup = 1;
  const int idxRemoveFromTopGroup = 2;
  const int idxRemoveFromAllGroups = 3;

  switch( whichOp )
  {
  case idxAddToGroup:
    ptr->AddToGroup(index);
    break;
  case idxRemoveFromGroup:
    ptr->RemoveFromGroup(index);
    break;
  case idxRemoveFromTopGroup:
    ptr->RemoveFromTopGroup();
    break;
  case idxRemoveFromAllGroups:
    ptr->RemoveFromAllGroups();
    break;
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HasDisplayModeOverride(const ON_3dmObjectAttributes* pConstObjectAttributes, ON_UUID viewportId)
{
  bool rc = false;
  if( pConstObjectAttributes )
  {
    ON_UUID dmr_id;
    if( pConstObjectAttributes->FindDisplayMaterialId(viewportId, &dmr_id) )
    {
      //make sure dmr is not the "invisible in detail" id
      if( dmr_id != ON_DisplayMaterialRef::m_invisible_in_detail_id )
        rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_UUID ON_3dmObjectAttributes_GetDisplayModeOverride(const ON_3dmObjectAttributes* pConstObjectAttributes, ON_UUID viewportId)
{
  ON_UUID rc = ON_nil_uuid;
  if (pConstObjectAttributes)
  {
    ON_UUID dmr_id;
    if (pConstObjectAttributes->FindDisplayMaterialId(viewportId, &dmr_id))
    {
      //make sure dmr is not the "invisible in detail" id
      if (dmr_id != ON_DisplayMaterialRef::m_invisible_in_detail_id)
        rc = dmr_id;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_UseDisplayMode(ON_3dmObjectAttributes* pObjectAttributes, ON_UUID rhinoViewportId, ON_UUID modeId)
{
  bool rc = false;
  if( pObjectAttributes )
  {
    ON_DisplayMaterialRef dmr;
    dmr.m_viewport_id = rhinoViewportId;
    dmr.m_display_material_id = modeId;
    rc = pObjectAttributes->AddDisplayMaterialRef(dmr);
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ClearDisplayMode(ON_3dmObjectAttributes* pObjectAttributes, ON_UUID rhinoViewportId)
{
  if( pObjectAttributes )
  {
    // 26-Aug-2015 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-31242
    // Calling RhinoCommon's Attributes.RemoveDisplayModeOverride() function is supposed to remove all
    // display modes. Passing an empty uuid isn't good enough, as ON_3dmObjectAttributes::RemoveDisplayMaterialRef()
    // requires a non-nil uuid to do anything. So, call ON_3dmObjectAttributes::RemoveAllDisplayMaterialRefs() instead.
    if (ON_UuidIsNil(rhinoViewportId))
      pObjectAttributes->RemoveAllDisplayMaterialRefs();
    else
      pObjectAttributes->RemoveDisplayMaterialRef(rhinoViewportId);
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HasMapping(ON_3dmObjectAttributes* pObjectAttributes)
{
  for (int i = 0; i < pObjectAttributes->m_rendering_attributes.m_mappings.Count(); i++)
  {
    const ON_MappingRef *pRef = pObjectAttributes->m_rendering_attributes.m_mappings.At(i);
    if (pRef->m_mapping_channels.Count())
      return true;
  }

  return false;
}

RH_C_FUNCTION const ON_MaterialRef* ON_3dmObjectAttributes_MaterialRef(ON_3dmObjectAttributes* pObjectAttributes, ON_UUID plugInId)
{
  if(pObjectAttributes == nullptr)
    return nullptr;
  const ON_MaterialRef* result = pObjectAttributes->m_rendering_attributes.MaterialRef(plugInId);
  return result;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_EmptyMaterialRefs(ON_3dmObjectAttributes* pObjectAttributes)
{
  if (pObjectAttributes)pObjectAttributes->m_rendering_attributes.m_materials.Empty();
}

RH_C_FUNCTION int ON_3dmObjectAttributes_MaterialRefCount(ON_3dmObjectAttributes* pObjectAttributes)
{
  return (pObjectAttributes ? pObjectAttributes->m_rendering_attributes.m_materials.Count() : 0);
}

RH_C_FUNCTION int ON_3dmObjectAttributes_MaterialRefIndexOf(ON_3dmObjectAttributes* pObjectAttributes, ON_UUID plugInId)
{
  if(pObjectAttributes == nullptr)
    return -1;
  for (int i = 0, count = pObjectAttributes->m_rendering_attributes.m_materials.Count(); i < count; i++)
    if (pObjectAttributes->m_rendering_attributes.m_materials[i].m_plugin_id == plugInId)
      return i;
  return -1;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_RemoveMaterialRefAt(ON_3dmObjectAttributes* pObjectAttributes, int index)
{
  if(pObjectAttributes == nullptr)
    return false;
  if (index < 0 || index >= pObjectAttributes->m_rendering_attributes.m_materials.Count()) return false;
  pObjectAttributes->m_rendering_attributes.m_materials.Remove(index);
  return true;
}

RH_C_FUNCTION const ON_MaterialRef* ON_3dmObjectAttributes_MaterialFromIndex(ON_3dmObjectAttributes* pObjectAttributes, int index)
{
  if(pObjectAttributes == nullptr || index < 0 || index >= pObjectAttributes->m_rendering_attributes.m_materials.Count())
    return nullptr;
  ON_MaterialRef& result = pObjectAttributes->m_rendering_attributes.m_materials[index];
  return &result;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_MaterialRefSource(ON_3dmObjectAttributes* pObjectAttributes, ON_UUID plugInId, int* value)
{
  if(pObjectAttributes == nullptr || value == nullptr)
    return false;
  const ON_MaterialRef* mat_ref = pObjectAttributes->m_rendering_attributes.MaterialRef(plugInId);
  if(mat_ref == nullptr)
    return false;
  *value = mat_ref->m_material_source;
  return true;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_MaterialId(ON_3dmObjectAttributes* pObjectAttributes, ON_UUID plugInId, ON_UUID* value, bool backFace)
{
  if(pObjectAttributes == nullptr || value == nullptr)
    return false;
  const ON_MaterialRef* mat_ref = pObjectAttributes->m_rendering_attributes.MaterialRef(plugInId);
  if(mat_ref == nullptr)
    return false;
  *value = backFace ? mat_ref->m_material_backface_id : mat_ref->m_material_id;
  return true;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_MaterialIndex(ON_3dmObjectAttributes* pObjectAttributes, ON_UUID plugInId, int* value, bool backFace)
{
  if(pObjectAttributes == nullptr || value == nullptr)
    return false;
  const ON_MaterialRef* mat_ref = pObjectAttributes->m_rendering_attributes.MaterialRef(plugInId);
  if(mat_ref == nullptr)
    return false;
  *value = backFace ? mat_ref->m_material_backface_index : mat_ref->m_material_index;
  return true;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_AddMaterialRef(ON_3dmObjectAttributes* pObjectAttributes, const ON_MaterialRef* pMaterialRef)
{
  if(pObjectAttributes == nullptr || pMaterialRef == nullptr || pMaterialRef->m_plugin_id == ON_nil_uuid)
    return false;
  ON_MaterialRef* mat_ref = const_cast<ON_MaterialRef*>(pObjectAttributes->m_rendering_attributes.MaterialRef(pMaterialRef->m_plugin_id));
  if(mat_ref == nullptr)
    mat_ref = &(pObjectAttributes->m_rendering_attributes.m_materials.AppendNew());
  *mat_ref = *pMaterialRef;
  return true;
}

RH_C_FUNCTION ON_MaterialRef* ON_MaterialRef_New(const ON_MaterialRef* other)
{
  ON_MaterialRef* result = (other ? new ON_MaterialRef(*other) : new ON_MaterialRef());
  return result;
}

RH_C_FUNCTION void ON_MaterialRef_Delete(ON_MaterialRef* pointer)
{
  if (pointer) delete pointer;
}

RH_C_FUNCTION bool ON_MaterialRef_PlugInId(const ON_MaterialRef* pointer, ON_UUID* value)
{
  if(pointer == nullptr || value == nullptr)
    return false;
  *value = pointer->m_plugin_id;
  return true;
}

RH_C_FUNCTION bool ON_MaterialRef_SetPlugInId(ON_MaterialRef* pointer, ON_UUID value)
{
  if(pointer == nullptr)
    return false;
  pointer->m_plugin_id = value;
  return true;
}

RH_C_FUNCTION bool ON_MaterialRef_SetMaterialId(ON_MaterialRef* pointer, ON_UUID value, bool backFace)
{
  if(pointer == nullptr)
    return false;
  if (backFace)
    pointer->m_material_backface_id = value;
  else
    pointer->m_material_id = value;
  return true;
}

RH_C_FUNCTION bool ON_MaterialRef_SetMaterialIndex(ON_MaterialRef* pointer, int value, bool backFace)
{
  if(pointer == nullptr)
    return false;
  if (backFace)
    pointer->m_material_backface_index = value;
  else
    pointer->m_material_index = value;
  return true;
}

RH_C_FUNCTION bool ON_MaterialRef_SetMaterialSource(ON_MaterialRef* pointer, int value)
{
  if (nullptr == pointer)
    return false;
  const ON::object_material_source material_source = ON::ObjectMaterialSource(value);
  if ((unsigned int)material_source != (unsigned int)value)
    return false;
  pointer->SetMaterialSource(material_source);
  return true;
}

enum DisplayModeSpecialType : int
{
  dmstWireframe = 0,
  dmstShaded = 1,
  dmstRendered = 2,
  dmstRenderedShadows = 3,
  dmstGhosted = 4,
  dmstXRay = 5,
  dmstTech = 6,
  dmstArtistic = 7,
  dmstPen = 8,
  dmstAmbientOcclusion = 9,
  dmstRaytraced = 10,
};

RH_C_FUNCTION ON_UUID ON_MaterialRef_DisplayModeSpecialType(const DisplayModeSpecialType displayModeSpecialType)
{
#if !defined(RHINO3DM_BUILD)
  ON_UUID id;
  switch (displayModeSpecialType)
  {
    case dmstWireframe:
      id = CRhinoDisplayAttrsMgr::WireframeModeId();
      break;
    case dmstShaded:
      id = CRhinoDisplayAttrsMgr::ShadedModeId();
      break;
    case dmstRendered:
      id = CRhinoDisplayAttrsMgr::RenderedModeId();
      break;
    case dmstRenderedShadows:
      id = CRhinoDisplayAttrsMgr::RenderedShadowsModeId();
      break;
    case dmstGhosted:
      id = CRhinoDisplayAttrsMgr::GhostedModeId();
      break;
    case dmstXRay:
      id = CRhinoDisplayAttrsMgr::XRayModeId();
      break;
    case dmstTech:
      id = CRhinoDisplayAttrsMgr::TechModeId();
      break;
    case dmstArtistic:
      id = CRhinoDisplayAttrsMgr::ArtisticModeId();
      break;
    case dmstPen:
      id = CRhinoDisplayAttrsMgr::PenModeId();
      break;
    case dmstAmbientOcclusion:
      id = CRhinoDisplayAttrsMgr::AmbientOcclusionModeId();
      break;
    case dmstRaytraced:
      id = CRhinoDisplayAttrsMgr::RaytracedModeId();
      break;
    default:
      id = ON_UUID();
  }
  return id;
#else
  return ON_nil_uuid;
#endif
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_HideInDetail(ON_3dmObjectAttributes* pObjectAttributes, bool add, ON_UUID detailId)
{
  if (pObjectAttributes)
  {
    if (add)
    {
      ON_DisplayMaterialRef dmr;
      dmr.m_display_material_id = ON_DisplayMaterialRef::m_invisible_in_detail_id;
      dmr.m_viewport_id = detailId;
      return pObjectAttributes->AddDisplayMaterialRef(dmr);
    }
    else
      return pObjectAttributes->RemoveDisplayMaterialRef(detailId, ON_DisplayMaterialRef::m_invisible_in_detail_id);
  }
  return false;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_HideInDetailIds(const ON_3dmObjectAttributes* pConstObjectAttributes, ON_SimpleArray<ON_UUID>* ids)
{
  if (pConstObjectAttributes && ids)
  {
    for (int i = 0; i < pConstObjectAttributes->m_dmref.Count(); i++)
    {
      if (pConstObjectAttributes->m_dmref[i].m_display_material_id == ON_DisplayMaterialRef::m_invisible_in_detail_id)
        ids->Append(pConstObjectAttributes->m_dmref[i].m_viewport_id);
    }
  }
}

RH_C_FUNCTION ON_MeshParameters* ON_3dmObjectAttributes_CustomRenderMeshParameters(const ON_3dmObjectAttributes* pConstObjectAttributes)
{
  ON_MeshParameters* rc = nullptr;
  if (pConstObjectAttributes)
  {
    const ON_MeshParameters* mp = pConstObjectAttributes->CustomRenderMeshParameters();
    if (mp)
      rc = new ON_MeshParameters(*mp);
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_SetCustomRenderMeshParameters(ON_3dmObjectAttributes* pObjectAttributes, const ON_MeshParameters* pConstMeshParameters)
{
  if (pObjectAttributes)
  {
    if (pConstMeshParameters)
    {
      if (pObjectAttributes->SetCustomRenderMeshParameters(*pConstMeshParameters))
        pObjectAttributes->EnableCustomRenderMeshParameters(true);
    }
    else
    {
      pObjectAttributes->DeleteCustomRenderMeshParameters();
    }
  }
}

RH_C_FUNCTION void ON_3dmObjectAttributes_SetClipParticipation(ON_3dmObjectAttributes* pObjectAttributes, bool forAll, bool forNone, const ON_SimpleArray<ON_UUID>* pIds)
{
  if (pObjectAttributes)
  {
    if (forAll)
    {
      pObjectAttributes->SetClipParticipationForAll();
    }
    else if (forNone)
    {
      pObjectAttributes->SetClipParticipationForNone();
    }
    else if (pIds)
    {
      pObjectAttributes->SetClipParticipationList(pIds->Array(), pIds->Count());
    }
  }
}

RH_C_FUNCTION void ON_3dmObjectAttributes_ClipParticipationList(const ON_3dmObjectAttributes* pConstAttr, ON_SimpleArray<ON_UUID>* uuids)
{
  if (pConstAttr && uuids)
  {
    bool forall = true;
    bool fornone = true;
    ON_UuidList uuidlist;
    pConstAttr->GetClipParticipation(forall, fornone, uuidlist);
    uuidlist.GetUuids(*uuids);
  }
}

RH_C_FUNCTION int ON_3dmObjectAttributes_DecalCount(const ON_3dmObjectAttributes* attr)
{
  if (nullptr == attr)
    return 0;

  return attr->GetDecalArray().Count();
}

RH_C_FUNCTION ON_Decal* ON_3dmObjectAttributes_DecalAt(const ON_3dmObjectAttributes* attr, int index)
{
  if (nullptr == attr)
    return nullptr;

  const auto& decals = attr->GetDecalArray();
  if ((index < 0) || (index >= decals.Count()))
    return nullptr;

  return decals[index];
}

RH_C_FUNCTION ON_Decal* ON_3dmObjectAttributes_AddDecal(ON_3dmObjectAttributes* attr)
{
  if (nullptr == attr)
    return nullptr;

  return attr->AddDecal();
}

RH_C_FUNCTION ON_Decal* ON_3dmObjectAttributes_AddDecalWithCreateParams(ON_3dmObjectAttributes* attr, const ON_Decal* create_params)
{
  if (nullptr == create_params)
    return 0;

  auto* decal = ON_3dmObjectAttributes_AddDecal(attr);
  if (nullptr == decal)
    return 0;

  *decal = *create_params;

  return decal;
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_RemoveDecal(ON_3dmObjectAttributes* attr, ON_Decal* decal)
{
  if ((nullptr == attr) || (nullptr == decal))
    return false;

  return attr->DeleteDecal(*decal);
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_RemoveAllDecals(ON_3dmObjectAttributes* attr)
{
  if (nullptr == attr)
    return false;

  attr->DeleteAllDecals();

  return true;
}

RH_C_FUNCTION ON_Linetype* ON_3dmObjectAttributes_GetCustomLinetype(const ON_3dmObjectAttributes* attr)
{
  if (attr)
  {
    const ON_Linetype* linetype = attr->CustomLinetype();
    if (linetype)
      return new ON_Linetype(*linetype);
  }
  return nullptr;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_SetCustomLinetype(ON_3dmObjectAttributes* attr, const ON_Linetype* linetype)
{
  if (attr)
  {
    if (linetype)
      attr->SetCustomLinetype(*linetype);
    else
      attr->RemoveCustomLinetype();
  }
}

RH_C_FUNCTION ON_SectionStyle* ON_3dmObjectAttributes_GetCustomSectionStyle(const ON_3dmObjectAttributes* attr)
{
  if (attr)
  {
    const ON_SectionStyle* sectionstyle = attr->CustomSectionStyle();
    if (sectionstyle)
      return new ON_SectionStyle(*sectionstyle);
  }
  return nullptr;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_SetCustomSectionStyle(ON_3dmObjectAttributes* attr, const ON_SectionStyle* sectionstyle)
{
  if (attr)
  {
    if (sectionstyle)
      attr->SetCustomSectionStyle(*sectionstyle);
    else
      attr->RemoveCustomSectionStyle();
  }
}

RH_C_FUNCTION bool ON_3dmObjectAttributes_ObjectFrame(ON_3dmObjectAttributes* const_ptr, ON_Xform* xform)
{
  if (const_ptr && xform)
  {
    const auto plane = const_ptr->ObjectFrame(ON_COMPONENT_INDEX::WholeObject);
    xform->ChangeBasis(plane, ON_Plane::World_xy);
    return true;
  }
  return false;
}

RH_C_FUNCTION void ON_3dmObjectAttributes_SetObjectFrame(ON_3dmObjectAttributes* ptr, const ON_Xform* pXform)
{
  if (ptr && pXform)
  {
    ptr->SetObjectFrame(ON_COMPONENT_INDEX::WholeObject, *pXform);
  }
}

