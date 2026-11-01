// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): the JSON forms Rhino.Compute exchanges, the old bindings' dictionaries key
// for key.
//   F1 CommonObject / File3dm Encode, Decode: kept until a portable JSON API is upstream
//   F2 the value types' (and MeshingParameters') Encode, Decode
//   F4 ArchivableDictionary EncodeDict, DecodeDict: a dict in RhinoCommon's ArchivableDictionary
//      binary form
// Hand-maintained members of ArchivableDictionary; the generated binding is (none: a holder class) (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"

// F4 (ArchivableDictionary: a holder class, gen_registry.HAND_CLASSES)
BND_DICT BND_ArchivableDictionary_EncodeDict(BND_DICT dict);
BND_DICT BND_ArchivableDictionary_DecodeDict(BND_DICT jsonObject);
