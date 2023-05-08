
#include "stdafx.h"

RH_C_FUNCTION int ON_Sun_GetMinYear()
{
  return ON_Sun::MinYear();
}

RH_C_FUNCTION int ON_Sun_GetMaxYear()
{
  return ON_Sun::MaxYear();
}

RH_C_FUNCTION bool ON_Sun_GetIsValid(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Sun().IsValid();
}

RH_C_FUNCTION bool ON_Sun_GetEnableAllowed(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Sun().EnableAllowed();
}

RH_C_FUNCTION bool ON_Sun_GetEnableOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Sun().EnableOn();
}

RH_C_FUNCTION bool ON_Sun_GetManualControlAllowed(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Sun().ManualControlAllowed();
}

RH_C_FUNCTION bool ON_Sun_GetManualControlOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Sun().ManualControlOn();
}

RH_C_FUNCTION double ON_Sun_GetNorth(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().North();
}

RH_C_FUNCTION bool ON_Sun_GetVector(const ONX_Model* ptrModel, ON_3dVector* vec)
{
  if ((nullptr == ptrModel) || (nullptr == vec))
    return false;

  //*vec = ptrModel->m_settings.m_RenderSettings.Sun().Vector();

  return true;
}

RH_C_FUNCTION double ON_Sun_GetAzimuth(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().Azimuth();
}

RH_C_FUNCTION double ON_Sun_GetAltitude(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().Altitude();
}

RH_C_FUNCTION double ON_Sun_GetLatitude(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().Latitude();
}

RH_C_FUNCTION double ON_Sun_GetLongitude(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().Longitude();
}

RH_C_FUNCTION double ON_Sun_GetTimeZone(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().TimeZone();
}

RH_C_FUNCTION bool ON_Sun_GetDaylightSavingOn(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return ptrModel->m_settings.m_RenderSettings.Sun().DaylightSavingOn();
}

RH_C_FUNCTION int ON_Sun_GetDaylightSavingMinutes(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0;

  return ptrModel->m_settings.m_RenderSettings.Sun().DaylightSavingMinutes();
}

RH_C_FUNCTION bool ON_Sun_GetLocalDateTime(const ONX_Model* ptrModel, int* y, int* m, int* d, double* h)
{
  if ((nullptr == ptrModel) || (nullptr == y) || (nullptr == m) || (nullptr == d) || (nullptr == h))
    return false;

  ptrModel->m_settings.m_RenderSettings.Sun().LocalDateTime(*y, *m, *d, *h);

  return true;
}

RH_C_FUNCTION double ON_Sun_GetIntensity(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().Intensity();
}

RH_C_FUNCTION double ON_Sun_GetShadowIntensity(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return 0.0;

  return ptrModel->m_settings.m_RenderSettings.Sun().ShadowIntensity();
}

RH_C_FUNCTION void ON_Sun_SetEnableAllowed(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetEnableAllowed(b);
  }
}

RH_C_FUNCTION void ON_Sun_SetEnableOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetEnableOn(b);
  }
}

RH_C_FUNCTION void ON_Sun_SetManualControlAllowed(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetManualControlAllowed(b);
  }
}

RH_C_FUNCTION void ON_Sun_SetManualControlOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetManualControlOn(b);
  }
}

RH_C_FUNCTION void ON_Sun_SetNorth(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetNorth(d);
  }
}

RH_C_FUNCTION void ON_Sun_SetVector(ONX_Model* ptrModel, const ON_3dVector* vec)
{
  if ((nullptr != ptrModel) && (nullptr != vec))
  {
    //ptrModel->m_settings.m_RenderSettings.Sun().SetVector(*vec);
  }
}

RH_C_FUNCTION void ON_Sun_SetAzimuth(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetAzimuth(d);
  }
}

RH_C_FUNCTION void ON_Sun_SetAltitude(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetAltitude(d);
  }
}

RH_C_FUNCTION void ON_Sun_SetLatitude(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetLatitude(d);
  }
}

RH_C_FUNCTION void ON_Sun_SetLongitude(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetLongitude(d);
  }
}

RH_C_FUNCTION void ON_Sun_SetTimeZone(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetTimeZone(d);
  }
}

RH_C_FUNCTION void ON_Sun_SetDaylightSavingOn(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetDaylightSavingOn(b);
  }
}

RH_C_FUNCTION void ON_Sun_SetDaylightSavingMinutes(ONX_Model* ptrModel, int m)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetDaylightSavingMinutes(m);
  }
}

RH_C_FUNCTION void ON_Sun_SetLocalDateTime(ONX_Model* ptrModel, int y, int m, int d, double h)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetLocalDateTime(y, m, d, h);
  }
}

RH_C_FUNCTION void ON_Sun_SetShadowIntensity(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetShadowIntensity(d);
  }
}

RH_C_FUNCTION void ON_Sun_SetIntensity(ONX_Model* ptrModel, double d)
{
  if (nullptr != ptrModel)
  {
    ptrModel->m_settings.m_RenderSettings.Sun().SetIntensity(d);
  }
}

RH_C_FUNCTION bool ON_Sun_GetLight(const ONX_Model* ptrModel, ON_Light* light)
{
  if ((nullptr == ptrModel) || (nullptr == light))
    return false;

  *light = ptrModel->m_settings.m_RenderSettings.Sun().Light();

  return true;
}

RH_C_FUNCTION int ON_Sun_GetSunColorFromAltitude(double altitude)
{
  return ON_Color(ON_Sun::SunColorFromAltitude(altitude)).WindowsRGB();
}

RH_C_FUNCTION unsigned int ON_Sun_GetDataCRC(const ONX_Model* ptrModel, unsigned int current_remainder)
{
  if (nullptr == ptrModel)
    return current_remainder;

  return ptrModel->m_settings.m_RenderSettings.Sun().DataCRC(current_remainder);
}
