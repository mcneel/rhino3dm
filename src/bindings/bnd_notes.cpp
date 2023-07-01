#include "bindings.h"

bool BND_File3dmNotes::IsHTML() const
{
  return m_notes.m_bHTML;
}

bool BND_File3dmNotes::IsVisible() const
{
  return m_notes.m_bVisible;
}

std::wstring BND_File3dmNotes::GetNotes() const
{
  return std::wstring(m_notes.m_notes);
}

void BND_File3dmNotes::SetNotes(std::wstring notes)
{
  m_notes.m_notes = notes.c_str();
}

BND_TUPLE BND_File3dmNotes::GetWindowRectangle() const
{
  BND_TUPLE rc = CreateTuple(4);
  SetTuple(rc, 0, m_notes.m_window_left);
  SetTuple(rc, 1, m_notes.m_window_top);
  SetTuple(rc, 2, abs(m_notes.m_window_right - m_notes.m_window_left));
  SetTuple(rc, 3, abs(m_notes.m_window_bottom - m_notes.m_window_top));
  return rc;
}

void BND_File3dmNotes::SetWindowRectangle(BND_TUPLE rect)
{
#if defined(ON_PYTHON_COMPILE)
  m_notes.m_window_left = rect[0].cast<int>();
  m_notes.m_window_top = rect[1].cast<int>();
  m_notes.m_window_right = m_notes.m_window_left + rect[2].cast<int>();
  m_notes.m_window_bottom = m_notes.m_window_top + rect[3].cast<int>();
#else
  m_notes.m_window_left = rect[0].as<int>();
  m_notes.m_window_top = rect[1].as<int>();
  m_notes.m_window_right = m_notes.m_window_left + rect[2].as<int>();
  m_notes.m_window_bottom = m_notes.m_window_top + rect[3].as<int>();
#endif
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initNotesBindings(pybind11::module& m)
{
  py::class_<BND_File3dmNotes>(m, "File3dmNotes")
    .def(py::init<>())
    .def_property_readonly("IsHTML", &BND_File3dmNotes::IsHTML)
    .def_property_readonly("IsVisible", &BND_File3dmNotes::IsVisible)
    .def_property("Notes", &BND_File3dmNotes::GetNotes, &BND_File3dmNotes::SetNotes)
    .def_property("WindowRectangle", &BND_File3dmNotes::GetWindowRectangle, &BND_File3dmNotes::SetWindowRectangle)
    .def("__str__", &BND_File3dmNotes::GetNotes)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initNotesBindings(void*)
{
  class_<BND_File3dmNotes>("File3dmNotes")
    .constructor<>()
    .property("isHTML", &BND_File3dmNotes::IsHTML)
    .property("isVisible", &BND_File3dmNotes::IsVisible)
    .property("notes", &BND_File3dmNotes::GetNotes, &BND_File3dmNotes::SetNotes)
    .property("windowRectangle", &BND_File3dmNotes::GetWindowRectangle, &BND_File3dmNotes::SetWindowRectangle)
    .function("toString", &BND_File3dmNotes::GetNotes)
    ;
}
#endif
