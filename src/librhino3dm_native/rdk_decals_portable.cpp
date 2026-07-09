#include "stdafx.h"

// rhino3dm-local (NOT synced from rhcommon_c). Portable replacements for the decal C exports that
// the synced RhinoCommon Rhino.Render.Decal binding (rdk_decals.cs) calls but which upstream
// implements in c_rdk/rdk_decals.cpp against the RDK runtime (ON_RdkUserData XML plumbing,
// RhinoApp, CRhinoDoc). The RDK is a closed, app-coupled Rhino plugin and is NOT redistributable,
// so rhino3dm cannot sync c_rdk; instead it provides the SAME exports here against the public
// opennurbs shared_ptr decal API (AddDecalEx / GetDecalArray). The RHINO_SDK-gated decal helpers
// (color, render CRC, custom-data-without-renderer) compile out of rdk_decals.cs and are
// intentionally NOT provided.
//
// Keep this file as a rhino3dm sync exception: on_decals.cpp / on_3dm_attributes.cpp sync verbatim
// from rhcommon_c; these RDK-gated shims live here so those synced files stay clean.

// The create params is just an ON_Decal (matches upstream's `using CRhRdkDecalCreateParams = ON_Decal`).
RH_C_FUNCTION ON_Decal* Rdk_DecalCreateParams_New()
{
  return new ON_Decal;
}

RH_C_FUNCTION void Rdk_DecalCreateParams_Set(ON_Decal* cp,
                   ON_UUID texture_id, int mapping, int proj, bool map_to_inside, double trans,
                   ON_3dPoint* origin, ON_3dVector* up, ON_3dVector* across,
                   double height, double radius,
                   double horz_sta, double horz_end, double vert_sta, double vert_end,
                   double min_u, double min_v, double max_u, double max_v)
{
  if ((nullptr == cp) || (nullptr == origin) || (nullptr == up) || (nullptr == across))
    return;

  cp->SetTextureInstanceId(texture_id);
  cp->SetMapping((ON_Decal::Mappings)mapping);
  cp->SetProjection((ON_Decal::Projections)proj);
  cp->SetMapToInside(map_to_inside);
  cp->SetTransparency(trans);
  cp->SetOrigin(*origin);
  cp->SetVectorUp(*up);
  cp->SetVectorAcross(*across);
  cp->SetHeight(height);
  cp->SetRadius(radius);
  cp->SetHorzSweep(horz_sta, horz_end);
  cp->SetVertSweep(vert_sta, vert_end);
  cp->SetUVBounds(min_u, min_v, max_u, max_v);
}

RH_C_FUNCTION void Rdk_DecalCreateParams_Delete(ON_Decal* cp)
{
  delete cp;
}

RH_C_FUNCTION unsigned int Rdk_Decal_AddDecal(const ON_Decal* decal, ON_3dmObjectAttributes* attr)
{
  if ((nullptr == decal) || (nullptr == attr))
    return ON_NIL_DECAL_CRC;

  // AddDecalEx() is the portable opennurbs creation API (no RDK runtime).
  std::shared_ptr<ON_Decal> sp = attr->AddDecalEx();
  if (!sp)
    return ON_NIL_DECAL_CRC;

  *sp = *decal;

  return sp->DecalCRC();
}

RH_C_FUNCTION std::shared_ptr<ON_Decal>* Rdk_Decal_NewDecalSharedPtrFromObjectAttributes(
                                         ON_3dmObjectAttributes* attr, unsigned int decal_crc)
{
  if (nullptr == attr)
    return nullptr;

  std::vector<std::shared_ptr<ON_Decal>> decals;
  attr->GetDecalArray(decals);

  for (const auto& sp : decals)
  {
    if (sp && (sp->DecalCRC() == decal_crc))
    {
      // Return an independent copy (matches upstream's `new ON_Decal(*decal_node)`) so the
      // wrapper survives teardown of the source attributes.
      return new std::shared_ptr<ON_Decal>(std::make_shared<ON_Decal>(*sp));
    }
  }

  return nullptr;
}
