// rhino3dm adds a count to RhinoCommon's Decals (Luis 2026-11-04, D31): Python len(attrs.Decals),
// JS attrs.decals.count. RhinoCommon's Decals is enumerable only.
// Hand-maintained members of Decals; the generated binding is bnd_decals_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_decals_gen.h"

// the number of decals iteration yields: the visible ones, as RhinoCommon's enumerator skips the others
int BND_Decals_Count(const BND_Decals& decals);
