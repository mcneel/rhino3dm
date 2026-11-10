// rhino3dm overloads RhinoCommon lacks (Luis 2026-10-30: kept by hand, upstream U38): a point cloud
// point with a value but no normal or color, and a group deleted by id. Python registers them as
// overloads of the generated Add / Insert / AddRange / Delete (as the old bindings did); JS keeps
// the old names (addPointValue, ...), as embind cannot hold two overloads of one arity.
// Hand-maintained members of File3dmGroupTable; the generated binding is bnd_file3dmgrouptable_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_file3dmgrouptable_gen.h"

// removes the group with that id; false when there is none
bool BND_File3dmGroupTable_DeleteId(const BND_File3dmGroupTable& table, BND_UUID id);
