
#include "stdafx.h"

static ON_Dithering::Methods Method(int m)
{
  return ON_Dithering::Methods(m);
}

RH_C_FUNCTION bool ON_Dithering_GetOn(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Dithering().On();
}

RH_C_FUNCTION void ON_Dithering_SetOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Dithering().SetOn(b);
  }
}

RH_C_FUNCTION int ON_Dithering_GetMethod(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return -1;

  return int(ptrModel->m_settings.m_RenderSettings.Dithering().Method());
}

RH_C_FUNCTION void ON_Dithering_SetMethod(ONX_Model* ptrModel, int m)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Dithering().SetMethod(Method(m));
  }
}

RH_C_FUNCTION unsigned int ON_Dithering_GetDataCRC(const ONX_Model* ptrModel, unsigned int current_remainder)
{
  if (nullptr == ptrModel)
    return current_remainder;

  return ptrModel->m_settings.m_RenderSettings.Dithering().DataCRC(current_remainder);
}
