#include "bnd_subdcomponent.h"
#include "bnd_subdface_gen.h"
#include "bnd_subdedge_gen.h"
#include "bnd_subdvertex_gen.h"

namespace
{
  // RhinoCommon's ComponentPtrType (ON_SubDComponentPtr::Type) and the owning SubD
  int ComponentType(const BND_SubDComponent& c, const ON_SubD** owner)
  {
    if (auto v = dynamic_cast<const BND_SubDVertex*>(&c)) { *owner = v->RH3DM_Owner(); return (int)ON_SubDComponentPtr::Type::Vertex; }
    if (auto e = dynamic_cast<const BND_SubDEdge*>(&c)) { *owner = e->RH3DM_Owner(); return (int)ON_SubDComponentPtr::Type::Edge; }
    if (auto f = dynamic_cast<const BND_SubDFace*>(&c)) { *owner = f->RH3DM_Owner(); return (int)ON_SubDComponentPtr::Type::Face; }
    *owner = nullptr;
    return (int)ON_SubDComponentPtr::Type::Unset;
  }
}

bool BND_SubDComponent_Equals(const BND_SubDComponent& self, const BND_SubDComponent* other)
{
  if (nullptr == other)
    return false;
  if (&self == other)
    return true;
  const ON_SubD* a = nullptr;
  const ON_SubD* b = nullptr;
  return ComponentType(self, &a) == ComponentType(*other, &b)
    && self.RH3DM_Key() == other->RH3DM_Key()
    && a == b;
}

bool BND_SubDComponent_NotEquals(const BND_SubDComponent& self, const BND_SubDComponent* other)
{
  return !BND_SubDComponent_Equals(self, other);
}

int BND_SubDComponent_GetHashCode(const BND_SubDComponent& self)
{
  // RhinoCommon's combination of type, Id, direction (always forward here) and parent, in unsigned
  // arithmetic (C#'s int wraps; C++'s signed overflow is undefined)
  const ON_SubD* owner = nullptr;
  const ON__UINT32 k = (ON__UINT32)-1521134295;
  ON__UINT32 hc = (ON__UINT32)ComponentType(self, &owner);
  hc = hc * k + self.RH3DM_Key();
  hc = hc * k + 0u;
  hc = hc * k + (ON__UINT32)(((ON__UINT64)owner) ^ (((ON__UINT64)owner) >> 32));
  return (int)hc;
}
