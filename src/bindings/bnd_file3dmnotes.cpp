#include "bnd_file3dmnotes.h"

static ON_3dmNotes& NotesOf(const BND_File3dmNotes& notes)
{
  if (nullptr == notes.m_ptr)
    throw std::runtime_error("Rhino.FileIO.File3dm: the model is gone");
  return notes.m_ptr->m_properties.m_Notes;
}

std::wstring BND_File3dmNotes_GetNotes(const BND_File3dmNotes& notes)
{
  const ON_wString& s = NotesOf(notes).m_notes;
  return std::wstring(static_cast<const wchar_t*>(s), (size_t)s.Length());
}

void BND_File3dmNotes_SetNotes(BND_File3dmNotes& notes, std::wstring value)
{
  NotesOf(notes).m_notes = ON_wString(value.c_str(), (int)value.length());
}

bool BND_File3dmNotes_GetIsVisible(const BND_File3dmNotes& notes)
{
  return NotesOf(notes).m_bVisible;
}

void BND_File3dmNotes_SetIsVisible(BND_File3dmNotes& notes, bool value)
{
  NotesOf(notes).m_bVisible = value;
}

bool BND_File3dmNotes_GetIsHtml(const BND_File3dmNotes& notes)
{
  return NotesOf(notes).m_bHTML;
}

void BND_File3dmNotes_SetIsHtml(BND_File3dmNotes& notes, bool value)
{
  NotesOf(notes).m_bHTML = value;
}

BND_TUPLE BND_File3dmNotes_GetWindowRectangle(const BND_File3dmNotes& notes)
{
  const ON_3dmNotes& n = NotesOf(notes);
  return RH3DM_RectLTRB(n.m_window_left, n.m_window_top, n.m_window_right, n.m_window_bottom);
}

void BND_File3dmNotes_SetWindowRectangle(BND_File3dmNotes& notes, BND_TUPLE value)
{
  ON_3dmNotes& n = NotesOf(notes);
  n.m_window_left = RH3DM_RectLeft(value);
  n.m_window_top = RH3DM_RectTop(value);
  n.m_window_right = RH3DM_RectRight(value);
  n.m_window_bottom = RH3DM_RectBottom(value);
}
