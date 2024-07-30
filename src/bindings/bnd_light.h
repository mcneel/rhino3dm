#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initLightBindings(py::module_& m);
#else
namespace py = pybind11;
void initLightBindings(py::module& m);
#endif

#else
void initLightBindings(void* m);
#endif

class BND_Light : public BND_GeometryBase
{
  ON_Light* m_light = nullptr;
public:
  BND_Light();
  BND_Light(ON_Light* light, const ON_ModelComponentReference* compref);

protected:
  void SetTrackedPointer(ON_Light* light, const ON_ModelComponentReference* compref);

public:
  bool IsEnabled() const { return m_light->IsEnabled(); }
  void SetEnabled(bool on) { m_light->Enable(on); }
  ON::light_style GetLightStyle() const { return m_light->Style(); }
  void SetLightStyle(ON::light_style ls) { m_light->SetStyle(ls); }
  bool IsPointLight() const { return m_light->IsPointLight(); }
  bool IsDirectionalLight() const { return m_light->IsDirectionalLight(); }
  bool IsSpotLight() const { return m_light->IsSpotLight(); }
  bool IsLinearLight() const { return m_light->IsLinearLight(); }
  bool IsRectangularLight() const { return m_light->IsRectangularLight(); }
  //    public DocObjects.CoordinateSystem CoordinateSystem {get;}
  ON_3dPoint GetLocation() const { return m_light->m_location; }
  void SetLocation(ON_3dPoint p) { m_light->m_location = p; }
  ON_3dVector GetDirection() const { return m_light->m_direction; }
  void SetDirection(ON_3dVector v) { m_light->m_direction = v; }
  ON_3dVector GetPerpendicularDirection() const { return m_light->PerpindicularDirection(); }
  double GetIntensity() const { return m_light->m_intensity; }
  void SetIntensity(double i) { m_light->m_intensity = i; }
  double GetPowerWatts() const { return m_light->m_watts; }
  void SetPowerWatts(double w) { m_light->m_watts = w; }
  double GetPowerLumens() const { return m_light->PowerLumens(); }
  void SetPowerLumens(double pl) { m_light->SetPowerLumens(pl); }
  double GetPowerCandela() const { return m_light->PowerCandela(); }
  void SetPowerCandela(double pc) { m_light->SetPowerCandela(pc); }
  BND_Color GetAmbient() const { return ON_Color_to_Binding(m_light->Ambient()); }
  void SetAmbient(BND_Color c) { m_light->SetAmbient(Binding_to_ON_Color(c)); }
  BND_Color GetDiffuse() const { return ON_Color_to_Binding(m_light->Diffuse()); }
  void SetDiffuse(BND_Color c) { m_light->SetDiffuse(Binding_to_ON_Color(c)); }
  BND_Color GetSpecular() const { return ON_Color_to_Binding(m_light->Specular()); }
  void SetSpecular(BND_Color c) { m_light->SetSpecular(Binding_to_ON_Color(c)); }
  void SetAttenuation(double a0, double a1, double a2) { m_light->SetAttenuation(a0, a1, a2); }
  ON_3dVector GetAttenuationVector() const { return m_light->m_attenuation; }
  void SetAttenuationVector(ON_3dVector v) { m_light->m_attenuation = v; }
  double GetAttenuation(double d) const { return m_light->Attenuation(d); }
  double GetSpotAngleRadians() const { return m_light->SpotAngleRadians(); }
  void SetSpotAngleRadians(double sa) { m_light->SetSpotAngleRadians(sa); }
  double GetSpotExponent() const { return m_light->SpotExponent(); }
  void SetSpotExponent(double se) { m_light->SetSpotExponent(se); }
  double GetHotSpot() const { return m_light->HotSpot(); }
  void SetHotSpot(double hs) { m_light->SetHotSpot(hs); }
  BND_TUPLE GetSpotLightRadii() const;
  ON_3dVector GetLength() const { return m_light->Length(); }
  void SetLength(ON_3dVector l) { m_light->SetLength(l); }
  ON_3dVector GetWidth() const { return m_light->Width(); }
  void SetWidth(ON_3dVector w) { m_light->SetWidth(w); }
  double GetShadowIntensity() const { return m_light->ShadowIntensity(); }
  void SetShadowIntensity(double si) { m_light->SetShadowIntensity(si); }
  std::wstring GetName() const { return std::wstring(m_light->m_light_name); }
  void SetName(std::wstring s) { m_light->SetLightName(s.c_str()); }

  //     public Guid Id {get; set;}
  //    public static readonly Vector3d ConstantAttenuationVector = new Vector3d(1, 0, 0);
//    public static readonly Vector3d LinearAttenuationVector = new Vector3d(0, 1, 0);
//    public static readonly Vector3d InverseSquaredAttenuationVector = new Vector3d(0, 0, 1);
  //    public enum Attenuation {
  //    public Attenuation AttenuationType {get;set;}
};
