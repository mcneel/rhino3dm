
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

RH_C_FUNCTION const ON_Skylight* ON_3dmRenderSettings_GetSkylight(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->Skylight();
}

RH_C_FUNCTION ON_Skylight* ON_3dmRenderSettings_BeginChange_ON_Skylight(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).Skylight();
}

RH_C_FUNCTION const ON_Skylight* ON_Skylight_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->Skylight();
}

RH_C_FUNCTION const ON_Skylight* ON_Skylight_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.Skylight();
}

RH_C_FUNCTION bool ON_Skylight_GetOn(ON_Skylight* p)
{
  if (p)
  {
    return p->On();
  }
  return false;
}

RH_C_FUNCTION void ON_Skylight_SetOn(ON_Skylight* p, bool v)
{
  if (p)
  {
    p->SetOn(v);
  }
}

RH_C_FUNCTION double ON_Skylight_GetShadowIntensity(ON_Skylight* p)
{
  // 14th April 2021 John Croudy, https://mcneel.myjetbrains.com/youtrack/issue/RH-63734
  // ShadowIntensity is an unused red herring.
  if (p)
  {
    return p->ShadowIntensity();
  }
  return 1.0;
}

RH_C_FUNCTION void ON_Skylight_SetShadowIntensity(ON_Skylight* p, double v)
{
  // 14th April 2021 John Croudy, https://mcneel.myjetbrains.com/youtrack/issue/RH-63734
  // ShadowIntensity is an unused red herring.
  if (p)
  {
    p->SetShadowIntensity(v);
  }
}

RH_C_FUNCTION bool ON_Skylight_GetEnvironmentOverride(ON_Skylight* p)
{
  if (p)
  {
    return p->EnvironmentOverride();
  }
  return false;
}

RH_C_FUNCTION void ON_Skylight_SetEnvironmentOverride(ON_Skylight* p, bool v)
{
  if (p)
  {
    p->SetEnvironmentOverride(v);
  }
}

RH_C_FUNCTION ON_UUID ON_Skylight_GetEnvironmentId(ON_Skylight* p)
{
  if (p)
  {
    return p->EnvironmentId();
  }
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_Skylight_SetEnvironmentId(ON_Skylight* p, ON_UUID v)
{
  if (p)
  {
    p->SetEnvironmentId(v);
  }
}

RH_C_FUNCTION ON_Skylight* ON_Skylight_New()
{
  return new ON_Skylight;
}

RH_C_FUNCTION void ON_Skylight_Delete(ON_Skylight* p)
{
  delete p;
}

RH_C_FUNCTION void ON_Skylight_CopyFrom(ON_Skylight* target, const ON_Skylight* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
