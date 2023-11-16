
#include "stdafx.h"

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

static time_t TimeFromYMDH(int y, int m, int d, double dh)
{
  int h = 0, n = 0, s = 0;
  DecimalHoursToHMS(dh, h, n, s);

  tm ttm = { 0 };
  ttm.tm_sec = s;
  ttm.tm_min = n;
  ttm.tm_hour = h;
  ttm.tm_mday = d;
  ttm.tm_mon = m - 1;
  ttm.tm_year = y - 1900;
  ttm.tm_isdst = -1;
  return mktime(&ttm);
}

static void GetYMDH(const ON_XMLVariant& v, int& y, int& m, int& d, double& h)
{
  const time_t time = v.AsTime();

  tm ttm = { 0 };

#ifdef ON_RUNTIME_WIN
  _localtime64_s(&ttm, &time);
#else
  ttm = *localtime(&time);
#endif

  y = ttm.tm_year + 1900;
  m = ttm.tm_mon + 1;
  d = ttm.tm_mday;
  h = DecimalHoursFromHMS(ttm.tm_hour, ttm.tm_min, ttm.tm_sec);
}

static time_t TimeFromSun(const ON_Sun& sun, bool local)
{
  int y = 0, m = 0, d = 0; double dh = 0.0;

  if (local) sun.LocalDateTime(y, m, d, dh);
  else       sun.UTCDateTime  (y, m, d, dh);

  return TimeFromYMDH(y, m, d, dh);
}

enum class SunSetting : int
{
  Accuracy,
  EnableAllowed,
  EnableOn,
  ManualControlAllowed,
  ManualControlOn,
  North,
  Azimuth,
  Altitude,
  Latitude,
  Longitude,
  TimeZone,
  DaylightSavingOn,
  DaylightSavingMinutes,
  Intensity,
  ShadowIntensity, // ShadowIntensity is currently unused. See [SHADOW_INTENSITY_UNUSED]
  Vector,
  LocalDateTime,
  UTCDateTime,
};

RH_C_FUNCTION void ON_Sun_GetValue(const ON_Sun* sun, SunSetting which, ON_XMLVariant* v)
{
  if (sun && v)
  {
    switch (which)
    {
    case SunSetting::Accuracy             : *v = int(sun->Accuracy());         break;
    case SunSetting::EnableAllowed        : *v = sun->EnableAllowed();         break;
    case SunSetting::EnableOn             : *v = sun->EnableOn();              break;
    case SunSetting::ManualControlAllowed : *v = sun->ManualControlAllowed();  break;
    case SunSetting::ManualControlOn      : *v = sun->ManualControlOn();       break;
    case SunSetting::North                : *v = sun->North();                 break;
    case SunSetting::Azimuth              : *v = sun->Azimuth();               break;
    case SunSetting::Altitude             : *v = sun->Altitude();              break;
    case SunSetting::Latitude             : *v = sun->Latitude();              break;
    case SunSetting::Longitude            : *v = sun->Longitude();             break;
    case SunSetting::TimeZone             : *v = sun->TimeZone();              break;
    case SunSetting::DaylightSavingOn     : *v = sun->DaylightSavingOn();      break;
    case SunSetting::DaylightSavingMinutes: *v = sun->DaylightSavingMinutes(); break;
    case SunSetting::Intensity            : *v = sun->Intensity();             break;
    case SunSetting::ShadowIntensity      : *v = sun->ShadowIntensity();       break;
    case SunSetting::LocalDateTime        : *v = TimeFromSun(*sun, true);      break;
    case SunSetting::UTCDateTime          : *v = TimeFromSun(*sun, false);     break;
    case SunSetting::Vector               : *v = ON_3dPoint(sun->CalculateVectorFromAzimuthAndAltitude()); break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_Sun_SetValue(ON_Sun* sun, SunSetting which, const ON_XMLVariant* v)
{
  if (sun && v)
  {
    int y = 0, m = 0, d = 0; double h = 0.0;

    switch (which)
    {
    case SunSetting::Accuracy             : sun->SetAccuracy(ON_SunEngine::Accuracy(v->AsInteger())); break;
    case SunSetting::EnableAllowed        : sun->SetEnableAllowed(v->AsBool());                       break;
    case SunSetting::EnableOn             : sun->SetEnableOn(v->AsBool());                            break;
    case SunSetting::ManualControlAllowed : sun->SetManualControlAllowed(v->AsBool());                break;
    case SunSetting::ManualControlOn      : sun->SetManualControlOn(v->AsBool());                     break;
    case SunSetting::North                : sun->SetNorth(v->AsDouble());                             break;
    case SunSetting::Azimuth              : sun->SetAzimuth(v->AsDouble());                           break;
    case SunSetting::Altitude             : sun->SetAltitude(v->AsDouble());                          break;
    case SunSetting::Latitude             : sun->SetLatitude(v->AsDouble());                          break;
    case SunSetting::Longitude            : sun->SetLongitude(v->AsDouble());                         break;
    case SunSetting::TimeZone             : sun->SetTimeZone(v->AsDouble());                          break;
    case SunSetting::DaylightSavingOn     : sun->SetDaylightSavingOn(v->AsBool());                    break;
    case SunSetting::DaylightSavingMinutes: sun->SetDaylightSavingMinutes(v->AsInteger());            break;
    case SunSetting::Intensity            : sun->SetIntensity(v->AsDouble());                         break;
    case SunSetting::ShadowIntensity      : sun->SetShadowIntensity(v->AsDouble());                   break;
    case SunSetting::Vector               : sun->SetAzimuthAndAltitudeFromVector(v->As3dPoint());     break;
    case SunSetting::LocalDateTime: GetYMDH(*v, y, m, d, h); sun->SetLocalDateTime(y, m, d, h);       break;
    case SunSetting::UTCDateTime:   GetYMDH(*v, y, m, d, h); sun->SetUTCDateTime  (y, m, d, h);       break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_Sun_SetValue(ON_3dmRenderSettings* rs, SunSetting which, const ON_XMLVariant* v)
{
  if (nullptr != rs)
  {
    ON_Sun_SetValue(&rs->Sun(), which, v);
  }
}

RH_C_FUNCTION const ON_Sun* ON_Sun_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.Sun();
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
  return new ON_Sun;
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

RH_C_FUNCTION double GetSunAltitude(double latitude, double longitude, double timeZoneHours,
                     int daylightMinutes, int year, int month, int day, double hours, bool fast)
{
	const auto acc = fast ? ON_SunEngine::Accuracy::Minimum : ON_SunEngine::Accuracy::Maximum;

	ON_SunEngine engine(acc);
	engine.SetLongitude(longitude);
	engine.SetLatitude(latitude);
	engine.SetTimeZoneHours(timeZoneHours);
	engine.SetDaylightSavingMinutes(daylightMinutes);
	engine.SetLocalDateTime(year, month, day, hours);

	return engine.Altitude();
}

RH_C_FUNCTION double GetSunJulianDay(double timeZoneHours, int daylightMinutes,
                                     int year, int month, int day, double hours)
{
	ON_SunEngine engine(ON_SunEngine::Accuracy::Minimum);
	engine.SetLocalDateTime(year, month, day, hours);
	engine.SetTimeZoneHours(timeZoneHours);
	engine.SetDaylightSavingMinutes(daylightMinutes);

	return engine.JulianDay();
}
