
#include "stdafx.h"

RH_C_FUNCTION int ON_RenderChannels_GetMode(const ON_RenderChannels* rch)
{
  if (rch)
  {
    return int(rch->Mode());
  }

  return 0;
}

RH_C_FUNCTION void ON_RenderChannels_GetCustomList(const ON_RenderChannels* rch, ON_SimpleArray<ON_UUID>* a)
{
  if (rch && a)
  {
    rch->GetCustomList(*a);
  }
}

RH_C_FUNCTION void ON_RenderChannels_SetMode(ON_RenderChannels* rch, int m)
{
  if (rch)
  {
    rch->SetMode(ON_RenderChannels::Modes(m));
  }
}

RH_C_FUNCTION void ON_RenderChannels_SetCustomList(ON_RenderChannels* rch, const ON_SimpleArray<ON_UUID>* a)
{
  if (rch && a)
  {
    rch->SetCustomList(*a);
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_RenderChannels_SetMode(ON_3dmRenderSettings* rs, int m)
{
  if (rs)
  {
    ON_RenderChannels_SetMode(&rs->RenderChannels(), m);
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_RenderChannels_SetCustomList(ON_3dmRenderSettings* rs, const ON_SimpleArray<ON_UUID>* a)
{
  if (rs && a)
  {
    ON_RenderChannels_SetCustomList(&rs->RenderChannels(), a);
  }
}

RH_C_FUNCTION const ON_RenderChannels* ON_RenderChannels_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.RenderChannels();
}

RH_C_FUNCTION ON_RenderChannels* ON_RenderChannels_New()
{
  return new ON_RenderChannels;
}

RH_C_FUNCTION void ON_RenderChannels_Delete(ON_RenderChannels* p)
{
  delete p;
}

RH_C_FUNCTION void ON_RenderChannels_CopyFrom(ON_RenderChannels* target, const ON_RenderChannels* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
