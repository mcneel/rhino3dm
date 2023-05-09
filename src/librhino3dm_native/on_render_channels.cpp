
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

RH_C_FUNCTION const ON_RenderChannels* ON_3dmRenderSettings_GetRenderChannels(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->RenderChannels();
}

RH_C_FUNCTION ON_RenderChannels* ON_3dmRenderSettings_BeginChange_ON_RenderChannels(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).RenderChannels();
}

RH_C_FUNCTION const ON_RenderChannels* ON_RenderChannels_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->RenderChannels();
}

RH_C_FUNCTION const ON_RenderChannels* ON_RenderChannels_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.RenderChannels();
}

RH_C_FUNCTION int ON_RenderChannels_Mode(const ON_RenderChannels* p)
{
  if (nullptr != p)
  {
    return int(p->Mode());
  }

  return int(ON_RenderChannels::Modes::Automatic);
}

RH_C_FUNCTION void ON_RenderChannels_SetMode(ON_RenderChannels* p, int m)
{
  if (nullptr != p)
  {
    p->SetMode(ON_RenderChannels::Modes(m));
  }
}

RH_C_FUNCTION void ON_RenderChannels_GetCustomList(const ON_RenderChannels* p, ON_SimpleArray<UUID>* paChan)
{
  if ((nullptr != p) && (nullptr != paChan))
  {
    p->GetCustomList(*paChan);
  }
}

RH_C_FUNCTION void ON_RenderChannels_SetCustomList(ON_RenderChannels* p, const ON_SimpleArray<UUID>* paChan)
{
  if ((nullptr != p) && (nullptr != paChan))
  {
    p->SetCustomList(*paChan);
  }
}

RH_C_FUNCTION ON_RenderChannels* ON_RenderChannels_New()
{
  return new ON_RenderChannels;
}

RH_C_FUNCTION void ON_RenderChannels_Delete(ON_Skylight* p)
{
  delete p;
}

RH_C_FUNCTION void ON_RenderChannels_CopyFrom(ON_Skylight* target, const ON_Skylight* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
