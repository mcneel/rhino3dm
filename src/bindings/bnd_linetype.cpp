#include "bindings.h"


#if defined(ON_PYTHON_COMPILE)

BND_LinetypeSegment ON_LinetypeSegment_to_Binding(const ON_LinetypeSegment& segment)
{
  return pybind11::make_tuple(segment.m_length, segment.m_seg_type);
}

ON_LinetypeSegment Binding_to_ON_LinetypeSegment(const BND_LinetypeSegment& segment)
{
  double length = segment[0].cast<double>();
  ON_LinetypeSegment::eSegType type = segment[1].cast<ON_LinetypeSegment::eSegType>();
  return ON_LinetypeSegment(length, type);
}

#endif

#if defined(ON_WASM_COMPILE)

BND_LinetypeSegment ON_LinetypeSegment_to_Binding(const ON_LinetypeSegment& segment)
{
  emscripten::val v(emscripten::val::object());
  v.set("length", emscripten::val(segment.m_length));
  v.set("type", emscripten::val(segment.m_seg_type));
  return v;
}

ON_LinetypeSegment Binding_to_ON_LinetypeSegment(const BND_LinetypeSegment& segment)
{
  double length = segment["length"].as<double>();
  ON_LinetypeSegment::eSegType type = segment["type"].as<ON_LinetypeSegment::eSegType>();
  return ON_LinetypeSegment(length, type);
}

#endif


BND_Linetype::BND_Linetype()
{
  SetTrackedPointer(new ON_Linetype(), nullptr);
}

BND_Linetype::BND_Linetype(const ON_Linetype &existing)
{
  SetTrackedPointer(new ON_Linetype(existing), nullptr);
}

BND_Linetype::BND_Linetype(ON_Linetype* linetype, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(linetype, compref);
}

void BND_Linetype::SetTrackedPointer(ON_Linetype* linetype, const ON_ModelComponentReference* compref)
{
  m_linetype = linetype;
  BND_ModelComponent::SetTrackedPointer(linetype, compref);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;

BND_LinetypeSegment BND_LinetypeSegmentIterator::next()
{
  if (m_index >= m_linetype->SegmentCount())
  {
    throw py::stop_iteration();
  }
  return ON_LinetypeSegment_to_Binding(m_linetype->Segment(m_index++));
}

void initLinetypeBindings(pybind11::module& m)
{
  py::enum_<ON_LinetypeSegment::eSegType>(m, "LinetypeSegmentType")
    .value("Unset", ON_LinetypeSegment::eSegType::Unset)
    .value("Line", ON_LinetypeSegment::eSegType::stLine)
    .value("Space", ON_LinetypeSegment::eSegType::stSpace)
    ;

  py::class_<BND_LinetypeSegmentIterator>(m, "__LinetypeSegmentIterator")
    .def("__iter__", &BND_LinetypeSegmentIterator::iter)
    .def("__next__", &BND_LinetypeSegmentIterator::next)
    ;

  py::class_<BND_LinetypeSegmentList>(m, "LinetypeSegmentList")
    .def(py::init<>())
    .def("__len__", &BND_LinetypeSegmentList::Count)
    .def("__getitem__", &BND_LinetypeSegmentList::Get)
    .def("__setitem__", &BND_LinetypeSegmentList::Set)
    .def("__delitem__", &BND_LinetypeSegmentList::Remove)
    .def("__iter__", &BND_LinetypeSegmentList::Iterator)
    .def("Append", &BND_LinetypeSegmentList::Append, py::arg("segment"))
    .def("Clear", &BND_LinetypeSegmentList::Clear)
    ;

  py::class_<BND_Linetype, BND_ModelComponent>(m, "Linetype")
    .def(py::init<>())
    .def_property("Name", &BND_Linetype::GetName, &BND_Linetype::SetName)
    .def_property_readonly("Index", &BND_Linetype::GetIndex)
    .def_property_readonly("PatternLength", &BND_Linetype::PatternLength)
    .def_property("Segments", &BND_Linetype::GetSegments, &BND_Linetype::SetSegments)
    .def_property_readonly_static("Border", &BND_Linetype::Border)
    .def_property_readonly_static("ByLayer", &BND_Linetype::ByLayer)
    .def_property_readonly_static("ByParent", &BND_Linetype::ByParent)
    .def_property_readonly_static("Center", &BND_Linetype::Center)
    .def_property_readonly_static("Continuous", &BND_Linetype::Continuous)
    .def_property_readonly_static("DashDot", &BND_Linetype::DashDot)
    .def_property_readonly_static("Dashed", &BND_Linetype::Dashed)
    .def_property_readonly_static("Dots", &BND_Linetype::Dots)
    .def_property_readonly_static("Hidden", &BND_Linetype::Hidden)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initLinetypeBindings(void*)
{
  class_<BND_LinetypeSegmentList>("LinetypeSegmentList")
    .constructor<>()
    .property("count", &BND_LinetypeSegmentList::Count)
    .function("get", &BND_LinetypeSegmentList::Get)
    .function("set", &BND_LinetypeSegmentList::Set)
    .function("append", &BND_LinetypeSegmentList::Append)
    .function("remove", &BND_LinetypeSegmentList::Remove)
    .function("clear", &BND_LinetypeSegmentList::Clear)
    ;

  class_<BND_Linetype, base<BND_ModelComponent>>("Linetype")
    .constructor<>()
    .property("name", &BND_Linetype::GetName, &BND_Linetype::SetName)
    .property("index", &BND_Linetype::GetIndex)
    .property("patternLength", &BND_Linetype::PatternLength)
    .property("segments", &BND_Linetype::GetSegments, &BND_Linetype::SetSegments)
    .class_property("border", &BND_Linetype::Border)
    .class_property("byLayer", &BND_Linetype::ByLayer)
    .class_property("byParent", &BND_Linetype::ByParent)
    .class_property("center", &BND_Linetype::Center)
    .class_property("continuous", &BND_Linetype::Continuous)
    .class_property("dashdot", &BND_Linetype::DashDot)
    .class_property("dashed", &BND_Linetype::Dashed)
    .class_property("dots", &BND_Linetype::Dots)
    .class_property("hidden", &BND_Linetype::Hidden)
    ;
}
#endif
