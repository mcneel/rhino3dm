// The hand path behind the generated File3dmObjectTable.Add(GeometryBase, ObjectAttributes)
// (G119, gen_registry.HAND_FALLBACKS; Luis 2026-10-28). RhinoCommon's Add gives up with
// NotSupportedException for every geometry its switch lacks -- a Leader, TextEntity, radial
// or ordinate dimension among them -- where rhino3dm 8.x's Add took any geometry. The
// generated body keeps RhinoCommon's switch and calls this where it would throw. To go when
// RhinoCommon's Add covers them (upstream U30; deviation D71).
#pragma once

#include "bnd_common_gen.h"

class BND_GeometryBase;
class BND_ObjectAttributes;

// the geometry (a view resolves) added to the model with the attributes, or default ones;
// the new object's id (Guid.Empty when the model refuses it)
BND_UUID BND_File3dmObjectTable_AddAny(ONX_Model* model, const BND_GeometryBase& item, const BND_ObjectAttributes* attributes);
