#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initLayerBindings(pybind11::module& m);
#else
void initLayerBindings(void* m);
#endif

class BND_Layer : public BND_CommonObject
{
  ON_Layer* m_layer = nullptr;
public:
  BND_Layer();
  BND_Layer(ON_Layer* layer, const ON_ModelComponentReference* compref);

  std::wstring GetName() const { return std::wstring(m_layer->NameAsPointer()); }
  void SetName(const std::wstring& name) { m_layer->SetName(name.c_str()); }
  //public string FullPath{ get; }
  //public override Guid Id {get;set;}
  BND_UUID GetId() const { return ON_UUID_to_Binding(m_layer->Id()); }
  //public Guid ParentLayerId {get;set;}
  BND_UUID GetParentLayerId() const { return ON_UUID_to_Binding(m_layer->ParentId()); }
  int GetIgesLevel() const { return m_layer->IgesLevel(); }
  void SetIgesLevel(int level) { m_layer->SetIgesLevel(level); }
  bool HasPerViewportSettings(BND_UUID viewportId) const;
  void DeletePerViewportSettings(BND_UUID viewportId);
  BND_Color GetColor() const;
  void SetColor(const BND_Color& color);
  //public System.Drawing.Color PerViewportColor(Guid viewportId)
  //public void SetPerViewportColor(Guid viewportId, System.Drawing.Color color)
  //public void DeletePerViewportColor(Guid viewportId)
  //public System.Drawing.Color PlotColor {get;set}
  //public System.Drawing.Color PerViewportPlotColor(Guid viewportId)
  //public void SetPerViewportPlotColor(Guid viewportId, System.Drawing.Color color)
  //public void DeletePerViewportPlotColor(Guid viewportId)
  //public double PlotWeight {get;set;}
  //public double PerViewportPlotWeight(Guid viewportId)
  //public void SetPerViewportPlotWeight(Guid viewportId, double plotWeight)
  //public void DeletePerViewportPlotWeight(Guid viewportId)
  //public int LinetypeIndex {get;set;}
  //public int RenderMaterialIndex {get;set;}
  //public bool IsVisible {get;set;}
  //public bool PerViewportIsVisible(Guid viewportId)
  //public void SetPerViewportVisible(Guid viewportId, bool visible)
  //public void DeletePerViewportVisible(Guid viewportId)
  //public bool PerViewportPersistentVisibility(Guid viewportId)
  //public void SetPerViewportPersistentVisibility(Guid viewportId, bool persistentVisibility)
  //public void UnsetPerViewportPersistentVisibility(Guid viewportId)
  //public bool IsLocked {get;set;}
  //public bool GetPersistentVisibility()
  //public void SetPersistentVisibility(bool persistentVisibility)
  //public void UnsetPersistentVisibility()
  //public bool GetPersistentLocking()
  //public void SetPersistentLocking(bool persistentLocking)
  //public void UnsetPersistentLocking()
  //public bool IsExpanded {get;set;}
  //public void CopyAttributesFrom(Layer otherLayer)
  //public bool SetUserString(string key, string value)
  //public string GetUserString(string key)
  //public int UserStringCount {get;}
  //public System.Collections.Specialized.NameValueCollection GetUserStrings()


protected:
  void SetTrackedPointer(ON_Layer* layer, const ON_ModelComponentReference* compref);
};
