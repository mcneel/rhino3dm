// SubDComponent equality (D25, Luis 2026-11-03): RhinoCommon's Equals / operator == / != / GetHashCode
// (opennurbs_subd.cs, since 8.36). Their bodies are pure C# over the managed parent reference
// (`ReferenceEquals(ParentSubD, other.ParentSubD)`), which the generator does not translate for a
// class, so they are written here over the generated component's owner and key.
// Hand-maintained members of SubDComponent; the generated binding is bnd_subdcomponent_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_subdcomponent_gen.h"

// RhinoCommon: same component type, same Id, same ComponentDirection, same parent SubD. A generated
// component is always referenced forward (ComponentDirection is not generated), so the direction
// test is always met; the parent is the owning ON_SubD.
bool BND_SubDComponent_Equals(const BND_SubDComponent& self, const BND_SubDComponent* other);
bool BND_SubDComponent_NotEquals(const BND_SubDComponent& self, const BND_SubDComponent* other);
int BND_SubDComponent_GetHashCode(const BND_SubDComponent& self);
