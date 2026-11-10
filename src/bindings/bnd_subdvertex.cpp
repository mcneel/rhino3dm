#include "bnd_subdvertex.h"
#include "bnd_subdface_gen.h"

using namespace bnd_shared;

std::vector<BND_SubDFace*> BND_SubDVertex_Faces(const BND_SubDVertex& vertex)
{
  const ON_SubDVertex* v = vertex.RH3DM_Const();
  return Neighbours<BND_SubDFace>(vertex.RH3DM_Owner(), v, v->FaceCount(), &ON_SubDVertex::Face);
}
