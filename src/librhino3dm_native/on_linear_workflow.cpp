
#include "stdafx.h"

enum class LinearWorkflowSetting : int
{
  PreProcessTexturesOn,
  PreProcessColorsOn,
  PostProcessFrameBufferOn,
  PreProcessGammaOn,
  PreProcessGamma,
  PostProcessGammaOn,
  PostProcessGamma,
};

RH_C_FUNCTION void ON_LinearWorkflow_GetValue(const ON_LinearWorkflow* lw, LinearWorkflowSetting which, ON_XMLVariant* v)
{
  if (lw && v)
  {
    switch (which)
    {
    case LinearWorkflowSetting::PreProcessTexturesOn:     *v = lw->PreProcessTexturesOn();     break;
    case LinearWorkflowSetting::PreProcessColorsOn:       *v = lw->PreProcessColorsOn();       break;
    case LinearWorkflowSetting::PostProcessFrameBufferOn: *v = lw->PostProcessFrameBufferOn(); break;
    case LinearWorkflowSetting::PreProcessGammaOn:        *v = lw->PreProcessGammaOn();        break;
    case LinearWorkflowSetting::PreProcessGamma:          *v = lw->PreProcessGamma();          break;
    case LinearWorkflowSetting::PostProcessGammaOn:       *v = lw->PostProcessGammaOn();       break;
    case LinearWorkflowSetting::PostProcessGamma:         *v = lw->PostProcessGamma();         break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_LinearWorkflow_SetValue(ON_LinearWorkflow* lw, LinearWorkflowSetting which, const ON_XMLVariant* v)
{
  if (lw && v)
  {
    switch (which)
    {
    case LinearWorkflowSetting::PreProcessTexturesOn:     lw->SetPreProcessTexturesOn(v->AsBool());     break;
    case LinearWorkflowSetting::PreProcessColorsOn:       lw->SetPreProcessColorsOn(v->AsBool());       break;
    case LinearWorkflowSetting::PostProcessFrameBufferOn: lw->SetPostProcessFrameBufferOn(v->AsBool()); break;
    case LinearWorkflowSetting::PreProcessGammaOn:        lw->SetPreProcessGammaOn(v->AsBool());        break;
    case LinearWorkflowSetting::PreProcessGamma:          lw->SetPreProcessGamma(v->AsFloat());         break;
    case LinearWorkflowSetting::PostProcessGammaOn:       lw->SetPostProcessGammaOn(v->AsBool());       break;
    case LinearWorkflowSetting::PostProcessGamma:         lw->SetPostProcessGamma(v->AsFloat());        break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_LinearWorkflow_SetValue(ON_3dmRenderSettings* rs, LinearWorkflowSetting which, const ON_XMLVariant* v)
{
  if (nullptr != rs)
  {
    ON_LinearWorkflow_SetValue(&rs->LinearWorkflow(), which, v);
  }
}

RH_C_FUNCTION const ON_LinearWorkflow* ON_LinearWorkflow_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.LinearWorkflow();
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
