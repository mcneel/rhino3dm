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

  //public override string Name {get;set;}
  //public string FullPath{ get; }
  //public override Guid Id {get;set;}
  //public Guid ParentLayerId {get;set;}
  //public int IgesLevel{ get; set }
  //public bool HasPerViewportSettings(Guid viewportId)
  //public void DeletePerViewportSettings(Guid viewportId)
  //public System.Drawing.Color Color {get;set;}
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
