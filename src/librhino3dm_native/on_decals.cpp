
#include "stdafx.h"

enum ON_DecalMapping : int
{
  // Same as ON_Decal::Mappings
  mapNone = -1,
  mapPlanar,
  mapCylindrical,
  mapSpherical,
  mapUV,
};

enum ON_DecalProjection : int
{
  // Same as ON_Decal::Projections
  projNone = -1,
  projForward,
  projBackward,
  projBoth
};

RH_C_FUNCTION std::shared_ptr<ON_Decal>* SharedPtr_ON_Decal_Copy(std::shared_ptr<ON_Decal>* decal_sp)
{
  if (nullptr == decal_sp)
    return nullptr;

  // Copy the shared pointer so that a new reference to the same decal is created.
  return new std::shared_ptr<ON_Decal>(*decal_sp);
}

RH_C_FUNCTION ON_Decal* SharedPtr_ON_Decal_RawPtr(std::shared_ptr<ON_Decal>* decal_sp)
{
  if (nullptr == decal_sp)
    return nullptr;

  // Return the decal that the shared pointer is pointing to.
  return decal_sp->get();
}

RH_C_FUNCTION void SharedPtr_ON_Decal_Delete(std::shared_ptr<ON_Decal>* decal_sp)
{
  // Delete the shared pointer. This may or may not delete the actual decal that it's pointing to.
  delete decal_sp;
}

RH_C_FUNCTION ON_UUID ON_Decal_AssetInstanceId(const ON_Decal* decal)
{
  if (nullptr == decal)
    return ON_nil_uuid;

  return decal->AssetInstanceId();
}

RH_C_FUNCTION void ON_Decal_SetAssetInstanceId(ON_Decal* decal, ON_UUID id)
{
  if (nullptr != decal)
  {
    decal->SetAssetInstanceId(id);
  }
}

RH_C_FUNCTION int ON_Decal_Mapping(const ON_Decal* decal)
{
  if (nullptr == decal)
    return int(ON_Decal::Mappings::None);

  return int(decal->Mapping());
}

RH_C_FUNCTION void ON_Decal_SetMapping(ON_Decal* decal, int m)
{
  if (nullptr != decal)
  {
    decal->SetMapping(ON_Decal::Mappings(m));
  }
}

RH_C_FUNCTION int ON_Decal_Projection(const ON_Decal* decal)
{
  if (nullptr == decal)
    return int(ON_Decal::Projections::None);

  return int(decal->Projection());
}

RH_C_FUNCTION void ON_Decal_SetProjection(ON_Decal* decal, int p)
{
  if (nullptr != decal)
  {
    decal->SetProjection(ON_Decal::Projections(p));
  }
}

RH_C_FUNCTION bool ON_Decal_MapToInside(const ON_Decal* decal)
{
  if (nullptr == decal)
    return false;

  return decal->MapToInside();
}

RH_C_FUNCTION void ON_Decal_SetMapToInside(ON_Decal* decal, bool b)
{
  if (nullptr != decal)
  {
    decal->SetMapToInside(b);
  }
}

RH_C_FUNCTION bool ON_Decal_OriginOffset(const ON_Decal* decal, ON_3dVector* vec)
{
  if ((nullptr == decal) || (nullptr == vec))
    return false;

  if (ON_Decal::Mappings::Planar == decal->Mapping())
  {
    *vec = decal->GetOriginOffset();
  }
  else
  {
    vec->Zero();
  }

  return true;
}

RH_C_FUNCTION bool ON_Decal_Origin(const ON_Decal* decal, ON_3dPoint* pt)
{
  if ((nullptr == decal) || (nullptr == pt))
    return false;

  *pt = decal->Origin();

  return true;
}

RH_C_FUNCTION void ON_Decal_SetOrigin(ON_Decal* decal, const ON_3dPoint* pt)
{
  if ((nullptr != decal) && (nullptr != pt))
  {
    decal->SetOrigin(*pt);
  }
}

RH_C_FUNCTION bool ON_Decal_VectorAcross(const ON_Decal* decal, ON_3dVector* vec)
{
  if ((nullptr == decal) || (nullptr == vec))
    return false;

  *vec = decal->VectorAcross();

  return true;
}

RH_C_FUNCTION void ON_Decal_SetVectorAcross(ON_Decal* decal, const ON_3dVector* vec)
{
  if ((nullptr != decal) && (nullptr != vec))
  {
    decal->SetVectorAcross(*vec);
  }
}

RH_C_FUNCTION bool ON_Decal_VectorUp(const ON_Decal* decal, ON_3dVector* vec)
{
  if ((nullptr == decal) || (nullptr == vec))
    return false;

  *vec = decal->VectorUp();

  return true;
}

RH_C_FUNCTION void ON_Decal_SetVectorUp(ON_Decal* decal, const ON_3dVector* vec)
{
  if ((nullptr != decal) && (nullptr != vec))
  {
    decal->SetVectorUp(*vec);
  }
}

RH_C_FUNCTION double ON_Decal_Transparency(const ON_Decal* decal)
{
  if (nullptr == decal)
    return 0.0;

  return decal->Transparency();
}

