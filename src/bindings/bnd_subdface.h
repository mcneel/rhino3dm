// SubD neighbours (Luis 2026-10-30, option B then option 1: Pierre's neighbouring-component access).
// rhino3dm 8.32 had `face.Edges`, `face.Vertices`, `edge.Faces`, `edge.Vertices`, `vertex.Faces` (and
// `vertex.Edges`, which RhinoCommon has: generated); RhinoCommon has only EdgeAt(i) and friends. Kept
// here as lists of the generated components, exactly as RhinoCommon's vertex.Edges is generated, so
// all six neighbour properties look alike (the 8.32 cursor methods First / Next / ... are dropped).
// Hand-maintained members of SubDFace; the generated binding is bnd_subdface_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_subdface_gen.h"
#include "bnd_subdedge_gen.h"
#include "bnd_subdvertex_gen.h"

// the neighbours of one component, in opennurbs' order (as ON_SubD*Iterator walks them); each a new
// generated component of the same SubD, owned by the caller
std::vector<BND_SubDEdge*> BND_SubDFace_Edges(const BND_SubDFace& face);
std::vector<BND_SubDVertex*> BND_SubDFace_Vertices(const BND_SubDFace& face);
