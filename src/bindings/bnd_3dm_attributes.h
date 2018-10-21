#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void init3dmAttributesBindings(pybind11::module& m);
#else
void init3dmAttributesBindings(void* m);
#endif

class BND_3dmAttributes : public BND_CommonObject
{
  ON_3dmObjectAttributes* m_attributes = nullptr;
public:
  BND_3dmAttributes();
  BND_3dmAttributes(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref);

  bool IsVisible() const { return m_attributes->IsVisible(); }
  void SetVisible(bool b) { m_attributes->SetVisible(b); }
  BND_UUID GetObjectId() const;
  std::wstring GetName() const;
  void SetName(const std::wstring name);
  int GetLayerIndex() const { return m_attributes->m_layer_index; }
  void SetLayerIndex(int index) { m_attributes->m_layer_index = index; }
  int GetLinetypeIndex() const { return m_attributes->m_linetype_index; }
  void SetLinetypeIndex(int i) { m_attributes->m_linetype_index = i; }
  int MaterialIndex() const { return m_attributes->m_material_index; }
  void SetMaterialIndex(int i) { m_attributes->m_material_index = i; }

  BND_Color GetObjectColor() const { return ON_Color_to_Binding(m_attributes->m_color); }
  void SetObjectColor(BND_Color c) { m_attributes->m_color = Binding_to_ON_Color(c); }
  BND_Color GetPlotColor() const { return ON_Color_to_Binding(m_attributes->m_plot_color); }
  void SetPlotColor(BND_Color c) { m_attributes->m_plot_color = Binding_to_ON_Color(c); }
  int GetDisplayOrder() const { return m_attributes->m_display_order; }
  void SetDisplayOrder(int i) { m_attributes->m_display_order = i; }
  double PlotWeight() const { return m_attributes->m_plot_weight_mm; }
  void SetPlotWeight(double w) { m_attributes->m_plot_weight_mm = w; }

  int WireDensity() const { return m_attributes->m_wire_density; }
  void SetWireDensity(int wd) { m_attributes->m_wire_density = wd; }

  BND_UUID GetViewportId() const { return ON_UUID_to_Binding(m_attributes->m_viewport_id); }
  void SetViewportId(BND_UUID viewportId) { m_attributes->m_viewport_id = Binding_to_ON_UUID(viewportId); }
  int GroupCount() const { return m_attributes->GroupCount(); }
  //public int[] GetGroupList()
  void AddToGroup(int i) { m_attributes->AddToGroup(i); }
  void RemoveFromGroup(int i) { m_attributes->RemoveFromGroup(i); }
  void RemoveFromAllGroups() { m_attributes->RemoveFromAllGroups(); }
protected:
  void SetTrackedPointer(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref);
};
