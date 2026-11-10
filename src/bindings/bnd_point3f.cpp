#include "bnd_point3f.h"

using namespace bnd_shared;

BND_DICT BND_Point3f_Encode(const BND_Point3f& p) { return XYZ(p); }
