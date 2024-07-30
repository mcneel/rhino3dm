#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)

#if defined(NANOBIND)
namespace py = nanobind;
void init3dmAttributesBindings(py::module_& m);
#else
namespace py = pybind11;
void init3dmAttributesBindings(py::module& m);
#endif


#else
void init3dmAttributesBindings(void* m);
#endif

class BND_3dmObjectAttributes : public BND_CommonObject
{
public:
  ON_3dmObjectAttributes* m_attributes = nullptr;
  BND_File3dmDecalTable m_decals;
  BND_File3dmMeshModifiers m_mesh_modifiers;

public:
  BND_3dmObjectAttributes();
  BND_3dmObjectAttributes(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref);

  ON::object_mode GetMode() const { return m_attributes->Mode(); }
  void SetMode(ON::object_mode mode) { m_attributes->SetMode(mode); }
  bool Transform(const class BND_Transform& transform);
  bool IsInstanceDefinitionObject() const { return m_attributes->IsInstanceDefinitionObject(); }
  bool IsVisible() const { return m_attributes->IsVisible(); }
  void SetVisible(bool b) { m_attributes->SetVisible(b); }
  bool CastsShadows() const { return m_attributes->m_rendering_attributes.m_bCastsShadows; }
  void SetCastsShadows(bool b) { m_attributes->m_rendering_attributes.m_bCastsShadows = b; }
  bool ReceivesShadows() const { return m_attributes->m_rendering_attributes.m_bReceivesShadows; }
  void SetReceivesShadows(bool b) { m_attributes->m_rendering_attributes.m_bReceivesShadows = b; }
  ON::object_linetype_source GetLinetypeSource() const { return m_attributes->LinetypeSource(); }
  void SetLinetypeSource(ON::object_linetype_source l) { m_attributes->SetLinetypeSource(l); }
  ON::object_color_source GetColorSource() const { return m_attributes->ColorSource(); }
  void SetColorSource(ON::object_color_source c) { m_attributes->SetColorSource(c); }
  ON::plot_color_source GetPlotColorSource() const { return m_attributes->PlotColorSource(); }
  void SetPlotColorSource(ON::plot_color_source p) { m_attributes->SetPlotColorSource(p); }
  ON::plot_weight_source GetPlotWeightSource() const { return m_attributes->PlotWeightSource(); }
  void SetPlotWeightSource(ON::plot_weight_source p) { m_attributes->SetPlotWeightSource(p); }
  bool HasDisplayModeOverride(BND_UUID viewportId) const;
  //public bool SetDisplayModeOverride(Display.DisplayModeDescription mode)
  //public bool SetDisplayModeOverride(Display.DisplayModeDescription mode, Guid rhinoViewportId)
  //public void RemoveDisplayModeOverride()
  //public void RemoveDisplayModeOverride(Guid rhinoViewportId)
  //public bool AddHideInDetailOverride(Guid detailId)
  //public bool RemoveHideInDetailOverride(Guid detailId)
  //public bool HasHideInDetailOverrideSet(Guid detailId)
  //public Guid[] GetHideInDetailOverrides()

  BND_UUID GetObjectId() const;
  void SetObjectId(BND_UUID id);
  std::wstring GetName() const;
  void SetName(const std::wstring name);
  std::wstring GetUrl() const { return std::wstring(m_attributes->m_url); }
  void SetUrl(const std::wstring url) { m_attributes->m_url = url.c_str(); }
  int GetLayerIndex() const { return m_attributes->m_layer_index; }
  void SetLayerIndex(int index) { m_attributes->m_layer_index = index; }
  int GetLinetypeIndex() const { return m_attributes->m_linetype_index; }
  void SetLinetypeIndex(int i) { m_attributes->m_linetype_index = i; }
  int MaterialIndex() const { return m_attributes->m_material_index; }
  void SetMaterialIndex(int i) { m_attributes->m_material_index = i; }
  ON::object_material_source GetMaterialSource() const { return m_attributes->MaterialSource(); }
  void SetMaterialSource(ON::object_material_source m) { m_attributes->SetMaterialSource(m); }
  //public MaterialRefs MaterialRefs
  BND_Color GetObjectColor() const { return ON_Color_to_Binding(m_attributes->m_color); }
  void SetObjectColor(BND_Color c) { m_attributes->m_color = Binding_to_ON_Color(c); }
  BND_Color GetPlotColor() const { return ON_Color_to_Binding(m_attributes->m_plot_color); }
  void SetPlotColor(BND_Color c) { m_attributes->m_plot_color = Binding_to_ON_Color(c); }
  //bool HasMapping() const;
  //I wonder if we could implement the following with a File3dm
  BND_Color GetDrawColor(class BND_ONXModel* document) const;
  //public System.Drawing.Color DrawColor(RhinoDoc document)
  //public System.Drawing.Color DrawColor(RhinoDoc document, Guid viewportId)
  //public System.Drawing.Color ComputedPlotColor(RhinoDoc document)
  //public System.Drawing.Color ComputedPlotColor(RhinoDoc document, Guid viewportId)
  //public double ComputedPlotWeight(RhinoDoc document)
  //public double ComputedPlotWeight(RhinoDoc document, Guid viewportId)

  int GetDisplayOrder() const { return m_attributes->m_display_order; }
  void SetDisplayOrder(int i) { m_attributes->m_display_order = i; }
  double PlotWeight() const { return m_attributes->m_plot_weight_mm; }
  void SetPlotWeight(double w) { m_attributes->m_plot_weight_mm = w; }
  ON::object_decoration GetObjectDecoration() const { return m_attributes->m_object_decoration; }
  void SetObjectDecoration(ON::object_decoration d) { m_attributes->m_object_decoration = d; }
  int WireDensity() const { return m_attributes->m_wire_density; }
  void SetWireDensity(int wd) { m_attributes->m_wire_density = wd; }
  BND_UUID GetViewportId() const { return ON_UUID_to_Binding(m_attributes->m_viewport_id); }
  void SetViewportId(BND_UUID viewportId) { m_attributes->m_viewport_id = Binding_to_ON_UUID(viewportId); }
  ON::active_space GetSpace() const { return m_attributes->m_space; }
  void SetSpace(ON::active_space a) { m_attributes->m_space = a; }

  int GroupCount() const { return m_attributes->GroupCount(); }
  BND_TUPLE GetGroupList() const;
  void AddToGroup(int i) { m_attributes->AddToGroup(i); }
  void RemoveFromGroup(int i) { m_attributes->RemoveFromGroup(i); }
  void RemoveFromAllGroups() { m_attributes->RemoveFromAllGroups(); }

  BND_File3dmDecalTable& Decals() { return m_decals; }
  BND_File3dmMeshModifiers MeshModifiers() { return m_mesh_modifiers; }

protected:
  void SetTrackedPointer(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref);
};
