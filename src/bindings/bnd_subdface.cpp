#include "bnd_subdface.h"
#include "bnd_subdedge_gen.h"
#include "bnd_subdvertex_gen.h"

using namespace bnd_shared;

std::vector<BND_SubDEdge*> BND_SubDFace_Edges(const BND_SubDFace& face)
{
  const ON_SubDFace* f = face.RH3DM_Const();
  return Neighbours<BND_SubDEdge>(face.RH3DM_Owner(), f, f->EdgeCount(), &ON_SubDFace::Edge);
}

std::vector<BND_SubDVertex*> BND_SubDFace_Vertices(const BND_SubDFace& face)
{
  const ON_SubDFace* f = face.RH3DM_Const();
  return Neighbours<BND_SubDVertex>(face.RH3DM_Owner(), f, f->EdgeCount(), &ON_SubDFace::Vertex);   // as many vertices as edges
}
