// Kept by hand (Luis 2026-11-18, RH3DM-208): the notes of a model, read-write as RhinoCommon's
// File3dmNotes. Its bodies also serve a notes object with no model (managed fields), which the
// generator does not translate; here every File3dmNotes belongs to a model (File3dm.Notes).
// Hand-maintained members of File3dmNotes; the generated binding is bnd_file3dmnotes_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_file3dmnotes_gen.h"

std::wstring BND_File3dmNotes_GetNotes(const BND_File3dmNotes& notes);
void BND_File3dmNotes_SetNotes(BND_File3dmNotes& notes, std::wstring value);
bool BND_File3dmNotes_GetIsVisible(const BND_File3dmNotes& notes);
void BND_File3dmNotes_SetIsVisible(BND_File3dmNotes& notes, bool value);
bool BND_File3dmNotes_GetIsHtml(const BND_File3dmNotes& notes);
void BND_File3dmNotes_SetIsHtml(BND_File3dmNotes& notes, bool value);
// the notes window's last position, as a rectangle is elsewhere: (x, y, width, height)
BND_TUPLE BND_File3dmNotes_GetWindowRectangle(const BND_File3dmNotes& notes);
void BND_File3dmNotes_SetWindowRectangle(BND_File3dmNotes& notes, BND_TUPLE value);
