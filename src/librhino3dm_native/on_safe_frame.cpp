
#include "stdafx.h"

RH_C_FUNCTION bool ON_SafeFrame_GetOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().On();
}

RH_C_FUNCTION void ON_SafeFrame_SetOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetOn(b);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_GetPerspectiveOnly(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().PerspectiveOnly();
}

RH_C_FUNCTION void ON_SafeFrame_SetPerspectiveOnly(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetPerspectiveOnly(b);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_GetFieldGridOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().FieldGridOn();
}

RH_C_FUNCTION void ON_SafeFrame_SetFieldGridOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetFieldGridOn(b);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_GetLiveFrameOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().LiveFrameOn();
}

RH_C_FUNCTION void ON_SafeFrame_SetLiveFrameOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetLiveFrameOn(b);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_GetActionFrameOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().ActionFrameOn();
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetActionFrameOn(b);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_GetActionFrameLinked(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().ActionFrameLinked();
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameLinked(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetActionFrameLinked(b);
  }
}

RH_C_FUNCTION double ON_SafeFrame_GetActionFrameXScale(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().ActionFrameXScale();
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameXScale(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetActionFrameXScale(d);
  }
}

RH_C_FUNCTION double ON_SafeFrame_GetActionFrameYScale(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().ActionFrameYScale();
}

RH_C_FUNCTION void ON_SafeFrame_SetActionFrameYScale(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetActionFrameYScale(d);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_GetTitleFrameOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().TitleFrameOn();
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetTitleFrameOn(b);
  }
}

RH_C_FUNCTION bool ON_SafeFrame_GetTitleFrameLinked(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().TitleFrameLinked();
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameLinked(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetTitleFrameLinked(b);
  }
}

RH_C_FUNCTION double ON_SafeFrame_GetTitleFrameXScale(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().TitleFrameXScale();
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameXScale(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetTitleFrameXScale(d);
  }
}

RH_C_FUNCTION double ON_SafeFrame_GetTitleFrameYScale(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.SafeFrame().TitleFrameYScale();
}

RH_C_FUNCTION void ON_SafeFrame_SetTitleFrameYScale(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.SafeFrame().SetTitleFrameYScale(d);
  }
}
