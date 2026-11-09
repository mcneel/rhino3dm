// Kept by hand (Luis 2026-11-08, pilot #111): 8.x could set the background image; RhinoCommon has a
// getter only (upstream: a setter in RhinoCommon). Attached to the generated property at load.
// Hand-maintained members of Environment; the generated binding is bnd_environment_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_environment_gen.h"
#include "bnd_texture_gen.h"

// Environment.BackgroundImage = texture: a copy of the texture (null: an empty texture)
void BND_Environment_SetBackgroundImage(BND_Environment& environment, const BND_Texture* value);