RH_C_FUNCTION bool ON_Decal_IsVisible(const ON_Decal* decal)
{
  if (nullptr == decal)
    return false;

  return decal->IsVisible();
}

RH_C_FUNCTION void ON_Decal_SetTransparency(ON_Decal* decal, double d)
{
  if (nullptr != decal)
  {
    decal->SetTransparency(d);
  }
}

RH_C_FUNCTION double ON_Decal_Height(const ON_Decal* decal)
{
  if (nullptr == decal)
    return 0.0;

  return decal->Height();
}

RH_C_FUNCTION void ON_Decal_SetHeight(ON_Decal* decal, double d)
{
  if (nullptr != decal)
  {
    decal->SetHeight(d);
  }
}

RH_C_FUNCTION double ON_Decal_Radius(const ON_Decal* decal)
{
  if (nullptr == decal)
    return 0.0;

  return decal->Radius();
}

RH_C_FUNCTION void ON_Decal_SetRadius(ON_Decal* decal, double d)
{
  if (nullptr != decal)
  {
    decal->SetRadius(d);
  }
}

RH_C_FUNCTION void ON_Decal_GetHorzSweep(const ON_Decal* decal, double* sta, double* end)
{
  if ((nullptr != decal) && (nullptr != sta) && (nullptr != end))
  {
    decal->GetHorzSweep(*sta, *end);
  }
}

RH_C_FUNCTION void ON_Decal_SetHorzSweep(ON_Decal* decal, double sta, double end)
{
  if (nullptr != decal)
  {
    decal->SetHorzSweep(sta, end);
  }
}

RH_C_FUNCTION void ON_Decal_GetVertSweep(const ON_Decal* decal, double* sta, double* end)
{
  if ((nullptr != decal) && (nullptr != sta) && (nullptr != end))
  {
    decal->GetVertSweep(*sta, *end);
  }
}

RH_C_FUNCTION void ON_Decal_SetVertSweep(ON_Decal* decal, double sta, double end)
{
  if (nullptr != decal)
  {
    decal->SetVertSweep(sta, end);
  }
}

RH_C_FUNCTION void ON_Decal_UVBounds(const ON_Decal* decal, double* min_u, double* min_v, double* max_u, double* max_v)
{
  if ((nullptr != decal) && (nullptr != min_u) && (nullptr != min_v) && (nullptr != max_u) && (nullptr != max_v))
  {
    decal->GetUVBounds(*min_u, *min_v, *max_u, *max_v);
  }
}

RH_C_FUNCTION void ON_Decal_SetUVBounds(ON_Decal* decal, double min_u, double min_v, double max_u, double max_v)
{
  if (nullptr != decal)
  {
    decal->SetUVBounds(min_u, min_v, max_u, max_v);
  }
}

RH_C_FUNCTION unsigned int ON_Decal_DataCRC(const ON_Decal* decal, unsigned int current_remainder)
{
  if (nullptr == decal)
    return current_remainder;

  return decal->DataCRC(current_remainder);
}

RH_C_FUNCTION unsigned int ON_Decal_DecalCRC(const ON_Decal* decal)
{
  if (nullptr == decal)
    return 0;

  return decal->DecalCRC();
}

RH_C_FUNCTION bool ON_Decal_TextureMapping(const ON_Decal* decal, ON_TextureMapping* tm)
{
  if (decal && tm)
  {
    return decal->GetTextureMapping(*tm);
  }

  return false;
}

RH_C_FUNCTION void ON_Decal_CustomData(const ON_Decal* decal, ON_XMLParameters* parms, const ON_UUID* renderer)
{
  if ((nullptr == decal) || (nullptr == parms) || (nullptr == renderer))
    return;

  ON_XMLRootNode node;
  decal->GetCustomXML(*renderer, node);
  parms->SetAsString(node.String());
}

RH_C_FUNCTION void ON_Decal_SetCustomData(ON_Decal* decal, const ON_XMLParameters* parms, const ON_UUID* renderer)
{
  if ((nullptr == decal) || (nullptr == parms) || (nullptr == renderer))
    return;

  decal->SetCustomXML(*renderer, parms->Node());
}

RH_C_FUNCTION ON_XMLParameters* ON_XMLParameters_NewParamBlock()
{
  return new ON_XMLParamBlock;
}

RH_C_FUNCTION void ON_XMLParameters_Delete(ON_XMLParameters* p)
{
  delete p;
}

RH_C_FUNCTION ON_XMLParameters::CIterator* ON_XMLParameters_GetIterator(ON_XMLParameters* p)
{
  if (nullptr == p)
    return nullptr;

  return p->NewIterator();
}

RH_C_FUNCTION bool ON_XMLParameters_NextParam(ON_XMLParameters* p, ON_XMLParameters::CIterator* it,
                                              CRhCmnStringHolder* sh, ON_XMLVariant* v)
{
  if ((nullptr != p) && (nullptr != it) && (nullptr != sh) && (nullptr != v))
  {
    ON_wString name;
    if (it->Next(name, *v))
    {
      sh->Set(name);
      return true;
    }
  }

  return false;
}

RH_C_FUNCTION void ON_XMLParameters_DeleteIterator(ON_XMLParameters::CIterator* it)
{
  delete it;
}
