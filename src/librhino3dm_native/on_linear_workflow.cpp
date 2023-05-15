
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

RH_C_FUNCTION const ON_LinearWorkflow* ON_3dmRenderSettings_GetLinearWorkflow(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->LinearWorkflow();
}

RH_C_FUNCTION ON_LinearWorkflow* ON_3dmRenderSettings_BeginChange_ON_LinearWorkflow(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).LinearWorkflow();
}

RH_C_FUNCTION const ON_LinearWorkflow* ON_LinearWorkflow_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->LinearWorkflow();
}

RH_C_FUNCTION const ON_LinearWorkflow* ON_LinearWorkflow_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.LinearWorkflow();
}

RH_C_FUNCTION bool ON_LinearWorkflow_PreProcessColorsOn(const ON_LinearWorkflow* p)
{
  return p ? p->PreProcessColorsOn() : false;
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPreProcessColorsOn(ON_LinearWorkflow* p, bool value)
{
  if (p)
  {
    p->SetPreProcessColorsOn(value);
  }
}

RH_C_FUNCTION bool ON_LinearWorkflow_PreProcessTexturesOn(const ON_LinearWorkflow* p)
{
  if (p)
  {
    return p->PreProcessTexturesOn();
  }

  return false;
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPreProcessTexturesOn(ON_LinearWorkflow* p, bool value)
{
  if (p)
  {
    p->SetPreProcessTexturesOn(value);
  }
}

RH_C_FUNCTION bool ON_LinearWorkflow_PostProcessFrameBufferOn(const ON_LinearWorkflow* p)
{
  if (p)
  {
    return p->PostProcessFrameBufferOn();
  }

  return false;
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPostProcessFrameBufferOn(ON_LinearWorkflow* p, bool value)
{
  if (p)
  {
    p->SetPostProcessFrameBufferOn(value);
  }
}

RH_C_FUNCTION bool ON_LinearWorkflow_PostProcessGammaOn(ON_LinearWorkflow* p)
{
  if (p)
  {
    return p->PostProcessGammaOn();
  }

  return true;
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPostProcessGammaOn(ON_LinearWorkflow* p, bool bOn)
{
  if (p)
  {
    p->SetPostProcessGammaOn(bOn);
  }
}

RH_C_FUNCTION float ON_LinearWorkflow_PreProcessGamma(const ON_LinearWorkflow* p)
{
  if (p)
  {
    return p->PreProcessGamma();
  }

  return 1.0f;
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPreProcessGamma(ON_LinearWorkflow* p, float value)
{
  if (p)
  {
    p->SetPreProcessGamma(value);
  }
}

RH_C_FUNCTION float ON_LinearWorkflow_PostProcessGamma(const ON_LinearWorkflow* p)
{
  if (p)
  {
    return p->PostProcessGamma();
  }

  return 1.0f;
}

RH_C_FUNCTION void ON_LinearWorkflow_SetPostProcessGamma(ON_LinearWorkflow* p, float value)
{
  if (p)
  {
    p->SetPostProcessGamma(value);
  }
}

RH_C_FUNCTION unsigned int ON_LinearWorkflow_ComputeCRC(ON_LinearWorkflow* p)
{
  return p ? p->DataCRC(0) : 0;
}

RH_C_FUNCTION ON_LinearWorkflow* ON_LinearWorkflow_New()
{
  return new ON_LinearWorkflow;
}

RH_C_FUNCTION void ON_LinearWorkflow_Delete(ON_LinearWorkflow* p)
{
  delete p;
}

RH_C_FUNCTION void ON_LinearWorkflow_CopyFrom(ON_LinearWorkflow* target, const ON_LinearWorkflow* source)
{
  if (target && source)
  {
    *target = *source;
  }
}
