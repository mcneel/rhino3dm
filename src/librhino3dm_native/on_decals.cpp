
#include "stdafx.h"

// TEMP
class ON_XMLParamBlock : public ON_XMLParameters
{
public:
  ON_XMLParamBlock();
  virtual ~ON_XMLParamBlock();

private:
  ON_XMLRootNode _node;
  ON__UINT64 _reserved;
};

ON_XMLParamBlock::ON_XMLParamBlock()
  :
  _reserved(0),
  ON_XMLParameters(_node)
{
}

ON_XMLParamBlock::~ON_XMLParamBlock()
{
}

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

RH_C_FUNCTION void ON_Decal_Delete(ON_Decal* decal)
{
  delete decal;
}

RH_C_FUNCTION ON_Decal* ON_Decal_NewDecal(const ON_Decal* other)
{
  if (nullptr == other)
    return nullptr;

  return new ON_Decal(*other);
}

RH_C_FUNCTION ON_UUID ON_Decal_TextureInstanceId(const ON_Decal* decal)
{
  if (nullptr == decal)
    return ON_nil_uuid;

  return decal->TextureInstanceId();
}

RH_C_FUNCTION void ON_Decal_SetTextureInstanceId(ON_Decal* decal, ON_UUID id)
{
  if (nullptr != decal)
  {
    decal->SetTextureInstanceId(id);
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

// Support functions for adding a decal to an object
//
// Roy: native class CDecalCreateParams, derived from ON_Decal is used for this.
// Per Steve B -- to avoid IDisposable in the managed code this support class is created and destroyed
// during the C# call to RhinoObject.AddDecal()-- see rhinosdkobject.cs
//
// Split the call up to make it more manageable and extensible-- too many args for a single function.
// Gets bracketed by CDecalCreateParams_New() and CDecalCreateParams_Delete() which news and deletes
// the native object

class CDecalCreateParams final : public ON_Decal
{
public:
  CDecalCreateParams()
  {
    SetTextureInstanceId(ON_nil_uuid);
    SetMapping(ON_Decal::Mappings::Planar);
    SetProjection(ON_Decal::Projections::Both);
    SetMapToInside(false);
    SetTransparency(0.0);
    SetOrigin(ON_3dPoint::Origin);
    SetVectorUp(ON_3dVector::ZAxis);
    SetVectorAcross(ON_3dVector::YAxis);
    SetHeight(5.0);
    SetRadius(2.0);
    SetHorzSweep(0.0, 40.0);
    SetVertSweep(0.0, 20.0);
    SetUVBounds(0.0, 0.0, 1.0, 1.0);
  }

  virtual const ON_Decal& operator = (const ON_Decal& d) override { return ON_Decal::operator = (d); }

  virtual bool operator == (const ON_Decal& d) const override { return ON_Decal::operator == (d); }
  virtual bool operator != (const ON_Decal& d) const override { return ON_Decal::operator != (d); }
};

RH_C_FUNCTION CDecalCreateParams* CDecalCreateParams_New()
{
  return new CDecalCreateParams;
}

RH_C_FUNCTION void CDecalCreateParams_SetMap(CDecalCreateParams* cp, ON_UUID textureInstanceId, int mapping, int projection, bool bMapToInside, double transparency)
{
  if (nullptr != cp)
  {
    cp->SetTextureInstanceId(textureInstanceId);
    cp->SetMapping(ON_Decal::Mappings(mapping));
    cp->SetProjection(ON_Decal::Projections(projection));
    cp->SetMapToInside(bMapToInside);
    cp->SetTransparency(transparency);
  }
}

RH_C_FUNCTION void CDecalCreateParams_SetFrame(CDecalCreateParams* cp, ON_3dPoint* origin, ON_3dVector* up, ON_3dVector* across)
{
  if (nullptr != cp)
  {
    if (nullptr != origin) cp->SetOrigin(*origin);
    if (nullptr != up)     cp->SetVectorUp(*up);
    if (nullptr != across) cp->SetVectorAcross(*across);
  }
}

RH_C_FUNCTION void CDecalCreateParams_SetCylindricalAndSpherical(CDecalCreateParams* cp, double height, double radius, double latStart, double latEnd, double longStart, double longEnd)
{
  if (nullptr != cp)
  {
    cp->SetHeight(height);
    cp->SetRadius(radius);
    cp->SetHorzSweep(latStart, latEnd);
    cp->SetVertSweep(longStart, longEnd);
  }
}

RH_C_FUNCTION void CDecalCreateParams_SetUV(CDecalCreateParams* cp, double minU, double minV, double maxU, double maxV)
{
  if (nullptr != cp)
  {
    cp->SetUVBounds(minU, minV, maxU, maxV);
  }
}

RH_C_FUNCTION void CDecalCreateParams_Delete(CDecalCreateParams* c)
{
  delete c;
}
