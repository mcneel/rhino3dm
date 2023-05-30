
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
  //bool _owned = false;

public:
  BND_File3dmSun() = default;
  BND_File3dmSun(ON_Sun* s);
  //BND_File3dmSun(ON_Sun* s) : _sun(s) {}
  //BND_File3dmSun() { _sun = new ON_Sun; _owned = true; }
  //BND_File3dmSun(const BND_File3dmSun& sun) { _sun = new ON_Sun(*sun._sun); _owned = true; }
  //
  //~BND_File3dmSun() { if (_owned) delete _sun; }

  int GetMinYear() const { return ON_Sun::MinYear(); }
  int GetMaxYear() const { return ON_Sun::MaxYear(); }

  bool GetEnableAllowed() const { return _sun->EnableAllowed(); }
  void SetEnableAllowed(bool v) { return _sun->SetEnableAllowed(v); }

  bool GetEnableOn() const { return _sun->EnableOn(); }
  void SetEnableOn(bool v) { return _sun->SetEnableOn(v); }

  bool GetManualControlAllowed() const { return _sun->ManualControlAllowed(); }
  void SetManualControlAllowed(bool v) { return _sun->SetManualControlAllowed(v); }

  bool GetManualControlOn() const { return _sun->ManualControlOn(); }
  void SetManualControlOn(bool v) { return _sun->SetManualControlOn(v); }

  double GetNorth() const { return _sun->North(); }
  void SetNorth(double v) { return _sun->SetNorth(v); }

  double GetAzimuth() const { return _sun->Azimuth(); }
  void SetAzimuth(double v) { return _sun->SetAzimuth(v); }

  double GetAltitude() const { return _sun->Altitude(); }
  void SetAltitude(double v) { return _sun->SetAltitude(v); }

  double GetLatitude() const { return _sun->Latitude(); }
  void SetLatitude(double v) { return _sun->SetLatitude(v); }

  double GetLongitude() const { return _sun->Longitude(); }
  void SetLongitude(double v) { return _sun->SetLongitude(v); }

  double GetTimeZone() const { return _sun->TimeZone(); }
  void SetTimeZone(double v) { return _sun->SetTimeZone(v); }

  bool GetDaylightSavingOn() const { return _sun->DaylightSavingOn(); }
  void SetDaylightSavingOn(bool v) { return _sun->SetDaylightSavingOn(v); }

  int GetDaylightSavingMinutes() const { return _sun->DaylightSavingMinutes(); }
  void SetDaylightSavingMinutes(int v) { return _sun->SetDaylightSavingMinutes(v); }

  int  GetYear() const;
  void SetYear(int v);

  int  GetMonth() const;
  void SetMonth(int v);

  int  GetDay() const;
  void SetDay(int v);

  double GetHours() const;
  void SetHours(double v);

  double GetIntensity() const { return _sun->Intensity(); }
  void SetIntensity(double v) { return _sun->SetIntensity(v); }

  double GetShadowIntensity() const { return _sun->ShadowIntensity(); }
  void SetShadowIntensity(double v) { return _sun->SetShadowIntensity(v); }

  bool GetIsValid() const { return _sun->IsValid(); }

  BND_Light GetLight() const;

  ON_3dVector GetVector() const { return _sun->CalculateVectorFromAzimuthAndAltitude(); }

  static BND_Color GetSunColorFromAltitude(double v);
};
