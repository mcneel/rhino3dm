
#include "stdafx.h"

RH_C_FUNCTION bool ON_LinearWorkflow_GetPreProcessTextures(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return false;// ptrModel->m_settings.m_RenderSettings.LinearWorkflow().PreProcessTextures();
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPreProcessTextures(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.LinearWorkflow().SetPreProcessTextures(b);
  }
}

RH_C_FUNCTION bool ON_LinearWorkflow_GetPreProcessColors(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return false;// ptrModel->m_settings.m_RenderSettings.LinearWorkflow().PreProcessColors();
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPreProcessColors(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.LinearWorkflow().SetPreProcessColors(b);
  }
}

RH_C_FUNCTION float ON_LinearWorkflow_GetPreProcessGamma(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.LinearWorkflow().PreProcessGamma();
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPreProcessGamma(ONX_Model* ptrModel, float gamma)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.LinearWorkflow().SetPreProcessGamma(gamma);
  }
}

RH_C_FUNCTION bool ON_LinearWorkflow_GetPostProcessGammaOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.LinearWorkflow().PostProcessGammaOn();
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPostProcessGammaOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.LinearWorkflow().SetPostProcessGammaOn(b);
  }
}

RH_C_FUNCTION float ON_LinearWorkflow_GetPostProcessGamma(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.LinearWorkflow().PostProcessGamma();
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPostProcessGamma(ONX_Model* ptrModel, float gamma)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.LinearWorkflow().SetPostProcessGamma(gamma);
  }
}

RH_C_FUNCTION unsigned int ON_LinearWorkflow_GetDataCRC(const ONX_Model* ptrModel, unsigned int current_remainder)
{
  if (nullptr == ptrModel)
    return current_remainder;

  return ptrModel->m_settings.m_RenderSettings.LinearWorkflow().DataCRC(current_remainder);
}
