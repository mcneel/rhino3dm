
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSunBindings(pybind11::module& m);
#else
void initSunBindings(void* m);
#endif

class BND_File3dmSun
{
public:
  ON_Sun* m_sun = nullptr;

protected:
  void SetTrackedPointer(ON_Sun* sun) { m_sun = sun; }

public:
  BND_File3dmSun();
  BND_File3dmSun(ON_Sun* sun);

  int GetMinYear(void) { return ON_Sun::MinYear(); }
  int GetMaxYear(void) { return ON_Sun::MaxYear(); }

  bool GetEnableAllowed(void) const { return m_sun->EnableAllowed(); }
  void SetEnableAllowed(bool v) const { return m_sun->SetEnableAllowed(v); }

  bool GetEnableOn(void) const { return m_sun->EnableOn(); }
  void SetEnableOn(bool v) const { return m_sun->SetEnableOn(v); }

  bool GetManualControlAllowed(void) const { return m_sun->ManualControlAllowed(); }
  void SetManualControlAllowed(bool v) const { return m_sun->SetManualControlAllowed(v); }

  bool GetManualControlOn(void) const { return m_sun->ManualControlOn(); }
  void SetManualControlOn(bool v) const { return m_sun->SetManualControlOn(v); }

  double GetNorth(void) const { return m_sun->North(); }
  void SetNorth(double v) const { return m_sun->SetNorth(v); }

  ON_3dVector GetVector(void) const { return m_sun->Vector(); }
  void SetVector(const ON_3dVector& v) const { return m_sun->SetVector(v); }

  double GetAzimuth(void) const { return m_sun->Azimuth(); }
  void SetAzimuth(double v) const { return m_sun->SetAzimuth(v); }

  double GetAltitude(void) const { return m_sun->Altitude(); }
  void SetAltitude(double v) const { return m_sun->SetAltitude(v); }

  double GetLatitude(void) const { return m_sun->Latitude(); }
  void SetLatitude(double v) const { return m_sun->SetLatitude(v); }

  double GetLongitude(void) const { return m_sun->Longitude(); }
  void SetLongitude(double v) const { return m_sun->SetLongitude(v); }

  double GetTimeZone(void) const { return m_sun->TimeZone(); }
  void SetTimeZone(double v) const { return m_sun->SetTimeZone(v); }

  bool GetDaylightSavingOn(void) const { return m_sun->DaylightSavingOn(); }
  void SetDaylightSavingOn(bool v) const { return m_sun->SetDaylightSavingOn(v); }

  int GetDaylightSavingMinutes(void) const { return m_sun->DaylightSavingMinutes(); }
  void SetDaylightSavingMinutes(int v) const { return m_sun->SetDaylightSavingMinutes(v); }

  int  GetYear(void) const;
  void SetYear(int v) const;

  int  GetMonth(void) const;
  void SetMonth(int v) const;

  int  GetDay(void) const;
  void SetDay(int v) const;

  double GetHours(void) const;
  void SetHours(double v) const;

  double GetIntensity(void) const { return m_sun->Intensity(); }
  void SetIntensity(double v) const { return m_sun->SetIntensity(v); }

  double GetShadowIntensity(void) const { return m_sun->ShadowIntensity(); }
  void SetShadowIntensity(double v) const { return m_sun->SetShadowIntensity(v); }

  bool GetIsValid(void) const { return m_sun->IsValid(); }

  BND_Light GetLight(void) const;

  static BND_Color GetSunColorFromAltitude(double v);
};
