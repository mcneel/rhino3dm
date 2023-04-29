
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

RH_C_FUNCTION const ON_GroundPlane* ON_3dmRenderSettings_GetGroundPlane(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->GroundPlane();
}

RH_C_FUNCTION ON_GroundPlane* ON_3dmRenderSettings_BeginChange_ON_GroundPlane(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).GroundPlane();
}

RH_C_FUNCTION const ON_GroundPlane* ON_GroundPlane_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->GroundPlane();
}

RH_C_FUNCTION const ON_GroundPlane* ON_GroundPlane_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.GroundPlane();
}

RH_C_FUNCTION bool ON_GroundPlane_GetOn(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->On();
  }
  return false;
}

RH_C_FUNCTION void ON_GroundPlane_SetOn(ON_GroundPlane* p, bool v)
{
  if (nullptr != p)
  {
    p->SetOn(v);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetShadowOnly(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->ShadowOnly();
  }
  return false;
}

RH_C_FUNCTION void ON_GroundPlane_SetShadowOnly(ON_GroundPlane* p, bool v)
{
  if (nullptr != p)
  {
    p->SetShadowOnly(v);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetAutoAltitude(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->AutoAltitude();
  }
  return false;
}

RH_C_FUNCTION void ON_GroundPlane_SetAutoAltitude(ON_GroundPlane* p, bool v)
{
  if (nullptr != p)
  {
    p->SetAutoAltitude(v);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetShowUnderside(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->ShowUnderside();
  }
  return false;
}

RH_C_FUNCTION void ON_GroundPlane_SetShowUnderside(ON_GroundPlane* p, bool v)
{
  if (nullptr != p)
  {
    p->SetShowUnderside(v);
  }
}

RH_C_FUNCTION double ON_GroundPlane_GetAltitude(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->Altitude();
  }

  return 0.0;
}

RH_C_FUNCTION void ON_GroundPlane_SetAltitude(ON_GroundPlane* p, double v)
{
  if (nullptr != p)
  {
    p->SetAltitude(v);
  }
}

RH_C_FUNCTION ON_UUID ON_GroundPlane_GetMaterialInstanceId(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->MaterialInstanceId();
  }

  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_GroundPlane_SetMaterialInstanceId(ON_GroundPlane* p, ON_UUID v)
{
  if (nullptr != p)
  {
    p->SetMaterialInstanceId(v);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetTextureOffsetLocked(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->TextureOffsetLocked();
  }
  return false;
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureOffsetLocked(ON_GroundPlane* p, bool locked)
{
  if (nullptr != p)
  {
    p->SetTextureOffsetLocked(locked);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetTextureSizeLocked(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->TextureSizeLocked();
  }
  return false;
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureSizeLocked(ON_GroundPlane* p, bool locked)
{
  if (nullptr != p)
  {
    p->SetTextureSizeLocked(locked);
  }
}

RH_C_FUNCTION void ON_GroundPlane_GetTextureOffset(const ON_GroundPlane* p, ON_2dVector* pVector)
{
  if ((nullptr != p) && (nullptr != pVector))
  {
    *pVector = p->TextureOffset();
  }
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureOffset(ON_GroundPlane* p, ON_2DVECTOR_STRUCT v)
{
  if (nullptr != p)
  {
    p->SetTextureOffset(ON_2dVector(v.val));
  }
}

RH_C_FUNCTION void ON_GroundPlane_GetTextureSize(const ON_GroundPlane* p, ON_2dVector* pVector)
{
  if ((nullptr != p) && (nullptr != pVector))
  {
    *pVector = p->TextureSize();
  }
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureSize(ON_GroundPlane* p, ON_2DVECTOR_STRUCT v)
{
  if (nullptr != p)
  {
    p->SetTextureSize(ON_2dVector(v.val));
  }
}

RH_C_FUNCTION double ON_GroundPlane_GetTextureRotation(const ON_GroundPlane* p)
{
  if (nullptr != p)
  {
    return p->TextureRotation();
  }

  return 0.0;
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureRotation(ON_GroundPlane* p, double v)
{
  if (nullptr != p)
  {
    p->SetTextureRotation(v);
  }
}

RH_C_FUNCTION ON_GroundPlane* ON_GroundPlane_New()
{
  return new ON_GroundPlane;
}

RH_C_FUNCTION void ON_GroundPlane_Delete(ON_GroundPlane* p)
{
  delete p;
}

RH_C_FUNCTION void ON_GroundPlane_CopyFrom(ON_GroundPlane* target, const ON_GroundPlane* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
