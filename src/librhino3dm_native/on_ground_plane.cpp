
#include "stdafx.h"

RH_C_FUNCTION bool ON_GroundPlane_GetOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().On();
}

RH_C_FUNCTION void ON_GroundPlane_SetOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetOn(b);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetShowUnderside(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().ShowUnderside();
}

RH_C_FUNCTION void ON_GroundPlane_SetShowUnderside(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetShowUnderside(b);
  }
}

RH_C_FUNCTION double ON_GroundPlane_GetAltitude(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().Altitude();
}

RH_C_FUNCTION void ON_GroundPlane_SetAltitude(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetAltitude(d);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetAutoAltitude(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().AutoAltitude();
}

RH_C_FUNCTION void ON_GroundPlane_SetAutoAltitude(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetAutoAltitude(b);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetShadowOnly(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().ShadowOnly();
}

RH_C_FUNCTION void ON_GroundPlane_SetShadowOnly(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetShadowOnly(b);
  }
}

RH_C_FUNCTION ON_UUID ON_GroundPlane_GetMaterialInstanceId(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return ON_nil_uuid;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().MaterialInstanceId();
}

RH_C_FUNCTION void ON_GroundPlane_SetMaterialInstanceId(ONX_Model* ptrModel, ON_UUID uuid)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetMaterialInstanceId(uuid);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetTextureOffset(const ONX_Model* ptrModel, ON_2dVector* vec)
{
  if ((nullptr == ptrModel) || (nullptr == vec))
    return false;

  *vec = ptrModel->m_settings.m_RenderSettings.GroundPlane().TextureOffset();

  return true;
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureOffset(ONX_Model* ptrModel, const ON_2dVector* vec)
{
  if ((nullptr != ptrModel) && (nullptr != vec))
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetTextureOffset(*vec);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetTextureOffsetLocked(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().TextureOffsetLocked();
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureOffsetLocked(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetTextureOffsetLocked(b);
  }
}

RH_C_FUNCTION bool ON_GroundPlane_GetTextureRepeatLocked(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return false;// ptrModel->m_settings.m_RenderSettings.GroundPlane().TextureRepeatLocked();
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureRepeatLocked(ONX_Model* ptrModel, bool b)
{
  //if (nullptr != ptrModel)
  //{
  //  ptrModel->m_settings.m_RenderSettings.GroundPlane().SetTextureRepeatLocked(b);
  //}
}

RH_C_FUNCTION bool ON_GroundPlane_GetTextureSize(const ONX_Model* ptrModel, ON_2dVector* vec)
{
  if ((nullptr == ptrModel) || (nullptr == vec))
    return false;

  *vec = ptrModel->m_settings.m_RenderSettings.GroundPlane().TextureSize();

  return true;
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureSize(ONX_Model* ptrModel, const ON_2dVector* vec)
{
  if ((nullptr != ptrModel) && (nullptr != vec))
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetTextureSize(*vec);
  }
}

RH_C_FUNCTION double ON_GroundPlane_GetTextureRotation(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.GroundPlane().TextureRotation();
}

RH_C_FUNCTION void ON_GroundPlane_SetTextureRotation(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.GroundPlane().SetTextureRotation(d);
  }
}
