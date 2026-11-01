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
// Hand-maintained members of CommonObject; the generated binding is bnd_commonobject_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_commonobject_gen.h"

// F1
BND_DICT BND_CommonObject_Encode(const BND_CommonObject& obj);
BND_DICT BND_CommonObject_ToJSON(const BND_CommonObject& obj, BND_DICT key);   // JS2, as above
BND_CommonObject* BND_CommonObject_Decode(BND_DICT jsonObject);      // owned; null when unreadable

// F3 ("" when there is none)
std::wstring BND_CommonObject_RdkXml(const BND_CommonObject& obj);
