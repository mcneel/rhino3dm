
#include "stdafx.h"

RH_C_FUNCTION bool ON_Skylight_GetOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Skylight().On();
}

RH_C_FUNCTION void ON_Skylight_SetOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Skylight().SetOn(b);
  }
}

RH_C_FUNCTION bool ON_Skylight_GetCustomEnvironmentOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return false;// ptrModel->m_settings.m_RenderSettings.Skylight().CustomEnvironmentOn();
}

RH_C_FUNCTION void ON_Skylight_SetCustomEnvironmentOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.Skylight().SetCustomEnvironmentOn(b);
  }
}

RH_C_FUNCTION ON_UUID ON_Skylight_GetCustomEnvironment(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return ON_nil_uuid;

  return ON_nil_uuid;// ptrModel->m_settings.m_RenderSettings.Skylight().CustomEnvironment();
}

RH_C_FUNCTION void ON_Skylight_SetCustomEnvironment(ONX_Model* ptrModel, ON_UUID uuid)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.Skylight().SetCustomEnvironment(uuid);
  }
}

RH_C_FUNCTION double ON_Skylight_GetShadowIntensity(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Skylight().ShadowIntensity();
}

RH_C_FUNCTION void ON_Skylight_SetShadowIntensity(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Skylight().SetShadowIntensity(d);
  }
}
