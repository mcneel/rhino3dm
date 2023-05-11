
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initSunBindings(pybind11::module& m);
#else
void initSunBindings(void* m);
#endif

class BND_File3dmSun
{
private:
  ON_Sun* _sun = nullptr;
  bool _owned = false;

public:
  BND_File3dmSun() { _sun = new ON_Sun; _owned = true; }
  BND_File3dmSun(const BND_File3dmSun& sun) { _sun = new ON_Sun(*sun._sun); _owned = true; }
  BND_File3dmSun(ON_Sun* s) : _sun(s) { }
  ~BND_File3dmSun() { if (_owned) delete _sun; }

  int GetMinYear(void) { return ON_Sun::MinYear(); }
  int GetMaxYear(void) { return ON_Sun::MaxYear(); }

  bool GetEnableAllowed(void) const { return _sun->EnableAllowed(); }
  void SetEnableAllowed(bool v) const { return _sun->SetEnableAllowed(v); }

  bool GetEnableOn(void) const { return _sun->EnableOn(); }
  void SetEnableOn(bool v) const { return _sun->SetEnableOn(v); }

  bool GetManualControlAllowed(void) const { return _sun->ManualControlAllowed(); }
  void SetManualControlAllowed(bool v) const { return _sun->SetManualControlAllowed(v); }

  bool GetManualControlOn(void) const { return _sun->ManualControlOn(); }
  void SetManualControlOn(bool v) const { return _sun->SetManualControlOn(v); }

  double GetNorth(void) const { return _sun->North(); }
  void SetNorth(double v) const { return _sun->SetNorth(v); }

  double GetAzimuth(void) const { return _sun->Azimuth(); }
  void SetAzimuth(double v) const { return _sun->SetAzimuth(v); }

  double GetAltitude(void) const { return _sun->Altitude(); }
  void SetAltitude(double v) const { return _sun->SetAltitude(v); }

  double GetLatitude(void) const { return _sun->Latitude(); }
  void SetLatitude(double v) const { return _sun->SetLatitude(v); }

  double GetLongitude(void) const { return _sun->Longitude(); }
  void SetLongitude(double v) const { return _sun->SetLongitude(v); }

  double GetTimeZone(void) const { return _sun->TimeZone(); }
  void SetTimeZone(double v) const { return _sun->SetTimeZone(v); }

  bool GetDaylightSavingOn(void) const { return _sun->DaylightSavingOn(); }
  void SetDaylightSavingOn(bool v) const { return _sun->SetDaylightSavingOn(v); }

  int GetDaylightSavingMinutes(void) const { return _sun->DaylightSavingMinutes(); }
  void SetDaylightSavingMinutes(int v) const { return _sun->SetDaylightSavingMinutes(v); }

  int  GetYear(void) const;
  void SetYear(int v) const;

  int  GetMonth(void) const;
  void SetMonth(int v) const;

  int  GetDay(void) const;
  void SetDay(int v) const;

  double GetHours(void) const;
  void SetHours(double v) const;

  double GetIntensity(void) const { return _sun->Intensity(); }
  void SetIntensity(double v) const { return _sun->SetIntensity(v); }

  double GetShadowIntensity(void) const { return _sun->ShadowIntensity(); }
  void SetShadowIntensity(double v) const { return _sun->SetShadowIntensity(v); }

  bool GetIsValid(void) const { return _sun->IsValid(); }

  BND_Light GetLight(void) const;

  ON_3dVector GetVector(void) const { return _sun->CalculateVectorFromAzimuthAndAltitude(); }

  static BND_Color GetSunColorFromAltitude(double v);
};
