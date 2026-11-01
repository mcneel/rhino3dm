#include "bnd_vector2d.h"

using namespace bnd_shared;

BND_DICT BND_Vector2d_Encode(const BND_Vector2d& v) { return XY(v); }
