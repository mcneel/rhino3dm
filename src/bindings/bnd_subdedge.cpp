#include "bnd_subdedge.h"
#include "bnd_subdface_gen.h"
#include "bnd_subdvertex_gen.h"

using namespace bnd_shared;

std::vector<BND_SubDFace*> BND_SubDEdge_Faces(const BND_SubDEdge& edge)
{
  const ON_SubDEdge* e = edge.RH3DM_Const();
  return Neighbours<BND_SubDFace>(edge.RH3DM_Owner(), e, e->FaceCount(), &ON_SubDEdge::Face);
}

std::vector<BND_SubDVertex*> BND_SubDEdge_Vertices(const BND_SubDEdge& edge)
{
  const ON_SubDEdge* e = edge.RH3DM_Const();
  return Neighbours<BND_SubDVertex>(edge.RH3DM_Owner(), e, e->VertexCount(), &ON_SubDEdge::Vertex);
}
