// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): the JSON forms Rhino.Compute exchanges, the old bindings' dictionaries key
// for key.
//   F1 CommonObject / File3dm Encode, Decode: kept until a portable JSON API is upstream
//   F2 the value types' (and MeshingParameters') Encode, Decode
//   F4 ArchivableDictionary EncodeDict, DecodeDict: a dict in RhinoCommon's ArchivableDictionary
//      binary form
// Hand-maintained members of Circle; the generated binding is bnd_circle_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_circle_gen.h"

BND_DICT BND_Circle_Encode(const BND_Circle& circle);
