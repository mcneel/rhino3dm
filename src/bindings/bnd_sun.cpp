
#include "bindings.h"

BND_File3dmSun::BND_File3dmSun()
{
  _sun = new ON_Sun;
  _owned = true;
}

BND_File3dmSun::BND_File3dmSun(const BND_File3dmSun& sun)
{
  _sun = new ON_Sun(*sun._sun);
  _owned = true;
}

BND_File3dmSun::BND_File3dmSun(ON_Sun* s)
: _sun(s)
{
}

int BND_File3dmSun::GetYear() const
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  return y;
}

void BND_File3dmSun::SetYear(int v)
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  _sun->SetLocalDateTime(v, m, d, h);
}

int BND_File3dmSun::GetMonth() const
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  return m;
}

void BND_File3dmSun::SetMonth(int v)
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  _sun->SetLocalDateTime(y, v, d, h);
}

int BND_File3dmSun::GetDay() const
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  return d;
}

void BND_File3dmSun::SetDay(int v)
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  _sun->SetLocalDateTime(y, m, v, h);
}

double BND_File3dmSun::GetHours() const
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  return h;
}

void BND_File3dmSun::SetHours(double v)
{
  int y = 0, m = 0, d = 0; double h = 0.0;
  _sun->LocalDateTime(y, m, d, h);
  _sun->SetLocalDateTime(y, m, d, v);
}

BND_Light BND_File3dmSun::GetLight() const
{
  auto* light = new ON_Light(_sun->Light());
  return BND_Light(light, nullptr);
}

