// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): file interop rhino3dm has and RhinoCommon does not.
//   F3 RdkXml: the RDK user data of an object / a model / a plug-in data entry, as XML
//   F4 File3dmEmbeddedFile.Read(path), File3dm.GetEmbeddedFileAsBase64(path[, strict])
// and File3dmRenderContentTable.Add (Luis 2026-10-28: upstream; hand-kept for 9.2, 2026-10-29),
// on the kind's collection as Find went (triage H1): File3dm.RenderMaterials.Add(material), ...
// Hand-maintained members of File3dmEmbeddedFile; the generated binding is bnd_file3dmembeddedfile_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_file3dmembeddedfile_gen.h"

// F4
BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFile_Read(std::wstring fileName);   // owned; null when unreadable
// JS only (Luis 2026-10-18): an embedded file from its bytes, as the old fromByteArray (owned)
BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFile_FromByteArray(std::string bytes);
