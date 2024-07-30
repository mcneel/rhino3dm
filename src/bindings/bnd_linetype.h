#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initLinetypeBindings(py::module_& m);
#else
namespace py = pybind11;
void initLinetypeBindings(py::module& m);
#endif

#else
void initLinetypeBindings(void* m);
#endif

class BND_Linetype : public BND_ModelComponent
{
public:
  ON_Linetype* m_linetype = nullptr;
protected:
  void SetTrackedPointer(ON_Linetype* linetype, const ON_ModelComponentReference* compref);
public:
  BND_Linetype();
  BND_Linetype(const ON_Linetype&);
  BND_Linetype(ON_Linetype* linetype, const ON_ModelComponentReference* compref);
  virtual ~BND_Linetype();

  std::wstring GetName() const { return std::wstring(m_linetype->NameAsPointer()); }
  void SetName(const std::wstring& name) { m_linetype->SetName(name.c_str()); }
  int GetIndex() const { return m_linetype->Index(); }
  double PatternLength() const { return m_linetype->PatternLength(); }
  int SegmentCount() const { return m_linetype->SegmentCount(); }
  BND_TUPLE GetSegment(int index) const;
  bool SetSegment(int index, double length, bool isSolid);
  int AppendSegment(double length, bool isSolid);
  bool RemoveSegment(int index) { return m_linetype->RemoveSegment(index); }
  bool ClearPattern() { return m_linetype->ClearPattern(); }
  //public Rhino.DocObjects.LineCapStyle LineCapStyle
  //public Rhino.DocObjects.LineJoinStyle LineJoinStyle
  //public double Width
  //public UnitSystem WidthUnits
  //public Point2d[] GetTaperPoints()
  //public void SetTaper(double startWidth, double endWidth)
  //public void SetTaper(double startWidth, Point2d taperPoint, double endWidth) 
  //public void RemoveTaper()
  //public bool SetSegments(IEnumerable<double> segments)






#if defined(ON_PYTHON_COMPILE)
  static BND_Linetype* Border(py::object /*self*/) { return new BND_Linetype(ON_Linetype::Border); }
  static BND_Linetype* ByLayer(py::object /*self*/) { return new BND_Linetype(ON_Linetype::ByLayer); }
  static BND_Linetype* ByParent(py::object /*self*/) { return new BND_Linetype(ON_Linetype::ByParent); }
  static BND_Linetype* Center(py::object /*self*/) { return new BND_Linetype(ON_Linetype::Center); }
  static BND_Linetype* Continuous(py::object /*self*/) { return new BND_Linetype(ON_Linetype::Continuous); }
  static BND_Linetype* DashDot(py::object /*self*/) { return new BND_Linetype(ON_Linetype::DashDot); }
  static BND_Linetype* Dashed(py::object /*self*/) { return new BND_Linetype(ON_Linetype::Dashed); }
  static BND_Linetype* Dots(py::object /*self*/) { return new BND_Linetype(ON_Linetype::Dots); }
  static BND_Linetype* Hidden(py::object /*self*/) { return new BND_Linetype(ON_Linetype::Hidden); }
#else
  static BND_Linetype* Border() { return new BND_Linetype(ON_Linetype::Border); }
  static BND_Linetype* ByLayer() { return new BND_Linetype(ON_Linetype::ByLayer); }
  static BND_Linetype* ByParent() { return new BND_Linetype(ON_Linetype::ByParent); }
  static BND_Linetype* Center() { return new BND_Linetype(ON_Linetype::Center); }
  static BND_Linetype* Continuous() { return new BND_Linetype(ON_Linetype::Continuous); }
  static BND_Linetype* DashDot() { return new BND_Linetype(ON_Linetype::DashDot); }
  static BND_Linetype* Dashed() { return new BND_Linetype(ON_Linetype::Dashed); }
  static BND_Linetype* Dots() { return new BND_Linetype(ON_Linetype::Dots); }
  static BND_Linetype* Hidden() { return new BND_Linetype(ON_Linetype::Hidden); }
#endif
};
