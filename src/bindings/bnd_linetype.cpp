#include "bindings.h"


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

BND_Linetype::~BND_Linetype()
{
    delete m_linetype;
}

void BND_Linetype::SetTrackedPointer(ON_Linetype* linetype, const ON_ModelComponentReference* compref)
{
  m_linetype = linetype;
  BND_ModelComponent::SetTrackedPointer(linetype, compref);
}

BND_TUPLE BND_Linetype::GetSegment(int index) const
{
  ON_LinetypeSegment segment = m_linetype->Segment(index);
#if defined(ON_PYTHON_COMPILE) && defined(NANOBIND)
  BND_TUPLE rc = py::make_tuple(segment.m_length, segment.m_seg_type == ON_LinetypeSegment::eSegType::stLine);
#else
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, segment.m_length);
  SetTuple(rc, 1, segment.m_seg_type == ON_LinetypeSegment::eSegType::stLine);
#endif
  return rc;
}

bool BND_Linetype::SetSegment(int index, double length, bool isSolid)
{
  ON_LinetypeSegment::eSegType type = isSolid ? ON_LinetypeSegment::eSegType::stLine : ON_LinetypeSegment::eSegType::stSpace;
  ON_LinetypeSegment segment(length, type);
  return m_linetype->SetSegment(index, segment);
}

int BND_Linetype::AppendSegment(double length, bool isSolid)
{
  ON_LinetypeSegment::eSegType type = isSolid ? ON_LinetypeSegment::eSegType::stLine : ON_LinetypeSegment::eSegType::stSpace;
  ON_LinetypeSegment segment(length, type);
  return m_linetype->AppendSegment(segment);
}


#if defined(ON_PYTHON_COMPILE)

void initLinetypeBindings(rh3dmpymodule& m)
{
  py::class_<BND_Linetype, BND_ModelComponent>(m, "Linetype")
    .def(py::init<>())
    .def_property("Name", &BND_Linetype::GetName, &BND_Linetype::SetName)
    .def_property_readonly("Index", &BND_Linetype::GetIndex)
    .def_property_readonly("PatternLength", &BND_Linetype::PatternLength)
    .def_property_readonly("SegmentCount", &BND_Linetype::SegmentCount)
    .def("GetSegment", &BND_Linetype::GetSegment, py::arg("index"))
    .def("SetSegment", &BND_Linetype::SetSegment, py::arg("index"), py::arg("length"), py::arg("isSolid"))
    .def("AppendSegment", &BND_Linetype::AppendSegment, py::arg("length"), py::arg("isSolid"))
    .def("RemoveSegment", &BND_Linetype::RemoveSegment, py::arg("index"))
    .def("ClearPattern", &BND_Linetype::ClearPattern)
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
  class_<BND_Linetype, base<BND_ModelComponent>>("Linetype")
    .constructor<>()
    .property("name", &BND_Linetype::GetName, &BND_Linetype::SetName)
    .property("index", &BND_Linetype::GetIndex)
    .property("patternLength", &BND_Linetype::PatternLength)
    .property("segmentCount", &BND_Linetype::SegmentCount)
    .function("getSegment", &BND_Linetype::GetSegment)
    .function("setSegment", &BND_Linetype::SetSegment)
    .function("appendSegment", &BND_Linetype::AppendSegment)
    .function("removeSegment", &BND_Linetype::RemoveSegment)
    .function("clearPattern", &BND_Linetype::ClearPattern)
    .class_function("border", &BND_Linetype::Border, allow_raw_pointers())
    .class_function("byLayer", &BND_Linetype::ByLayer, allow_raw_pointers())
    .class_function("byParent", &BND_Linetype::ByParent, allow_raw_pointers())
    .class_function("center", &BND_Linetype::Center, allow_raw_pointers())
    .class_function("continuous", &BND_Linetype::Continuous, allow_raw_pointers())
    .class_function("dashdot", &BND_Linetype::DashDot, allow_raw_pointers())
    .class_function("dashed", &BND_Linetype::Dashed, allow_raw_pointers())
    .class_function("dots", &BND_Linetype::Dots, allow_raw_pointers())
    .class_function("hidden", &BND_Linetype::Hidden, allow_raw_pointers())
    ;
}
#endif
