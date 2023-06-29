#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initMaterialBindings(pybind11::module& m);
#else
void initMaterialBindings(void* m);
#endif

class BND_Material : public BND_ModelComponent
{
public:
  ON_Material* m_material = nullptr;
protected:
  void SetTrackedPointer(ON_Material* material, const ON_ModelComponentReference* compref);

public:
  BND_Material();
  BND_Material(const BND_Material& other);
  BND_Material(ON_Material* material, const ON_ModelComponentReference* compref);

  static int CompareAppearance(const BND_Material& mat1, const BND_Material& mat2);

  //public void CopyFrom(Material other)
  BND_UUID GetRenderPlugInId() const { return ON_UUID_to_Binding(m_material->MaterialPlugInId()); }
  void SetRenderPlugInId(const BND_UUID& id) { m_material->SetMaterialPlugInId(Binding_to_ON_UUID(id)); }
  int GetMaterialIndex() const { return m_material->Index(); }
  void SetMaterialIndex(int i) { m_material->SetIndex(i); }
  std::wstring GetName() const { return std::wstring(m_material->NameAsPointer()); }
  void SetName(const std::wstring& n) { m_material->SetName(n.c_str()); }
  //public override ModelComponentType ComponentType
  static double MaxShine() { return ON_Material::MaxShine; }
  double GetShine() const { return m_material->Shine(); }
  void SetShine(double s) { m_material->SetShine(s); }
  double GetTransparency() const { return m_material->Transparency(); }
  void SetTransparency(double t) { m_material->SetTransparency(t); }
  double GetIndexOfRefraction() const { return m_material->m_index_of_refraction; }
  void SetIndexOfRefraction(double ior) { m_material->m_index_of_refraction = ior; }
  double GetFresnelIndexOfRefraction() const { return m_material->m_fresnel_index_of_refraction; }
  void SetFresnelIndexOfRefraction(double ior) { m_material->m_fresnel_index_of_refraction = ior; }
  double GetRefractionGlossiness() const { return m_material->m_refraction_glossiness; }
  void SetRefractionGlossiness(double r) { m_material->m_refraction_glossiness = r; }
  double GetReflectionGlossiness() const { return m_material->m_reflection_glossiness; }
  void SetReflectionGlossiness(double r) { m_material->m_reflection_glossiness = r; }
  bool GetFresnelReflections() const { return m_material->FresnelReflections(); }
  void SetFresnelReflections(bool b) { m_material->SetFresnelReflections(b); }
  bool GetDisableLighting() const { return m_material->DisableLighting(); }
  void SetDisableLighting(bool b) { m_material->SetDisableLighting(b); }
  //public bool AlphaTransparency | get; set;
  class BND_PhysicallyBasedMaterial* PhysicallyBased();
  double GetReflectivity() const { return m_material->Reflectivity(); }
  void SetReflectivity(double r) { m_material->SetReflectivity(r); }
  BND_Color GetPreviewColor() const { return ON_Color_to_Binding(m_material->PreviewColor()); }
  BND_Color GetDiffuseColor() const { return ON_Color_to_Binding(m_material->Diffuse()); }
  void SetDiffuseColor(const BND_Color& c) { m_material->SetDiffuse(Binding_to_ON_Color(c)); }
  BND_Color GetAmbientColor() const { return ON_Color_to_Binding(m_material->Ambient()); }
  void SetAmbientColor(const BND_Color& ambient) { m_material->SetAmbient(Binding_to_ON_Color(ambient)); }
  BND_Color GetEmissionColor() const { return ON_Color_to_Binding(m_material->Emission()); }
  void SetEmissionColor(const BND_Color& c) { m_material->SetEmission(Binding_to_ON_Color(c)); }
  BND_Color GetSpecularColor() const { return ON_Color_to_Binding(m_material->Specular()); }
  void SetSpecularColor(const BND_Color& c) { m_material->SetSpecular(Binding_to_ON_Color(c)); }
  BND_Color GetReflectionColor() const { return ON_Color_to_Binding(m_material->m_reflection); }
  void SetReflectionColor(const BND_Color& c) { m_material->m_reflection = Binding_to_ON_Color(c); }
  BND_Color GetTransparentColor() const { return ON_Color_to_Binding(m_material->m_transparent); }
  void SetTransparentColor(const BND_Color& c) { m_material->m_transparent = Binding_to_ON_Color(c); }
  void Default() { *m_material = ON_Material::Default; }
  class BND_Texture* GetTexture(ON_Texture::TYPE t) const;
  //public Texture[] GetTextures()
  class BND_Texture* GetBitmapTexture() const;
  bool SetBitmapTexture(std::wstring filename);
  bool SetBitmapTexture2(const class BND_Texture& texture);
  class BND_Texture* GetBumpTexture() const;
  bool SetBumpTexture(std::wstring filename);
  bool SetBumpTexture2(const class BND_Texture& texture);
  class BND_Texture* GetEnvironmentTexture() const;
  bool SetEnvironmentTexture(std::wstring filename);
  bool SetEnvironmentTexture2(const class BND_Texture& texture);
  class BND_Texture* GetTransparencyTexture() const;
  bool SetTransparencyTexture(std::wstring filename);
  bool SetTransparencyTexture2(const class BND_Texture& texture);
};

