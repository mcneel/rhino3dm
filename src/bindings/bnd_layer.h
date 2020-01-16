#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initLayerBindings(pybind11::module& m);
#else
void initLayerBindings(void* m);
#endif

class BND_Layer : public BND_CommonObject
{
public:
  ON_Layer* m_layer = nullptr;
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_Layer();
  BND_Layer(ON_Layer* layer, const ON_ModelComponentReference* compref);
  BND_Layer(ON_Layer* layer, const ON_ModelComponentReference* compref, std::shared_ptr<ONX_Model>& model);

  static std::wstring PathSeparator() { return std::wstring(ON_ModelComponent::NamePathSeparator.Array()); }

  std::wstring GetName() const { return std::wstring(m_layer->NameAsPointer()); }
  void SetName(const std::wstring& name) { m_layer->SetName(name.c_str()); }
  std::wstring GetFullPath() const;
  BND_UUID GetId() const { return ON_UUID_to_Binding(m_layer->Id()); }
  void SetId(BND_UUID id) { m_layer->SetId(Binding_to_ON_UUID(id)); }

  int GetIndex() const { return m_layer->Index(); }
  //public Guid ParentLayerId {get;set;}
  BND_UUID GetParentLayerId() const { return ON_UUID_to_Binding(m_layer->ParentId()); }
  int GetIgesLevel() const { return m_layer->IgesLevel(); }
  void SetIgesLevel(int level) { m_layer->SetIgesLevel(level); }
  bool HasPerViewportSettings(BND_UUID viewportId) const;
  void DeletePerViewportSettings(BND_UUID viewportId);
  BND_Color GetColor() const;
  void SetColor(const BND_Color& color);
  BND_Color PerViewportColor(BND_UUID viewportId) const;
  void SetPerViewportColor(BND_UUID viewportId, BND_Color color);
  void DeletePerViewportColor(BND_UUID viewportId);
  BND_Color GetPlotColor() const;
  void SetPlotColor(const BND_Color& color);
//  BND_Color PerViewportPlotColor(BND_UUID viewportId) const;
//  void SetPerViewportPlotColor(BND_UUID viewportId, BND_Color color);
//  void DeletePerViewportPlotColor(BND_UUID viewportId);
  double GetPlotWeight() const { return m_layer->PlotWeight(); }
  void SetPlotWeight(double weight) { m_layer->SetPlotWeight(weight); }
//  double PerViewportPlotWeight(BND_UUID viewportId) const;
//  void SetPerViewportPlotWeight(BND_UUID viewportId, double plotWeight);
//  void DeletePerViewportPlotWeight(BND_UUID viewportId);
  int GetLinetypeIndex() const { return m_layer->LinetypeIndex(); }
  void SetLinetypeIndex(int index) { m_layer->SetLinetypeIndex(index); }
  int GetRenderMaterialIndex() const { return m_layer->RenderMaterialIndex(); }
  void SetRenderMaterialIndex(int index) { m_layer->SetRenderMaterialIndex(index); }
  bool IsVisible() const { return m_layer->IsVisible(); }
  void SetVisible(bool b) { m_layer->SetVisible(b); }
//  bool PerViewportIsVisible(BND_UUID viewportId) const;
//  void SetPerViewportVisible(BND_UUID viewportId, bool visible);
//  void DeletePerViewportVisible(BND_UUID viewportId);
//  bool PerViewportPersistentVisibility(BND_UUID viewportId) const;
//  void SetPerViewportPersistentVisibility(BND_UUID viewportId, bool persistentVisibility);
//  void UnsetPerViewportPersistentVisibility(BND_UUID viewportId);
  bool IsLocked() const { return m_layer->IsLocked(); }
  void SetLocked(bool l) { m_layer->SetLocked(l); }
  bool GetPersistentVisibility() const { return m_layer->PersistentVisibility(); }
  void SetPersistentVisibility(bool persistentVisibility) { m_layer->SetPersistentVisibility(persistentVisibility); }
  void UnsetPersistentVisibility() { m_layer->UnsetPersistentVisibility(); }
  bool GetPersistentLocking() const { return m_layer->PersistentLocking(); }
  void SetPersistentLocking(bool persistentLocking) { m_layer->SetPersistentLocking(persistentLocking); }
  void UnsetPersistentLocking() { m_layer->UnsetPersistentLocking(); }
  bool IsExpanded() const { return m_layer->m_bExpanded; }
  void SetExpanded(bool e) { m_layer->m_bExpanded = e; }
  //public void CopyAttributesFrom(Layer otherLayer)
  //public bool SetUserString(string key, string value)
  //public string GetUserString(string key)
  //public int UserStringCount {get;}
  //public System.Collections.Specialized.NameValueCollection GetUserStrings()


protected:
  void SetTrackedPointer(ON_Layer* layer, const ON_ModelComponentReference* compref);
};
