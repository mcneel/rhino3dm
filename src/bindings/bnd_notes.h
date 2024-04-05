#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initNotesBindings(pybind11::module& m);
#else
void initNotesBindings(void* m);
#endif

class BND_File3dmNotes
{
public:
  ON_3dmNotes m_notes;
public:
  BND_File3dmNotes() = default;
  BND_File3dmNotes(const ON_3dmNotes& notes) : m_notes(notes) {}
  bool IsHTML() const;
  bool IsVisible() const;
  std::wstring GetNotes() const;
  void SetNotes(const std::wstring notes);
  BND_TUPLE GetWindowRectangle() const;
  void SetWindowRectangle(BND_TUPLE rect);
};