BND_Color BND_File3dmSun::GetSunColorFromAltitude(double v) // Static.
{
  const auto c = ON_Sun::SunColorFromAltitude(v);

  const int r = int(255.5 * c.Red());
  const int g = int(255.5 * c.Green());
  const int b = int(255.5 * c.Blue());

  const ON_Color col(r, g, b);
  return ON_Color_to_Binding(col);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSunBindings(pybind11::module& m)
{
  py::class_<BND_File3dmSun>(m, "Sun")
    .def(py::init<>())
    .def(py::init<const BND_File3dmSun&>(), py::arg("other"))
    .def_property_readonly("MinYear", &BND_File3dmSun::GetMinYear)
    .def_property_readonly("MaxYear", &BND_File3dmSun::GetMaxYear)
    .def_property_readonly("Vector", &BND_File3dmSun::GetVector)
    .def_property("EnableAllowed", &BND_File3dmSun::GetEnableAllowed, &BND_File3dmSun::SetEnableAllowed)
    .def_property("EnableOn", &BND_File3dmSun::GetEnableOn, &BND_File3dmSun::SetEnableOn)
    .def_property("ManualControlAllowed", &BND_File3dmSun::GetManualControlAllowed, &BND_File3dmSun::SetManualControlAllowed)
    .def_property("ManualControlOn", &BND_File3dmSun::GetManualControlOn, &BND_File3dmSun::SetManualControlOn)
    .def_property("North", &BND_File3dmSun::GetNorth, &BND_File3dmSun::SetNorth)
    .def_property("Azimuth", &BND_File3dmSun::GetAzimuth, &BND_File3dmSun::SetAzimuth)
    .def_property("Altitude", &BND_File3dmSun::GetAltitude, &BND_File3dmSun::SetAltitude)
    .def_property("Latitude", &BND_File3dmSun::GetLatitude, &BND_File3dmSun::SetLatitude)
    .def_property("Longitude", &BND_File3dmSun::GetLongitude, &BND_File3dmSun::SetLongitude)
    .def_property("TimeZone", &BND_File3dmSun::GetTimeZone, &BND_File3dmSun::SetTimeZone)
    .def_property("DaylightSavingOn", &BND_File3dmSun::GetDaylightSavingOn, &BND_File3dmSun::SetDaylightSavingOn)
    .def_property("DaylightSavingMinutes", &BND_File3dmSun::GetDaylightSavingMinutes, &BND_File3dmSun::SetDaylightSavingMinutes)
    .def_property("Year", &BND_File3dmSun::GetYear, &BND_File3dmSun::SetYear)
    .def_property("Month", &BND_File3dmSun::GetMonth, &BND_File3dmSun::SetMonth)
    .def_property("Day", &BND_File3dmSun::GetDay, &BND_File3dmSun::SetDay)
    .def_property("Hours", &BND_File3dmSun::GetHours, &BND_File3dmSun::SetHours)
    .def_property("Intensity", &BND_File3dmSun::GetIntensity, &BND_File3dmSun::SetIntensity)
    .def_property("ShadowIntensity", &BND_File3dmSun::GetShadowIntensity, &BND_File3dmSun::SetShadowIntensity)
    .def_property_readonly("IsValid", &BND_File3dmSun::GetIsValid)
    .def_property_readonly("Light", &BND_File3dmSun::GetLight)
    .def_static("SunColorFromAltitude", &BND_File3dmSun::GetSunColorFromAltitude)
   ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSunBindings(void*)
{
  class_<BND_File3dmSun>("Sun")
    .constructor<>()
    //.constructor<const BND_File3dmSun&>()
    .property("minYear", &BND_File3dmSun::GetMinYear)
    .property("maxYear", &BND_File3dmSun::GetMaxYear)
    .property("vector", &BND_File3dmSun::GetVector)
    .property("enableAllowed", &BND_File3dmSun::GetEnableAllowed, &BND_File3dmSun::SetEnableAllowed)
    .property("enableOn", &BND_File3dmSun::GetEnableOn, &BND_File3dmSun::SetEnableOn)
    .property("manualControlAllowed", &BND_File3dmSun::GetManualControlAllowed, &BND_File3dmSun::SetManualControlAllowed)
    .property("manualControlOn", &BND_File3dmSun::GetManualControlOn, &BND_File3dmSun::SetManualControlOn)
    .property("north", &BND_File3dmSun::GetNorth, &BND_File3dmSun::SetNorth)
    .property("azimuth", &BND_File3dmSun::GetAzimuth, &BND_File3dmSun::SetAzimuth)
    .property("altitude", &BND_File3dmSun::GetAltitude, &BND_File3dmSun::SetAltitude)
    .property("latitude", &BND_File3dmSun::GetLatitude, &BND_File3dmSun::SetLatitude)
    .property("longitude", &BND_File3dmSun::GetLongitude, &BND_File3dmSun::SetLongitude)
    .property("timeZone", &BND_File3dmSun::GetTimeZone, &BND_File3dmSun::SetTimeZone)
    .property("daylightSavingOn", &BND_File3dmSun::GetDaylightSavingOn, &BND_File3dmSun::SetDaylightSavingOn)
    .property("daylightSavingMinutes", &BND_File3dmSun::GetDaylightSavingMinutes, &BND_File3dmSun::SetDaylightSavingMinutes)
    .property("year", &BND_File3dmSun::GetYear, &BND_File3dmSun::SetYear)
    .property("month", &BND_File3dmSun::GetMonth, &BND_File3dmSun::SetMonth)
    .property("day", &BND_File3dmSun::GetDay, &BND_File3dmSun::SetDay)
    .property("hours", &BND_File3dmSun::GetHours, &BND_File3dmSun::SetHours)
    .property("intensity", &BND_File3dmSun::GetIntensity, &BND_File3dmSun::SetIntensity)
    .property("shadowIntensity", &BND_File3dmSun::GetShadowIntensity, &BND_File3dmSun::SetShadowIntensity)
    .property("isValid", &BND_File3dmSun::GetIsValid)
    .property("light", &BND_File3dmSun::GetLight)
    .class_function("sunColorFromAltitude", &BND_File3dmSun::GetSunColorFromAltitude)
    ;
}
#endif
