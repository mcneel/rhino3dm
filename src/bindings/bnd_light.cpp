#include "bindings.h"

BND_Light::BND_Light()
{
  SetTrackedPointer(new ON_Light(), nullptr);
}

BND_Light::BND_Light(ON_Light* light, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(light, compref);
}


void BND_Light::SetTrackedPointer(ON_Light* light, const ON_ModelComponentReference* compref)
{
  m_light = light;
  BND_GeometryBase::SetTrackedPointer(light, compref);
}

BND_TUPLE BND_Light:: GetSpotLightRadii() const
{
  bool success = false;
  double* inner_radius = 0;
  double* outer_radius = 0;
  success = m_light->GetSpotLightRadii(inner_radius, outer_radius);
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, inner_radius);
  SetTuple(rc, 2, outer_radius);
  return rc;
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLightBindings(pybind11::module& m)
{
  py::class_<BND_Light, BND_GeometryBase>(m, "Light")
    .def(py::init<>())
    .def_property("IsEnabled", &BND_Light::IsEnabled, &BND_Light::SetEnabled)
    .def_property("LightStyle", &BND_Light::GetLightStyle, &BND_Light::SetLightStyle)
    .def_property_readonly("IsPointLight", &BND_Light::IsPointLight)
    .def_property_readonly("IsDirectionalLight", &BND_Light::IsDirectionalLight)
    .def_property_readonly("IsSpotLight", &BND_Light::IsSpotLight)
    .def_property_readonly("IsLinearLight", &BND_Light::IsLinearLight)
    .def_property_readonly("IsRectangularLight", &BND_Light::IsRectangularLight)
    .def_property("Location", &BND_Light::GetLocation, &BND_Light::SetLocation)
    .def_property("Direction", &BND_Light::GetDirection, &BND_Light::SetDirection)
    .def_property_readonly("PerpendicularDirection", &BND_Light::GetPerpendicularDirection)
    .def_property("Intensity", &BND_Light::GetIntensity, &BND_Light::SetIntensity)
    .def_property("PowerWatts", &BND_Light::GetPowerWatts, &BND_Light::SetPowerWatts)
    .def_property("PowerLumens", &BND_Light::GetPowerLumens, &BND_Light::SetPowerLumens)
    .def_property("PowerCandela", &BND_Light::GetPowerCandela, &BND_Light::SetPowerCandela)
    .def_property("Ambient", &BND_Light::GetAmbient, &BND_Light::SetAmbient)
    .def_property("Diffuse", &BND_Light::GetDiffuse, &BND_Light::SetDiffuse)
    .def_property("Specular", &BND_Light::GetSpecular, &BND_Light::SetSpecular)
    .def("SetAttenuation", &BND_Light::SetAttenuation, py::arg("a0"), py::arg("a1"), py::arg("a2"))
    .def_property("AttenuationVector", &BND_Light::GetAttenuationVector, &BND_Light::SetAttenuationVector)
    .def("GetAttenuation", &BND_Light::GetAttenuation, py::arg("d"))
    .def_property("SpotAngleRadians", &BND_Light::GetSpotAngleRadians, &BND_Light::SetSpotAngleRadians)
    .def_property("SpotExponent", &BND_Light::GetSpotExponent, &BND_Light::SetSpotExponent)
    .def("GetSpotLightRadii", &BND_Light::GetSpotLightRadii)
    .def_property("HotSpot", &BND_Light::GetHotSpot, &BND_Light::SetHotSpot)
    .def_property("Length", &BND_Light::GetLength, &BND_Light::SetLength)
    .def_property("Width", &BND_Light::GetWidth, &BND_Light::SetWidth)
    .def_property("ShadowIntensity", &BND_Light::GetShadowIntensity, &BND_Light::SetShadowIntensity)
    .def_property("Name", &BND_Light::GetName, &BND_Light::SetName)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initLightBindings(void*)
{
  class_<BND_Light, base<BND_GeometryBase>>("Light")
    .constructor<>()
    .property("isEnabled", &BND_Light::IsEnabled, &BND_Light::SetEnabled)
    .property("lightStyle", &BND_Light::GetLightStyle, &BND_Light::SetLightStyle)
    .property("isPointLight", &BND_Light::IsPointLight)
    .property("isDirectionalLight", &BND_Light::IsDirectionalLight)
    .property("isSpotLight", &BND_Light::IsSpotLight)
    .property("isLinearLight", &BND_Light::IsLinearLight)
    .property("isRectangularLight", &BND_Light::IsRectangularLight)
    .property("location", &BND_Light::GetLocation, &BND_Light::SetLocation)
    .property("direction", &BND_Light::GetDirection, &BND_Light::SetDirection)
    .property("perpendicularDirection", &BND_Light::GetPerpendicularDirection)
    .property("intensity", &BND_Light::GetIntensity, &BND_Light::SetIntensity)
    .property("powerWatts", &BND_Light::GetPowerWatts, &BND_Light::SetPowerWatts)
    .property("powerLumens", &BND_Light::GetPowerLumens, &BND_Light::SetPowerLumens)
    .property("powerCandela", &BND_Light::GetPowerCandela, &BND_Light::SetPowerCandela)
    .property("ambient", &BND_Light::GetAmbient, &BND_Light::SetAmbient)
    .property("diffuse", &BND_Light::GetDiffuse, &BND_Light::SetDiffuse)
    .property("specular", &BND_Light::GetSpecular, &BND_Light::SetSpecular)
    .function("setAttenuation", &BND_Light::SetAttenuation)
    .property("attenuationVector", &BND_Light::GetAttenuationVector, &BND_Light::SetAttenuationVector)
    .function("getAttenuation", &BND_Light::GetAttenuation)
    .property("spotAngleRadians", &BND_Light::GetSpotAngleRadians, &BND_Light::SetSpotAngleRadians)
    .property("spotExponent", &BND_Light::GetSpotExponent, &BND_Light::SetSpotExponent)
    .function("getSpotLightRadii", &BND_Light::GetSpotLightRadii)
    .property("hotSpot", &BND_Light::GetHotSpot, &BND_Light::SetHotSpot)
    .property("length", &BND_Light::GetLength, &BND_Light::SetLength)
    .property("width", &BND_Light::GetWidth, &BND_Light::SetWidth)
    .property("shadowIntensity", &BND_Light::GetShadowIntensity, &BND_Light::SetShadowIntensity)
    .property("name", &BND_Light::GetName, &BND_Light::SetName)
    ;
  }
#endif
