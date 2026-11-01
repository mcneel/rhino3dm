// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): the JSON forms Rhino.Compute exchanges, the old bindings' dictionaries key
// for key.
//   F1 CommonObject / File3dm Encode, Decode: kept until a portable JSON API is upstream
//   F2 the value types' (and MeshingParameters') Encode, Decode
//   F4 ArchivableDictionary EncodeDict, DecodeDict: a dict in RhinoCommon's ArchivableDictionary
//      binary form
// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): file interop rhino3dm has and RhinoCommon does not.
//   F3 RdkXml: the RDK user data of an object / a model / a plug-in data entry, as XML
//   F4 File3dmEmbeddedFile.Read(path), File3dm.GetEmbeddedFileAsBase64(path[, strict])
// and File3dmRenderContentTable.Add (Luis 2026-10-28: upstream; hand-kept for 9.2, 2026-10-29),
// on the kind's collection as Find went (triage H1): File3dm.RenderMaterials.Add(material), ...
// Hand-maintained members of File3dm; the generated binding is bnd_file3dm_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_file3dm_gen.h"
#include "bnd_file3dmwriteoptions_gen.h"

std::string BND_File3dm_Encode(const BND_File3dm& file);              // base64 of the .3dm bytes
std::string BND_File3dm_EncodeOptions(const BND_File3dm& file, const BND_File3dmWriteOptions* options);
BND_File3dm* BND_File3dm_Decode(std::string buffer);                   // owned; null when unreadable
std::wstring BND_File3dm_RdkXml(const BND_File3dm& file);
// the bytes of the file the model's RDK data embeds under `path`, base64 ("" when there is none):
// strict -- that exact path; otherwise the path ignoring case, then the file name alone
std::string BND_File3dm_GetEmbeddedFileAsBase64(const BND_File3dm& file, std::wstring path);
std::string BND_File3dm_GetEmbeddedFileAsBase64Strict(const BND_File3dm& file, std::wstring path, bool strict);
