
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

static double DecimalHoursFromHMS(int iHour, int iMinute, int iSecond)
{
	return iHour + (iMinute / 60.0) + (iSecond / 3600.0);
}

static void DecimalHoursToHMS(double dHours, int& iHour, int& iMinute, int& iSecond)
{
	while (dHours >= 24.0)
		dHours -= 24.0;

	while (dHours < 0.0)
		dHours += 24.0;

	iHour = (int)dHours;

	const double dMinute = (dHours - iHour) * 60.0;
	iMinute = (int)dMinute;

	const double dSecond = (dMinute - iMinute) * 60.0;
	iSecond = (int)dSecond;
}

RH_C_FUNCTION const ON_Sun* ON_3dmRenderSettings_GetSun(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->Sun();
}

RH_C_FUNCTION ON_Sun* ON_3dmRenderSettings_BeginChange_ON_Sun(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).Sun();
}

RH_C_FUNCTION const ON_Sun* ON_Sun_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->Sun();
}

RH_C_FUNCTION const ON_Sun* ON_Sun_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.Sun();
}

RH_C_FUNCTION bool ON_Sun_Enabled(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->EnableOn();
  }
  return false;
}

RH_C_FUNCTION void ON_Sun_SetEnabled(ON_Sun* sun, bool value)
{
  if (sun)
  {
    sun->SetEnableOn(value);
  }
}

RH_C_FUNCTION bool ON_Sun_ManualControlOn(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->ManualControlOn();
  }
  return false;
}

RH_C_FUNCTION void ON_Sun_SetManualControlOn(ON_Sun* sun, bool value)
{
  if (sun)
  {
    sun->SetManualControlOn(value);
  }
}

RH_C_FUNCTION double ON_Sun_North(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->North();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_Sun_SetNorth(ON_Sun* sun, double value)
{
  if (sun)
  {
    sun->SetNorth(value);
  }
}

RH_C_FUNCTION double ON_Sun_Intensity(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->Intensity();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_Sun_SetIntensity(ON_Sun* sun, double value)
{
  if (sun)
  {
    sun->SetIntensity(value);
  }
}

RH_C_FUNCTION void ON_Sun_Vector(const ON_Sun* sun, ON_3dVector* pValue)
{
  if (sun && pValue)
  {
    *pValue = sun->CalculateVectorFromAzimuthAndAltitude();
  }
}

RH_C_FUNCTION void ON_Sun_SetVector(ON_Sun* sun, ON_3DVECTOR_STRUCT value)
{
  if (sun)
  {
    sun->SetAzimuthAndAltitudeFromVector(ON_3dVector(value.val));
  }
}

RH_C_FUNCTION double ON_Sun_Azimuth(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->Azimuth();
  }
  return 0.0;
}

RH_C_FUNCTION double ON_Sun_Altitude(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->Altitude();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_Sun_SetAzimuthAltitude(ON_Sun* sun, double azimuth, double altitude)
{
  if (sun)
  {
    sun->SetManualControlOn(true);
    sun->SetAzimuth(azimuth);
    sun->SetAltitude(altitude);
  }
}

RH_C_FUNCTION double ON_Sun_Latitude(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->Latitude();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_Sun_SetLatitudeLongitude(ON_Sun* sun, double latitude, double longitude)
{
  if (sun == nullptr)
    return;

  sun->SetManualControlOn(false);
  sun->SetLatitude(latitude);
  sun->SetLongitude(longitude);
}

RH_C_FUNCTION double ON_Sun_Longitude(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->Longitude();
  }
  return 0.0;
}

RH_C_FUNCTION double ON_Sun_TimeZone(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->TimeZone();
  }
  return 0.0;
}

RH_C_FUNCTION void ON_Sun_SetTimeZone(ON_Sun* sun, double value)
{
  if (sun)
  {
    sun->SetTimeZone(value);
  }
}

RH_C_FUNCTION bool ON_Sun_DaylightSavingOn(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->DaylightSavingOn();
  }
  return false;
}

RH_C_FUNCTION void ON_Sun_SetDaylightSavingOn(ON_Sun* sun, bool value)
{
  if (sun)
  {
    sun->SetDaylightSavingOn(value);
  }
}

RH_C_FUNCTION int ON_Sun_DaylightSavingMinutes(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->DaylightSavingMinutes();
  }
  return 0;
}

RH_C_FUNCTION void ON_Sun_SetDaylightSavingMinutes(ON_Sun* sun, int value)
{
  if (sun)
  {
    sun->SetDaylightSavingMinutes(value);
  }
}

RH_C_FUNCTION void ON_Sun_DateTime(const ON_Sun* sun, int local, int* y, int* m, int* d, int* h, int* n, int* s)
{
  if (sun == nullptr)
    return;

  if (y && m && d && h && n && s)
  {
    double hours = 0.0;
    if (local != 0)
    {
      sun->LocalDateTime(*y, *m, *d, hours);
    }
    else
    {
      sun->UTCDateTime(*y, *m, *d, hours);
    }

    DecimalHoursToHMS(hours, *h, *n, *s);
  }
}

RH_C_FUNCTION void ON_Sun_SetDateTime(ON_Sun* sun, int local, int y, int m, int d, int h, int n, int s)
{
  if (sun)
  {
    sun->SetManualControlOn(false);

    const auto hours = DecimalHoursFromHMS(h, n, s);
    if (local != 0)
    {
      sun->SetLocalDateTime(y, m, d, hours);
    }
    else
    {
      sun->SetUTCDateTime(y, m, d, hours);
    }
  }
}

RH_C_FUNCTION void ON_Sun_Light(const ON_Sun* sun, ON_Light* light)
{
  if ((sun == nullptr) || (light == nullptr))
    return;
  
  *light = sun->Light();
}

RH_C_FUNCTION unsigned int ON_Sun_CRC(const ON_Sun* sun)
{
  if (nullptr == sun)
    return 0;

  return sun->DataCRC(0);
}

RH_C_FUNCTION int ON_Sun_ColorFromAltitude(double altitude)
{
  ON_4fColor col = ON_Sun::SunColorFromAltitude(altitude);

  // JohnC: I don't understand why the saturation was modified. SunColorFromAltitude() gives
  // you the sun color -- and that's the color you're supposed to use. What am I missing?
  // If we really need this, we should add it to SunColorFromAltitude().
  //const double sat = col.Saturation() * 2.0;
  //col.SetHSV(col.Hue(), min(1.0, sat), col.Value());

  return int((ON_Color)col);
}

RH_C_FUNCTION unsigned int ON_Sun_GetDataCRC(const ON_Sun* sun)
{
  if (sun)
  {
    return sun->DataCRC(0);
  }
  return false;
}

RH_C_FUNCTION ON_Sun* ON_Sun_New()
{
  // Return an ON_SunEx so we get the automatic azimuth and altitude calculations.
  return new ON_SunEx;
}

RH_C_FUNCTION void ON_Sun_Delete(ON_Sun* p)
{
  delete p;
}

RH_C_FUNCTION void ON_Sun_CopyFrom(ON_Sun* target, const ON_Sun* source)
{
  if (target && source)
  {
    *target = *source;
  }
}
