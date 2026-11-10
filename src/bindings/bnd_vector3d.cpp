#include "bnd_vector3d.h"

using namespace bnd_shared;

BND_DICT BND_Vector3d_Encode(const BND_Vector3d& v) { return XYZ(v); }
