#include "bnd_point2f.h"

using namespace bnd_shared;

BND_DICT BND_Point2f_Encode(const BND_Point2f& p) { return XY(p); }
