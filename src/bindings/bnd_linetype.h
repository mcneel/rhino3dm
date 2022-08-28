#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
typedef pybind11::tuple BND_LinetypeSegment;
void initLinetypeBindings(pybind11::module& m);
#else
typedef emscripten::val BND_LinetypeSegment;
void initLinetypeBindings(void* m);
#endif

BND_LinetypeSegment ON_LinetypeSegment_to_Binding(const ON_LinetypeSegment& segment);
ON_LinetypeSegment Binding_to_ON_LinetypeSegment(const BND_LinetypeSegment& segment);

#if defined(ON_PYTHON_COMPILE)
class BND_LinetypeSegmentIterator
{
  ON_Linetype* m_linetype = nullptr;
  int m_index = 0;
public:
  BND_LinetypeSegmentIterator(ON_Linetype* linetype) : m_linetype(linetype) {}
  BND_LinetypeSegmentIterator iter() { return BND_LinetypeSegmentIterator(m_linetype); }
  BND_LinetypeSegment next();
};
#endif

class BND_LinetypeSegmentList
{
  ON_Linetype* m_linetype = nullptr;
public:
  BND_LinetypeSegmentList() : m_linetype(new ON_Linetype()) {}
  BND_LinetypeSegmentList(ON_Linetype* linetype) : m_linetype(linetype) {}

  int Count() const { return m_linetype->SegmentCount(); }
  BND_LinetypeSegment Get(int index) const { return ON_LinetypeSegment_to_Binding(m_linetype->Segment(index)); }
  int Append(BND_LinetypeSegment segment) { return m_linetype->AppendSegment(Binding_to_ON_LinetypeSegment(segment)); }
  bool Set(int index, BND_LinetypeSegment segment) { return m_linetype->SetSegment(index, Binding_to_ON_LinetypeSegment(segment)); }
  bool Remove(int index) { return m_linetype->RemoveSegment(index); }
  bool Clear() { return m_linetype->ClearPattern(); }
  BND_LinetypeSegmentIterator Iterator() { return BND_LinetypeSegmentIterator(m_linetype); }

  friend class BND_Linetype;
};

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

  std::wstring GetName() const { return std::wstring(m_linetype->NameAsPointer()); }
  void SetName(const std::wstring& name) { m_linetype->SetName(name.c_str()); }
  int GetIndex() const { return m_linetype->Index(); }
  double PatternLength() const { return m_linetype->PatternLength(); }
  BND_LinetypeSegmentList GetSegments() const { return BND_LinetypeSegmentList(m_linetype); }
  bool SetSegments(BND_LinetypeSegmentList segments) { return m_linetype->SetSegments(segments.m_linetype->Segments()); }

#if defined(ON_PYTHON_COMPILE)
  static BND_Linetype* Border(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::Border); }
  static BND_Linetype* ByLayer(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::ByLayer); }
  static BND_Linetype* ByParent(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::ByParent); }
  static BND_Linetype* Center(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::Center); }
  static BND_Linetype* Continuous(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::Continuous); }
  static BND_Linetype* DashDot(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::DashDot); }
  static BND_Linetype* Dashed(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::Dashed); }
  static BND_Linetype* Dots(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::Dots); }
  static BND_Linetype* Hidden(pybind11::object /*self*/) { return new BND_Linetype(ON_Linetype::Hidden); }
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
