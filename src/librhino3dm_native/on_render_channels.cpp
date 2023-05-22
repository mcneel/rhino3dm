#include "stdafx.h"


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

RH_C_FUNCTION void ON_RenderChannels_GetCustomList(const ON_RenderChannels* p, ON_SimpleArray<ON_UUID>* paChan)
{
  if ((nullptr != p) && (nullptr != paChan))
  {
    p->GetCustomList(*paChan);
  }
}

RH_C_FUNCTION void ON_RenderChannels_SetCustomList(ON_RenderChannels* p, const ON_SimpleArray<ON_UUID>* paChan)
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
