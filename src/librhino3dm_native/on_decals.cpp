
#include "stdafx.h"

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