class BND_PhysicallyBasedMaterial
{
public:
  ON_Material* m_material;
  ON_ModelComponentReference m_component_ref;

  //public bool SetTexture(DocObjects.Texture texture, DocObjects.TextureType which)
  //    public DocObjects.Texture GetTexture(DocObjects.TextureType which)
  //public DocObjects.Texture[] GetTextures()
  bool Supported() const;
  //public void SynchronizeLegacyMaterial()
  //public Rhino.Display.Color4f BaseColor{ get; set; }
  //public BRDFs BRDF{ get; set; }
  //public Rhino.Display.Color4f SubsurfaceScatteringColor{ get; set; }
  double Subsurface() const { return m_material->PhysicallyBased()->Subsurface(); }
  void SetSubsurface(double s) { m_material->PhysicallyBased()->SetSubsurface(s); }
  double SubsurfaceScatteringRadius() const { return m_material->PhysicallyBased()->SubsurfaceScatteringRadius(); }
  void SetSubsurfaceScatteringRadius(double s) { m_material->PhysicallyBased()->SetSubsurfaceScatteringRadius(s); }
  double Metallic() const { return m_material->PhysicallyBased()->Metallic(); }
  void SetMetallic(double m) { m_material->PhysicallyBased()->SetMetallic(m); }
  double Specular() const { return m_material->PhysicallyBased()->Specular(); }
  void SetSpecular(double s) { m_material->PhysicallyBased()->SetSpecular(s); }
  double ReflectiveIOR() const { return m_material->PhysicallyBased()->ReflectiveIOR(); }
  void SetReflectiveIOR(double r) { m_material->PhysicallyBased()->SetReflectiveIOR(r); }
  double SpecularTint() const { return m_material->PhysicallyBased()->SpecularTint(); }
  void SetSpecularTint(double st) { m_material->PhysicallyBased()->SetSpecularTint(st); }
  double Roughness() const { return m_material->PhysicallyBased()->Roughness(); }
  void SetRoughness(double r) { m_material->PhysicallyBased()->SetRoughness(r); }
  double Anisotropic() const { return m_material->PhysicallyBased()->Anisotropic(); }
  void SetAnisotropic(double a) { m_material->PhysicallyBased()->SetAnisotropic(a); }
  double AnisotropicRotation() const { return m_material->PhysicallyBased()->AnisotropicRotation(); }
  void SetAnisotropicRotation(double ar) { m_material->PhysicallyBased()->SetAnisotropicRotation(ar); }
  double Sheen() const { return m_material->PhysicallyBased()->Sheen(); }
  void SetSheen(double s) { m_material->PhysicallyBased()->SetSheen(s); }
  double SheenTint() const { return m_material->PhysicallyBased()->SheenTint(); }
  void SetSheenTint(double st) { m_material->PhysicallyBased()->SetSheenTint(st); }
  double Clearcoat() const { return m_material->PhysicallyBased()->Clearcoat(); }
  void SetClearcoat(double cc) { m_material->PhysicallyBased()->SetClearcoat(cc); }
  double ClearcoatRoughness() const { return m_material->PhysicallyBased()->ClearcoatRoughness(); }
  void SetClearcoatRoughness(double c) { m_material->PhysicallyBased()->SetClearcoatRoughness(c); }
  double OpacityIOR() const { return m_material->PhysicallyBased()->OpacityIOR(); }
  void SetOpacityIOR(double o) { m_material->PhysicallyBased()->SetOpacityIOR(o); }
  double Opacity() const { return m_material->PhysicallyBased()->Opacity(); }
  void SetOpacity(double o) { m_material->PhysicallyBased()->SetOpacity(o); }
  double OpacityRoughness() const { return m_material->PhysicallyBased()->OpacityRoughness(); }
  void SetOpacityRoughness(double o) { m_material->PhysicallyBased()->SetOpacityRoughness(o); }
  //public Rhino.Display.Color4f Emission{ get; set; }
};
