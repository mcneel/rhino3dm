// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): the JSON forms Rhino.Compute exchanges, the old bindings' dictionaries key
// for key.
//   F1 CommonObject / File3dm Encode, Decode: kept until a portable JSON API is upstream
//   F2 the value types' (and MeshingParameters') Encode, Decode
//   F4 ArchivableDictionary EncodeDict, DecodeDict: a dict in RhinoCommon's ArchivableDictionary
//      binary form
// Hand-maintained members of BoundingBox; the generated binding is bnd_boundingbox_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_boundingbox_gen.h"

// F2
BND_DICT BND_BoundingBox_Encode(const BND_BoundingBox& box);
BND_BoundingBox BND_BoundingBox_Decode(BND_DICT jsonObject);
