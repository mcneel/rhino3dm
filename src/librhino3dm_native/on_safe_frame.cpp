
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

RH_C_FUNCTION const ON_SafeFrame* ON_3dmRenderSettings_GetSafeFrame(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->SafeFrame();
}

RH_C_FUNCTION ON_SafeFrame* ON_3dmRenderSettings_BeginChange_ON_SafeFrame(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).SafeFrame();
}

RH_C_FUNCTION const ON_SafeFrame* ON_SafeFrame_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->SafeFrame();
}

RH_C_FUNCTION const ON_SafeFrame* ON_SafeFrame_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.SafeFrame();
}

RH_C_FUNCTION bool ON_SafeFrame_Enabled(ON_SafeFrame* p)
{
  if (p)
  {
    return p->On();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetEnabled(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetOn(v);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_PerspectiveOnly(ON_SafeFrame* p)
{
  if (p)
  {
    return p->PerspectiveOnly();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetPerspectiveOnly(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetPerspectiveOnly(v);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_FieldsOn(ON_SafeFrame* p)
{
  if (p)
  {
    return p->FieldGridOn();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetFieldsOn(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetFieldGridOn(v);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_LiveFrameOn(ON_SafeFrame* p)
{
  if (p)
  {
    return p->LiveFrameOn();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetLiveFrameOn(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetLiveFrameOn(v);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_ActionFrameOn(ON_SafeFrame* p)
{
  if (p)
  {
    return p->ActionFrameOn();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameOn(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetActionFrameOn(v);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_ActionFrameLinked(ON_SafeFrame* p)
{
  if (p)
  {
    return p->ActionFrameLinked();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameLinked(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetActionFrameLinked(v);
  }
}

RH_C_FUNCTION double ON_SafeFrame_ActionFrameXScale(ON_SafeFrame* p)
{
  if (p)
  {
    return p->ActionFrameXScale();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameXScale(ON_SafeFrame* p, double d)
{
  if (p)
  {
    p->SetActionFrameXScale(d);
  }
}

RH_C_FUNCTION double ON_SafeFrame_ActionFrameYScale(ON_SafeFrame* p)
{
  if (p)
  {
    return p->ActionFrameYScale();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameYScale(ON_SafeFrame* p, double d)
{
  if (p)
  {
    p->SetActionFrameYScale(d);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_TitleFrameOn(ON_SafeFrame* p)
{
  if (p)
  {
    return p->TitleFrameOn();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameOn(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetTitleFrameOn(v);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_TitleFrameLinked(ON_SafeFrame* p)
{
  if (p)
  {
    return p->TitleFrameLinked();
  }
  return false;
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameLinked(ON_SafeFrame* p, bool v)
{
  if (p)
  {
    p->SetTitleFrameLinked(v);
  }
}

RH_C_FUNCTION double ON_SafeFrame_TitleFrameXScale(ON_SafeFrame* p)
{
  if (p)
  {
    return p->TitleFrameXScale();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameXScale(ON_SafeFrame* p, double d)
{
  if (p)
  {
    p->SetTitleFrameXScale(d);
  }
}

RH_C_FUNCTION double ON_SafeFrame_TitleFrameYScale(ON_SafeFrame* p)
{
  if (p)
  {
    return p->TitleFrameYScale();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameYScale(ON_SafeFrame* p, double d)
{
  if (p)
  {
    p->SetTitleFrameYScale(d);
  }
}

RH_C_FUNCTION ON_SafeFrame* ON_SafeFrame_New()
{
  return new ON_SafeFrame;
}

RH_C_FUNCTION void ON_SafeFrame_Delete(ON_SafeFrame* p)
{
  delete p;
}

RH_C_FUNCTION void ON_SafeFrame_CopyFrom(ON_SafeFrame* target, const ON_SafeFrame* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
