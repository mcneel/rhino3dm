#include "bnd_vector3f.h"

using namespace bnd_shared;

BND_DICT BND_Vector3f_Encode(const BND_Vector3f& v) { return XYZ(v); }
