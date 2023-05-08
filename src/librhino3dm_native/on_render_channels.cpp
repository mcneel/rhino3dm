
#include "stdafx.h"

static ON_RenderChannels::Modes Mode(int m)
{
  return ON_RenderChannels::Modes(m);
}

RH_C_FUNCTION int ON_RenderChannels_GetMode(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return -1;

  return int(ptrModel->m_settings.m_RenderSettings.RenderChannels().Mode());
}

RH_C_FUNCTION void ON_RenderChannels_SetMode(ONX_Model* ptrModel, int m)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.RenderChannels().SetMode(Mode(m));
  }
}

RH_C_FUNCTION void ON_RenderChannels_GetCustomList(const ONX_Model* ptrModel, ON_SimpleArray<ON_UUID>* list)
{
  if ((nullptr != ptrModel) && (nullptr != list))
  {
    ptrModel->m_settings.m_RenderSettings.RenderChannels().GetCustomList(*list);
  }
}

RH_C_FUNCTION void ON_RenderChannels_SetCustomList(ONX_Model* ptrModel, const ON_SimpleArray<ON_UUID>* list)
{
  if ((nullptr != ptrModel) && (nullptr != list))
  {
    ptrModel->m_settings.m_RenderSettings.RenderChannels().SetCustomList(*list);
  }
}