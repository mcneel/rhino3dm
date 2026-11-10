#include "bnd_point3d.h"

using namespace bnd_shared;

BND_DICT BND_Point3d_Encode(const BND_Point3d& p) { return XYZ(p); }
