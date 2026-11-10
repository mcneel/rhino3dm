// Kept by hand (Luis 2026-11-08): adding an embedded file object, as 8.x's
// File3dmEmbeddedFileTable.Add(EmbeddedFile) did. RhinoCommon adds from a path only, which JS
// cannot give (no host file system): File3dmEmbeddedFile.fromByteArray(bytes, filename), then Add.
// Hand-maintained members of File3dmEmbeddedFiles; the generated binding is bnd_file3dmembeddedfiles_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_file3dmembeddedfiles_gen.h"
#include "bnd_file3dmembeddedfile_gen.h"

// a copy of the embedded file into the file; false when it has no data
bool BND_File3dmEmbeddedFiles_AddFile(const BND_File3dmEmbeddedFiles& table, const BND_File3dmEmbeddedFile& embeddedFile);
