#include "bnd_point2d.h"

using namespace bnd_shared;

BND_DICT BND_Point2d_Encode(const BND_Point2d& p) { return XY(p); }
